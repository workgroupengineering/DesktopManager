#if NET8_0_OR_GREATER
using System.IO;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI process start text output.
/// </summary>
public class ProcessStartCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures the process start text output includes resolved process and window details when available.
    /// </summary>
    public void WriteStartResult_WritesResolvedProcessAndWindowFields() {
        var result = new global::DesktopManager.Cli.ProcessLaunchResult {
            FilePath = "notepad.exe",
            Arguments = "--example",
            WorkingDirectory = "C:\\Temp",
            ProcessId = 100,
            ResolvedProcessId = 200,
            MainWindow = new global::DesktopManager.Cli.WindowResult {
                Title = "Untitled - Notepad",
                ProcessId = 200
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.ProcessCommands.WriteStartResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "start: PID 100");
        StringAssert.Contains(output, "- ResolvedPID: 200");
        StringAssert.Contains(output, "- File: notepad.exe");
        StringAssert.Contains(output, "- Arguments: --example");
        StringAssert.Contains(output, "- WorkingDirectory: C:\\Temp");
        StringAssert.Contains(output, "- Window: Untitled - Notepad");
    }

    [TestMethod]
    /// <summary>
    /// Ensures the process start text output omits optional fields when they are not populated.
    /// </summary>
    public void WriteStartResult_OmitsOptionalFieldsWhenUnset() {
        var result = new global::DesktopManager.Cli.ProcessLaunchResult {
            FilePath = "cmd.exe",
            ProcessId = 123
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.ProcessCommands.WriteStartResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "start: PID 123");
        StringAssert.Contains(output, "- File: cmd.exe");
        Assert.IsFalse(output.Contains("ResolvedPID"));
        Assert.IsFalse(output.Contains("Arguments:"));
        Assert.IsFalse(output.Contains("WorkingDirectory:"));
        Assert.IsFalse(output.Contains("Window:"));
    }
}
#endif
