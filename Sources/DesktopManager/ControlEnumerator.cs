using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopManager;

/// <summary>
/// Provides methods for enumerating child window controls.
/// </summary>
public class ControlEnumerator {
    /// <summary>Enumerates all child controls of the given parent window.</summary>
    /// <param name="parent">Handle of the parent window.</param>
    /// <returns>List of control information.</returns>
    public List<WindowControlInfo> EnumerateControls(IntPtr parent) {
        List<WindowControlInfo> controls = new List<WindowControlInfo>();
        MonitorNativeMethods.EnumChildWindows(parent, (hWnd, lParam) => {
            WindowControlInfo info = new WindowControlInfo {
                Handle = hWnd,
                Id = MonitorNativeMethods.GetDlgCtrlID(hWnd)
            };

            int len = MonitorNativeMethods.GetWindowTextLength(hWnd);
            if (len > 0) {
                StringBuilder sb = new StringBuilder(len + 1);
                MonitorNativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
                info.Text = sb.ToString();
            } else {
                info.Text = string.Empty;
            }

            StringBuilder classSb = new StringBuilder(256);
            MonitorNativeMethods.GetClassName(hWnd, classSb, classSb.Capacity);
            info.ClassName = classSb.ToString();

            controls.Add(info);
            return true;
        }, IntPtr.Zero);
        return controls;
    }
}
