using System;

namespace DesktopManager.Cli;

internal static class ProcessCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "start" => Start(arguments),
            "start-and-wait" => StartAndWait(arguments),
            _ => throw new CommandLineException($"Unknown process command '{action}'.")
        };
    }

    private static int Start(CommandLineArguments arguments) {
        ProcessLaunchResult result = DesktopOperations.LaunchProcess(
            arguments.GetRequiredCommandPart(2, "process path"),
            arguments.GetOption("arguments"),
            arguments.GetOption("working-directory"),
            arguments.GetIntOption("wait-for-input-idle-ms"),
            arguments.GetIntOption("wait-for-window-ms"),
            arguments.GetIntOption("wait-for-window-interval-ms"),
            arguments.GetOption("window-title"),
            arguments.GetOption("window-class"),
            arguments.GetBoolFlag("require-window"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        Console.WriteLine($"start: PID {result.ProcessId}");
        if (result.ResolvedProcessId.HasValue && result.ResolvedProcessId.Value != result.ProcessId) {
            Console.WriteLine($"- ResolvedPID: {result.ResolvedProcessId.Value}");
        }
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

    private static int StartAndWait(CommandLineArguments arguments) {
        LaunchAndWaitResult result = DesktopOperations.LaunchAndWaitForWindow(
            arguments.GetRequiredCommandPart(2, "process path"),
            arguments.GetOption("arguments"),
            arguments.GetOption("working-directory"),
            arguments.GetIntOption("wait-for-input-idle-ms"),
            arguments.GetIntOption("launch-wait-for-window-ms"),
            arguments.GetIntOption("launch-wait-for-window-interval-ms"),
            arguments.GetOption("launch-window-title"),
            arguments.GetOption("launch-window-class"),
            arguments.GetOption("window-title"),
            arguments.GetOption("window-class"),
            arguments.GetBoolFlag("include-hidden"),
            arguments.GetBoolFlag("include-empty"),
            arguments.GetBoolFlag("all"),
            arguments.GetIntOption("timeout-ms") ?? 10000,
            arguments.GetIntOption("interval-ms") ?? 200,
            CreateArtifactOptions(arguments));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return result.Success ? 0 : 2;
        }

        Console.WriteLine($"{result.Action}: success={result.Success} elapsed-ms={result.ElapsedMilliseconds}");
        Console.WriteLine($"- PID: {result.Launch.ProcessId}");
        if (result.Launch.ResolvedProcessId.HasValue && result.Launch.ResolvedProcessId.Value != result.Launch.ProcessId) {
            Console.WriteLine($"- ResolvedPID: {result.Launch.ResolvedProcessId.Value}");
        }
        Console.WriteLine($"- File: {result.Launch.FilePath}");
        Console.WriteLine($"- WaitCount: {result.WindowWait.Count}");
        Console.WriteLine($"- WaitElapsedMs: {result.WindowWait.ElapsedMilliseconds}");
        foreach (WindowResult window in result.WindowWait.Windows) {
            Console.WriteLine($"- Window: {window.Title} [PID {window.ProcessId}]");
        }

        if (result.BeforeScreenshots.Count > 0 || result.AfterScreenshots.Count > 0) {
            Console.WriteLine($"- Artifacts: before={result.BeforeScreenshots.Count} after={result.AfterScreenshots.Count}");
        }

        foreach (string warning in result.ArtifactWarnings) {
            Console.WriteLine($"- Warning: {warning}");
        }

        foreach (string note in result.Notes) {
            Console.WriteLine($"- Note: {note}");
        }

        return result.Success ? 0 : 2;
    }

    private static MutationArtifactOptions? CreateArtifactOptions(CommandLineArguments arguments) {
        bool captureBefore = arguments.GetBoolFlag("capture-before");
        bool captureAfter = arguments.GetBoolFlag("capture-after");
        string? artifactDirectory = arguments.GetOption("artifact-directory");
        if (!captureBefore && !captureAfter && string.IsNullOrWhiteSpace(artifactDirectory)) {
            return null;
        }

        return new MutationArtifactOptions {
            CaptureBefore = captureBefore,
            CaptureAfter = captureAfter,
            ArtifactDirectory = artifactDirectory
        };
    }
}
