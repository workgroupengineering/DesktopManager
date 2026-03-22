#if NET8_0_OR_GREATER
using System.IO;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI workflow text output.
/// </summary>
public class WorkflowCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures workflow text output prefers focused window details and includes minimized/artifact metadata.
    /// </summary>
    public void WriteWorkflowResult_WritesFocusedWindowAndArtifacts() {
        var result = new global::DesktopManager.Cli.WorkflowResult {
            Action = "prepare-for-coding",
            Success = true,
            ElapsedMilliseconds = 250,
            LayoutName = "Pairing",
            LayoutApplied = true,
            FocusedWindow = new global::DesktopManager.Cli.WindowResult {
                Title = "Editor",
                ProcessId = 100
            },
            ResolvedWindow = new global::DesktopManager.Cli.WindowResult {
                Title = "Fallback",
                ProcessId = 101
            },
            MinimizedWindows = new[] {
                new global::DesktopManager.Cli.WindowResult {
                    Title = "Chat",
                    ProcessId = 200
                },
                new global::DesktopManager.Cli.WindowResult {
                    Title = "Mail",
                    ProcessId = 201
                }
            },
            BeforeScreenshots = new[] {
                new global::DesktopManager.Cli.ScreenshotResult()
            },
            AfterScreenshots = new[] {
                new global::DesktopManager.Cli.ScreenshotResult(),
                new global::DesktopManager.Cli.ScreenshotResult()
            },
            ArtifactWarnings = new[] { "Capture path normalized." },
            Notes = new[] { "Prepared coding layout." }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.WorkflowCommands.WriteWorkflowResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "prepare-for-coding: success=True elapsed-ms=250");
        StringAssert.Contains(output, "layout: applied Pairing");
        StringAssert.Contains(output, "focused: Editor [PID 100]");
        Assert.IsFalse(output.Contains("resolved: Fallback"));
        StringAssert.Contains(output, "minimized: 2 window(s)");
        StringAssert.Contains(output, "- Chat [PID 200]");
        StringAssert.Contains(output, "- Mail [PID 201]");
        StringAssert.Contains(output, "artifacts: before=1 after=2");
        StringAssert.Contains(output, "warning: Capture path normalized.");
        StringAssert.Contains(output, "note: Prepared coding layout.");
    }

    [TestMethod]
    /// <summary>
    /// Ensures workflow text output falls back to resolved window details and omits empty optional sections.
    /// </summary>
    public void WriteWorkflowResult_WritesResolvedWindowFallbackAndOmitsEmptySections() {
        var result = new global::DesktopManager.Cli.WorkflowResult {
            Action = "clean-up-distractions",
            Success = false,
            ElapsedMilliseconds = 90,
            LayoutName = "Sharing",
            LayoutApplied = false,
            ResolvedWindow = new global::DesktopManager.Cli.WindowResult {
                Title = "Browser",
                ProcessId = 300
            },
            Notes = new[] { "Nothing to minimize." }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.WorkflowCommands.WriteWorkflowResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(2, exitCode);
        StringAssert.Contains(output, "clean-up-distractions: success=False elapsed-ms=90");
        StringAssert.Contains(output, "layout: not-applied Sharing");
        StringAssert.Contains(output, "resolved: Browser [PID 300]");
        Assert.IsFalse(output.Contains("focused:"));
        Assert.IsFalse(output.Contains("minimized:"));
        Assert.IsFalse(output.Contains("artifacts:"));
        Assert.IsFalse(output.Contains("warning:"));
        StringAssert.Contains(output, "note: Nothing to minimize.");
    }
}
#endif
