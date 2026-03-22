#if NET8_0_OR_GREATER
using System.IO;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI window assertion and wait text output.
/// </summary>
public class WindowAssertionCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures window wait text output includes elapsed timing and each matched window.
    /// </summary>
    public void WriteWaitResult_WritesMatchedWindows() {
        var result = new global::DesktopManager.Cli.WaitForWindowResult {
            ElapsedMilliseconds = 350,
            Count = 2,
            Windows = new[] {
                new global::DesktopManager.Cli.WindowResult {
                    Title = "Editor",
                    ProcessId = 101
                },
                new global::DesktopManager.Cli.WindowResult {
                    Title = "Terminal",
                    ProcessId = 202
                }
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.WindowCommands.WriteWaitResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "wait: 2 window(s) after 350ms");
        StringAssert.Contains(output, "- Editor [PID 101]");
        StringAssert.Contains(output, "- Terminal [PID 202]");
    }

    [TestMethod]
    /// <summary>
    /// Ensures window assertion text output includes active-window context and matched windows.
    /// </summary>
    public void WriteAssertionResult_WritesActiveWindowAndMatches() {
        var payload = new global::DesktopManager.Cli.WindowAssertionResult {
            Matched = true,
            Count = 1,
            ActiveWindow = new global::DesktopManager.Cli.WindowResult {
                Title = "Editor",
                ProcessId = 101
            },
            Windows = new[] {
                new global::DesktopManager.Cli.WindowResult {
                    Title = "Editor",
                    ProcessId = 101
                }
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.WindowCommands.WriteAssertionResult(payload, "Active window matches.", "Active window does not match.", writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "Active window matches.");
        StringAssert.Contains(output, "Active: Editor [PID 101]");
        StringAssert.Contains(output, "- Editor [PID 101]");
    }

    [TestMethod]
    /// <summary>
    /// Ensures window assertion text output omits optional sections when no active or matched window is available.
    /// </summary>
    public void WriteAssertionResult_WritesFailureWithoutOptionalSections() {
        var payload = new global::DesktopManager.Cli.WindowAssertionResult {
            Matched = false,
            Count = 0
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.WindowCommands.WriteAssertionResult(payload, "Matching window found.", "No matching windows found.", writer);
        string output = writer.ToString();

        Assert.AreEqual(2, exitCode);
        StringAssert.Contains(output, "No matching windows found.");
        Assert.IsFalse(output.Contains("Active:"));
        Assert.IsFalse(output.Contains("[PID"));
    }
}
#endif
