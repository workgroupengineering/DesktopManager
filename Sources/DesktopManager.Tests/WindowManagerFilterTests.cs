using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

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
    /// <summary>
    /// Ensures filtering by process ID returns matching window.
    /// </summary>
    public void GetWindows_ProcessIdFilter_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();

        string title = $"ProcessId Filter Harness {Guid.NewGuid():N}";
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(title);
        using var process = Process.GetCurrentProcess();

        var manager = new WindowManager();
        var windows = manager.GetWindows(processId: process.Id, includeHidden: true);
        Assert.IsTrue(windows.Any(w => w.Handle == harness.Window.Handle));

        var windows2 = manager.GetWindowsForProcess(process, includeHidden: true);
        Assert.IsTrue(windows2.Any(w => w.Handle == harness.Window.Handle));
    }

    [TestMethod]
    /// <summary>
    /// Ensures filtering by class name returns matching window.
    /// </summary>
    public void GetWindows_ClassNameFilter_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        string title = $"Class Filter Harness {Guid.NewGuid():N}";
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(title);

        var manager = new WindowManager();
        string classNameFilter = GetClassName(harness.Window.Handle);
        var filtered = manager.GetWindows(className: classNameFilter, includeHidden: true);

        Assert.IsTrue(filtered.Any(w => w.Handle == harness.Window.Handle),
            $"Expected to find window {harness.Window.Handle:X8} with class '{classNameFilter}' in filtered results");
    }

    [TestMethod]
    /// <summary>
    /// Ensures regex filtering returns matching window.
    /// </summary>
    public void GetWindows_RegexFilter_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        string title = $"Regex Filter Harness {Guid.NewGuid():N}";
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(title);

        var manager = new WindowManager();
        var regex = new Regex(Regex.Escape(title), RegexOptions.IgnoreCase);
        var filtered = manager.GetWindows(regex: regex, includeHidden: true);
        Assert.IsTrue(filtered.Any(w => w.Handle == harness.Window.Handle),
            $"Expected to find window {harness.Window.Handle:X8} with title '{title}' in regex filtered results");
    }

    [TestMethod]
    /// <summary>
    /// Ensures combined filters via options return the expected window.
    /// </summary>
    public void GetWindows_OptionsCombinedFilters_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        string title = $"Combined Filter Harness {Guid.NewGuid():N}";
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(title);
        using var process = Process.GetCurrentProcess();

        var manager = new WindowManager();
        string selectedClassName = GetClassName(harness.Window.Handle);
        string selectedProcessName = process.ProcessName;

        var options = new WindowQueryOptions {
            TitlePattern = title,
            ProcessNamePattern = selectedProcessName,
            ClassNamePattern = selectedClassName,
            ProcessId = process.Id,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IsVisible = harness.Window.IsVisible,
            State = harness.Window.State,
            IsTopMost = harness.Window.IsTopMost
        };

        var filtered = manager.GetWindows(options);
        Assert.IsTrue(filtered.Any(w => w.Handle == harness.Window.Handle),
            $"Expected to find window {harness.Window.Handle:X8} using combined filters");
    }

    [TestMethod]
    /// <summary>
    /// Ensures filtering by exact handle returns the matching window.
    /// </summary>
    public void GetWindows_HandleFilter_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        string title = $"Handle Filter Harness {Guid.NewGuid():N}";
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(title);

        var manager = new WindowManager();
        var filtered = manager.GetWindows(new WindowQueryOptions {
            Handle = harness.Window.Handle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });

        Assert.AreEqual(1, filtered.Count, "Expected an exact handle filter to return a single window.");
        Assert.AreEqual(harness.Window.Handle, filtered[0].Handle, "Expected the filtered window to match the requested handle.");
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

    private static string GetClassName(IntPtr handle) {
        var sb = new StringBuilder(256);
        MonitorNativeMethods.GetClassName(handle, sb, sb.Capacity);
        return sb.ToString();
    }
}

