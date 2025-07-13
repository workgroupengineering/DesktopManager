using System;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>
/// Native methods for enumerating window controls.
/// </summary>
public static partial class MonitorNativeMethods {
    /// <summary>Callback for <see cref="EnumChildWindows"/>.</summary>
    /// <param name="hWnd">Handle of the child window.</param>
    /// <param name="lParam">Application-defined value.</param>
    /// <returns><c>true</c> to continue enumeration.</returns>
    public delegate bool EnumChildProc(IntPtr hWnd, IntPtr lParam);

    /// <summary>Enumerates the child windows of a parent window.</summary>
    /// <param name="hwndParent">Parent window handle.</param>
    /// <param name="lpEnumFunc">Callback to invoke.</param>
    /// <param name="lParam">Application-defined value.</param>
    /// <returns><c>true</c> if enumeration completes.</returns>
    [DllImport("user32.dll")]
    public static extern bool EnumChildWindows(IntPtr hwndParent, EnumChildProc lpEnumFunc, IntPtr lParam);

    /// <summary>Retrieves the identifier of the specified child window.</summary>
    /// <param name="hWnd">Handle of the child window.</param>
    /// <returns>The control identifier.</returns>
    [DllImport("user32.dll")]
    public static extern int GetDlgCtrlID(IntPtr hWnd);
}
