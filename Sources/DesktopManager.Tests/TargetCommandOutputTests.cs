#if NET8_0_OR_GREATER
using System.IO;
using System.Collections.Generic;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI target text output.
/// </summary>
public class TargetCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures save output writes the target name and persisted path.
    /// </summary>
    public void WriteSavedTargetResult_WritesTargetNameAndPath() {
        var result = new global::DesktopManager.Cli.WindowTargetResult {
            Name = "EditorCenter",
            Path = @"C:\Targets\EditorCenter.json"
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.TargetCommands.WriteSavedTargetResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "save: EditorCenter");
        StringAssert.Contains(output, "C:\\Targets\\EditorCenter.json");
    }

    [TestMethod]
    /// <summary>
    /// Ensures target get output includes the configured coordinates and optional description.
    /// </summary>
    public void WriteTargetResult_WritesRichTargetSummary() {
        var result = new global::DesktopManager.Cli.WindowTargetResult {
            Name = "EditorCenter",
            Path = @"C:\Targets\EditorCenter.json",
            Target = new global::DesktopManager.Cli.WindowTargetDefinitionResult {
                ClientArea = true,
                X = 20,
                Y = 30,
                XRatio = 0.5d,
                YRatio = 0.25d,
                Width = 640,
                Height = 480,
                WidthRatio = 0.4d,
                HeightRatio = 0.3d,
                Description = "Centered editor target"
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.TargetCommands.WriteTargetResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "EditorCenter");
        StringAssert.Contains(output, "- Path: C:\\Targets\\EditorCenter.json");
        StringAssert.Contains(output, "- ClientArea: Yes");
        StringAssert.Contains(output, "- X: 20");
        StringAssert.Contains(output, "- Y: 30");
        StringAssert.Contains(output, "- XRatio: 0.5");
        StringAssert.Contains(output, "- YRatio: 0.25");
        StringAssert.Contains(output, "- Width: 640");
        StringAssert.Contains(output, "- Height: 480");
        StringAssert.Contains(output, "- WidthRatio: 0.4");
        StringAssert.Contains(output, "- HeightRatio: 0.3");
        StringAssert.Contains(output, "- Description: Centered editor target");
    }

    [TestMethod]
    /// <summary>
    /// Ensures target list output sorts names and shows the empty-state message when no targets are present.
    /// </summary>
    public void WriteTargetNames_SortsNamesAndSupportsEmptyState() {
        using var populatedWriter = new StringWriter();

        int populatedExitCode = global::DesktopManager.Cli.TargetCommands.WriteTargetNames(
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

        int emptyExitCode = global::DesktopManager.Cli.TargetCommands.WriteTargetNames(Array.Empty<string>(), emptyWriter);

        Assert.AreEqual(0, emptyExitCode);
        StringAssert.Contains(emptyWriter.ToString(), "No named targets found.");
    }

    [TestMethod]
    /// <summary>
    /// Ensures resolved target output includes optional area only when the resolved dimensions are available.
    /// </summary>
    public void WriteResolvedTargets_WritesRichAndSparseResolvedTargetSummaries() {
        var results = new List<global::DesktopManager.Cli.ResolvedWindowTargetResult> {
            new() {
                Name = "EditorCenter",
                Target = new global::DesktopManager.Cli.WindowTargetDefinitionResult {
                    ClientArea = true
                },
                Window = new global::DesktopManager.Cli.WindowResult {
                    Title = "DesktopManager Test App",
                    Handle = "0x1234"
                },
                RelativeX = 200,
                RelativeY = 150,
                ScreenX = 210,
                ScreenY = 170,
                ScreenWidth = 320,
                ScreenHeight = 180
            },
            new() {
                Name = "EditorAnchor",
                Target = new global::DesktopManager.Cli.WindowTargetDefinitionResult {
                    ClientArea = false
                },
                Window = new global::DesktopManager.Cli.WindowResult {
                    Title = "DesktopManager Command Surface",
                    Handle = "0x5678"
                },
                RelativeX = 12,
                RelativeY = 18,
                ScreenX = 112,
                ScreenY = 118
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.TargetCommands.WriteResolvedTargets(results, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "EditorCenter: DesktopManager Test App (0x1234)");
        StringAssert.Contains(output, "- Relative: 200,150");
        StringAssert.Contains(output, "- Screen: 210,170");
        StringAssert.Contains(output, "- Area: 320x180");
        StringAssert.Contains(output, "- ClientArea: Yes");
        StringAssert.Contains(output, "EditorAnchor: DesktopManager Command Surface (0x5678)");
        StringAssert.Contains(output, "- Relative: 12,18");
        StringAssert.Contains(output, "- Screen: 112,118");
        StringAssert.Contains(output, "- ClientArea: No");

        int firstAreaIndex = output.IndexOf("- Area: 320x180", System.StringComparison.Ordinal);
        int secondTargetIndex = output.IndexOf("EditorAnchor: DesktopManager Command Surface (0x5678)", System.StringComparison.Ordinal);
        Assert.IsTrue(firstAreaIndex >= 0);
        Assert.IsTrue(secondTargetIndex > firstAreaIndex);
        Assert.AreEqual(-1, output.IndexOf("- Area:", secondTargetIndex, System.StringComparison.Ordinal));
    }
}
#endif
