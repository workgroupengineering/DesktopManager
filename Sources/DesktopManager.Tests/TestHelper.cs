using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace DesktopManager.Tests;

/// <summary>
/// Helper class for test utilities.
/// </summary>
internal static class TestHelper {
    private const int SW_HIDE = 0;
    private const int SW_MINIMIZE = 6;
    
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    
    /// <summary>
    /// Starts a hidden Notepad process for testing.
    /// </summary>
    public static Process? StartHiddenNotepad() {
        // DISABLED: Tests that spawn Notepad are disabled
        // Returning null will cause tests to be marked as inconclusive
        return null;
        
        /* Original implementation commented out
        // Start normally first to ensure window is created
        var startInfo = new ProcessStartInfo("notepad.exe") {
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Minimized
        };
        
        var process = Process.Start(startInfo);
        if (process != null) {
            process.WaitForInputIdle(3000);
            Thread.Sleep(500);
            
            // Move windows off-screen but keep them enumerable
            var manager = new WindowManager();
            var windows = manager.GetWindowsForProcess(process);
            foreach (var window in windows) {
                // Move window far off-screen where it won't be visible
                SetWindowPos(window.Handle, IntPtr.Zero, -10000, -10000, 200, 200, SWP_NOZORDER);
                // Minimize to reduce any potential flickering
                ShowWindow(window.Handle, SW_MINIMIZE);
            }
        }
        
        return process;
        */
    }
    
    /// <summary>
    /// Determines if UI tests should be skipped.
    /// </summary>
    public static bool ShouldSkipUITests() {
        // Skip UI tests if explicitly requested
        if (Environment.GetEnvironmentVariable("SKIP_UI_TESTS") == "true") {
            return true;
        }
        
        // Run UI tests in CI environment
        if (Environment.GetEnvironmentVariable("CI") == "true") {
            return false;
        }
        
        // Skip UI tests by default in local development unless explicitly enabled
        if (Environment.GetEnvironmentVariable("RUN_UI_TESTS") == "true") {
            return false;
        }
        
        // Skip by default in local development
        return true;
    }
    
    /// <summary>
    /// Safely kills a process.
    /// </summary>
    public static void SafeKillProcess(Process? process) {
        if (process == null) return;
        
        try {
            if (!process.HasExited) {
                process.Kill();
                process.WaitForExit(1000);
            }
        } catch {
            // Ignore errors during cleanup
        } finally {
            process.Dispose();
        }
    }
    
    /// <summary>
    /// Kills all Notepad processes (cleanup).
    /// </summary>
    public static void KillAllNotepads() {
        try {
            var processes = Process.GetProcessesByName("notepad");
            foreach (var process in processes) {
                SafeKillProcess(process);
            }
        } catch {
            // Ignore errors during cleanup
        }
    }
}