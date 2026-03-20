using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DesktopManager.Cli;

internal static partial class DesktopOperations {
    public static WindowLayoutAssertionResult AssertWindowLayout(string name, int positionTolerancePixels, int sizeTolerancePixels, bool includeHidden, bool includeEmpty, bool checkState, MutationArtifactOptions? artifactOptions = null) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new CommandLineException("Layout name is required.");
        }

        if (positionTolerancePixels < 0) {
            throw new CommandLineException("Position tolerance must be zero or greater.");
        }

        if (sizeTolerancePixels < 0) {
            throw new CommandLineException("Size tolerance must be zero or greater.");
        }

        return ExecuteCore(() => {
            string path = DesktopStateStore.GetLayoutPath(name);
            if (!File.Exists(path)) {
                throw new InvalidOperationException($"Named layout '{name}' was not found.");
            }

            WindowLayout layout = ReadWindowLayout(path);
            var warnings = new List<string>();
            MutationArtifactOptions options = artifactOptions ?? new MutationArtifactOptions();
            IReadOnlyList<ScreenshotResult> beforeScreenshots = options.CaptureBefore
                ? CaptureDesktopArtifacts("assert-window-layout", "before", options, warnings)
                : Array.Empty<ScreenshotResult>();

            DesktopAutomationService automation = new DesktopAutomationService();
            IReadOnlyList<WindowInfo> currentWindows = automation.GetWindows(new WindowQueryOptions {
                TitlePattern = "*",
                ProcessNamePattern = "*",
                ClassNamePattern = "*",
                IncludeHidden = includeHidden,
                IncludeCloaked = false,
                IncludeOwned = true,
                IncludeEmptyTitles = includeEmpty
            });

            var missingWindows = new List<SavedWindowLayoutEntryResult>();
            var mismatchedWindows = new List<WindowLayoutMismatchResult>();
            var usedHandles = new HashSet<IntPtr>();
            int matchedCount = 0;

            foreach (WindowPosition expected in layout.Windows) {
                WindowInfo? current = FindMatchingWindow(expected, currentWindows, usedHandles);
                SavedWindowLayoutEntryResult expectedResult = MapSavedWindowLayoutEntry(expected);
                if (current == null) {
                    missingWindows.Add(expectedResult);
                    continue;
                }

                int leftDelta = Math.Abs(expected.Left - current.Left);
                int topDelta = Math.Abs(expected.Top - current.Top);
                int widthDelta = Math.Abs(expected.Width - current.Width);
                int heightDelta = Math.Abs(expected.Height - current.Height);
                bool geometryMatched = leftDelta <= positionTolerancePixels &&
                    topDelta <= positionTolerancePixels &&
                    widthDelta <= sizeTolerancePixels &&
                    heightDelta <= sizeTolerancePixels;
                bool stateMatched = !checkState || !expected.State.HasValue || expected.State == current.State;

                if (geometryMatched && stateMatched) {
                    matchedCount++;
                    continue;
                }

                mismatchedWindows.Add(new WindowLayoutMismatchResult {
                    Expected = expectedResult,
                    Current = MapWindow(current),
                    LeftDelta = leftDelta,
                    TopDelta = topDelta,
                    WidthDelta = widthDelta,
                    HeightDelta = heightDelta,
                    StateMatched = stateMatched
                });
            }

            IReadOnlyList<ScreenshotResult> afterScreenshots = options.CaptureAfter
                ? CaptureDesktopArtifacts("assert-window-layout", "after", options, warnings)
                : Array.Empty<ScreenshotResult>();

            return new WindowLayoutAssertionResult {
                Matched = missingWindows.Count == 0 && mismatchedWindows.Count == 0 && matchedCount == layout.Windows.Count,
                Assertion = "window-layout",
                Name = name,
                Path = path,
                ExpectedCount = layout.Windows.Count,
                MatchedCount = matchedCount,
                MissingCount = missingWindows.Count,
                MismatchCount = mismatchedWindows.Count,
                PositionTolerancePixels = positionTolerancePixels,
                SizeTolerancePixels = sizeTolerancePixels,
                CheckState = checkState,
                MissingWindows = missingWindows,
                MismatchedWindows = mismatchedWindows,
                BeforeScreenshots = beforeScreenshots,
                AfterScreenshots = afterScreenshots,
                ArtifactWarnings = warnings
            };
        });
    }

    private static WindowLayout ReadWindowLayout(string path) {
        string json = File.ReadAllText(path);
        WindowLayout? layout;
        try {
            layout = JsonSerializer.Deserialize<WindowLayout>(json);
        } catch (JsonException ex) {
            throw new InvalidOperationException($"Invalid layout file: {ex.Message}", ex);
        }

        if (layout?.Windows == null) {
            throw new InvalidDataException("Layout does not contain any windows.");
        }

        return layout;
    }

    private static WindowInfo? FindMatchingWindow(WindowPosition expected, IReadOnlyList<WindowInfo> currentWindows, HashSet<IntPtr> usedHandles) {
        WindowInfo? exact = currentWindows.FirstOrDefault(window =>
            window.Handle != IntPtr.Zero &&
            !usedHandles.Contains(window.Handle) &&
            window.ProcessId == expected.ProcessId &&
            string.Equals(window.Title, expected.Title, StringComparison.Ordinal));
        if (exact != null) {
            usedHandles.Add(exact.Handle);
            return exact;
        }

        WindowInfo? byTitle = currentWindows.FirstOrDefault(window =>
            window.Handle != IntPtr.Zero &&
            !usedHandles.Contains(window.Handle) &&
            string.Equals(window.Title, expected.Title, StringComparison.Ordinal));
        if (byTitle != null) {
            usedHandles.Add(byTitle.Handle);
            return byTitle;
        }

        WindowInfo? byProcessId = currentWindows.FirstOrDefault(window =>
            window.Handle != IntPtr.Zero &&
            !usedHandles.Contains(window.Handle) &&
            window.ProcessId == expected.ProcessId);
        if (byProcessId != null) {
            usedHandles.Add(byProcessId.Handle);
            return byProcessId;
        }

        return null;
    }

    private static SavedWindowLayoutEntryResult MapSavedWindowLayoutEntry(WindowPosition window) {
        return new SavedWindowLayoutEntryResult {
            Title = window.Title,
            ProcessId = window.ProcessId,
            Left = window.Left,
            Top = window.Top,
            Width = window.Width,
            Height = window.Height,
            State = window.State?.ToString()
        };
    }
}
