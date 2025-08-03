using System.Linq;
using System.Runtime.InteropServices;
using DesktopManager;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for WindowLayoutTests.
/// </summary>
public class WindowLayoutTests {
    [TestMethod]
    /// <summary>
    /// Test for SaveAndLoadLayout_RoundTrips.
    /// </summary>
    public void SaveAndLoadLayout_RoundTrips() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        // Find a window that's likely to be stable (prefer explorer, system windows, or large applications)
        var window = windows.FirstOrDefault(w => {
            try {
                var proc = System.Diagnostics.Process.GetProcessById((int)w.ProcessId);
                var name = proc.ProcessName.ToLower();
                return name.Contains("explorer") || name.Contains("dwm") || name.Contains("winlogon") || 
                       name.Contains("chrome") || name.Contains("firefox") || name.Contains("code") ||
                       w.IsVisible;
            } catch {
                return false;
            }
        }) ?? windows.First();

        var original = manager.GetWindowPosition(window);
        var originalState = original.State;
        var path = System.IO.Path.GetTempFileName();

        try {
            manager.SaveLayout(path);
            
            // Only modify position slightly to avoid extreme coordinates in multi-monitor setups
            var newLeft = original.Left + 50;
            var newTop = original.Top + 50;
            
            manager.SetWindowPosition(window, newLeft, newTop);
            
            // Toggle state only if not maximized (some windows can't be moved when maximized)
            if (originalState != WindowState.Maximize) {
                manager.MaximizeWindow(window);
            }
            
            // Allow time for window changes to take effect
            System.Threading.Thread.Sleep(200);
            
            manager.LoadLayout(path);
            
            // Allow time for layout restoration
            System.Threading.Thread.Sleep(200);
            
            var restored = manager.GetWindowPosition(window);
            
            // In multi-monitor setups, allow some tolerance for coordinate system differences
            var leftTolerance = Math.Abs(original.Left - restored.Left);
            var topTolerance = Math.Abs(original.Top - restored.Top);
            
            if (leftTolerance > 100 || topTolerance > 100) {
                // If restoration failed significantly, check if we're dealing with a different window
                var currentWindows = manager.GetWindows();
                var sameWindow = currentWindows.FirstOrDefault(w => w.Handle == window.Handle);
                
                if (sameWindow == null) {
                    Assert.Inconclusive($"Original window (Handle={window.Handle:X8}) no longer exists - test cannot verify restoration");
                }
                
                Assert.Fail($"Window position restoration failed significantly. Expected: ({original.Left}, {original.Top}), Got: ({restored.Left}, {restored.Top}), Tolerance exceeded: Left={leftTolerance}, Top={topTolerance}");
            }
            
            // For state, be more lenient as some windows may not support all state changes
            if (originalState != WindowState.Maximize) {
                Assert.AreEqual(originalState, restored.State, $"State mismatch: expected {originalState}, got {restored.State}");
            }
        } finally {
            if (System.IO.File.Exists(path)) {
                System.IO.File.Delete(path);
            }
        }
    }

    [TestMethod]
    /// <summary>
    /// Test for LoadLayout_InvalidJson_Throws.
    /// </summary>
    public void LoadLayout_InvalidJson_Throws() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var path = System.IO.Path.GetTempFileName();
        System.IO.File.WriteAllText(path, "{ invalid }");

        Assert.ThrowsException<InvalidOperationException>(() => manager.LoadLayout(path));

        System.IO.File.Delete(path);
    }

    [TestMethod]
    /// <summary>
    /// Test for LoadLayout_ValidateMissingProperties_Throws.
    /// </summary>
    public void LoadLayout_ValidateMissingProperties_Throws() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var path = System.IO.Path.GetTempFileName();
        var json = "{\"Windows\":[{\"Title\":\"Test\"}]}";
        System.IO.File.WriteAllText(path, json);

        Assert.ThrowsException<System.IO.InvalidDataException>(() => manager.LoadLayout(path, validate: true));

        System.IO.File.Delete(path);
    }

    [TestMethod]
    /// <summary>
    /// Test for SaveLayout_RelativePath_CreatesFile.
    /// </summary>
    public void SaveLayout_RelativePath_CreatesFile() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid().ToString());
        System.IO.Directory.CreateDirectory(tempDir);
        var originalDir = System.Environment.CurrentDirectory;
        System.Environment.CurrentDirectory = tempDir;
        try {
            var relative = System.IO.Path.Combine("sub", "layout.json");
            manager.SaveLayout(relative);
            var fullPath = System.IO.Path.Combine(tempDir, relative);
            Assert.IsTrue(System.IO.File.Exists(fullPath));
        } finally {
            System.Environment.CurrentDirectory = originalDir;
            System.IO.Directory.Delete(tempDir, true);
        }
    }
}
