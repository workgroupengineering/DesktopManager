using System;
using System.IO;

namespace DesktopManager.Cli;

internal static class WorkflowCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "prepare-coding" => PrepareCoding(arguments),
            "prepare-screen-sharing" => PrepareScreenSharing(arguments),
            "clean-up-distractions" => CleanUpDistractions(arguments),
            _ => throw new CommandLineException($"Unknown workflow command '{action}'.")
        };
    }

    private static int PrepareCoding(CommandLineArguments arguments) {
        WorkflowResult result = DesktopOperations.PrepareForCoding(
            arguments.GetOption("layout"),
            CreateFocusCriteria(arguments),
            CreateArtifactOptions(arguments));
        return WriteWorkflow(arguments, result);
    }

    private static int PrepareScreenSharing(CommandLineArguments arguments) {
        WorkflowResult result = DesktopOperations.PrepareForScreenSharing(
            arguments.GetOption("layout"),
            CreateFocusCriteria(arguments),
            CreateArtifactOptions(arguments));
        return WriteWorkflow(arguments, result);
    }

    private static int CleanUpDistractions(CommandLineArguments arguments) {
        return WriteWorkflow(arguments, DesktopOperations.CleanUpDistractions(CreateArtifactOptions(arguments)));
    }

    private static int WriteWorkflow(CommandLineArguments arguments, WorkflowResult result) {
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return result.Success ? 0 : 2;
        }

        return WriteWorkflowResult(result, Console.Out);
    }

    internal static int WriteWorkflowResult(WorkflowResult result, TextWriter writer) {
        writer.WriteLine($"{result.Action}: success={result.Success} elapsed-ms={result.ElapsedMilliseconds}");
        if (result.LayoutApplied && !string.IsNullOrWhiteSpace(result.LayoutName)) {
            writer.WriteLine($"layout: applied {result.LayoutName}");
        } else if (!string.IsNullOrWhiteSpace(result.LayoutName)) {
            writer.WriteLine($"layout: not-applied {result.LayoutName}");
        }

        if (result.FocusedWindow != null) {
            writer.WriteLine($"focused: {result.FocusedWindow.Title} [PID {result.FocusedWindow.ProcessId}]");
        } else if (result.ResolvedWindow != null) {
            writer.WriteLine($"resolved: {result.ResolvedWindow.Title} [PID {result.ResolvedWindow.ProcessId}]");
        }

        if (result.MinimizedWindows.Count > 0) {
            writer.WriteLine($"minimized: {result.MinimizedWindows.Count} window(s)");
            foreach (WindowResult window in result.MinimizedWindows) {
                writer.WriteLine($"- {window.Title} [PID {window.ProcessId}]");
            }
        }

        if (result.BeforeScreenshots.Count > 0 || result.AfterScreenshots.Count > 0) {
            writer.WriteLine($"artifacts: before={result.BeforeScreenshots.Count} after={result.AfterScreenshots.Count}");
        }

        foreach (string warning in result.ArtifactWarnings) {
            writer.WriteLine($"warning: {warning}");
        }

        foreach (string note in result.Notes) {
            writer.WriteLine($"note: {note}");
        }

        return result.Success ? 0 : 2;
    }

    internal static WindowSelectionCriteria CreateFocusCriteria(CommandLineArguments arguments) {
        return new WindowSelectionCriteria {
            TitlePattern = arguments.GetOption("title") ?? "*",
            ProcessNamePattern = arguments.GetOption("process") ?? "*",
            ClassNamePattern = arguments.GetOption("class") ?? "*",
            ProcessId = arguments.GetIntOption("pid"),
            Handle = arguments.GetOption("handle"),
            Active = arguments.GetBoolFlag("active"),
            IncludeHidden = arguments.GetBoolFlag("include-hidden"),
            IncludeCloaked = !arguments.GetBoolFlag("exclude-cloaked"),
            IncludeOwned = !arguments.GetBoolFlag("exclude-owned"),
            IncludeEmptyTitles = arguments.GetBoolFlag("include-empty"),
            All = false
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
}
