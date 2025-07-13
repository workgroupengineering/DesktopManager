using System;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>
/// Provides helper methods for interacting with window controls.
/// </summary>
[SupportedOSPlatform("windows")]
public static class WindowControlService {
    /// <summary>
    /// Clicks the specified control.
    /// </summary>
    /// <param name="control">Control to click.</param>
    /// <param name="button">Mouse button to use.</param>
    public static void ControlClick(WindowControlInfo control, MouseButton button) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }
        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        // Try to use BM_CLICK first
        MonitorNativeMethods.SendMessage(control.Handle, MonitorNativeMethods.BM_CLICK, 0, 0);

        if (MonitorNativeMethods.GetWindowRect(control.Handle, out RECT rect)) {
            int x = (rect.Left + rect.Right) / 2;
            int y = (rect.Top + rect.Bottom) / 2;
            MouseInputService.MoveCursor(x, y);
            MouseInputService.Click(button);
        }
    }
}
