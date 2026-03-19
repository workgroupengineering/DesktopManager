using System;
using System.Collections.Generic;

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

        var enumerator = new ControlEnumerator();
        var controls = enumerator.EnumerateControls(window.Handle);
        WindowControlQueryOptions filter = options ?? new WindowControlQueryOptions();

        if (filter.Handle.HasValue && filter.Handle.Value != IntPtr.Zero) {
            controls = controls.FindAll(control => control.Handle == filter.Handle.Value);
        }

        if (filter.Id.HasValue) {
            controls = controls.FindAll(control => control.Id == filter.Id.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.ClassNamePattern) && filter.ClassNamePattern != "*") {
            controls = controls.FindAll(control => MatchesWildcard(control.ClassName, filter.ClassNamePattern));
        }

        if (!string.IsNullOrWhiteSpace(filter.TextPattern) && filter.TextPattern != "*") {
            controls = controls.FindAll(control => MatchesWildcard(control.Text ?? string.Empty, filter.TextPattern));
        }

        return controls;
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
}
