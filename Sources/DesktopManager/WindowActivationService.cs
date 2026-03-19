using System;
using System.Threading;

namespace DesktopManager;

internal static class WindowActivationService {
    public static bool TryActivateWindow(IntPtr handle, int retryCount = 5, int retryDelayMilliseconds = 100) {
        if (handle == IntPtr.Zero) {
            return false;
        }

        if (retryCount < 1) {
            retryCount = 1;
        }

        if (retryDelayMilliseconds < 0) {
            retryDelayMilliseconds = 0;
        }

        for (int attempt = 0; attempt < retryCount; attempt++) {
            if (MonitorNativeMethods.IsIconic(handle)) {
                MonitorNativeMethods.ShowWindow(handle, MonitorNativeMethods.SW_RESTORE);
            } else {
                MonitorNativeMethods.ShowWindow(handle, MonitorNativeMethods.SW_SHOW);
            }

            MonitorNativeMethods.BringWindowToTop(handle);
            MonitorNativeMethods.SetForegroundWindow(handle);

            if (MonitorNativeMethods.GetForegroundWindow() == handle) {
                return true;
            }

            if (attempt < retryCount - 1 && retryDelayMilliseconds > 0) {
                Thread.Sleep(retryDelayMilliseconds);
            }
        }

        return MonitorNativeMethods.GetForegroundWindow() == handle;
    }
}
