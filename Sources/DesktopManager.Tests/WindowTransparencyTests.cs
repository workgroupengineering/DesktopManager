using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for verifying window transparency adjustments.
/// </summary>
public class WindowTransparencyTests {
    [TestMethod]
    /// <summary>
    /// Ensures SetWindowTransparency applies layered style and alpha.
    /// </summary>
    public void SetWindowTransparency_AppliesLayeredStyleAndAlpha() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireOwnedWindowMutationTests();

        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager Transparency Harness");

        var manager = new WindowManager();
        long originalStyle = MonitorNativeMethods.GetWindowLongPtr(harness.Window.Handle, MonitorNativeMethods.GWL_EXSTYLE).ToInt64();
        uint key;
        byte origAlpha;
        uint flags;
        MonitorNativeMethods.GetLayeredWindowAttributes(harness.Window.Handle, out key, out origAlpha, out flags);

        manager.SetWindowTransparency(harness.Window, 128);
        Application.DoEvents();
        long layeredStyle = MonitorNativeMethods.GetWindowLongPtr(harness.Window.Handle, MonitorNativeMethods.GWL_EXSTYLE).ToInt64();
        MonitorNativeMethods.GetLayeredWindowAttributes(harness.Window.Handle, out key, out byte newAlpha, out flags);

        Assert.IsTrue((layeredStyle & MonitorNativeMethods.WS_EX_LAYERED) != 0);
        Assert.AreEqual((byte)128, newAlpha);

        MonitorNativeMethods.SetLayeredWindowAttributes(harness.Window.Handle, 0, origAlpha, MonitorNativeMethods.LWA_ALPHA);
        MonitorNativeMethods.SetWindowLongPtr(harness.Window.Handle, MonitorNativeMethods.GWL_EXSTYLE, new IntPtr(originalStyle));
    }
}
