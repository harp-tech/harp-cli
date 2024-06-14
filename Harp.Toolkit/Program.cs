using System.CommandLine;
using System.IO.Ports;
using Bonsai.Harp;

namespace Harp.Toolkit;

internal class Program
{
    static async Task Main(string[] args)
    {
        var portName = new Option<string>(
            name: "--port",
            description: "Specifies the name of the serial port used to communicate with the device."
        ) { IsRequired = true };
        portName.ArgumentHelpName = nameof(portName);

        var portTimeout = new Option<int?>(
            name: "--timeout",
            description: "Specifies an optional timeout to receive a response from the device."
        ) { ArgumentHelpName = "milliseconds" };

        var firmwarePath = new Option<FileInfo>(
            name: "--path",
            description: "Specifies the path of the firmware file to write to the device."
        ) { IsRequired = true };
        firmwarePath.ArgumentHelpName = nameof(firmwarePath);

        var forceUpdate = new Option<bool>(
            name: "--force",
            description: "Indicates whether to force a firmware update on the device regardless of compatibility."
        );

        var listCommand = new Command("list", description: "");
        listCommand.SetHandler(() =>
        {
            var portNames = SerialPort.GetPortNames();
            Console.WriteLine($"PortNames: [{string.Join(", ", portNames)}]");
        });

        var updateCommand = new Command("update", description: "");
        updateCommand.AddOption(portName);
        updateCommand.AddOption(firmwarePath);
        updateCommand.AddOption(forceUpdate);
        updateCommand.SetHandler(async (portName, firmwarePath, forceUpdate) =>
        {
            var firmware = DeviceFirmware.FromFile(firmwarePath.FullName);
            Console.WriteLine($"{firmware.Metadata}");
            ProgressBar.Write(0);
            try
            {
                var progress = new Progress<int>(ProgressBar.Update);
                await Bootloader.UpdateFirmwareAsync(portName, firmware, forceUpdate, progress);
            }
            finally { Console.WriteLine(); }
        }, portName, firmwarePath, forceUpdate);

        var rootCommand = new RootCommand("Tool for inspecting, updating and interfacing with Harp devices.");
        rootCommand.AddOption(portName);
        rootCommand.AddOption(portTimeout);
        rootCommand.AddCommand(listCommand);
        rootCommand.AddCommand(updateCommand);
        rootCommand.SetHandler(async (portName, portTimeout) =>
        {
            using var device = new AsyncDevice(portName);
            var whoAmI = await device.ReadWhoAmIAsync().WithTimeout(portTimeout);
            var hardwareVersion = await device.ReadHardwareVersionAsync();
            var firmwareVersion = await device.ReadFirmwareVersionAsync();
            var timestamp = await device.ReadTimestampSecondsAsync();
            var deviceName = await device.ReadDeviceNameAsync();
            Console.WriteLine($"Harp device found in {portName}");
            Console.WriteLine($"DeviceName: {deviceName}");
            Console.WriteLine($"WhoAmI: {whoAmI}");
            Console.WriteLine($"Hw: {hardwareVersion.Major}.{hardwareVersion.Minor}");
            Console.WriteLine($"Fw: {firmwareVersion.Major}.{firmwareVersion.Minor}");
            Console.WriteLine($"Timestamp (s): {timestamp}");
            Console.WriteLine();
        }, portName, portTimeout);
        await rootCommand.InvokeAsync(args);
    }
}
