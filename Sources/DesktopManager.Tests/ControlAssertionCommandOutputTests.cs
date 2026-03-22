#if NET8_0_OR_GREATER
using System.IO;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI control wait and assertion text output.
/// </summary>
public class ControlAssertionCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures control wait text output includes elapsed timing and matched control details.
    /// </summary>
    public void WriteWaitResult_WritesMatchedControls() {
        var result = new global::DesktopManager.Cli.WaitForControlResult {
            ElapsedMilliseconds = 420,
            Count = 2,
            Controls = new[] {
                new global::DesktopManager.Cli.ControlResult {
                    ControlType = "Edit",
                    Text = "Search",
                    ParentWindow = new global::DesktopManager.Cli.WindowResult {
                        Title = "DesktopManager Test App"
                    }
                },
                new global::DesktopManager.Cli.ControlResult {
                    ControlType = "Button",
                    Text = "Run",
                    ParentWindow = new global::DesktopManager.Cli.WindowResult {
                        Title = "DesktopManager Command Surface"
                    }
                }
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.ControlCommands.WriteWaitResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "wait: 2 control(s) after 420ms");
        StringAssert.Contains(output, "- Edit Search in DesktopManager Test App");
        StringAssert.Contains(output, "- Button Run in DesktopManager Command Surface");
    }

    [TestMethod]
    /// <summary>
    /// Ensures control value assertion text output includes the expected predicate and matched controls.
    /// </summary>
    public void WriteValueAssertionResult_WritesPassingAssertionSummary() {
        var result = new global::DesktopManager.Cli.ControlValueAssertionResult {
            Matched = true,
            PropertyName = "Value",
            MatchMode = "contains",
            Expected = "DesktopManager",
            Count = 2,
            MatchedCount = 1,
            Controls = new[] {
                new global::DesktopManager.Cli.ControlResult {
                    ControlType = "Edit",
                    Value = "DesktopManager MCP E2E",
                    Text = string.Empty,
                    ParentWindow = new global::DesktopManager.Cli.WindowResult {
                        Title = "DesktopManager Test App"
                    }
                },
                new global::DesktopManager.Cli.ControlResult {
                    ControlType = "Edit",
                    Value = "Other value",
                    Text = string.Empty,
                    ParentWindow = new global::DesktopManager.Cli.WindowResult {
                        Title = "DesktopManager Secondary"
                    }
                }
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.ControlCommands.WriteValueAssertionResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "Control value assertion passed.");
        StringAssert.Contains(output, "assertion: Value contains \"DesktopManager\"");
        StringAssert.Contains(output, "matched: 1/2");
        StringAssert.Contains(output, "- Edit value=\"DesktopManager MCP E2E\" text=\"\" in DesktopManager Test App");
        StringAssert.Contains(output, "- Edit value=\"Other value\" text=\"\" in DesktopManager Secondary");
    }

    [TestMethod]
    /// <summary>
    /// Ensures control value assertion text output reports failures cleanly when no controls match.
    /// </summary>
    public void WriteValueAssertionResult_WritesFailingAssertionSummary() {
        var result = new global::DesktopManager.Cli.ControlValueAssertionResult {
            Matched = false,
            PropertyName = "Text",
            MatchMode = "equals",
            Expected = "Run",
            Count = 0,
            MatchedCount = 0
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.ControlCommands.WriteValueAssertionResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(2, exitCode);
        StringAssert.Contains(output, "Control value assertion failed.");
        StringAssert.Contains(output, "assertion: Text equals \"Run\"");
        StringAssert.Contains(output, "matched: 0/0");
        Assert.IsFalse(output.Contains(" in "));
    }
}
#endif
