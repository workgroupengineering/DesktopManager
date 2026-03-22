#if NET8_0_OR_GREATER
using System.IO;
using System.Collections.Generic;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI layout text output.
/// </summary>
public class LayoutCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures path output writes the action and persisted layout path.
    /// </summary>
    public void WritePathResult_WritesActionAndPath() {
        var payload = new global::DesktopManager.Cli.NamedStateResult {
            Action = "save-layout",
            Name = "Coding",
            Path = @"C:\Layouts\Coding.layout.json"
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.LayoutCommands.WritePathResult(payload, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "save-layout: Coding");
        StringAssert.Contains(output, "C:\\Layouts\\Coding.layout.json");
    }

    [TestMethod]
    /// <summary>
    /// Ensures list output sorts names and shows the empty-state message when no layouts are present.
    /// </summary>
    public void WriteLayoutNames_SortsNamesAndSupportsEmptyState() {
        using var populatedWriter = new StringWriter();

        int populatedExitCode = global::DesktopManager.Cli.LayoutCommands.WriteLayoutNames(
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

        int emptyExitCode = global::DesktopManager.Cli.LayoutCommands.WriteLayoutNames(Array.Empty<string>(), emptyWriter);

        Assert.AreEqual(0, emptyExitCode);
        StringAssert.Contains(emptyWriter.ToString(), "No named layouts found.");
    }

    [TestMethod]
    /// <summary>
    /// Ensures layout assertion output includes artifacts, missing windows, mismatches, and warnings.
    /// </summary>
    public void WriteLayoutAssertionResult_WritesRichAssertionSummary() {
        var payload = new global::DesktopManager.Cli.WindowLayoutAssertionResult {
            Matched = false,
            ExpectedCount = 3,
            MatchedCount = 1,
            MissingCount = 1,
            MismatchCount = 1,
            Path = @"C:\Layouts\Coding.layout.json",
            BeforeScreenshots = new List<global::DesktopManager.Cli.ScreenshotResult> {
                new() { Path = @"C:\Artifacts\before.png" }
            },
            AfterScreenshots = new List<global::DesktopManager.Cli.ScreenshotResult> {
                new() { Path = @"C:\Artifacts\after.png" }
            },
            MissingWindows = new List<global::DesktopManager.Cli.SavedWindowLayoutEntryResult> {
                new() { Title = "Missing App", ProcessId = 42 }
            },
            MismatchedWindows = new List<global::DesktopManager.Cli.WindowLayoutMismatchResult> {
                new() {
                    Expected = new global::DesktopManager.Cli.SavedWindowLayoutEntryResult {
                        Title = "Editor",
                        ProcessId = 77
                    },
                    LeftDelta = 10,
                    TopDelta = -5,
                    WidthDelta = 20,
                    HeightDelta = 15,
                    StateMatched = false
                }
            },
            ArtifactWarnings = new List<string> { "after screenshot truncated" }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.LayoutCommands.WriteLayoutAssertionResult(payload, writer);
        string output = writer.ToString();

        Assert.AreEqual(2, exitCode);
        StringAssert.Contains(output, "assert-layout: matched=False expected=3 matched-count=1 missing=1 mismatched=1");
        StringAssert.Contains(output, "C:\\Layouts\\Coding.layout.json");
        StringAssert.Contains(output, "artifacts: before=1 after=1");
        StringAssert.Contains(output, "missing: Missing App [PID 42]");
        StringAssert.Contains(output, "mismatch: Editor [PID 77] left=10 top=-5 width=20 height=15 state=False");
        StringAssert.Contains(output, "warning: after screenshot truncated");
    }

    [TestMethod]
    /// <summary>
    /// Ensures matching layout assertion output omits optional artifact and warning lines.
    /// </summary>
    public void WriteLayoutAssertionResult_OmitsOptionalSectionsWhenUnset() {
        var payload = new global::DesktopManager.Cli.WindowLayoutAssertionResult {
            Matched = true,
            ExpectedCount = 1,
            MatchedCount = 1,
            MissingCount = 0,
            MismatchCount = 0,
            Path = @"C:\Layouts\Coding.layout.json"
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.LayoutCommands.WriteLayoutAssertionResult(payload, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "assert-layout: matched=True expected=1 matched-count=1 missing=0 mismatched=0");
        Assert.IsFalse(output.Contains("artifacts:", System.StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("missing:", System.StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("mismatch:", System.StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("warning:", System.StringComparison.Ordinal));
    }
}
#endif
