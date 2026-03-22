using System;
using System.IO;

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
        ProcessStartCommandOptions options = CreateStartOptions(arguments);
        ProcessLaunchResult result = DesktopOperations.LaunchProcess(
            options.FilePath,
            options.Arguments,
            options.WorkingDirectory,
            options.WaitForInputIdleMilliseconds,
            options.WaitForWindowMilliseconds,
            options.WaitForWindowIntervalMilliseconds,
            options.WindowTitlePattern,
            options.WindowClassNamePattern,
            options.RequireWindow);

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteStartResult(result, Console.Out);
    }

    private static int StartAndWait(CommandLineArguments arguments) {
        LaunchAndWaitCommandOptions options = CreateStartAndWaitOptions(arguments);
        LaunchAndWaitResult result = DesktopOperations.LaunchAndWaitForWindow(
            options.FilePath,
            options.Arguments,
            options.WorkingDirectory,
            options.WaitForInputIdleMilliseconds,
            options.LaunchWaitForWindowMilliseconds,
            options.LaunchWaitForWindowIntervalMilliseconds,
            options.LaunchWindowTitlePattern,
            options.LaunchWindowClassNamePattern,
            options.WindowTitlePattern,
            options.WindowClassNamePattern,
            options.IncludeHidden,
            options.IncludeEmpty,
            options.All,
            options.FollowProcessFamily,
            options.TimeoutMilliseconds,
            options.IntervalMilliseconds,
            options.ArtifactOptions);

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return result.Success ? 0 : 2;
        }

        return WriteStartAndWaitResult(result, Console.Out);
    }

    internal static ProcessStartCommandOptions CreateStartOptions(CommandLineArguments arguments) {
        return new ProcessStartCommandOptions {
            FilePath = arguments.GetRequiredCommandPart(2, "process path"),
            Arguments = arguments.GetOption("arguments"),
            WorkingDirectory = arguments.GetOption("working-directory"),
            WaitForInputIdleMilliseconds = arguments.GetIntOption("wait-for-input-idle-ms"),
            WaitForWindowMilliseconds = arguments.GetIntOption("wait-for-window-ms"),
            WaitForWindowIntervalMilliseconds = arguments.GetIntOption("wait-for-window-interval-ms"),
            WindowTitlePattern = arguments.GetOption("window-title"),
            WindowClassNamePattern = arguments.GetOption("window-class"),
            RequireWindow = arguments.GetBoolFlag("require-window")
        };
    }

    internal static LaunchAndWaitCommandOptions CreateStartAndWaitOptions(CommandLineArguments arguments) {
        return new LaunchAndWaitCommandOptions {
            FilePath = arguments.GetRequiredCommandPart(2, "process path"),
            Arguments = arguments.GetOption("arguments"),
            WorkingDirectory = arguments.GetOption("working-directory"),
            WaitForInputIdleMilliseconds = arguments.GetIntOption("wait-for-input-idle-ms"),
            LaunchWaitForWindowMilliseconds = arguments.GetIntOption("launch-wait-for-window-ms"),
            LaunchWaitForWindowIntervalMilliseconds = arguments.GetIntOption("launch-wait-for-window-interval-ms"),
            LaunchWindowTitlePattern = arguments.GetOption("launch-window-title"),
            LaunchWindowClassNamePattern = arguments.GetOption("launch-window-class"),
            WindowTitlePattern = arguments.GetOption("window-title"),
            WindowClassNamePattern = arguments.GetOption("window-class"),
            IncludeHidden = arguments.GetBoolFlag("include-hidden"),
            IncludeEmpty = arguments.GetBoolFlag("include-empty"),
            All = arguments.GetBoolFlag("all"),
            FollowProcessFamily = arguments.GetBoolFlag("follow-process-family"),
            TimeoutMilliseconds = arguments.GetIntOption("timeout-ms") ?? 10000,
            IntervalMilliseconds = arguments.GetIntOption("interval-ms") ?? 200,
            ArtifactOptions = CreateArtifactOptions(arguments)
        };
    }

    internal static MutationArtifactOptions? CreateArtifactOptions(CommandLineArguments arguments) {
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

    internal static int WriteStartAndWaitResult(LaunchAndWaitResult result, TextWriter writer) {
        writer.WriteLine($"{result.Action}: success={result.Success} elapsed-ms={result.ElapsedMilliseconds}");
        writer.WriteLine($"- PID: {result.Launch.ProcessId}");
        if (result.Launch.ResolvedProcessId.HasValue && result.Launch.ResolvedProcessId.Value != result.Launch.ProcessId) {
            writer.WriteLine($"- ResolvedPID: {result.Launch.ResolvedProcessId.Value}");
        }
        writer.WriteLine($"- File: {result.Launch.FilePath}");
        writer.WriteLine($"- WaitBinding: {result.WaitBinding}");
        if (result.BoundProcessId.HasValue) {
            writer.WriteLine($"- BoundProcessId: {result.BoundProcessId.Value}");
        }
        if (!string.IsNullOrWhiteSpace(result.BoundProcessName)) {
            writer.WriteLine($"- BoundProcessName: {result.BoundProcessName}");
        }
        writer.WriteLine($"- WaitCount: {result.WindowWait.Count}");
        writer.WriteLine($"- WaitElapsedMs: {result.WindowWait.ElapsedMilliseconds}");
        foreach (WindowResult window in result.WindowWait.Windows) {
            writer.WriteLine($"- Window: {window.Title} [PID {window.ProcessId}]");
        }

        if (result.BeforeScreenshots.Count > 0 || result.AfterScreenshots.Count > 0) {
            writer.WriteLine($"- Artifacts: before={result.BeforeScreenshots.Count} after={result.AfterScreenshots.Count}");
        }

        foreach (string warning in result.ArtifactWarnings) {
            writer.WriteLine($"- Warning: {warning}");
        }

        foreach (string note in result.Notes) {
            writer.WriteLine($"- Note: {note}");
        }

        return result.Success ? 0 : 2;
    }

    internal static int WriteStartResult(ProcessLaunchResult result, TextWriter writer) {
        writer.WriteLine($"start: PID {result.ProcessId}");
        if (result.ResolvedProcessId.HasValue && result.ResolvedProcessId.Value != result.ProcessId) {
            writer.WriteLine($"- ResolvedPID: {result.ResolvedProcessId.Value}");
        }
        writer.WriteLine($"- File: {result.FilePath}");
        if (!string.IsNullOrWhiteSpace(result.Arguments)) {
            writer.WriteLine($"- Arguments: {result.Arguments}");
        }
        if (!string.IsNullOrWhiteSpace(result.WorkingDirectory)) {
            writer.WriteLine($"- WorkingDirectory: {result.WorkingDirectory}");
        }
        if (result.MainWindow != null) {
            writer.WriteLine($"- Window: {result.MainWindow.Title}");
        }

        return 0;
    }
}
