using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for showing and hiding windows.
/// </summary>
public class WindowVisibilityTests {
    [TestMethod]
    /// <summary>
    /// ShowWindow toggles visibility.
    /// </summary>
    public void ShowWindow_TogglesVisibility() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var window = windows.First();
        bool wasVisible = MonitorNativeMethods.IsWindowVisible(window.Handle);

        manager.ShowWindow(window, !wasVisible);
        bool toggled = MonitorNativeMethods.IsWindowVisible(window.Handle);
        Assert.AreEqual(!wasVisible, toggled);

        manager.ShowWindow(window, wasVisible);
        bool reverted = MonitorNativeMethods.IsWindowVisible(window.Handle);
        Assert.AreEqual(wasVisible, reverted);
    }

    [TestMethod]
    /// <summary>
    /// ShowWindow throws on invalid handle.
    /// </summary>
    public void ShowWindow_InvalidHandle_Throws() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var dummy = new WindowInfo { Handle = IntPtr.Zero };
        Assert.ThrowsException<InvalidOperationException>(() => manager.ShowWindow(dummy, true));
    }

    [TestMethod]
    /// <summary>
    /// GetWindows includes hidden windows when requested.
    /// </summary>
    public void GetWindows_IncludesHidden() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var all = manager.GetWindows(includeHidden: true);
        var visible = manager.GetWindows();

        Assert.IsTrue(all.Count >= visible.Count);
        if (!all.Any(w => !w.IsVisible)) {
            Assert.Inconclusive("No hidden windows found");
        }
    }
}
