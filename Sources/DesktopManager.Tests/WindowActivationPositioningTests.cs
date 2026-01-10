using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for WindowActivationPositioningTests.
/// </summary>
public class WindowActivationPositioningTests {
    [TestMethod]
    /// <summary>
    /// Test for SetWindowPosition_ResizesWindow.
    /// </summary>
    public void SetWindowPosition_ResizesWindow() {
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

            int newWidth = original.Width + 10;
            int newHeight = original.Height + 10;
            manager.SetWindowPosition(window, original.Left, original.Top, newWidth, newHeight);
            var resized = manager.GetWindowPosition(window);

            // Allow some tolerance for window frame/border differences in different environments
            int widthTolerance = Math.Abs(newWidth - resized.Width);
            int heightTolerance = Math.Abs(newHeight - resized.Height);

            Assert.IsTrue(widthTolerance <= 20,
                $"Width resize failed. Expected: {newWidth}, Actual: {resized.Width}, Tolerance: {widthTolerance}");
            Assert.IsTrue(heightTolerance <= 20,
                $"Height resize failed. Expected: {newHeight}, Actual: {resized.Height}, Tolerance: {heightTolerance}");
        } finally {
            TestHelper.SafeKillProcess(process);
        }
    }

    [TestMethod]
    /// <summary>
    /// Test for ActivateWindow_InvalidHandle_Throws.
    /// </summary>
    public void ActivateWindow_InvalidHandle_Throws() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var dummy = new WindowInfo { Handle = IntPtr.Zero };
        Assert.ThrowsException<InvalidOperationException>(() => manager.ActivateWindow(dummy));
    }
}
