using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DesktopManager;

public partial class WindowManager {
    /// <summary>
    /// Clicks the specified control.
    /// </summary>
    /// <param name="control">Control to click.</param>
    /// <param name="button">Mouse button to use.</param>
    public void ClickControl(WindowControlInfo control, MouseButton button = MouseButton.Left) {
        WindowControlService.ControlClick(control, button);
    }

    /// <summary>
    /// Gets child controls for the specified window.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="options">Optional control filter options.</param>
    /// <returns>A list of matching controls.</returns>
    public List<WindowControlInfo> GetControls(WindowInfo window, WindowControlQueryOptions? options = null) {
        ValidateWindowInfo(window);

        WindowControlQueryOptions filter = options ?? new WindowControlQueryOptions();
        PrepareWindowForUiAutomation(window, filter);
        List<WindowControlInfo> controls = GetControlsInternal(window.Handle, filter);

        return controls.FindAll(control => MatchesControl(control, filter));
    }

    /// <summary>
    /// Collects shared diagnostics for control discovery against a single window.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="options">Optional control filter options.</param>
    /// <param name="sampleLimit">Maximum number of sample controls to include.</param>
    /// <returns>Discovery diagnostics for the supplied window.</returns>
    public DesktopControlDiscoveryDiagnostics DiagnoseControls(WindowInfo window, WindowControlQueryOptions? options = null, int sampleLimit = 10) {
        ValidateWindowInfo(window);
        if (sampleLimit < 0) {
            throw new ArgumentOutOfRangeException(nameof(sampleLimit), "sampleLimit must be zero or greater.");
        }

        WindowControlQueryOptions filter = options ?? new WindowControlQueryOptions();
        UiAutomationPreparationResult preparation = PrepareWindowForUiAutomation(window, filter);

        var enumerator = new ControlEnumerator();
        List<WindowControlInfo> win32Controls = enumerator.EnumerateControls(window.Handle);
        var uiAutomation = new UiAutomationControlService();
        List<WindowControlInfo> uiAutomationControls = filter.RequiresUiAutomation()
            ? uiAutomation.EnumerateControls(window.Handle)
            : new List<WindowControlInfo>();
        List<WindowControlInfo> effectiveControls = SelectDiscoveredControls(filter, win32Controls, uiAutomationControls);
        List<WindowControlInfo> matchedControls = effectiveControls.FindAll(control => MatchesControl(control, filter));

        return new DesktopControlDiscoveryDiagnostics {
            Window = window,
            RequiresUiAutomation = filter.RequiresUiAutomation(),
            UseUiAutomation = filter.UseUiAutomation,
            IncludeUiAutomation = filter.IncludeUiAutomation,
            EnsureForegroundWindow = filter.EnsureForegroundWindow,
            UiAutomationAvailable = uiAutomation.IsAvailable,
            PreparationAttempted = preparation.Attempted,
            PreparationSucceeded = preparation.Succeeded,
            EffectiveSource = GetEffectiveSource(filter, uiAutomationControls),
            Win32ControlCount = win32Controls.Count,
            UiAutomationControlCount = uiAutomationControls.Count,
            EffectiveControlCount = effectiveControls.Count,
            MatchedControlCount = matchedControls.Count,
            SampleControls = effectiveControls.Take(sampleLimit).ToArray()
        };
    }

    private UiAutomationPreparationResult PrepareWindowForUiAutomation(WindowInfo window, WindowControlQueryOptions filter) {
        if (!filter.RequiresUiAutomation() || !filter.EnsureForegroundWindow) {
            return UiAutomationPreparationResult.None;
        }

        if (WindowActivationService.TryPrepareWindowForAutomation(window.Handle)) {
            Thread.Sleep(200);
            return UiAutomationPreparationResult.Success;
        }

        return UiAutomationPreparationResult.Failed;
    }

    private List<WindowControlInfo> GetControlsInternal(IntPtr windowHandle, WindowControlQueryOptions filter) {
        var enumerator = new ControlEnumerator();
        List<WindowControlInfo> win32Controls = enumerator.EnumerateControls(windowHandle);

        if (!filter.RequiresUiAutomation()) {
            return win32Controls;
        }

        var uiAutomation = new UiAutomationControlService();
        List<WindowControlInfo> uiAutomationControls = uiAutomation.EnumerateControls(windowHandle);

        return SelectDiscoveredControls(filter, win32Controls, uiAutomationControls);
    }

    private static List<WindowControlInfo> SelectDiscoveredControls(WindowControlQueryOptions filter, List<WindowControlInfo> win32Controls, List<WindowControlInfo> uiAutomationControls) {
        if (filter.UseUiAutomation && !filter.IncludeUiAutomation) {
            return uiAutomationControls;
        }

        if (!filter.IncludeUiAutomation) {
            return uiAutomationControls.Count > 0 ? uiAutomationControls : win32Controls;
        }

        return MergeControls(win32Controls, uiAutomationControls);
    }

    private static string GetEffectiveSource(WindowControlQueryOptions filter, List<WindowControlInfo> uiAutomationControls) {
        if (!filter.RequiresUiAutomation()) {
            return "Win32";
        }

        if (filter.UseUiAutomation && !filter.IncludeUiAutomation) {
            return "UiAutomation";
        }

        if (filter.IncludeUiAutomation) {
            return "Merged";
        }

        return uiAutomationControls.Count > 0 ? "UiAutomationFallback" : "Win32Fallback";
    }

    private static List<WindowControlInfo> MergeControls(List<WindowControlInfo> win32Controls, List<WindowControlInfo> uiAutomationControls) {
        var merged = new List<WindowControlInfo>(win32Controls.Count + uiAutomationControls.Count);
        merged.AddRange(win32Controls);

        foreach (WindowControlInfo uiAutomationControl in uiAutomationControls) {
            bool alreadyPresent = merged.Any(existing =>
                (existing.Handle != IntPtr.Zero &&
                uiAutomationControl.Handle != IntPtr.Zero &&
                existing.Handle == uiAutomationControl.Handle) ||
                (string.Equals(existing.AutomationId, uiAutomationControl.AutomationId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing.ControlType, uiAutomationControl.ControlType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing.Text, uiAutomationControl.Text, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing.ClassName, uiAutomationControl.ClassName, StringComparison.OrdinalIgnoreCase)));
            if (!alreadyPresent) {
                merged.Add(uiAutomationControl);
            }
        }

        return merged;
    }

    private bool MatchesControl(WindowControlInfo control, WindowControlQueryOptions filter) {
        if (filter.Handle.HasValue && filter.Handle.Value != IntPtr.Zero) {
            if (control.Handle != filter.Handle.Value) {
                return false;
            }
        }

        if (filter.Id.HasValue) {
            if (control.Id != filter.Id.Value) {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.ClassNamePattern) && filter.ClassNamePattern != "*") {
            if (!MatchesWildcard(control.ClassName, filter.ClassNamePattern)) {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.TextPattern) && filter.TextPattern != "*") {
            if (!MatchesWildcard(control.Text ?? string.Empty, filter.TextPattern)) {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.AutomationIdPattern) && filter.AutomationIdPattern != "*") {
            if (!MatchesWildcard(control.AutomationId, filter.AutomationIdPattern)) {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.ControlTypePattern) && filter.ControlTypePattern != "*") {
            if (!MatchesWildcard(control.ControlType, filter.ControlTypePattern)) {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.FrameworkIdPattern) && filter.FrameworkIdPattern != "*") {
            if (!MatchesWildcard(control.FrameworkId, filter.FrameworkIdPattern)) {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.ValuePattern) && filter.ValuePattern != "*") {
            if (!MatchesWildcard(control.Value ?? string.Empty, filter.ValuePattern)) {
                return false;
            }
        }

        if (filter.IsEnabled.HasValue) {
            if (!control.IsEnabled.HasValue || control.IsEnabled.Value != filter.IsEnabled.Value) {
                return false;
            }
        }

        if (filter.IsKeyboardFocusable.HasValue) {
            if (!control.IsKeyboardFocusable.HasValue || control.IsKeyboardFocusable.Value != filter.IsKeyboardFocusable.Value) {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets child controls for one or more windows matched by the supplied window query.
    /// </summary>
    /// <param name="windowOptions">Window query options.</param>
    /// <param name="controlOptions">Optional control filter options.</param>
    /// <param name="allWindows">Whether to enumerate controls for all matching windows.</param>
    /// <returns>A list of matching control targets.</returns>
    public List<WindowControlTargetInfo> GetControls(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions = null, bool allWindows = false) {
        if (windowOptions == null) {
            throw new ArgumentNullException(nameof(windowOptions));
        }

        List<WindowInfo> windows = GetWindows(windowOptions);
        if (!allWindows && windows.Count > 1) {
            windows = new List<WindowInfo> { windows[0] };
        }

        var results = new List<WindowControlTargetInfo>();
        foreach (WindowInfo window in windows) {
            List<WindowControlInfo> controls = GetControls(window, controlOptions);
            foreach (WindowControlInfo control in controls) {
                results.Add(new WindowControlTargetInfo {
                    Window = window,
                    Control = control
                });
            }
        }

        return results;
    }

    private readonly struct UiAutomationPreparationResult {
        public static UiAutomationPreparationResult None => new(false, false);
        public static UiAutomationPreparationResult Success => new(true, true);
        public static UiAutomationPreparationResult Failed => new(true, false);

        public UiAutomationPreparationResult(bool attempted, bool succeeded) {
            Attempted = attempted;
            Succeeded = succeeded;
        }

        public bool Attempted { get; }
        public bool Succeeded { get; }
    }
}
