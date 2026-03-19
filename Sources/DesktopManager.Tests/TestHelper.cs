using System;
using System.Collections.Concurrent;
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
    private static readonly ConcurrentDictionary<int, byte> StartedProcessIds = new();

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
            if (ShouldSkipUITests()) {
                return false;
            }

            var startInfo = new ProcessStartInfo("notepad.exe") {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized
            };

            process = Process.Start(startInfo);
            if (process == null) {
                return false;
            }
            TrackProcess(process);

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

        // Run UI tests only when explicitly enabled
        if (Environment.GetEnvironmentVariable("RUN_UI_TESTS") == "true" ||
            Environment.GetEnvironmentVariable("DESKTOPMANAGER_RUN_UI_TESTS") == "true") {
            return false;
        }

        // Skip by default
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
            Assert.Inconclusive("UI tests skipped by default. Set RUN_UI_TESTS=true (or DESKTOPMANAGER_RUN_UI_TESTS=true) to run.");
        }
    }

    /// <summary>
    /// Skips desktop-changing tests unless explicitly enabled.
    /// </summary>
    public static void RequireDesktopChanges() {
        RequireInteractive();
        if (ShouldSkipDesktopChangeTests()) {
            Assert.Inconclusive("Desktop-changing UI tests skipped. Set RUN_DESTRUCTIVE_UI_TESTS=true (or DESKTOPMANAGER_RUN_DESTRUCTIVE_UI_TESTS=true) to run.");
        }
    }

    /// <summary>
    /// Skips experimental desktop-changing tests unless explicitly enabled.
    /// </summary>
    public static void RequireExperimentalDesktopChanges() {
        RequireDesktopChanges();
        if (Environment.GetEnvironmentVariable("RUN_EXPERIMENTAL_UI_TESTS") == "true" ||
            Environment.GetEnvironmentVariable("DESKTOPMANAGER_RUN_EXPERIMENTAL_UI_TESTS") == "true") {
            return;
        }

        Assert.Inconclusive("Experimental desktop-changing UI tests skipped. Set RUN_EXPERIMENTAL_UI_TESTS=true (or DESKTOPMANAGER_RUN_EXPERIMENTAL_UI_TESTS=true) to run.");
    }

    /// <summary>
    /// Safely kills a process.
    /// </summary>
    public static void SafeKillProcess(Process? process) {
        if (process == null) {
            return;
        }

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
            UntrackProcess(process);
            process.Dispose();
        }
    }

    /// <summary>
    /// Kills all Notepad processes (cleanup).
    /// </summary>
    public static void KillAllNotepads() {
        try {
            foreach (var processId in StartedProcessIds.Keys) {
                try {
                    using var process = Process.GetProcessById(processId);
                    SafeKillProcess(process);
                } catch {
                    // Ignore missing or already exited processes
                }
            }
        } catch {
            // Ignore errors during cleanup
        }
    }

    private static void TrackProcess(Process process) {
        try {
            StartedProcessIds.TryAdd(process.Id, 0);
        } catch {
            // Ignore tracking failures
        }
    }

    private static void UntrackProcess(Process process) {
        try {
            StartedProcessIds.TryRemove(process.Id, out _);
        } catch {
            // Ignore tracking failures
        }
    }
}
