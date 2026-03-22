#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI screenshot command criteria mapping.
/// </summary>
public class ScreenshotCommandCriteriaTests {
    [TestMethod]
    /// <summary>
    /// Ensures desktop screenshot options map monitor, device, bounds, and output fields.
    /// </summary>
    public void CreateDesktopOptions_MapsMonitorDeviceBoundsAndOutput() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "screenshot",
            "desktop",
            "--monitor", "2",
            "--device-id", "DISPLAY-2",
            "--device-name", @"\\.\DISPLAY2",
            "--left", "100",
            "--top", "200",
            "--width", "1280",
            "--height", "720",
            "--output", @"C:\Captures\desktop.png"
        });

        global::DesktopManager.Cli.DesktopScreenshotCommandOptions options = global::DesktopManager.Cli.ScreenshotCommands.CreateDesktopOptions(arguments);

        Assert.AreEqual(2, options.MonitorIndex);
        Assert.AreEqual("DISPLAY-2", options.DeviceId);
        Assert.AreEqual(@"\\.\DISPLAY2", options.DeviceName);
        Assert.AreEqual(100, options.Left);
        Assert.AreEqual(200, options.Top);
        Assert.AreEqual(1280, options.Width);
        Assert.AreEqual(720, options.Height);
        Assert.AreEqual(@"C:\Captures\desktop.png", options.OutputPath);
    }

    [TestMethod]
    /// <summary>
    /// Ensures desktop screenshot options default to nulls when no capture constraints are provided.
    /// </summary>
    public void CreateDesktopOptions_UsesNullDefaultsWhenUnset() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "screenshot",
            "desktop"
        });

        global::DesktopManager.Cli.DesktopScreenshotCommandOptions options = global::DesktopManager.Cli.ScreenshotCommands.CreateDesktopOptions(arguments);

        Assert.IsNull(options.MonitorIndex);
        Assert.IsNull(options.DeviceId);
        Assert.IsNull(options.DeviceName);
        Assert.IsNull(options.Left);
        Assert.IsNull(options.Top);
        Assert.IsNull(options.Width);
        Assert.IsNull(options.Height);
        Assert.IsNull(options.OutputPath);
    }

    [TestMethod]
    /// <summary>
    /// Ensures screenshot window criteria map selector flags and always keep the broad screenshot defaults enabled.
    /// </summary>
    public void CreateWindowCriteria_MapsSelectorFlagsAndBroadDefaults() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "screenshot",
            "window",
            "--title", "*Editor*",
            "--process", "DesktopManager.TestApp",
            "--class", "TestWindowClass",
            "--pid", "321",
            "--handle", "0x1234",
            "--active"
        });

        global::DesktopManager.Cli.WindowSelectionCriteria criteria = global::DesktopManager.Cli.ScreenshotCommands.CreateWindowCriteria(arguments);

        Assert.AreEqual("*Editor*", criteria.TitlePattern);
        Assert.AreEqual("DesktopManager.TestApp", criteria.ProcessNamePattern);
        Assert.AreEqual("TestWindowClass", criteria.ClassNamePattern);
        Assert.AreEqual(321, criteria.ProcessId);
        Assert.AreEqual("0x1234", criteria.Handle);
        Assert.IsTrue(criteria.Active);
        Assert.IsTrue(criteria.IncludeHidden);
        Assert.IsTrue(criteria.IncludeCloaked);
        Assert.IsTrue(criteria.IncludeOwned);
        Assert.IsTrue(criteria.IncludeEmptyTitles);
        Assert.IsFalse(criteria.All);
    }

    [TestMethod]
    /// <summary>
    /// Ensures screenshot window criteria fall back to wildcard defaults when no selectors are provided.
    /// </summary>
    public void CreateWindowCriteria_UsesWildcardDefaults() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "screenshot",
            "target",
            "EditorCenter"
        });

        global::DesktopManager.Cli.WindowSelectionCriteria criteria = global::DesktopManager.Cli.ScreenshotCommands.CreateWindowCriteria(arguments);

        Assert.AreEqual("*", criteria.TitlePattern);
        Assert.AreEqual("*", criteria.ProcessNamePattern);
        Assert.AreEqual("*", criteria.ClassNamePattern);
        Assert.IsNull(criteria.ProcessId);
        Assert.IsNull(criteria.Handle);
        Assert.IsFalse(criteria.Active);
        Assert.IsTrue(criteria.IncludeHidden);
        Assert.IsTrue(criteria.IncludeCloaked);
        Assert.IsTrue(criteria.IncludeOwned);
        Assert.IsTrue(criteria.IncludeEmptyTitles);
        Assert.IsFalse(criteria.All);
    }
}
#endif
