#if NET8_0_OR_GREATER
using System.IO;
using System.Collections.Generic;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI control-target text output.
/// </summary>
public class ControlTargetCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures save output writes the control target name and persisted path.
    /// </summary>
    public void WriteSavedTargetResult_WritesTargetNameAndPath() {
        var result = new global::DesktopManager.Cli.ControlTargetResult {
            Name = "CommandBar",
            Path = @"C:\Targets\CommandBar.control.json"
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.ControlTargetCommands.WriteSavedTargetResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "save: CommandBar");
        StringAssert.Contains(output, "C:\\Targets\\CommandBar.control.json");
    }

    [TestMethod]
    /// <summary>
    /// Ensures get output includes configured control criteria and optional description.
    /// </summary>
    public void WriteTargetResult_WritesRichControlTargetSummary() {
        var result = new global::DesktopManager.Cli.ControlTargetResult {
            Name = "CommandBar",
            Path = @"C:\Targets\CommandBar.control.json",
            Target = new global::DesktopManager.Cli.ControlTargetDefinitionResult {
                ClassNamePattern = "HwndWrapper*",
                TextPattern = "*Desktop*",
                ValuePattern = "DesktopManager",
                ControlTypePattern = "Edit",
                AutomationIdPattern = "CommandBarTextBox",
                SupportsBackgroundClick = false,
                SupportsBackgroundText = false,
                SupportsBackgroundKeys = false,
                SupportsForegroundInputFallback = true,
                Description = "Command bar editor"
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.ControlTargetCommands.WriteTargetResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "CommandBar");
        StringAssert.Contains(output, "- Path: C:\\Targets\\CommandBar.control.json");
        StringAssert.Contains(output, "- Class: HwndWrapper*");
        StringAssert.Contains(output, "- Text: *Desktop*");
        StringAssert.Contains(output, "- Value: DesktopManager");
        StringAssert.Contains(output, "- ControlType: Edit");
        StringAssert.Contains(output, "- AutomationId: CommandBarTextBox");
        StringAssert.Contains(output, "- BackgroundClick: False");
        StringAssert.Contains(output, "- BackgroundText: False");
        StringAssert.Contains(output, "- BackgroundKeys: False");
        StringAssert.Contains(output, "- ForegroundFallback: True");
        StringAssert.Contains(output, "- Description: Command bar editor");
    }

    [TestMethod]
    /// <summary>
    /// Ensures control-target list output sorts names and shows the empty-state message when no targets are present.
    /// </summary>
    public void WriteTargetNames_SortsNamesAndSupportsEmptyState() {
        using var populatedWriter = new StringWriter();

        int populatedExitCode = global::DesktopManager.Cli.ControlTargetCommands.WriteTargetNames(
            new List<string> { "zeta", "Alpha", "beta" },
            populatedWriter);

        string populatedOutput = populatedWriter.ToString();
        int alphaIndex = populatedOutput.IndexOf("Alpha", System.StringComparison.Ordinal);
        int betaIndex = populatedOutput.IndexOf("beta", System.StringComparison.Ordinal);
        int zetaIndex = populatedOutput.IndexOf("zeta", System.StringComparison.Ordinal);

        Assert.AreEqual(0, populatedExitCode);
        Assert.IsTrue(alphaIndex >= 0);
        Assert.IsTrue(betaIndex > alphaIndex);
        Assert.IsTrue(zetaIndex > betaIndex);

        using var emptyWriter = new StringWriter();

        int emptyExitCode = global::DesktopManager.Cli.ControlTargetCommands.WriteTargetNames(Array.Empty<string>(), emptyWriter);

        Assert.AreEqual(0, emptyExitCode);
        StringAssert.Contains(emptyWriter.ToString(), "No named control targets found.");
    }

    [TestMethod]
    /// <summary>
    /// Ensures resolved control target output includes window, control, and capability details.
    /// </summary>
    public void WriteResolvedTargets_WritesResolvedControlTargetSummary() {
        var results = new List<global::DesktopManager.Cli.ResolvedControlTargetResult> {
            new() {
                Name = "CommandBar",
                Window = new global::DesktopManager.Cli.WindowResult {
                    Title = "DesktopManager Command Surface",
                    Handle = "0x2222"
                },
                Control = new global::DesktopManager.Cli.ControlResult {
                    ControlType = "Edit",
                    Text = "DesktopManager",
                    Handle = "0x0",
                    SupportsBackgroundText = false,
                    SupportsForegroundInputFallback = true
                }
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.ControlTargetCommands.WriteResolvedTargets(results, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "CommandBar: DesktopManager Command Surface (0x2222)");
        StringAssert.Contains(output, "- Control: Edit DesktopManager");
        StringAssert.Contains(output, "- Handle: 0x0");
        StringAssert.Contains(output, "- BackgroundText: False");
        StringAssert.Contains(output, "- ForegroundFallback: True");
    }
}
#endif
