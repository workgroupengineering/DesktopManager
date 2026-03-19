using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DesktopManager.Cli;

internal static partial class DesktopOperations {
    public static LaunchAndWaitResult LaunchAndWaitForWindow(string filePath, string? arguments, string? workingDirectory, int? waitForInputIdleMilliseconds, int? launchWaitForWindowMilliseconds, int? launchWaitForWindowIntervalMilliseconds, string? launchWindowTitlePattern, string? launchWindowClassNamePattern, string? windowTitlePattern, string? windowClassNamePattern, bool includeHidden, bool includeEmpty, bool all, int timeoutMilliseconds, int intervalMilliseconds, MutationArtifactOptions? artifactOptions = null) {
        if (string.IsNullOrWhiteSpace(filePath)) {
            throw new CommandLineException("Process path is required.");
        }

        if (timeoutMilliseconds <= 0) {
            throw new CommandLineException("Wait timeout must be greater than 0.");
        }

        if (intervalMilliseconds <= 0) {
            throw new CommandLineException("Wait interval must be greater than 0.");
        }

        return ExecuteCore(() => {
            var warnings = new List<string>();
            var notes = new List<string>();
            Stopwatch stopwatch = Stopwatch.StartNew();
            MutationArtifactOptions options = artifactOptions ?? new MutationArtifactOptions();

            IReadOnlyList<ScreenshotResult> beforeScreenshots = options.CaptureBefore
                ? CaptureDesktopArtifacts("launch-and-wait-for-window", "before", options, warnings)
                : Array.Empty<ScreenshotResult>();

            ProcessLaunchResult launch = LaunchProcess(
                filePath,
                arguments,
                workingDirectory,
                waitForInputIdleMilliseconds,
                launchWaitForWindowMilliseconds,
                launchWaitForWindowIntervalMilliseconds,
                launchWindowTitlePattern,
                launchWindowClassNamePattern,
                requireWindow: false);

            int processId = launch.ResolvedProcessId ?? launch.ProcessId;
            notes.Add($"Launched process {processId} from '{launch.FilePath}'.");

            var waitCriteria = new WindowSelectionCriteria {
                TitlePattern = windowTitlePattern ?? launchWindowTitlePattern ?? "*",
                ClassNamePattern = windowClassNamePattern ?? launchWindowClassNamePattern ?? "*",
                ProcessId = processId,
                IncludeHidden = includeHidden,
                IncludeCloaked = false,
                IncludeOwned = true,
                IncludeEmptyTitles = includeEmpty,
                All = all
            };
            notes.Add($"Wait selector: processId={processId}, title='{waitCriteria.TitlePattern}', class='{waitCriteria.ClassNamePattern}'.");

            WaitForWindowResult waitResult = WaitForWindow(waitCriteria, timeoutMilliseconds, intervalMilliseconds);
            notes.Add($"Resolved {waitResult.Count} window(s) for process {processId}.");

            IReadOnlyList<ScreenshotResult> afterScreenshots;
            if (options.CaptureAfter) {
                var automation = new DesktopAutomationService();
                afterScreenshots = CaptureWindowArtifacts(
                    automation,
                    automation.GetWindows(CreateWindowQuery(waitCriteria)),
                    "launch-and-wait-for-window",
                    "after",
                    options,
                    warnings);
            } else {
                afterScreenshots = Array.Empty<ScreenshotResult>();
            }

            stopwatch.Stop();
            return new LaunchAndWaitResult {
                Action = "launch-and-wait-for-window",
                Success = waitResult.Count > 0,
                ElapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds,
                WaitTimeoutMilliseconds = timeoutMilliseconds,
                WaitIntervalMilliseconds = intervalMilliseconds,
                Launch = launch,
                WindowWait = waitResult,
                Notes = notes,
                BeforeScreenshots = beforeScreenshots,
                AfterScreenshots = afterScreenshots,
                ArtifactWarnings = warnings
            };
        });
    }
}
