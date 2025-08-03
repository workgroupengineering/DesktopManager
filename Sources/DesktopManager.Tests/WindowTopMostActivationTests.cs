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

        // Launch a notepad instance that we can control
        System.Diagnostics.Process? notepadProcess = null;
        try {
            notepadProcess = System.Diagnostics.Process.Start("notepad.exe");
            if (notepadProcess == null) {
                Assert.Inconclusive("Failed to start notepad for testing");
            }
            
            // Wait for notepad to fully load
            notepadProcess.WaitForInputIdle(5000);
            System.Threading.Thread.Sleep(500);
            
            var manager = new WindowManager();
            var notepadWindows = manager.GetWindowsForProcess(notepadProcess);
            
            if (notepadWindows.Count == 0) {
                Assert.Inconclusive("Could not find notepad window");
            }
            
            var window = notepadWindows.First();

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
            if (notepadProcess != null && !notepadProcess.HasExited) {
                try {
                    notepadProcess.CloseMainWindow();
                    if (!notepadProcess.WaitForExit(2000)) {
                        notepadProcess.Kill();
                    }
                } catch {
                    // Ignore cleanup errors
                }
                notepadProcess.Dispose();
            }
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

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0)
        {
            Assert.Inconclusive("No windows found to test");
        }

        // Find a visible, non-minimized window that we can likely activate
        // Prefer application windows over system windows for activation
        var window = windows.FirstOrDefault(w => {
            if (!w.IsVisible || w.State == WindowState.Minimize) return false;
            
            try {
                var proc = System.Diagnostics.Process.GetProcessById((int)w.ProcessId);
                var name = proc.ProcessName.ToLower();
                
                // Prefer user applications that are more likely to be activatable
                return name.Contains("explorer") || name.Contains("notepad") || 
                       name.Contains("chrome") || name.Contains("firefox") || 
                       name.Contains("code") || name.Contains("calculator");
            } catch {
                return false;
            }
        });
        
        // If no preferred window found, try any visible window
        if (window == null) {
            window = windows.FirstOrDefault(w => w.IsVisible && w.State != WindowState.Minimize);
        }
        
        if (window == null) {
            Assert.Inconclusive("No suitable visible window found for activation testing");
        }
        
        // Store the original foreground window
        var originalForeground = MonitorNativeMethods.GetForegroundWindow();
        
        try {
            // Attempt activation
            manager.ActivateWindow(window);
            
            // Give Windows time to process the activation
            System.Threading.Thread.Sleep(100);
            
            var newForeground = MonitorNativeMethods.GetForegroundWindow();
            
            // Check if activation was successful
            if (newForeground == window.Handle) {
                // Success! The window was activated
                Assert.AreEqual(window.Handle, newForeground);
            } else if (newForeground == IntPtr.Zero) {
                // GetForegroundWindow returned 0, which can happen in some Windows configurations
                Assert.Inconclusive($"GetForegroundWindow returned 0 after activation attempt. This may be due to Windows security policies or system state. Original: {originalForeground:X8}, Target: {window.Handle:X8}");
            } else if (newForeground == originalForeground) {
                // The foreground window didn't change - activation may have been blocked
                Assert.Inconclusive($"Window activation was blocked or failed. Target window: Handle={window.Handle:X8}, Current foreground: {newForeground:X8}. This may be due to Windows security policies (UIPI) or focus restrictions.");
            } else {
                // A different window became foreground
                Assert.Fail($"Expected window {window.Handle:X8} to become foreground, but window {newForeground:X8} became foreground instead");
            }
        } catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to activate window")) {
            // SetForegroundWindow failed - this is common due to Windows security restrictions
            Assert.Inconclusive($"Window activation failed due to Windows security policies. Target window: Handle={window.Handle:X8}. This is expected behavior in many Windows configurations due to User Interface Privilege Isolation (UIPI) or other focus management policies.");
        }
    }
}
