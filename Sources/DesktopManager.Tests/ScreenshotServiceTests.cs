using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DesktopManager;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for ScreenshotServiceTests.
/// </summary>
public class ScreenshotServiceTests {
    [TestCleanup]
    public void Cleanup() {
        TestHelper.KillAllNotepads();
    }
    
    [TestMethod]
    /// <summary>
    /// Test for CaptureRegion_InvalidDimensions_Throws.
    /// </summary>
    public void CaptureRegion_InvalidDimensions_Throws() {
        Assert.ThrowsException<ArgumentException>(() => ScreenshotService.CaptureRegion(0, 0, 0, 0));
    }

    [TestMethod]
    /// <summary>
    /// Test for CaptureRegion_OutOfBounds_Throws.
    /// </summary>
    public void CaptureRegion_OutOfBounds_Throws() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        Rectangle bounds;
#if NETFRAMEWORK
        bounds = SystemInformation.VirtualScreen;
#else
        bounds = new Rectangle(
            MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_XVIRTUALSCREEN),
            MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_YVIRTUALSCREEN),
            MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_CXVIRTUALSCREEN),
            MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_CYVIRTUALSCREEN));
#endif
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => ScreenshotService.CaptureRegion(bounds.Right + 1, bounds.Bottom + 1, 10, 10));
    }

    [TestMethod]
    /// <summary>
    /// Test for CaptureScreen_ReturnsBitmap.
    /// </summary>
    public void CaptureScreen_ReturnsBitmap() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();

        using var bmp = ScreenshotService.CaptureScreen();
        Assert.IsNotNull(bmp);
        Assert.IsTrue(bmp.Width > 0);
        Assert.IsTrue(bmp.Height > 0);
    }

    [TestMethod]
    /// <summary>
    /// Test for CaptureMonitor_InvalidIndex_Throws.
    /// </summary>
    public void CaptureMonitor_InvalidIndex_Throws() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        Assert.ThrowsException<ArgumentException>(() => ScreenshotService.CaptureMonitor(index: 999));
    }

    [TestMethod]
    /// <summary>
    /// Test for CaptureMonitor_ByIndex_ReturnsBitmap.
    /// </summary>
    public void CaptureMonitor_ByIndex_ReturnsBitmap() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();

        using var bmp = ScreenshotService.CaptureMonitor(index: 0);
        Assert.IsNotNull(bmp);
        Assert.IsTrue(bmp.Width > 0);
        Assert.IsTrue(bmp.Height > 0);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// CaptureWindow size matches window bounds.
    /// </summary>
    public void CaptureWindow_SizeMatchesBounds() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("Screenshot Window Harness");

        Assert.IsTrue(MonitorNativeMethods.GetWindowRect(harness.Window.Handle, out RECT rect));
        using var bmp = ScreenshotService.CaptureWindow(harness.Window.Handle);
        Assert.AreEqual(rect.Right - rect.Left, bmp.Width);
        Assert.AreEqual(rect.Bottom - rect.Top, bmp.Height);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// CaptureControl size matches control bounds.
    /// </summary>
    public void CaptureControl_SizeMatchesBounds() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using TextBox textBox = new() { Text = "Capture" };
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("Screenshot Control Harness", form => form.Controls.Add(textBox));
        textBox.CreateControl();
        Application.DoEvents();

        var enumerator = new ControlEnumerator();
        var controls = enumerator.EnumerateControls(harness.Window.Handle);
        var edit = controls.FirstOrDefault(c => c.Handle == textBox.Handle);
        Assert.IsNotNull(edit, "Edit control not found");

        Assert.IsTrue(MonitorNativeMethods.GetWindowRect(edit.Handle, out RECT rect));
        using var bmp = ScreenshotService.CaptureControl(edit.Handle);
        Assert.AreEqual(rect.Right - rect.Left, bmp.Width);
        Assert.AreEqual(rect.Bottom - rect.Top, bmp.Height);
    }
}
