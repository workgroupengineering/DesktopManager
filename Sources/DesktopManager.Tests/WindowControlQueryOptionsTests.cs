namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for control query option behavior.
/// </summary>
public class WindowControlQueryOptionsTests {
    [TestMethod]
    /// <summary>
    /// Ensures default control queries do not require UI Automation.
    /// </summary>
    public void RequiresUiAutomation_DefaultOptions_ReturnsFalse() {
        var options = new WindowControlQueryOptions();

        Assert.IsFalse(options.RequiresUiAutomation());
    }

    [TestMethod]
    /// <summary>
    /// Ensures automation id filters enable UI Automation.
    /// </summary>
    public void RequiresUiAutomation_AutomationIdFilter_ReturnsTrue() {
        var options = new WindowControlQueryOptions {
            AutomationIdPattern = "SearchBox"
        };

        Assert.IsTrue(options.RequiresUiAutomation());
    }

    [TestMethod]
    /// <summary>
    /// Ensures explicit UI Automation selection enables UI Automation.
    /// </summary>
    public void RequiresUiAutomation_ExplicitSwitch_ReturnsTrue() {
        var options = new WindowControlQueryOptions {
            UseUiAutomation = true
        };

        Assert.IsTrue(options.RequiresUiAutomation());
    }
}
