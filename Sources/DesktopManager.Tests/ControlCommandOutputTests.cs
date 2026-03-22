#if NET8_0_OR_GREATER
using System.IO;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI control command text output.
/// </summary>
public class ControlCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures control mutation text output includes target, artifact, warning, and matched control details.
    /// </summary>
    public void WriteActionResult_WritesRichControlMutationSummary() {
        var result = new global::DesktopManager.Cli.ControlActionResult {
            Action = "send-control-keys",
            Success = true,
            Count = 2,
            ElapsedMilliseconds = 187,
            SafetyMode = "foreground-input-fallback",
            TargetName = "CommandBar",
            TargetKind = "control-target",
            BeforeScreenshots = new[] {
                new global::DesktopManager.Cli.ScreenshotResult()
            },
            AfterScreenshots = new[] {
                new global::DesktopManager.Cli.ScreenshotResult(),
                new global::DesktopManager.Cli.ScreenshotResult()
            },
            ArtifactWarnings = new[] { "Foreground input requested explicitly." },
            Controls = new[] {
                new global::DesktopManager.Cli.ControlResult {
                    ClassName = "WindowsForms10.EDIT.app.0.1",
                    Id = 101,
                    ParentWindow = new global::DesktopManager.Cli.WindowResult {
                        Title = "DesktopManager Test App"
                    }
                },
                new global::DesktopManager.Cli.ControlResult {
                    ClassName = "HwndWrapper[DesktopManager.TestApp;;123456]",
                    Id = 0,
                    ParentWindow = new global::DesktopManager.Cli.WindowResult {
                        Title = "DesktopManager Command Surface"
                    }
                }
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.ControlCommands.WriteActionResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "send-control-keys: 2 control(s) success=True safety=foreground-input-fallback elapsed-ms=187");
        StringAssert.Contains(output, "target: control-target CommandBar");
        StringAssert.Contains(output, "artifacts: before=1 after=2");
        StringAssert.Contains(output, "warning: Foreground input requested explicitly.");
        StringAssert.Contains(output, "- WindowsForms10.EDIT.app.0.1 [101] in DesktopManager Test App");
        StringAssert.Contains(output, "- HwndWrapper[DesktopManager.TestApp;;123456] [0] in DesktopManager Command Surface");
    }

    [TestMethod]
    /// <summary>
    /// Ensures control mutation text output omits optional sections when they are empty.
    /// </summary>
    public void WriteActionResult_OmitsEmptyOptionalSections() {
        var result = new global::DesktopManager.Cli.ControlActionResult {
            Action = "set-control-text",
            Success = false,
            Count = 0,
            ElapsedMilliseconds = 44,
            SafetyMode = "uia-direct-value"
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.ControlCommands.WriteActionResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "set-control-text: 0 control(s) success=False safety=uia-direct-value elapsed-ms=44");
        Assert.IsFalse(output.Contains("target:"));
        Assert.IsFalse(output.Contains("artifacts:"));
        Assert.IsFalse(output.Contains("warning:"));
        Assert.IsFalse(output.Contains(" in "));
    }
}
#endif
