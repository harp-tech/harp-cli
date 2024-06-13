using System.CommandLine;
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

        var rootCommand = new RootCommand("Tool for inspecting, updating and interfacing with Harp devices.");
        rootCommand.AddOption(portName);
        rootCommand.SetHandler(async (portName) =>
        {
            using var device = new AsyncDevice(portName);
            var whoAmI = await device.ReadWhoAmIAsync();
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
        }, portName);
        await rootCommand.InvokeAsync(args);
    }
}
