using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

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
        TestHelper.RequireForegroundWindowUiTests();

        var automation = new DesktopAutomationService();
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager Assertion Harness Exists");
        new WindowManager().ActivateWindow(harness.Window);
        Application.DoEvents();
        Thread.Sleep(100);

        WindowInfo? activeWindow = automation.GetActiveWindow(includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
        if (activeWindow == null) {
            Assert.Inconclusive("No active window could be resolved.");
        }

        Assert.AreEqual(harness.Window.Handle, activeWindow.Handle, "Expected the owned harness window to be active for the assertion test.");

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
        TestHelper.RequireForegroundWindowUiTests();

        var automation = new DesktopAutomationService();
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager Assertion Harness Active");
        new WindowManager().ActivateWindow(harness.Window);
        Application.DoEvents();
        Thread.Sleep(100);

        WindowInfo? activeWindow = automation.GetActiveWindow(includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
        if (activeWindow == null) {
            Assert.Inconclusive("No active window could be resolved.");
        }

        Assert.AreEqual(harness.Window.Handle, activeWindow.Handle, "Expected the owned harness window to be active for the assertion test.");

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
