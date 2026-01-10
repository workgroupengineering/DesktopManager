using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for WindowPositionTests.
/// </summary>
public class WindowPositionTests {
    [TestMethod]
    /// <summary>
    /// Test for GetAndSetWindowPosition_RoundTrips.
    /// </summary>
    public void GetAndSetWindowPosition_RoundTrips() {
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
            var original = manager.GetWindowPosition(window);

            manager.SetWindowPosition(window, original.Left, original.Top);
            var updated = manager.GetWindowPosition(window);

            Assert.AreEqual(original.Left, updated.Left);
            Assert.AreEqual(original.Top, updated.Top);
        } finally {
            TestHelper.SafeKillProcess(process);
        }
    }

    [TestMethod]
    /// <summary>
    /// Test for MoveWindow_DoesNotChangeZOrder.
    /// </summary>
    public void MoveWindow_DoesNotChangeZOrder() {
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
            var windowsBefore = manager.GetWindows(includeHidden: true);
            int indexBefore = windowsBefore.FindIndex(w => w.Handle == window.Handle);

            var original = manager.GetWindowPosition(window);
            manager.SetWindowPosition(window, original.Left + 1, original.Top + 1);

            var windowsAfterMove = manager.GetWindows(includeHidden: true);
            int indexAfterMove = windowsAfterMove.FindIndex(w => w.Handle == window.Handle);

            Assert.AreEqual(indexBefore, indexAfterMove);
        } finally {
            TestHelper.SafeKillProcess(process);
        }
    }

    [TestMethod]
    /// <summary>
    /// Test for GetWindowPosition_InvalidHandle_Throws.
    /// </summary>
    public void GetWindowPosition_InvalidHandle_Throws() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var dummy = new WindowInfo { Handle = IntPtr.Zero };
        Assert.ThrowsException<InvalidOperationException>(() => manager.GetWindowPosition(dummy));
    }

    [TestMethod]
    /// <summary>
    /// Test for MoveWindowToMonitor_OnSameMonitor_ReturnsFalse.
    /// </summary>
    public void MoveWindowToMonitor_OnSameMonitor_ReturnsFalse() {
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
            var monitors = new Monitors().GetMonitors(index: window.MonitorIndex);
            var monitor = monitors.FirstOrDefault();
            if (monitor == null) {
                Assert.Inconclusive("Monitor not found");
                return;
            }

            bool moved = manager.MoveWindowToMonitor(window, monitor);

            Assert.IsFalse(moved);
        } finally {
            TestHelper.SafeKillProcess(process);
        }
    }

    [TestMethod]
    /// <summary>
    /// Test for MoveWindowToMonitor_OnDifferentMonitor_ReturnsTrue.
    /// </summary>
    public void MoveWindowToMonitor_OnDifferentMonitor_ReturnsTrue() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireInteractive();

        var monitors = new Monitors().GetMonitors();
        if (monitors.Count < 2) {
            Assert.Inconclusive("Need at least two monitors");
        }

        Process? process = null;
        WindowInfo? window = null;

        try {
            if (!TestHelper.TryStartNotepadWindow(out process, out window, hideWindow: true) || window == null) {
                Assert.Inconclusive("Failed to start Notepad for testing");
                return;
            }

            var manager = new WindowManager();
            var target = monitors.First(m => m.Index != window.MonitorIndex);

            bool moved = manager.MoveWindowToMonitor(window, target);

            Assert.IsTrue(moved);
        } finally {
            TestHelper.SafeKillProcess(process);
        }
    }
}
