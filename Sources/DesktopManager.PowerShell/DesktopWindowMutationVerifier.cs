using System;
using System.Collections.Generic;

namespace DesktopManager.PowerShell;

internal static class DesktopWindowMutationVerifier {
    internal static DesktopWindowMutationRecord Verify(
        DesktopAutomationService automation,
        string action,
        WindowInfo requestedWindow,
        int tolerancePixels,
        int? expectedMonitorIndex = null,
        int? expectedLeft = null,
        int? expectedTop = null,
        int? expectedWidth = null,
        int? expectedHeight = null,
        WindowState? expectedState = null,
        bool? expectedTopMost = null,
        bool requireForeground = false,
        bool expectClosed = false) {
        WindowInfo observedWindow = ObserveWindowByHandle(automation, requestedWindow.Handle);
        WindowInfo activeWindow = SafeGetActiveWindow(automation);
        var notes = new List<string>();

        if (expectClosed) {
            bool closed = observedWindow == null;
            return new DesktopWindowMutationRecord {
                Action = action,
                Success = true,
                VerificationPerformed = true,
                Verified = closed,
                VerificationMode = "closed",
                VerificationSummary = closed
                    ? $"Observed the window closed after '{action}'."
                    : $"DesktopManager requested '{action}', but the window is still observable afterward.",
                VerificationTolerancePixels = tolerancePixels,
                RequestedWindow = requestedWindow,
                ObservedWindow = observedWindow,
                ActiveWindow = activeWindow,
                VerificationNotes = closed ? Array.Empty<string>() : new[] { $"Window '{requestedWindow.Title}' [{requestedWindow.Handle.ToInt64():X}] is still observable." }
            };
        }

        if (observedWindow == null) {
            return new DesktopWindowMutationRecord {
                Action = action,
                Success = true,
                VerificationPerformed = true,
                Verified = false,
                VerificationMode = "presence",
                VerificationSummary = $"DesktopManager requested '{action}', but the window was no longer observable afterward.",
                VerificationTolerancePixels = tolerancePixels,
                RequestedWindow = requestedWindow,
                ActiveWindow = activeWindow,
                VerificationNotes = new[] { $"Window '{requestedWindow.Title}' [{requestedWindow.Handle.ToInt64():X}] could not be re-queried after the mutation." }
            };
        }

        bool hasSpecificExpectation = expectedMonitorIndex.HasValue ||
            expectedLeft.HasValue ||
            expectedTop.HasValue ||
            expectedWidth.HasValue ||
            expectedHeight.HasValue ||
            expectedState.HasValue ||
            expectedTopMost.HasValue ||
            requireForeground;

        if (!hasSpecificExpectation) {
            return new DesktopWindowMutationRecord {
                Action = action,
                Success = true,
                VerificationPerformed = true,
                Verified = true,
                VerificationMode = "presence",
                VerificationSummary = $"Observed the window after '{action}'.",
                VerificationTolerancePixels = tolerancePixels,
                RequestedWindow = requestedWindow,
                ObservedWindow = observedWindow,
                ActiveWindow = activeWindow
            };
        }

        bool verified = true;
        string mode = "postcondition";
        if (expectedMonitorIndex.HasValue && observedWindow.MonitorIndex != expectedMonitorIndex.Value) {
            verified = false;
            notes.Add($"Observed monitor index {observedWindow.MonitorIndex} instead of {expectedMonitorIndex.Value}.");
            mode = "geometry";
        }
        if (expectedLeft.HasValue && Math.Abs(observedWindow.Left - expectedLeft.Value) > tolerancePixels) {
            verified = false;
            notes.Add($"Observed left={observedWindow.Left} instead of approximately {expectedLeft.Value}.");
            mode = "geometry";
        }
        if (expectedTop.HasValue && Math.Abs(observedWindow.Top - expectedTop.Value) > tolerancePixels) {
            verified = false;
            notes.Add($"Observed top={observedWindow.Top} instead of approximately {expectedTop.Value}.");
            mode = "geometry";
        }
        if (expectedWidth.HasValue && Math.Abs(observedWindow.Width - expectedWidth.Value) > tolerancePixels) {
            verified = false;
            notes.Add($"Observed width={observedWindow.Width} instead of approximately {expectedWidth.Value}.");
            mode = "geometry";
        }
        if (expectedHeight.HasValue && Math.Abs(observedWindow.Height - expectedHeight.Value) > tolerancePixels) {
            verified = false;
            notes.Add($"Observed height={observedWindow.Height} instead of approximately {expectedHeight.Value}.");
            mode = "geometry";
        }
        if (expectedState.HasValue && observedWindow.State != expectedState.Value) {
            verified = false;
            notes.Add($"Observed state '{observedWindow.State?.ToString() ?? "<unknown>"}' instead of '{expectedState.Value}'.");
            mode = "state";
        }
        if (expectedTopMost.HasValue && observedWindow.IsTopMost != expectedTopMost.Value) {
            verified = false;
            notes.Add($"Observed IsTopMost={observedWindow.IsTopMost} instead of {expectedTopMost.Value}.");
            mode = "topmost";
        }
        if (requireForeground) {
            mode = mode == "postcondition" ? "foreground" : mode + "-foreground";
            if (activeWindow == null || activeWindow.Handle != requestedWindow.Handle) {
                verified = false;
                notes.Add(activeWindow == null
                    ? "Windows did not report an active foreground window after the mutation."
                    : $"Foreground window was '{activeWindow.Title}' [{activeWindow.Handle.ToInt64():X}] instead of the requested window.");
            }
        }

        return new DesktopWindowMutationRecord {
            Action = action,
            Success = true,
            VerificationPerformed = true,
            Verified = verified,
            VerificationMode = mode,
            VerificationSummary = verified
                ? $"Observed the requested postcondition after '{action}'."
                : $"DesktopManager requested '{action}', but the observed postcondition did not match.",
            VerificationTolerancePixels = tolerancePixels,
            RequestedWindow = requestedWindow,
            ObservedWindow = observedWindow,
            ActiveWindow = activeWindow,
            VerificationNotes = notes
        };
    }

    internal static DesktopWindowMutationRecord CreateFailureRecord(string action, WindowInfo requestedWindow, string message, bool verificationPerformed, int tolerancePixels) {
        return new DesktopWindowMutationRecord {
            Action = action,
            Success = false,
            VerificationPerformed = verificationPerformed,
            Verified = verificationPerformed ? false : null,
            VerificationMode = verificationPerformed ? "error" : string.Empty,
            VerificationSummary = message,
            VerificationTolerancePixels = tolerancePixels,
            RequestedWindow = requestedWindow,
            VerificationNotes = new[] { message }
        };
    }

    private static WindowInfo ObserveWindowByHandle(DesktopAutomationService automation, IntPtr handle) {
        if (handle == IntPtr.Zero) {
            return null;
        }

        IReadOnlyList<WindowInfo> windows = automation.GetWindows(new WindowQueryOptions {
            Handle = handle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });
        return windows.Count > 0 ? windows[0] : null;
    }

    private static WindowInfo SafeGetActiveWindow(DesktopAutomationService automation) {
        try {
            return automation.GetActiveWindow(includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
        } catch {
            return null;
        }
    }
}
