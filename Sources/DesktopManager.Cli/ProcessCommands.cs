using System;

namespace DesktopManager.Cli;

internal static class ProcessCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "start" => Start(arguments),
            _ => throw new CommandLineException($"Unknown process command '{action}'.")
        };
    }

    private static int Start(CommandLineArguments arguments) {
        ProcessLaunchResult result = DesktopOperations.LaunchProcess(
            arguments.GetRequiredCommandPart(2, "process path"),
            arguments.GetOption("arguments"),
            arguments.GetOption("working-directory"),
            arguments.GetIntOption("wait-for-input-idle-ms"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        Console.WriteLine($"start: PID {result.ProcessId}");
        Console.WriteLine($"- File: {result.FilePath}");
        if (!string.IsNullOrWhiteSpace(result.Arguments)) {
            Console.WriteLine($"- Arguments: {result.Arguments}");
        }
        if (!string.IsNullOrWhiteSpace(result.WorkingDirectory)) {
            Console.WriteLine($"- WorkingDirectory: {result.WorkingDirectory}");
        }
        if (result.MainWindow != null) {
            Console.WriteLine($"- Window: {result.MainWindow.Title}");
        }
        return 0;
    }
}
