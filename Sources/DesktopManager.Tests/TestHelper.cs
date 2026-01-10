using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

/// <summary>
/// Helper class for test utilities.
/// </summary>
internal static class TestHelper {
    private const int SW_HIDE = 0;
    private const int SW_MINIMIZE = 6;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    /// <summary>
    /// Starts a minimized Notepad process for testing.
    /// </summary>
    public static Process? StartHiddenNotepad() {
        if (TryStartNotepadWindow(out var process, out _, hideWindow: true)) {
            return process;
        }

        return null;
    }

    /// <summary>
    /// Starts a Notepad process and returns the first window, optionally hidden.
    /// </summary>
    public static bool TryStartNotepadWindow(out Process? process, out WindowInfo? window, bool hideWindow = true) {
        process = null;
        window = null;

        try {
            var startInfo = new ProcessStartInfo("notepad.exe") {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized
            };

            process = Process.Start(startInfo);
            if (process == null) {
                return false;
            }

            process.WaitForInputIdle(3000);

            var manager = new WindowManager();
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < 3000) {
                var windows = manager.GetWindowsForProcess(process, includeHidden: true);
                if (windows.Count > 0) {
                    window = windows[0];
                    break;
                }

                Thread.Sleep(100);
                process.Refresh();
            }

            var mainHandle = process.MainWindowHandle;
            if (window == null && mainHandle != IntPtr.Zero) {
                var allWindows = manager.GetWindows(includeHidden: true);
                window = allWindows.Find(w => w.Handle == mainHandle);
            }

            if (window == null) {
                SafeKillProcess(process);
                process = null;
                return false;
            }

            if (hideWindow) {
                ShowWindow(window.Handle, SW_HIDE);
            } else {
                ShowWindow(window.Handle, SW_MINIMIZE);
            }

            return true;
        } catch {
            SafeKillProcess(process);
            process = null;
            window = null;
            return false;
        }
    }

    /// <summary>
    /// Determines if UI tests should be skipped.
    /// </summary>
    public static bool ShouldSkipUITests() {
        // Skip UI tests if explicitly requested
        if (Environment.GetEnvironmentVariable("SKIP_UI_TESTS") == "true" ||
            Environment.GetEnvironmentVariable("DESKTOPMANAGER_SKIP_UI_TESTS") == "true") {
            return true;
        }

        // Run UI tests in CI environment
        if (Environment.GetEnvironmentVariable("CI") == "true") {
            return false;
        }

        // Skip UI tests by default in local development unless explicitly enabled
        if (Environment.GetEnvironmentVariable("RUN_UI_TESTS") == "true" ||
            Environment.GetEnvironmentVariable("DESKTOPMANAGER_RUN_UI_TESTS") == "true") {
            return false;
        }

        // Skip by default in local development
        return true;
    }

    /// <summary>
    /// Determines if desktop-changing tests should be skipped.
    /// </summary>
    public static bool ShouldSkipDesktopChangeTests() {
        if (ShouldSkipUITests()) {
            return true;
        }

        if (Environment.GetEnvironmentVariable("RUN_DESTRUCTIVE_UI_TESTS") == "true" ||
            Environment.GetEnvironmentVariable("DESKTOPMANAGER_RUN_DESTRUCTIVE_UI_TESTS") == "true") {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Skips UI-impacting tests unless explicitly enabled.
    /// </summary>
    public static void RequireInteractive() {
        if (ShouldSkipUITests()) {
            Assert.Inconclusive("UI tests skipped in local development. Set RUN_UI_TESTS=true to run.");
        }
    }

    /// <summary>
    /// Skips desktop-changing tests unless explicitly enabled.
    /// </summary>
    public static void RequireDesktopChanges() {
        RequireInteractive();
        if (ShouldSkipDesktopChangeTests()) {
            Assert.Inconclusive("Desktop-changing UI tests skipped. Set RUN_DESTRUCTIVE_UI_TESTS=true to run.");
        }
    }

    /// <summary>
    /// Safely kills a process.
    /// </summary>
    public static void SafeKillProcess(Process? process) {
        if (process == null) return;

        try {
            if (!process.HasExited) {
                try {
                    process.CloseMainWindow();
                } catch {
                    // Ignore close errors
                }
                process.WaitForExit(1000);
            }
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
