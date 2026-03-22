#if NET8_0_OR_GREATER
using System.IO;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI entrypoint help and dispatch behavior.
/// </summary>
public class CliApplicationTests {
    [TestMethod]
    /// <summary>
    /// Ensures the CLI prints general help when invoked without arguments.
    /// </summary>
    public void Run_WithNoArguments_WritesGeneralHelpToStandardOutput() {
        (int exitCode, string standardOutput, string standardError) = RunCli();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(standardOutput, "desktopmanager - Windows desktop automation CLI");
        StringAssert.Contains(standardOutput, "Groups:");
        Assert.AreEqual(string.Empty, standardError);
    }

    [TestMethod]
    /// <summary>
    /// Ensures group help routes to the requested topic instead of the general help text.
    /// </summary>
    public void Run_WithHelpTopic_WritesGroupHelpToStandardOutput() {
        (int exitCode, string standardOutput, string standardError) = RunCli("help", "window");

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(standardOutput, "Window commands:");
        Assert.AreEqual(string.Empty, standardError);
    }

    [TestMethod]
    /// <summary>
    /// Ensures the shared help flag returns the general help text before command dispatch.
    /// </summary>
    public void Run_WithHelpFlag_WritesGeneralHelpToStandardOutput() {
        (int exitCode, string standardOutput, string standardError) = RunCli("window", "list", "--help");

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(standardOutput, "desktopmanager - Windows desktop automation CLI");
        Assert.AreEqual(string.Empty, standardError);
    }

    [TestMethod]
    /// <summary>
    /// Ensures unknown command groups fail with an error and include the general help text on standard error.
    /// </summary>
    public void Run_WithUnknownGroup_WritesErrorAndGeneralHelpToStandardError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("unknown-group");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Unknown command group 'unknown-group'.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [DataTestMethod]
    [DataRow("window")]
    [DataRow("control")]
    [DataRow("monitor")]
    [DataRow("process")]
    [DataRow("screenshot")]
    [DataRow("target")]
    [DataRow("control-target")]
    [DataRow("layout")]
    [DataRow("snapshot")]
    [DataRow("workflow")]
    [DataRow("mcp")]
    /// <summary>
    /// Ensures known groups return a consistent command-line error when the action is unknown.
    /// </summary>
    public void Run_WithUnknownActionForKnownGroup_WritesGroupSpecificErrorAndGeneralHelp(string group) {
        (int exitCode, string standardOutput, string standardError) = RunCli(group, "unknown-action");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, $"Error: Unknown {group} command 'unknown-action'.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [DataTestMethod]
    [DataRow("window")]
    [DataRow("control")]
    [DataRow("monitor")]
    [DataRow("process")]
    [DataRow("screenshot")]
    [DataRow("target")]
    [DataRow("control-target")]
    [DataRow("layout")]
    [DataRow("snapshot")]
    [DataRow("workflow")]
    [DataRow("mcp")]
    /// <summary>
    /// Ensures known groups fail consistently when the action is omitted entirely.
    /// </summary>
    public void Run_WithMissingActionForKnownGroup_WritesEmptyActionErrorAndGeneralHelp(string group) {
        (int exitCode, string standardOutput, string standardError) = RunCli(group);

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, $"Error: Unknown {group} command ''.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures process start reports a missing process path through the CLI entrypoint.
    /// </summary>
    public void Run_ProcessStartWithoutPath_WritesMissingProcessPathError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("process", "start");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Missing required process path.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures target get reports a missing target name through the CLI entrypoint.
    /// </summary>
    public void Run_TargetGetWithoutName_WritesMissingTargetNameError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("target", "get");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Missing required target name.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures window snap reports a missing required option through the CLI entrypoint.
    /// </summary>
    public void Run_WindowSnapWithoutPosition_WritesMissingRequiredOptionError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("window", "snap");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Missing required option '--position'.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures malformed empty option names are reported through the CLI entrypoint.
    /// </summary>
    public void Run_WithMalformedEmptyOptionName_WritesParseError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("--");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Encountered an empty option name.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures invalid integer values are reported through the CLI entrypoint before command execution.
    /// </summary>
    public void Run_ProcessStartWithInvalidIntegerOption_WritesIntegerError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("process", "start", "notepad.exe", "--wait-for-input-idle-ms", "abc");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Option '--wait-for-input-idle-ms' expects an integer value.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures invalid numeric values are reported through the CLI entrypoint before command execution.
    /// </summary>
    public void Run_TargetSaveWithInvalidNumericOption_WritesNumericError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("target", "save", "editor-center", "--x-ratio", "not-a-number", "--y-ratio", "0.5");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Option '--x-ratio' expects a numeric value.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    [TestMethod]
    /// <summary>
    /// Ensures invalid assertion tolerances are reported through the CLI entrypoint before layout checks run.
    /// </summary>
    public void Run_LayoutAssertWithInvalidTolerance_WritesIntegerError() {
        (int exitCode, string standardOutput, string standardError) = RunCli("layout", "assert", "coding", "--position-tolerance-px", "oops");

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, standardOutput);
        StringAssert.Contains(standardError, "Error: Option '--position-tolerance-px' expects an integer value.");
        StringAssert.Contains(standardError, "desktopmanager - Windows desktop automation CLI");
    }

    private static (int ExitCode, string StandardOutput, string StandardError) RunCli(params string[] args) {
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();

        int exitCode = global::DesktopManager.Cli.CliApplication.Run(args, standardOutput, standardError);
        return (exitCode, standardOutput.ToString(), standardError.ToString());
    }
}
#endif
