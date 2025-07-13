using System;

namespace DesktopManager;

public partial class WindowManager {
    /// <summary>
    /// Retrieves window style bits.
    /// </summary>
    /// <param name="windowInfo">Target window.</param>
    /// <param name="extended">True to get extended style.</param>
    /// <returns>Style flags as a long.</returns>
    public long GetWindowStyle(WindowInfo windowInfo, bool extended = false) {
        if (windowInfo.Handle == IntPtr.Zero) {
            throw new InvalidOperationException("Invalid window handle");
        }

        int index = extended ? MonitorNativeMethods.GWL_EXSTYLE : MonitorNativeMethods.GWL_STYLE;
        return MonitorNativeMethods.GetWindowLongPtr(windowInfo.Handle, index).ToInt64();
    }

    /// <summary>
    /// Adds or removes style bits on a window.
    /// </summary>
    /// <param name="windowInfo">Target window.</param>
    /// <param name="flags">Style flags to modify.</param>
    /// <param name="enable">True to set bits; false to clear them.</param>
    /// <param name="extended">True to modify extended style.</param>
    public void SetWindowStyle(WindowInfo windowInfo, long flags, bool enable, bool extended = false) {
        if (windowInfo.Handle == IntPtr.Zero) {
            throw new InvalidOperationException("Invalid window handle");
        }

        int index = extended ? MonitorNativeMethods.GWL_EXSTYLE : MonitorNativeMethods.GWL_STYLE;
        long style = GetWindowStyle(windowInfo, extended);
        long newStyle = enable ? (style | flags) : (style & ~flags);
        MonitorNativeMethods.SetWindowLongPtr(windowInfo.Handle, index, new IntPtr(newStyle));
    }
}

