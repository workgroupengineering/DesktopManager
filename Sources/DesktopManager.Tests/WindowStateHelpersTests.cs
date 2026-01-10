using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for window state helper methods.
/// </summary>
public class WindowStateHelpersTests {
    [TestMethod]
    /// <summary>
    /// Ensures MinimizeWindows and RestoreWindows toggle state.
    /// </summary>
    public void MinimizeAndRestoreWindows_TogglesState() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireInteractive();

        Process? process = null;
        WindowInfo? window = null;

        try {
            if (!TestHelper.TryStartNotepadWindow(out process, out window, hideWindow: true) || window == null) {
                Assert.Inconclusive("Failed to start Notepad for testing");
                return;
            }

            var manager = new WindowManager();
            bool initialMinimized = IsMinimized(window.Handle);

            manager.MinimizeWindows(new[] { window }, ignoreErrors: false);
            Assert.IsTrue(IsMinimized(window.Handle) || initialMinimized);

            manager.RestoreWindows(new[] { window }, ignoreErrors: false);
            Assert.IsFalse(IsMinimized(window.Handle));
        } finally {
            TestHelper.SafeKillProcess(process);
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures EnsureWindowOnScreen moves a window back to a visible monitor.
    /// </summary>
    public void EnsureWindowOnScreen_MovesOffscreenWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireInteractive();

        Process? process = null;
        WindowInfo? window = null;

        try {
            if (!TestHelper.TryStartNotepadWindow(out process, out window, hideWindow: true) || window == null) {
                Assert.Inconclusive("Failed to start Notepad for testing");
                return;
            }

            var manager = new WindowManager();
            manager.SetWindowPosition(window, -5000, -5000);

            bool moved = manager.EnsureWindowOnScreen(window);
            Assert.IsTrue(moved);

            Assert.IsTrue(MonitorNativeMethods.GetWindowRect(window.Handle, out RECT rect));
            var monitors = new Monitors().GetMonitors();
            if (monitors.Count == 0) {
                Assert.Inconclusive("No monitors found for verification");
            }

            Assert.IsTrue(IsRectOnAnyMonitor(rect, monitors));
        } finally {
            TestHelper.SafeKillProcess(process);
        }
    }

    private static bool IsMinimized(IntPtr handle) {
        IntPtr stylePtr = MonitorNativeMethods.GetWindowLongPtr(handle, MonitorNativeMethods.GWL_STYLE);
        long style = stylePtr.ToInt64();
        return (style & MonitorNativeMethods.WS_MINIMIZE) != 0;
    }

    private static bool IsRectOnAnyMonitor(RECT rect, IEnumerable<Monitor> monitors) {
        foreach (var monitor in monitors) {
            var bounds = monitor.GetMonitorBounds();
            if (rect.Right <= bounds.Left || rect.Left >= bounds.Right ||
                rect.Bottom <= bounds.Top || rect.Top >= bounds.Bottom) {
                continue;
            }
            return true;
        }
        return false;
    }
}
