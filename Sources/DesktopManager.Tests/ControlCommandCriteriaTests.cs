#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI control command criteria mapping.
/// </summary>
public class ControlCommandCriteriaTests {
    [TestMethod]
    /// <summary>
    /// Ensures control command window criteria map window-scoped flags and keep the broad control defaults.
    /// </summary>
    public void CreateWindowCriteria_MapsWindowScopedFlags() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "control",
            "list",
            "--window-title", "*Editor*",
            "--window-process", "DesktopManager.TestApp",
            "--window-class", "TestWindowClass",
            "--window-pid", "321",
            "--window-handle", "0x1234",
            "--window-active"
        });

        global::DesktopManager.Cli.WindowSelectionCriteria criteria = global::DesktopManager.Cli.ControlCommands.CreateWindowCriteria(arguments);

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
    }

    [TestMethod]
    /// <summary>
    /// Ensures control command control criteria map selector and capability flags.
    /// </summary>
    public void CreateControlCriteria_MapsSelectorAndCapabilityFlags() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "control",
            "list",
            "--class", "Edit",
            "--text-pattern", "*Desktop*",
            "--value-pattern", "DesktopManager",
            "--id", "101",
            "--handle", "0x2222",
            "--automation-id", "EditorTextBox",
            "--control-type", "Edit",
            "--framework-id", "WinForms",
            "--enabled",
            "--focusable",
            "--background-click",
            "--background-text",
            "--background-keys",
            "--foreground-fallback",
            "--ensure-foreground",
            "--allow-foreground-input",
            "--uia",
            "--include-uia",
            "--all"
        });

        global::DesktopManager.Cli.ControlSelectionCriteria criteria = global::DesktopManager.Cli.ControlCommands.CreateControlCriteria(arguments);

        Assert.AreEqual("Edit", criteria.ClassNamePattern);
        Assert.AreEqual("*Desktop*", criteria.TextPattern);
        Assert.AreEqual("DesktopManager", criteria.ValuePattern);
        Assert.AreEqual(101, criteria.Id);
        Assert.AreEqual("0x2222", criteria.Handle);
        Assert.AreEqual("EditorTextBox", criteria.AutomationIdPattern);
        Assert.AreEqual("Edit", criteria.ControlTypePattern);
        Assert.AreEqual("WinForms", criteria.FrameworkIdPattern);
        Assert.AreEqual(true, criteria.IsEnabled);
        Assert.AreEqual(true, criteria.IsKeyboardFocusable);
        Assert.AreEqual(true, criteria.SupportsBackgroundClick);
        Assert.AreEqual(true, criteria.SupportsBackgroundText);
        Assert.AreEqual(true, criteria.SupportsBackgroundKeys);
        Assert.AreEqual(true, criteria.SupportsForegroundInputFallback);
        Assert.IsTrue(criteria.EnsureForegroundWindow);
        Assert.IsTrue(criteria.AllowForegroundInputFallback);
        Assert.IsTrue(criteria.UiAutomation);
        Assert.IsTrue(criteria.IncludeUiAutomation);
        Assert.IsTrue(criteria.All);
    }

    [TestMethod]
    /// <summary>
    /// Ensures control-target window criteria respect include-empty defaults and window-selection flags.
    /// </summary>
    public void ControlTargetCreateWindowCriteria_MapsIncludeEmptyAndWindowFlags() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "control-target",
            "resolve",
            "--title", "*Command*",
            "--process", "DesktopManager.TestApp",
            "--class", "Chrome_WidgetWin_1",
            "--pid", "654",
            "--handle", "0x5678",
            "--active",
            "--include-hidden",
            "--exclude-cloaked",
            "--exclude-owned",
            "--all-windows"
        });

        global::DesktopManager.Cli.WindowSelectionCriteria criteria = global::DesktopManager.Cli.ControlTargetCommands.CreateWindowCriteria(arguments, includeEmptyDefault: true);

        Assert.AreEqual("*Command*", criteria.TitlePattern);
        Assert.AreEqual("DesktopManager.TestApp", criteria.ProcessNamePattern);
        Assert.AreEqual("Chrome_WidgetWin_1", criteria.ClassNamePattern);
        Assert.AreEqual(654, criteria.ProcessId);
        Assert.AreEqual("0x5678", criteria.Handle);
        Assert.IsTrue(criteria.Active);
        Assert.IsTrue(criteria.IncludeHidden);
        Assert.IsFalse(criteria.IncludeCloaked);
        Assert.IsFalse(criteria.IncludeOwned);
        Assert.IsTrue(criteria.IncludeEmptyTitles);
        Assert.IsTrue(criteria.All);
    }

    [TestMethod]
    /// <summary>
    /// Ensures control-target criteria map disabled and not-focusable flags plus saved capability hints.
    /// </summary>
    public void ControlTargetCreateControlCriteria_MapsSelectorHints() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "control-target",
            "save",
            "--class", "Button",
            "--text-pattern", "Save",
            "--value-pattern", "",
            "--id", "7",
            "--handle", "0x7777",
            "--automation-id", "SaveButton",
            "--control-type", "Button",
            "--framework-id", "WPF",
            "--disabled",
            "--not-focusable",
            "--background-click",
            "--foreground-fallback",
            "--ensure-foreground",
            "--uia",
            "--include-uia",
            "--all"
        });

        global::DesktopManager.Cli.ControlSelectionCriteria criteria = global::DesktopManager.Cli.ControlTargetCommands.CreateControlCriteria(arguments);

        Assert.AreEqual("Button", criteria.ClassNamePattern);
        Assert.AreEqual("Save", criteria.TextPattern);
        Assert.AreEqual(string.Empty, criteria.ValuePattern);
        Assert.AreEqual(7, criteria.Id);
        Assert.AreEqual("0x7777", criteria.Handle);
        Assert.AreEqual("SaveButton", criteria.AutomationIdPattern);
        Assert.AreEqual("Button", criteria.ControlTypePattern);
        Assert.AreEqual("WPF", criteria.FrameworkIdPattern);
        Assert.AreEqual(false, criteria.IsEnabled);
        Assert.AreEqual(false, criteria.IsKeyboardFocusable);
        Assert.AreEqual(true, criteria.SupportsBackgroundClick);
        Assert.AreEqual(true, criteria.SupportsForegroundInputFallback);
        Assert.IsTrue(criteria.EnsureForegroundWindow);
        Assert.IsTrue(criteria.UiAutomation);
        Assert.IsTrue(criteria.IncludeUiAutomation);
        Assert.IsTrue(criteria.All);
    }
}
#endif
