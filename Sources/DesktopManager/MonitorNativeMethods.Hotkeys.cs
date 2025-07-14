using System;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>
/// Native methods for registering hotkeys.
/// </summary>
public static partial class MonitorNativeMethods {
    /// <summary>Registers a system wide hotkey.</summary>
    /// <param name="hWnd">Window handle that will receive the hotkey message.</param>
    /// <param name="id">Identifier of the hotkey.</param>
    /// <param name="fsModifiers">Modifier flags.</param>
    /// <param name="vk">Virtual key code.</param>
    /// <returns>True if successful.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    /// <summary>Unregisters a system wide hotkey.</summary>
    /// <param name="hWnd">Window handle used in registration.</param>
    /// <param name="id">Identifier of the hotkey.</param>
    /// <returns>True if successful.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    /// <summary>Message identifier for hotkeys.</summary>
    public const uint WM_HOTKEY = 0x0312;
}
