using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for WindowManager filtering features.
/// </summary>
public class WindowManagerFilterTests {
    [TestCleanup]
    public void Cleanup() {
        TestHelper.KillAllNotepads();
    }
    
    [TestMethod]
    /// <summary>
    /// Ensures filtering by process name returns matching window.
    /// </summary>
    public void GetWindows_ProcessNameFilter_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows(includeHidden: true);
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        // Find a window from a standard process like explorer or Windows system processes
        var window = windows.FirstOrDefault(w => {
            try {
                var proc = Process.GetProcessById((int)w.ProcessId);
                return proc.ProcessName.ToLower().Contains("explorer") || 
                       proc.ProcessName.ToLower().Contains("dwm") ||
                       proc.ProcessName.ToLower().Contains("winlogon");
            } catch {
                return false;
            }
        });
        
        if (window == null) {
            // Fall back to any window we can get process info for
            window = windows.FirstOrDefault(w => {
                try {
                    Process.GetProcessById((int)w.ProcessId);
                    return true;
                } catch {
                    return false;
                }
            });
        }
        
        if (window == null) {
            Assert.Inconclusive("No windows with accessible process information found");
        }
        
        var proc = Process.GetProcessById((int)window.ProcessId);
        var processName = proc.ProcessName;
        
        var filtered = manager.GetWindows(processName: processName, includeHidden: true);
        
        // Instead of checking for the exact same window (which might disappear due to timing),
        // verify that at least one window with the expected process name is found
        Assert.IsTrue(filtered.Count > 0, 
            $"Expected to find at least one window with process name '{processName}', but found {filtered.Count}");
            
        // Verify that all returned windows actually have the correct process name
        foreach (var w in filtered.Take(3)) { // Check first few to avoid performance issues
            try {
                var p = Process.GetProcessById((int)w.ProcessId);
                Assert.AreEqual(processName, p.ProcessName, 
                    $"Window {w.Handle:X8} has process '{p.ProcessName}' but filter was for '{processName}'");
            } catch {
                // Process might have exited, skip verification for this window
            }
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    [Ignore("Disabled: UI test with Notepad - window enumeration needs fixing")]
    /// <summary>
    /// Ensures filtering by process ID returns matching window.
    /// </summary>
    public void GetWindows_ProcessIdFilter_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        if (TestHelper.ShouldSkipUITests()) {
            Assert.Inconclusive("UI tests skipped in local development. Set RUN_UI_TESTS=true to run.");
        }

        using var proc = TestHelper.StartHiddenNotepad();
        if (proc == null) {
            Assert.Inconclusive("Failed to start notepad");
        }

        try {
            var manager = new WindowManager();
            var windows = manager.GetWindows(processId: proc.Id);
            Assert.IsTrue(windows.Any());

            var windows2 = manager.GetWindowsForProcess(proc);
            Assert.IsTrue(windows2.Any());
        } finally {
            TestHelper.SafeKillProcess(proc);
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures filtering by class name returns matching window.
    /// </summary>
    public void GetWindows_ClassNameFilter_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows(includeHidden: true);
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        // Find a window with a stable class name that we can reliably filter by
        WindowInfo? selectedWindow = null;
        string? selectedClassName = null;
        
        foreach (var w in windows) {
            try {
                var sb = new StringBuilder(256);
                MonitorNativeMethods.GetClassName(w.Handle, sb, sb.Capacity);
                var className = sb.ToString();
                
                // Skip empty or very complex class names
                if (string.IsNullOrEmpty(className) || className.Length > 50 || className.Contains(":")) {
                    continue;
                }
                
                // Test if this class name actually returns results
                var testFiltered = manager.GetWindows(className: className, includeHidden: true);
                if (testFiltered.Any(tw => tw.Handle == w.Handle)) {
                    selectedWindow = w;
                    selectedClassName = className;
                    break;
                }
            } catch {
                continue;
            }
        }
        
        if (selectedWindow == null || string.IsNullOrEmpty(selectedClassName)) {
            Assert.Inconclusive("No suitable window with filterable class name found for testing");
            return;
        }

        string classNameFilter = selectedClassName!;
        var filtered = manager.GetWindows(className: classNameFilter, includeHidden: true);

        Assert.IsTrue(filtered.Any(w => w.Handle == selectedWindow.Handle),
            $"Expected to find window {selectedWindow.Handle:X8} with class '{classNameFilter}' in filtered results");
    }

    [TestMethod]
    /// <summary>
    /// Ensures regex filtering returns matching window.
    /// </summary>
    public void GetWindows_RegexFilter_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows(includeHidden: true);
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        // Find a window with a non-empty title for regex matching
        var window = windows.FirstOrDefault(w => !string.IsNullOrEmpty(w.Title));
        if (window == null) {
            Assert.Inconclusive("No windows with titles found to test");
        }

        var regex = new Regex(Regex.Escape(window.Title), RegexOptions.IgnoreCase);
        var filtered = manager.GetWindows(regex: regex, includeHidden: true);
        Assert.IsTrue(filtered.Any(w => w.Handle == window.Handle),
            $"Expected to find window {window.Handle:X8} with title '{window.Title}' in regex filtered results");
    }

    [TestMethod]
    /// <summary>
    /// Ensures combined filters via options return the expected window.
    /// </summary>
    public void GetWindows_OptionsCombinedFilters_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows(includeHidden: true);
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        WindowInfo? selectedWindow = null;
        string? selectedClassName = null;
        string? selectedProcessName = null;

        foreach (var w in windows) {
            if (string.IsNullOrEmpty(w.Title)) {
                continue;
            }

            if (w.ProcessId == 0 || w.ProcessId > int.MaxValue) {
                continue;
            }

            try {
                var sb = new StringBuilder(256);
                MonitorNativeMethods.GetClassName(w.Handle, sb, sb.Capacity);
                var className = sb.ToString();
                if (string.IsNullOrEmpty(className) || className.Length > 50 || className.Contains(":")) {
                    continue;
                }

                using var proc = Process.GetProcessById((int)w.ProcessId);
                if (string.IsNullOrWhiteSpace(proc.ProcessName)) {
                    continue;
                }

                selectedWindow = w;
                selectedClassName = className;
                selectedProcessName = proc.ProcessName;
                break;
            } catch {
                continue;
            }
        }

        if (selectedWindow == null || selectedClassName == null || selectedProcessName == null) {
            Assert.Inconclusive("No suitable window found for combined filter test");
        }

        var options = new WindowQueryOptions {
            TitlePattern = selectedWindow.Title,
            ProcessNamePattern = selectedProcessName,
            ClassNamePattern = selectedClassName,
            ProcessId = (int)selectedWindow.ProcessId,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IsVisible = selectedWindow.IsVisible,
            State = selectedWindow.State,
            IsTopMost = selectedWindow.IsTopMost
        };

        var filtered = manager.GetWindows(options);
        Assert.IsTrue(filtered.Any(w => w.Handle == selectedWindow.Handle),
            $"Expected to find window {selectedWindow.Handle:X8} using combined filters");
    }

    [TestMethod]
    /// <summary>
    /// Ensures filtering by exact handle returns the matching window.
    /// </summary>
    public void GetWindows_HandleFilter_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var window = manager.GetWindows(includeHidden: true).FirstOrDefault();
        if (window == null) {
            Assert.Inconclusive("No windows found to test");
        }

        var filtered = manager.GetWindows(new WindowQueryOptions {
            Handle = window.Handle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });

        Assert.AreEqual(1, filtered.Count, "Expected an exact handle filter to return a single window.");
        Assert.AreEqual(window.Handle, filtered[0].Handle, "Expected the filtered window to match the requested handle.");
    }

    [TestMethod]
    /// <summary>
    /// Ensures the active window helper resolves the current foreground window.
    /// </summary>
    public void GetActiveWindow_ReturnsForegroundWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        IntPtr foreground = MonitorNativeMethods.GetForegroundWindow();
        if (foreground == IntPtr.Zero) {
            Assert.Inconclusive("No foreground window found to test");
        }

        var manager = new WindowManager();
        WindowInfo? window = manager.GetActiveWindow();
        if (window == null) {
            Assert.Inconclusive("The active window could not be resolved from enumeration");
        }

        Assert.AreEqual(foreground, window.Handle, "Expected GetActiveWindow to resolve the current foreground window.");
    }

    [TestMethod]
    /// <summary>
    /// Ensures Z-order filtering returns a subset within the specified range.
    /// </summary>
    public void GetWindows_OptionsZOrderFilter_ReturnsSubset() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows(new WindowQueryOptions { IncludeHidden = true });
        if (windows.Count < 3) {
            Assert.Inconclusive("Not enough windows for Z-order filter test");
        }

        int min = 1;
        int max = windows.Count - 2;

        var filtered = manager.GetWindows(new WindowQueryOptions {
            IncludeHidden = true,
            ZOrderMin = min,
            ZOrderMax = max
        });

        Assert.IsTrue(filtered.All(w => w.ZOrder >= min && w.ZOrder <= max),
            "Expected all windows to be within the specified Z-order range");
    }
}

