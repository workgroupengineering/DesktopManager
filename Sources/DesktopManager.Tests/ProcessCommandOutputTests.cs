#if NET8_0_OR_GREATER
using System.IO;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI process command text output.
/// </summary>
public class ProcessCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures the start-and-wait text output includes resolved-process wait binding metadata.
    /// </summary>
    public void WriteStartAndWaitResult_WritesResolvedProcessBindingFields() {
        var result = new global::DesktopManager.Cli.LaunchAndWaitResult {
            Action = "launch-and-wait-for-window",
            Success = true,
            ElapsedMilliseconds = 321,
            WaitBinding = "resolved-process-id",
            BoundProcessId = 200,
            BoundProcessName = null,
            Launch = new global::DesktopManager.Cli.ProcessLaunchResult {
                FilePath = "notepad.exe",
                ProcessId = 100,
                ResolvedProcessId = 200
            },
            WindowWait = new global::DesktopManager.Cli.WaitForWindowResult {
                ElapsedMilliseconds = 150,
                Count = 1,
                Windows = new[] {
                    new global::DesktopManager.Cli.WindowResult {
                        Title = "Untitled - Notepad",
                        ProcessId = 200
                    }
                }
            },
            Notes = new[] { "Resolved launch window." }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.ProcessCommands.WriteStartAndWaitResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "launch-and-wait-for-window: success=True elapsed-ms=321");
        StringAssert.Contains(output, "- PID: 100");
        StringAssert.Contains(output, "- ResolvedPID: 200");
        StringAssert.Contains(output, "- File: notepad.exe");
        StringAssert.Contains(output, "- WaitBinding: resolved-process-id");
        StringAssert.Contains(output, "- BoundProcessId: 200");
        Assert.IsFalse(output.Contains("BoundProcessName"));
        StringAssert.Contains(output, "- WaitCount: 1");
        StringAssert.Contains(output, "- WaitElapsedMs: 150");
        StringAssert.Contains(output, "- Window: Untitled - Notepad [PID 200]");
        StringAssert.Contains(output, "- Note: Resolved launch window.");
    }

    [TestMethod]
    /// <summary>
    /// Ensures the start-and-wait text output includes process-family wait binding metadata.
    /// </summary>
    public void WriteStartAndWaitResult_WritesProcessFamilyBindingFields() {
        var result = new global::DesktopManager.Cli.LaunchAndWaitResult {
            Action = "launch-and-wait-for-window",
            Success = false,
            ElapsedMilliseconds = 654,
            WaitBinding = "process-name-family",
            BoundProcessId = null,
            BoundProcessName = "Code",
            Launch = new global::DesktopManager.Cli.ProcessLaunchResult {
                FilePath = "\"C:\\Program Files\\Microsoft VS Code\\Code.exe\"",
                ProcessId = 100
            },
            WindowWait = new global::DesktopManager.Cli.WaitForWindowResult {
                ElapsedMilliseconds = 500,
                Count = 0
            },
            ArtifactWarnings = new[] { "Capture disabled." }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.ProcessCommands.WriteStartAndWaitResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(2, exitCode);
        StringAssert.Contains(output, "launch-and-wait-for-window: success=False elapsed-ms=654");
        StringAssert.Contains(output, "- PID: 100");
        Assert.IsFalse(output.Contains("ResolvedPID"));
        StringAssert.Contains(output, "- WaitBinding: process-name-family");
        Assert.IsFalse(output.Contains("BoundProcessId"));
        StringAssert.Contains(output, "- BoundProcessName: Code");
        StringAssert.Contains(output, "- WaitCount: 0");
        StringAssert.Contains(output, "- WaitElapsedMs: 500");
        StringAssert.Contains(output, "- Warning: Capture disabled.");
    }
}
#endif
