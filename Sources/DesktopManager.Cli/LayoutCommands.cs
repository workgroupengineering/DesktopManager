using System;
using System.Linq;

namespace DesktopManager.Cli;

internal static class LayoutCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "save" => Save(arguments),
            "apply" => Apply(arguments),
            "assert" => Assert(arguments),
            "list" => List(arguments),
            _ => throw new CommandLineException($"Unknown layout command '{action}'.")
        };
    }

    private static int Save(CommandLineArguments arguments) {
        return WritePathResult(arguments, DesktopOperations.SaveLayout(arguments.GetRequiredCommandPart(2, "layout name")));
    }

    private static int Apply(CommandLineArguments arguments) {
        return WritePathResult(
            arguments,
            DesktopOperations.ApplyLayout(arguments.GetRequiredCommandPart(2, "layout name"), arguments.GetBoolFlag("validate")));
    }

    private static int List(CommandLineArguments arguments) {
        var names = DesktopOperations.ListLayouts();
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(names);
            return 0;
        }

        if (names.Count == 0) {
            Console.WriteLine("No named layouts found.");
            return 0;
        }

        foreach (string name in names.OrderBy(value => value)) {
            Console.WriteLine(name);
        }
        return 0;
    }

    private static int Assert(CommandLineArguments arguments) {
        WindowLayoutAssertionResult result = DesktopOperations.AssertWindowLayout(
            arguments.GetRequiredCommandPart(2, "layout name"),
            arguments.GetIntOption("position-tolerance-px") ?? 50,
            arguments.GetIntOption("size-tolerance-px") ?? 50,
            arguments.GetBoolFlag("include-hidden"),
            arguments.GetBoolFlag("include-empty"),
            !arguments.GetBoolFlag("ignore-state"),
            CreateArtifactOptions(arguments));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return result.Matched ? 0 : 2;
        }

        Console.WriteLine($"assert-layout: matched={result.Matched} expected={result.ExpectedCount} matched-count={result.MatchedCount} missing={result.MissingCount} mismatched={result.MismatchCount}");
        Console.WriteLine(result.Path);
        if (result.BeforeScreenshots.Count > 0 || result.AfterScreenshots.Count > 0) {
            Console.WriteLine($"artifacts: before={result.BeforeScreenshots.Count} after={result.AfterScreenshots.Count}");
        }

        foreach (SavedWindowLayoutEntryResult window in result.MissingWindows) {
            Console.WriteLine($"missing: {window.Title} [PID {window.ProcessId}]");
        }

        foreach (WindowLayoutMismatchResult mismatch in result.MismatchedWindows) {
            Console.WriteLine($"mismatch: {mismatch.Expected.Title} [PID {mismatch.Expected.ProcessId}] left={mismatch.LeftDelta} top={mismatch.TopDelta} width={mismatch.WidthDelta} height={mismatch.HeightDelta} state={mismatch.StateMatched}");
        }

        foreach (string warning in result.ArtifactWarnings) {
            Console.WriteLine($"warning: {warning}");
        }

        return result.Matched ? 0 : 2;
    }

    private static int WritePathResult(CommandLineArguments arguments, NamedStateResult payload) {
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(payload);
            return 0;
        }

        Console.WriteLine($"{payload.Action}: {payload.Name}");
        Console.WriteLine(payload.Path);
        return 0;
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
