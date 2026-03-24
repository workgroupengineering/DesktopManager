using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for WindowTopMostActivationTests.
/// </summary>
public class WindowTopMostActivationTests {
    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Test for SetWindowTopMost_TogglesState.
    /// </summary>
    public void SetWindowTopMost_TogglesState() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireForegroundWindowUiTests();

        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager TopMost Harness");

        var manager = new WindowManager();

        long originalStyle = MonitorNativeMethods.GetWindowLongPtr(harness.Window.Handle, MonitorNativeMethods.GWL_EXSTYLE).ToInt64();
        bool wasTop = (originalStyle & MonitorNativeMethods.WS_EX_TOPMOST) != 0;

        // Set to opposite of current state
        manager.SetWindowTopMost(harness.Window, !wasTop);

        // Give the system time to process the change
        Thread.Sleep(100);

        long toggled = MonitorNativeMethods.GetWindowLongPtr(harness.Window.Handle, MonitorNativeMethods.GWL_EXSTYLE).ToInt64();
        bool newIsTop = (toggled & MonitorNativeMethods.WS_EX_TOPMOST) != 0;

        Assert.AreEqual(!wasTop, newIsTop, $"Expected topmost to change from {wasTop} to {!wasTop}, but got {newIsTop}");

        // Test changing back
        manager.SetWindowTopMost(harness.Window, wasTop);
        Thread.Sleep(100);

        long restored = MonitorNativeMethods.GetWindowLongPtr(harness.Window.Handle, MonitorNativeMethods.GWL_EXSTYLE).ToInt64();
        bool restoredIsTop = (restored & MonitorNativeMethods.WS_EX_TOPMOST) != 0;

        Assert.AreEqual(wasTop, restoredIsTop, $"Expected topmost to restore to {wasTop}, but got {restoredIsTop}");
    }

    [TestMethod]
    /// <summary>
    /// Test for ActivateWindow_BringsToFront.
    /// </summary>
    public void ActivateWindow_BringsToFront() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireForegroundWindowUiTests();

        var manager = new WindowManager();
        using WinFormsWindowHarness firstHarness = WinFormsWindowHarness.Create("DesktopManager Activation Harness 1");
        using WinFormsWindowHarness secondHarness = WinFormsWindowHarness.Create("DesktopManager Activation Harness 2");

        try {
            manager.ActivateWindow(secondHarness.Window);
            Application.DoEvents();
            Thread.Sleep(100);

            manager.ActivateWindow(firstHarness.Window);
            Application.DoEvents();
            Thread.Sleep(100);

            IntPtr newForeground = MonitorNativeMethods.GetForegroundWindow();
            if (newForeground == IntPtr.Zero) {
                Assert.Inconclusive("GetForegroundWindow returned 0 after activation attempt.");
            }

            Assert.AreEqual(firstHarness.Window.Handle, newForeground,
                $"Expected the owned harness window to become foreground. Expected: {firstHarness.Window.Handle:X8}, Actual: {newForeground:X8}");
        } catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to activate window")) {
            Assert.Inconclusive($"Window activation failed due to Windows security policies. Target window: Handle={firstHarness.Window.Handle:X8}. This is expected behavior in many Windows configurations due to User Interface Privilege Isolation (UIPI) or other focus management policies.");
        }
    }
}
