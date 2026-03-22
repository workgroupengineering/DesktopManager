using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DesktopManager.Cli;

internal static partial class DesktopOperations {
    public static LaunchAndWaitResult LaunchAndWaitForWindow(string filePath, string? arguments, string? workingDirectory, int? waitForInputIdleMilliseconds, int? launchWaitForWindowMilliseconds, int? launchWaitForWindowIntervalMilliseconds, string? launchWindowTitlePattern, string? launchWindowClassNamePattern, string? windowTitlePattern, string? windowClassNamePattern, bool includeHidden, bool includeEmpty, bool all, bool followProcessFamily, int timeoutMilliseconds, int intervalMilliseconds, MutationArtifactOptions? artifactOptions = null) {
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

            LaunchWaitBindingPlan waitPlan = CreateLaunchWaitBindingPlan(
                launch,
                launchWindowTitlePattern,
                launchWindowClassNamePattern,
                windowTitlePattern,
                windowClassNamePattern,
                includeHidden,
                includeEmpty,
                all,
                followProcessFamily);
            WindowSelectionCriteria waitCriteria = waitPlan.Criteria;

            notes.Add($"Launched process {launch.ProcessId} from '{launch.FilePath}'.");
            if (launch.ResolvedProcessId.HasValue && launch.ResolvedProcessId.Value != launch.ProcessId) {
                notes.Add($"Launch-time window correlation resolved process {launch.ResolvedProcessId.Value}.");
            }

            if (waitPlan.BoundProcessId.HasValue) {
                notes.Add($"Wait selector bound to processId={waitPlan.BoundProcessId.Value}, title='{waitCriteria.TitlePattern}', class='{waitCriteria.ClassNamePattern}'.");
            } else {
                notes.Add($"Wait selector bound to processName='{waitPlan.BoundProcessName}', title='{waitCriteria.TitlePattern}', class='{waitCriteria.ClassNamePattern}'.");
            }

            WaitForWindowResult waitResult = WaitForWindow(waitCriteria, timeoutMilliseconds, intervalMilliseconds);
            notes.Add(waitPlan.BoundProcessId.HasValue
                ? $"Resolved {waitResult.Count} window(s) for process {waitPlan.BoundProcessId.Value}."
                : $"Resolved {waitResult.Count} window(s) for process family '{waitPlan.BoundProcessName}'.");

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
                WaitBinding = waitPlan.WaitBinding,
                BoundProcessId = waitPlan.BoundProcessId,
                BoundProcessName = waitPlan.BoundProcessName,
                Launch = launch,
                WindowWait = waitResult,
                Notes = notes,
                BeforeScreenshots = beforeScreenshots,
                AfterScreenshots = afterScreenshots,
                ArtifactWarnings = warnings
            };
        });
    }

    internal static LaunchWaitBindingPlan CreateLaunchWaitBindingPlan(ProcessLaunchResult launch, string? launchWindowTitlePattern, string? launchWindowClassNamePattern, string? windowTitlePattern, string? windowClassNamePattern, bool includeHidden, bool includeEmpty, bool all, bool followProcessFamily) {
        var criteria = new WindowSelectionCriteria {
            TitlePattern = windowTitlePattern ?? launchWindowTitlePattern ?? "*",
            ClassNamePattern = windowClassNamePattern ?? launchWindowClassNamePattern ?? "*",
            IncludeHidden = includeHidden,
            IncludeCloaked = false,
            IncludeOwned = true,
            IncludeEmptyTitles = includeEmpty,
            All = all
        };

        if (launch.ResolvedProcessId.HasValue) {
            return new LaunchWaitBindingPlan {
                Criteria = {
                    TitlePattern = criteria.TitlePattern,
                    ClassNamePattern = criteria.ClassNamePattern,
                    IncludeHidden = criteria.IncludeHidden,
                    IncludeCloaked = criteria.IncludeCloaked,
                    IncludeOwned = criteria.IncludeOwned,
                    IncludeEmptyTitles = criteria.IncludeEmptyTitles,
                    All = criteria.All,
                    ProcessId = launch.ResolvedProcessId.Value
                },
                WaitBinding = "resolved-process-id",
                BoundProcessId = launch.ResolvedProcessId.Value
            };
        }

        if (!followProcessFamily) {
            return new LaunchWaitBindingPlan {
                Criteria = {
                    TitlePattern = criteria.TitlePattern,
                    ClassNamePattern = criteria.ClassNamePattern,
                    IncludeHidden = criteria.IncludeHidden,
                    IncludeCloaked = criteria.IncludeCloaked,
                    IncludeOwned = criteria.IncludeOwned,
                    IncludeEmptyTitles = criteria.IncludeEmptyTitles,
                    All = criteria.All,
                    ProcessId = launch.ProcessId
                },
                WaitBinding = "launcher-process-id",
                BoundProcessId = launch.ProcessId
            };
        }

        string? processNameHint = GetProcessNameHint(launch.FilePath);
        if (string.IsNullOrWhiteSpace(processNameHint)) {
            return new LaunchWaitBindingPlan {
                Criteria = {
                    TitlePattern = criteria.TitlePattern,
                    ClassNamePattern = criteria.ClassNamePattern,
                    IncludeHidden = criteria.IncludeHidden,
                    IncludeCloaked = criteria.IncludeCloaked,
                    IncludeOwned = criteria.IncludeOwned,
                    IncludeEmptyTitles = criteria.IncludeEmptyTitles,
                    All = criteria.All,
                    ProcessId = launch.ProcessId
                },
                WaitBinding = "launcher-process-id",
                BoundProcessId = launch.ProcessId
            };
        }

        return new LaunchWaitBindingPlan {
            Criteria = {
                TitlePattern = criteria.TitlePattern,
                ClassNamePattern = criteria.ClassNamePattern,
                IncludeHidden = criteria.IncludeHidden,
                IncludeCloaked = criteria.IncludeCloaked,
                IncludeOwned = criteria.IncludeOwned,
                IncludeEmptyTitles = criteria.IncludeEmptyTitles,
                All = criteria.All,
                ProcessNamePattern = processNameHint
            },
            WaitBinding = "process-name-family",
            BoundProcessName = processNameHint
        };
    }

    private static string? GetProcessNameHint(string filePath) {
        string trimmed = filePath.Trim().Trim('"');
        string executableName = Path.GetFileNameWithoutExtension(trimmed);
        return string.IsNullOrWhiteSpace(executableName) ? null : executableName;
    }
}
