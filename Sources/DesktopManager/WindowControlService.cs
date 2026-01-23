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

        RECT rect;
        if (MonitorNativeMethods.GetClientRect(control.Handle, out rect)) {
            int x = Math.Max(1, (rect.Right - rect.Left) / 2);
            int y = Math.Max(1, (rect.Bottom - rect.Top) / 2);
            SendMouseClick(control.Handle, button, x, y);
        } else {
            SendMouseClick(control.Handle, button, 0, 0);
        }
    }

    /// <summary>
    /// Retrieves the check state of a button control.
    /// </summary>
    /// <param name="control">Control to query.</param>
    /// <returns><c>true</c> if checked; otherwise <c>false</c>.</returns>
    public static bool GetCheckState(WindowControlInfo control) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }
        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        int state = (int)MonitorNativeMethods.SendMessage(control.Handle, MonitorNativeMethods.BM_GETCHECK, 0u, 0u);
        return state != 0;
    }

    /// <summary>
    /// Sets the check state of a button control.
    /// </summary>
    /// <param name="control">Control to modify.</param>
    /// <param name="check">Desired check state.</param>
    public static void SetCheckState(WindowControlInfo control, bool check) {   
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }
        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        MonitorNativeMethods.SendMessage(control.Handle, MonitorNativeMethods.BM_SETCHECK, check ? 1u : 0u, 0u);
    }

    private static void SendMouseClick(IntPtr handle, MouseButton button, int x, int y) {
        uint messageDown = button == MouseButton.Left ? MonitorNativeMethods.WM_LBUTTONDOWN : MonitorNativeMethods.WM_RBUTTONDOWN;
        uint messageUp = button == MouseButton.Left ? MonitorNativeMethods.WM_LBUTTONUP : MonitorNativeMethods.WM_RBUTTONUP;
        uint wParamDown = button == MouseButton.Left ? MonitorNativeMethods.MK_LBUTTON : MonitorNativeMethods.MK_RBUTTON;
        uint lParam = CreateLParam(x, y);

        MonitorNativeMethods.SendMessage(handle, messageDown, wParamDown, lParam);
        MonitorNativeMethods.SendMessage(handle, messageUp, 0, lParam);
    }

    private static uint CreateLParam(int x, int y) {
        return unchecked((uint)((y << 16) | (x & 0xFFFF)));
    }
}
