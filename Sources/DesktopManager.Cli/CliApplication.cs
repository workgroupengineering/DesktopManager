using System;
using System.IO;

namespace DesktopManager.Cli;

internal static class CliApplication {
    public static int Run(string[] args) {
        return Run(args, Console.Out, Console.Error);
    }

    internal static int Run(string[] args, TextWriter output, TextWriter error) {
        try {
            var parsed = CommandLineArguments.Parse(args);
            if (parsed.IsEmpty || parsed.HasFlag("help")) {
                output.WriteLine(HelpText.GetGeneralHelp());
                return 0;
            }

            string group = parsed.GetCommandPart(0)?.ToLowerInvariant() ?? string.Empty;
            string action = parsed.GetCommandPart(1)?.ToLowerInvariant() ?? string.Empty;

            return group switch {
                "window" => WindowCommands.Run(action, parsed),
                "control" => ControlCommands.Run(action, parsed),
                "monitor" => MonitorCommands.Run(action, parsed),
                "process" => ProcessCommands.Run(action, parsed),
                "screenshot" => ScreenshotCommands.Run(action, parsed),
                "target" => TargetCommands.Run(action, parsed),
                "control-target" => ControlTargetCommands.Run(action, parsed),
                "layout" => LayoutCommands.Run(action, parsed),
                "snapshot" => SnapshotCommands.Run(action, parsed),
                "workflow" => WorkflowCommands.Run(action, parsed),
                "mcp" => McpCommands.Run(action, parsed),
                "help" => ShowGroupHelp(parsed, output),
                _ => throw new CommandLineException($"Unknown command group '{group}'.")
            };
        } catch (CommandLineException ex) {
            error.WriteLine($"Error: {ex.Message}");
            error.WriteLine();
            error.WriteLine(HelpText.GetGeneralHelp());
            return 1;
        } catch (Exception ex) {
            error.WriteLine($"Unhandled error: {ex.Message}");
            return 1;
        }
    }

    internal static string GetHelpText(string? topic) {
        return topic?.ToLowerInvariant() switch {
            "window" => HelpText.GetWindowHelp(),
            "control" => HelpText.GetControlHelp(),
            "monitor" => HelpText.GetMonitorHelp(),
            "process" => HelpText.GetProcessHelp(),
            "screenshot" => HelpText.GetScreenshotHelp(),
            "target" => HelpText.GetTargetHelp(),
            "control-target" => HelpText.GetControlTargetHelp(),
            "layout" => HelpText.GetLayoutHelp(),
            "snapshot" => HelpText.GetSnapshotHelp(),
            "workflow" => HelpText.GetWorkflowHelp(),
            "mcp" => HelpText.GetMcpHelp(),
            _ => HelpText.GetGeneralHelp()
        };
    }

    private static int ShowGroupHelp(CommandLineArguments parsed, TextWriter output) {
        string? topic = parsed.GetCommandPart(1);
        string help = GetHelpText(topic);

        output.WriteLine(help);
        return 0;
    }
}
