using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DesktopManager.Cli;

internal static class MonitorCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "list" => List(arguments),
            _ => throw new CommandLineException($"Unknown monitor command '{action}'.")
        };
    }

    private static int List(CommandLineArguments arguments) {
        IReadOnlyList<MonitorResult> results = DesktopOperations.ListMonitors(
            connectedOnly: arguments.GetBoolFlag("connected") ? true : null,
            primaryOnly: arguments.GetBoolFlag("primary") ? true : null,
            index: arguments.GetIntOption("index"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(results);
            return 0;
        }

        return WriteMonitorResults(results, Console.Out);
    }

    internal static int WriteMonitorResults(IReadOnlyList<MonitorResult> results, TextWriter writer) {
        IReadOnlyList<IReadOnlyList<string>> rows = results
            .Select(monitor => (IReadOnlyList<string>)new[] {
                monitor.Index.ToString(),
                monitor.IsPrimary ? "Yes" : "No",
                monitor.IsConnected ? "Yes" : "No",
                $"{monitor.Left},{monitor.Top},{monitor.Right},{monitor.Bottom}",
                monitor.DeviceName,
                monitor.DeviceString
            })
            .ToArray();

        OutputFormatter.WriteTable(writer, new[] { "Idx", "Primary", "Connected", "Bounds", "DeviceName", "Device" }, rows);
        return 0;
    }
}
