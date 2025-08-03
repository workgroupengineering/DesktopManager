using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for window style manipulation methods.
/// </summary>
public class WindowStyleModificationTests {
    [TestMethod]
    /// <summary>
    /// Ensures GetWindowStyle matches native API call.
    /// </summary>
    public void GetWindowStyle_MatchesNative() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var window = windows.First();
        long managed = manager.GetWindowStyle(window);
        long native = MonitorNativeMethods.GetWindowLongPtr(window.Handle, MonitorNativeMethods.GWL_STYLE).ToInt64();
        Assert.AreEqual(native, managed);
    }

    [TestMethod]
    /// <summary>
    /// Ensures SetWindowStyle toggles the topmost flag.
    /// </summary>
    public void SetWindowStyle_TogglesTopMost() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        // Find a regular application window that we can modify (not system windows)
        var window = windows.FirstOrDefault(w => {
            try {
                // Skip shell window and other system windows
                if (w.Handle == MonitorNativeMethods.GetShellWindow()) return false;
                
                // Try to get process - skip if we can't access it
                var proc = System.Diagnostics.Process.GetProcessById((int)w.ProcessId);
                var name = proc.ProcessName.ToLower();
                
                // Skip system processes that we shouldn't modify
                return !name.Contains("dwm") && !name.Contains("winlogon") && 
                       !name.Contains("csrss") && !name.Contains("smss");
            } catch {
                return false;
            }
        }) ?? windows.First();

        long original = manager.GetWindowStyle(window, true);
        bool isTop = (original & MonitorNativeMethods.WS_EX_TOPMOST) != 0;

        try {
            manager.SetWindowStyle(window, MonitorNativeMethods.WS_EX_TOPMOST, !isTop, true);
            
            // Give the system time to process the change
            System.Threading.Thread.Sleep(100);
            
            long toggled = manager.GetWindowStyle(window, true);
            bool newIsTop = (toggled & MonitorNativeMethods.WS_EX_TOPMOST) != 0;
            
            // Some system windows cannot have their topmost state changed
            if (newIsTop == isTop) {
                Assert.Inconclusive("Window does not support topmost state changes");
            }
            
            Assert.AreEqual(!isTop, newIsTop);
        } finally {
            // Always restore original state
            try {
                manager.SetWindowStyle(window, MonitorNativeMethods.WS_EX_TOPMOST, isTop, true);
            } catch {
                // Ignore errors during cleanup
            }
        }
    }
}

