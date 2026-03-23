#if NET8_0_OR_GREATER
using System.IO;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI window command text output.
/// </summary>
public class WindowCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures window mutation text output includes target, artifact, warning, and matched window details.
    /// </summary>
    public void WriteWindowMutationResult_WritesRichMutationSummary() {
        var payload = new global::DesktopManager.Cli.WindowChangeResult {
            Action = "move-window",
            Success = true,
            Count = 2,
            ElapsedMilliseconds = 145,
            SafetyMode = "background",
            TargetName = "EditorCenter",
            TargetKind = "window-target",
            BeforeScreenshots = new[] {
                new global::DesktopManager.Cli.ScreenshotResult()
            },
            AfterScreenshots = new[] {
                new global::DesktopManager.Cli.ScreenshotResult(),
                new global::DesktopManager.Cli.ScreenshotResult()
            },
            Verification = new global::DesktopManager.Cli.WindowMutationVerificationResult {
                Verified = true,
                Mode = "geometry",
                Summary = "Observed both windows at the requested post-mutation geometry.",
                ExpectedCount = 2,
                ObservedCount = 2,
                MatchedCount = 2,
                MismatchCount = 0,
                TolerancePixels = 10,
                ActiveWindow = new global::DesktopManager.Cli.WindowResult {
                    Title = "Editor",
                    ProcessId = 100
                },
                Notes = new[] { "Editor geometry matched within tolerance." }
            },
            ArtifactWarnings = new[] { "Capture path normalized." },
            Windows = new[] {
                new global::DesktopManager.Cli.WindowResult {
                    Title = "Editor",
                    ProcessId = 100
                },
                new global::DesktopManager.Cli.WindowResult {
                    Title = "Terminal",
                    ProcessId = 101
                }
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.WindowCommands.WriteWindowMutationResult(payload, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "move-window: 2 window(s) success=True safety=background elapsed-ms=145");
        StringAssert.Contains(output, "target: window-target EditorCenter");
        StringAssert.Contains(output, "verification: verified=True mode=geometry observed=2/2 matched=2 mismatches=0 tolerance-px=10");
        StringAssert.Contains(output, "verification-summary: Observed both windows at the requested post-mutation geometry.");
        StringAssert.Contains(output, "verification-active: Editor [PID 100]");
        StringAssert.Contains(output, "verification-note: Editor geometry matched within tolerance.");
        StringAssert.Contains(output, "artifacts: before=1 after=2");
        StringAssert.Contains(output, "warning: Capture path normalized.");
        StringAssert.Contains(output, "- Editor [PID 100]");
        StringAssert.Contains(output, "- Terminal [PID 101]");
    }

    [TestMethod]
    /// <summary>
    /// Ensures window mutation text output omits optional sections when they are empty.
    /// </summary>
    public void WriteWindowMutationResult_OmitsEmptyOptionalSections() {
        var payload = new global::DesktopManager.Cli.WindowChangeResult {
            Action = "focus-window",
            Success = false,
            Count = 0,
            ElapsedMilliseconds = 33,
            SafetyMode = "foreground"
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.WindowCommands.WriteWindowMutationResult(payload, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "focus-window: 0 window(s) success=False safety=foreground elapsed-ms=33");
        Assert.IsFalse(output.Contains("target:"));
        Assert.IsFalse(output.Contains("artifacts:"));
        Assert.IsFalse(output.Contains("warning:"));
        Assert.IsFalse(output.Contains("[PID"));
    }
}
#endif
