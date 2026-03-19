using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for shared desktop automation assertions.
/// </summary>
public class DesktopAutomationAssertionTests {
    [TestMethod]
    /// <summary>
    /// Ensures a query by the active window handle reports that the window exists.
    /// </summary>
    public void WindowExists_ActiveWindowHandle_ReturnsTrue() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var automation = new DesktopAutomationService();
        WindowInfo? activeWindow = automation.GetActiveWindow(includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
        if (activeWindow == null) {
            Assert.Inconclusive("No active window could be resolved.");
        }

        bool result = automation.WindowExists(new WindowQueryOptions {
            Handle = activeWindow.Handle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });

        Assert.IsTrue(result);
    }

    [TestMethod]
    /// <summary>
    /// Ensures the active window matches a selector targeting its own handle.
    /// </summary>
    public void ActiveWindowMatches_ActiveWindowHandle_ReturnsTrue() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var automation = new DesktopAutomationService();
        WindowInfo? activeWindow = automation.GetActiveWindow(includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
        if (activeWindow == null) {
            Assert.Inconclusive("No active window could be resolved.");
        }

        bool result = automation.ActiveWindowMatches(new WindowQueryOptions {
            Handle = activeWindow.Handle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });

        Assert.IsTrue(result);
    }
}
