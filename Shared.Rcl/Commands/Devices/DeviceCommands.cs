using System.ComponentModel;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Devices;

public class DeviceNameSettings : CommandSettings
{
    [CommandArgument(0, "<name>")]
    [Description("The device name.")]
    public string Name { get; set; } = default!;
}
