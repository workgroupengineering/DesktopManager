using System;

namespace DesktopManager.Cli;

internal static class CliApplication {
    public static int Run(string[] args) {
        try {
            var parsed = CommandLineArguments.Parse(args);
            if (parsed.IsEmpty || parsed.HasFlag("help")) {
                Console.WriteLine(HelpText.GetGeneralHelp());
                return 0;
            }

            string group = parsed.GetCommandPart(0)?.ToLowerInvariant() ?? string.Empty;
            string action = parsed.GetCommandPart(1)?.ToLowerInvariant() ?? string.Empty;

            return group switch {
                "window" => WindowCommands.Run(action, parsed),
                "monitor" => MonitorCommands.Run(action, parsed),
                "process" => ProcessCommands.Run(action, parsed),
                "screenshot" => ScreenshotCommands.Run(action, parsed),
                "layout" => LayoutCommands.Run(action, parsed),
                "snapshot" => SnapshotCommands.Run(action, parsed),
                "mcp" => McpCommands.Run(action, parsed),
                "help" => ShowGroupHelp(parsed),
                _ => throw new CommandLineException($"Unknown command group '{group}'.")
            };
        } catch (CommandLineException ex) {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine();
            Console.Error.WriteLine(HelpText.GetGeneralHelp());
            return 1;
        } catch (Exception ex) {
            Console.Error.WriteLine($"Unhandled error: {ex.Message}");
            return 1;
        }
    }

    private static int ShowGroupHelp(CommandLineArguments parsed) {
        string topic = parsed.GetCommandPart(1)?.ToLowerInvariant() ?? string.Empty;
        string help = topic switch {
            "window" => HelpText.GetWindowHelp(),
            "monitor" => HelpText.GetMonitorHelp(),
            "process" => HelpText.GetProcessHelp(),
            "screenshot" => HelpText.GetScreenshotHelp(),
            "layout" => HelpText.GetLayoutHelp(),
            "snapshot" => HelpText.GetSnapshotHelp(),
            "mcp" => HelpText.GetMcpHelp(),
            _ => HelpText.GetGeneralHelp()
        };

        Console.WriteLine(help);
        return 0;
    }
}
