#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI window and target criteria mapping.
/// </summary>
public class WindowAndTargetCriteriaTests {
    [TestMethod]
    /// <summary>
    /// Ensures window command criteria map selector flags and keep include-empty disabled by default for list scenarios.
    /// </summary>
    public void WindowCreateCriteria_MapsFlagsAndRespectsListDefault() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "window",
            "list",
            "--title", "*Editor*",
            "--process", "DesktopManager.TestApp",
            "--class", "TestWindowClass",
            "--pid", "321",
            "--handle", "0x1234",
            "--active",
            "--include-hidden",
            "--exclude-cloaked",
            "--exclude-owned",
            "--all"
        });

        global::DesktopManager.Cli.WindowSelectionCriteria criteria = global::DesktopManager.Cli.WindowCommands.CreateCriteria(arguments, includeEmptyDefault: false);

        Assert.AreEqual("*Editor*", criteria.TitlePattern);
        Assert.AreEqual("DesktopManager.TestApp", criteria.ProcessNamePattern);
        Assert.AreEqual("TestWindowClass", criteria.ClassNamePattern);
        Assert.AreEqual(321, criteria.ProcessId);
        Assert.AreEqual("0x1234", criteria.Handle);
        Assert.IsTrue(criteria.Active);
        Assert.IsTrue(criteria.IncludeHidden);
        Assert.IsFalse(criteria.IncludeCloaked);
        Assert.IsFalse(criteria.IncludeOwned);
        Assert.IsFalse(criteria.IncludeEmptyTitles);
        Assert.IsTrue(criteria.All);
    }

    [TestMethod]
    /// <summary>
    /// Ensures window command criteria honor explicit include-empty and default wildcard values.
    /// </summary>
    public void WindowCreateCriteria_UsesWildcardDefaultsAndIncludeEmptyFlag() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "window",
            "exists",
            "--include-empty"
        });

        global::DesktopManager.Cli.WindowSelectionCriteria criteria = global::DesktopManager.Cli.WindowCommands.CreateCriteria(arguments, includeEmptyDefault: false);

        Assert.AreEqual("*", criteria.TitlePattern);
        Assert.AreEqual("*", criteria.ProcessNamePattern);
        Assert.AreEqual("*", criteria.ClassNamePattern);
        Assert.IsFalse(criteria.IncludeHidden);
        Assert.IsTrue(criteria.IncludeCloaked);
        Assert.IsTrue(criteria.IncludeOwned);
        Assert.IsTrue(criteria.IncludeEmptyTitles);
        Assert.IsFalse(criteria.All);
    }

    [TestMethod]
    /// <summary>
    /// Ensures target command criteria use include-empty by default for resolve scenarios and honor selector flags.
    /// </summary>
    public void TargetCreateCriteria_MapsFlagsAndUsesResolveDefault() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "target",
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
            "--all"
        });

        global::DesktopManager.Cli.WindowSelectionCriteria criteria = global::DesktopManager.Cli.TargetCommands.CreateCriteria(arguments, includeEmptyDefault: true);

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
    /// Ensures target command criteria fall back to wildcard defaults when no selectors are provided.
    /// </summary>
    public void TargetCreateCriteria_UsesWildcardDefaults() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "target",
            "resolve"
        });

        global::DesktopManager.Cli.WindowSelectionCriteria criteria = global::DesktopManager.Cli.TargetCommands.CreateCriteria(arguments, includeEmptyDefault: true);

        Assert.AreEqual("*", criteria.TitlePattern);
        Assert.AreEqual("*", criteria.ProcessNamePattern);
        Assert.AreEqual("*", criteria.ClassNamePattern);
        Assert.IsFalse(criteria.Active);
        Assert.IsFalse(criteria.IncludeHidden);
        Assert.IsTrue(criteria.IncludeCloaked);
        Assert.IsTrue(criteria.IncludeOwned);
        Assert.IsTrue(criteria.IncludeEmptyTitles);
        Assert.IsFalse(criteria.All);
    }
}
#endif
