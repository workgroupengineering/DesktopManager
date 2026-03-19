using System;
using System.Runtime.Versioning;
using System.Text;

namespace DesktopManager;

/// <summary>
/// Provides helper methods for interacting with window controls.
/// </summary>
[SupportedOSPlatform("windows")]
public static class WindowControlService {
    private const uint MessageTimeoutMilliseconds = 1000;

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
        SendMessageWithTimeout(control.Handle, MonitorNativeMethods.BM_CLICK, 0, 0);

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

        SendMessageWithTimeout(control.Handle, MonitorNativeMethods.BM_SETCHECK, check ? 1u : 0u, 0u);
    }

    /// <summary>
    /// Sets control text without relying on foreground focus.
    /// </summary>
    /// <param name="control">Control to modify.</param>
    /// <param name="text">Text to apply.</param>
    public static void SetText(WindowControlInfo control, string text) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        var buffer = new StringBuilder(text);
        IntPtr result = MonitorNativeMethods.SendMessageTimeout(
            control.Handle,
            MonitorNativeMethods.WM_SETTEXT,
            IntPtr.Zero,
            buffer,
            MonitorNativeMethods.SMTO_ABORTIFHUNG,
            MessageTimeoutMilliseconds,
            out _);
        if (result == IntPtr.Zero) {
            MonitorNativeMethods.SendMessage(control.Handle, MonitorNativeMethods.WM_SETTEXT, IntPtr.Zero, text);
        }
    }

    /// <summary>
    /// Sends key messages directly to a control without stealing focus.
    /// </summary>
    /// <param name="control">Target control.</param>
    /// <param name="keys">Keys to send.</param>
    public static void SendKeys(WindowControlInfo control, params VirtualKey[] keys) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        if (keys == null || keys.Length == 0) {
            throw new ArgumentException("No keys specified", nameof(keys));
        }

        var heldModifiers = new List<VirtualKey>();
        for (int index = 0; index < keys.Length; index++) {
            VirtualKey key = keys[index];
            bool hasTrailingKey = index < keys.Length - 1;
            if (IsModifierKey(key) && hasTrailingKey) {
                SendMessageWithTimeout(control.Handle, MonitorNativeMethods.WM_KEYDOWN, (uint)key, 0);
                heldModifiers.Add(key);
                continue;
            }

            if (IsPrintableKey(key) && heldModifiers.Count == 0) {
                uint end = unchecked((uint)0xFFFFFFFF);
                SendMessageWithTimeout(control.Handle, MonitorNativeMethods.EM_SETSEL, end, end);
                if (!TrySendMessageWithTimeout(control.Handle, MonitorNativeMethods.WM_CHAR, (uint)key, 0)) {
                    MonitorNativeMethods.PostMessage(control.Handle, MonitorNativeMethods.WM_CHAR, (uint)key, 0);
                }
            } else {
                SendMessageWithTimeout(control.Handle, MonitorNativeMethods.WM_KEYDOWN, (uint)key, 0);
                SendMessageWithTimeout(control.Handle, MonitorNativeMethods.WM_KEYUP, (uint)key, 0);
            }

            ReleaseHeldModifiers(control.Handle, heldModifiers);
        }

        ReleaseHeldModifiers(control.Handle, heldModifiers);
    }

    private static void SendMouseClick(IntPtr handle, MouseButton button, int x, int y) {
        uint messageDown = button == MouseButton.Left ? MonitorNativeMethods.WM_LBUTTONDOWN : MonitorNativeMethods.WM_RBUTTONDOWN;
        uint messageUp = button == MouseButton.Left ? MonitorNativeMethods.WM_LBUTTONUP : MonitorNativeMethods.WM_RBUTTONUP;
        uint wParamDown = button == MouseButton.Left ? MonitorNativeMethods.MK_LBUTTON : MonitorNativeMethods.MK_RBUTTON;
        uint lParam = CreateLParam(x, y);

        SendMessageWithTimeout(handle, messageDown, wParamDown, lParam);
        SendMessageWithTimeout(handle, messageUp, 0, lParam);
    }

    private static uint CreateLParam(int x, int y) {
        return unchecked((uint)((y << 16) | (x & 0xFFFF)));
    }

    private static bool IsPrintableKey(VirtualKey key) {
        return (key >= VirtualKey.VK_SPACE && key <= VirtualKey.VK_Z) ||
            (key >= VirtualKey.VK_0 && key <= VirtualKey.VK_9);
    }

    private static bool IsModifierKey(VirtualKey key) {
        return key == VirtualKey.VK_CONTROL ||
            key == VirtualKey.VK_LCONTROL ||
            key == VirtualKey.VK_RCONTROL ||
            key == VirtualKey.VK_SHIFT ||
            key == VirtualKey.VK_LSHIFT ||
            key == VirtualKey.VK_RSHIFT ||
            key == VirtualKey.VK_MENU ||
            key == VirtualKey.VK_LMENU ||
            key == VirtualKey.VK_RMENU ||
            key == VirtualKey.VK_LWIN ||
            key == VirtualKey.VK_RWIN;
    }

    private static void ReleaseHeldModifiers(IntPtr handle, List<VirtualKey> heldModifiers) {
        for (int index = heldModifiers.Count - 1; index >= 0; index--) {
            SendMessageWithTimeout(handle, MonitorNativeMethods.WM_KEYUP, (uint)heldModifiers[index], 0);
        }

        heldModifiers.Clear();
    }

    private static void SendMessageWithTimeout(IntPtr handle, uint message, uint wParam, uint lParam) {
        TrySendMessageWithTimeout(handle, message, wParam, lParam);
    }

    private static bool TrySendMessageWithTimeout(IntPtr handle, uint message, uint wParam, uint lParam) {
        IntPtr result = MonitorNativeMethods.SendMessageTimeout(
            handle,
            message,
            new IntPtr(unchecked((int)wParam)),
            new IntPtr(unchecked((int)lParam)),
            MonitorNativeMethods.SMTO_ABORTIFHUNG,
            MessageTimeoutMilliseconds,
            out _);
        return result != IntPtr.Zero;
    }
}
