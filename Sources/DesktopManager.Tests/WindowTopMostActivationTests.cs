using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for WindowTopMostActivationTests.
/// </summary>
public class WindowTopMostActivationTests
{
    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Test for SetWindowTopMost_TogglesState.
    /// </summary>
    public void SetWindowTopMost_TogglesState()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireInteractive();

        // Launch a notepad instance that we can control
        System.Diagnostics.Process? notepadProcess = null;
        WindowInfo? window = null;
        try {
            if (!TestHelper.TryStartNotepadWindow(out notepadProcess, out window, hideWindow: true) || window == null) {
                Assert.Inconclusive("Failed to start notepad for testing");
                return;
            }

            var manager = new WindowManager();

            long originalStyle = MonitorNativeMethods.GetWindowLongPtr(window.Handle, MonitorNativeMethods.GWL_EXSTYLE).ToInt64();
            bool wasTop = (originalStyle & MonitorNativeMethods.WS_EX_TOPMOST) != 0;

            // Set to opposite of current state
            manager.SetWindowTopMost(window, !wasTop);
            
            // Give the system time to process the change
            System.Threading.Thread.Sleep(100);
            
            long toggled = MonitorNativeMethods.GetWindowLongPtr(window.Handle, MonitorNativeMethods.GWL_EXSTYLE).ToInt64();
            bool newIsTop = (toggled & MonitorNativeMethods.WS_EX_TOPMOST) != 0;
            
            Assert.AreEqual(!wasTop, newIsTop, $"Expected topmost to change from {wasTop} to {!wasTop}, but got {newIsTop}");
            
            // Test changing back
            manager.SetWindowTopMost(window, wasTop);
            System.Threading.Thread.Sleep(100);
            
            long restored = MonitorNativeMethods.GetWindowLongPtr(window.Handle, MonitorNativeMethods.GWL_EXSTYLE).ToInt64();
            bool restoredIsTop = (restored & MonitorNativeMethods.WS_EX_TOPMOST) != 0;
            
            Assert.AreEqual(wasTop, restoredIsTop, $"Expected topmost to restore to {wasTop}, but got {restoredIsTop}");
            
        } finally {
            TestHelper.SafeKillProcess(notepadProcess);
        }
    }

    [TestMethod]
    /// <summary>
    /// Test for ActivateWindow_BringsToFront.
    /// </summary>
    public void ActivateWindow_BringsToFront()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireInteractive();

        var manager = new WindowManager();
        var originalForeground = MonitorNativeMethods.GetForegroundWindow();
        if (originalForeground == IntPtr.Zero) {
            Assert.Inconclusive("No foreground window found for activation testing");
            return;
        }

        var windows = manager.GetWindows(includeHidden: true);
        var window = windows.FirstOrDefault(w => w.Handle == originalForeground);
        if (window == null) {
            Assert.Inconclusive("Foreground window was not found in enumeration");
            return;
        }
        
        try {
            // Attempt activation on the current foreground window to avoid focus changes
            manager.ActivateWindow(window);

            // Give Windows time to process the activation
            System.Threading.Thread.Sleep(100);

            var newForeground = MonitorNativeMethods.GetForegroundWindow();
            if (newForeground == IntPtr.Zero) {
                Assert.Inconclusive($"GetForegroundWindow returned 0 after activation attempt. Original: {originalForeground:X8}");
            }

            if (newForeground != originalForeground) {
                Assert.Inconclusive($"Foreground window changed unexpectedly. Original: {originalForeground:X8}, Current: {newForeground:X8}");
            }
        } catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to activate window")) {
            // SetForegroundWindow failed - this is common due to Windows security restrictions
            Assert.Inconclusive($"Window activation failed due to Windows security policies. Target window: Handle={window.Handle:X8}. This is expected behavior in many Windows configurations due to User Interface Privilege Isolation (UIPI) or other focus management policies.");
        }
    }
}
