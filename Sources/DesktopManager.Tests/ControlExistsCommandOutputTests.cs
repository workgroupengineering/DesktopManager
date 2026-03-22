#if NET8_0_OR_GREATER
using System.IO;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI control exists text output.
/// </summary>
public class ControlExistsCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures control exists text output reports matches and lists matching controls.
    /// </summary>
    public void WriteAssertionResult_WritesMatchingControls() {
        var result = new global::DesktopManager.Cli.ControlAssertionResult {
            Matched = true,
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

        int exitCode = global::DesktopManager.Cli.ControlCommands.WriteAssertionResult(result, "Matching control found.", "No matching controls found.", writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "Matching control found.");
        StringAssert.Contains(output, "- Edit Search in DesktopManager Test App");
        StringAssert.Contains(output, "- Button Run in DesktopManager Command Surface");
    }

    [TestMethod]
    /// <summary>
    /// Ensures control exists text output reports failures cleanly when no controls match.
    /// </summary>
    public void WriteAssertionResult_WritesFailureWithoutControlLines() {
        var result = new global::DesktopManager.Cli.ControlAssertionResult {
            Matched = false,
            Count = 0
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.ControlCommands.WriteAssertionResult(result, "Matching control found.", "No matching controls found.", writer);
        string output = writer.ToString();

        Assert.AreEqual(2, exitCode);
        StringAssert.Contains(output, "No matching controls found.");
        Assert.IsFalse(output.Contains(" in "));
    }
}
#endif
