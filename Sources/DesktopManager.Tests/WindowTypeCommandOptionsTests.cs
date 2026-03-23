#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI window typing option mapping.
/// </summary>
public class WindowTypeCommandOptionsTests {
    [TestMethod]
    /// <summary>
    /// Ensures window type options map explicit foreground-input and delay flags.
    /// </summary>
    public void CreateTypeOptions_MapsForegroundTypingFlags() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "window",
            "type",
            "--text", "safe probe",
            "--foreground-input",
            "--delay-ms", "25"
        });

        global::DesktopManager.Cli.WindowTextCommandOptions options = global::DesktopManager.Cli.WindowCommands.CreateTypeOptions(arguments);

        Assert.AreEqual("safe probe", options.Text);
        Assert.IsFalse(options.Paste);
        Assert.IsTrue(options.ForegroundInput);
        Assert.IsFalse(options.PhysicalKeys);
        Assert.AreEqual(25, options.DelayMilliseconds);
    }

    [TestMethod]
    /// <summary>
    /// Ensures physical-key typing implies the strict foreground-input path.
    /// </summary>
    public void CreateTypeOptions_PhysicalKeys_EnablesForegroundInputToo() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "window",
            "type",
            "--text", "safe probe",
            "--physical-keys"
        });

        global::DesktopManager.Cli.WindowTextCommandOptions options = global::DesktopManager.Cli.WindowCommands.CreateTypeOptions(arguments);

        Assert.AreEqual("safe probe", options.Text);
        Assert.IsFalse(options.Paste);
        Assert.IsTrue(options.ForegroundInput);
        Assert.IsTrue(options.PhysicalKeys);
        Assert.AreEqual(0, options.DelayMilliseconds);
    }

    [TestMethod]
    /// <summary>
    /// Ensures script typing maps chunking and line pacing options without forcing a different delivery mode.
    /// </summary>
    public void CreateTypeOptions_MapsScriptFlags() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "window",
            "type",
            "--text", "line1\nline2",
            "--script",
            "--chunk-size", "32",
            "--line-delay-ms", "15"
        });

        global::DesktopManager.Cli.WindowTextCommandOptions options = global::DesktopManager.Cli.WindowCommands.CreateTypeOptions(arguments);

        Assert.AreEqual("line1\nline2", options.Text);
        Assert.IsFalse(options.Paste);
        Assert.IsFalse(options.ForegroundInput);
        Assert.IsFalse(options.PhysicalKeys);
        Assert.IsTrue(options.ScriptMode);
        Assert.AreEqual(32, options.ScriptChunkLength);
        Assert.AreEqual(15, options.ScriptLineDelayMilliseconds);
    }

    [TestMethod]
    /// <summary>
    /// Ensures hosted-session typing enables the physical foreground path and uses safer pacing defaults.
    /// </summary>
    public void CreateTypeOptions_HostedSession_UsesSaferForegroundDefaults() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "window",
            "type",
            "--text", "safe probe",
            "--hosted-session",
            "--script"
        });

        global::DesktopManager.Cli.WindowTextCommandOptions options = global::DesktopManager.Cli.WindowCommands.CreateTypeOptions(arguments);

        Assert.AreEqual("safe probe", options.Text);
        Assert.IsTrue(options.ForegroundInput);
        Assert.IsFalse(options.PhysicalKeys);
        Assert.IsTrue(options.HostedSession);
        Assert.IsTrue(options.ScriptMode);
        Assert.AreEqual(35, options.DelayMilliseconds);
        Assert.AreEqual(120, options.ScriptLineDelayMilliseconds);
    }

    [TestMethod]
    /// <summary>
    /// Ensures window type options default to simulated typing without strict foreground requirements.
    /// </summary>
    public void CreateTypeOptions_UsesDefaultsWhenUnset() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "window",
            "type",
            "--text", "hello"
        });

        global::DesktopManager.Cli.WindowTextCommandOptions options = global::DesktopManager.Cli.WindowCommands.CreateTypeOptions(arguments);

        Assert.AreEqual("hello", options.Text);
        Assert.IsFalse(options.Paste);
        Assert.IsFalse(options.ForegroundInput);
        Assert.IsFalse(options.PhysicalKeys);
        Assert.IsFalse(options.HostedSession);
        Assert.IsFalse(options.ScriptMode);
        Assert.AreEqual(120, options.ScriptChunkLength);
        Assert.AreEqual(0, options.ScriptLineDelayMilliseconds);
        Assert.AreEqual(0, options.DelayMilliseconds);
    }
}
#endif
