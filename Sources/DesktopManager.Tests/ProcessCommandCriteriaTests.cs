#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI process command criteria mapping.
/// </summary>
public class ProcessCommandCriteriaTests {
    [TestMethod]
    /// <summary>
    /// Ensures start options map explicit launch-related flags.
    /// </summary>
    public void CreateStartOptions_MapsLaunchFlags() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "process",
            "start",
            "notepad.exe",
            "--arguments", "file.txt",
            "--working-directory", @"C:\Work",
            "--wait-for-input-idle-ms", "2500",
            "--wait-for-window-ms", "4000",
            "--wait-for-window-interval-ms", "300",
            "--window-title", "*Notepad*",
            "--window-class", "Notepad",
            "--require-window"
        });

        global::DesktopManager.Cli.ProcessStartCommandOptions options = global::DesktopManager.Cli.ProcessCommands.CreateStartOptions(arguments);

        Assert.AreEqual("notepad.exe", options.FilePath);
        Assert.AreEqual("file.txt", options.Arguments);
        Assert.AreEqual(@"C:\Work", options.WorkingDirectory);
        Assert.AreEqual(2500, options.WaitForInputIdleMilliseconds);
        Assert.AreEqual(4000, options.WaitForWindowMilliseconds);
        Assert.AreEqual(300, options.WaitForWindowIntervalMilliseconds);
        Assert.AreEqual("*Notepad*", options.WindowTitlePattern);
        Assert.AreEqual("Notepad", options.WindowClassNamePattern);
        Assert.IsTrue(options.RequireWindow);
    }

    [TestMethod]
    /// <summary>
    /// Ensures start options use nulls and false for optional fields when no flags are provided.
    /// </summary>
    public void CreateStartOptions_UsesDefaultsWhenUnset() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "process",
            "start",
            "notepad.exe"
        });

        global::DesktopManager.Cli.ProcessStartCommandOptions options = global::DesktopManager.Cli.ProcessCommands.CreateStartOptions(arguments);

        Assert.AreEqual("notepad.exe", options.FilePath);
        Assert.IsNull(options.Arguments);
        Assert.IsNull(options.WorkingDirectory);
        Assert.IsNull(options.WaitForInputIdleMilliseconds);
        Assert.IsNull(options.WaitForWindowMilliseconds);
        Assert.IsNull(options.WaitForWindowIntervalMilliseconds);
        Assert.IsNull(options.WindowTitlePattern);
        Assert.IsNull(options.WindowClassNamePattern);
        Assert.IsFalse(options.RequireWindow);
    }

    [TestMethod]
    /// <summary>
    /// Ensures start-and-wait options map launch, wait, and artifact flags.
    /// </summary>
    public void CreateStartAndWaitOptions_MapsLaunchWaitFlagsAndArtifacts() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "process",
            "start-and-wait",
            "notepad.exe",
            "--arguments", "file.txt",
            "--working-directory", @"C:\Work",
            "--wait-for-input-idle-ms", "1500",
            "--launch-wait-for-window-ms", "5000",
            "--launch-wait-for-window-interval-ms", "250",
            "--launch-window-title", "*Launcher*",
            "--launch-window-class", "LauncherWindow",
            "--window-title", "*Editor*",
            "--window-class", "EditorWindow",
            "--include-hidden",
            "--include-empty",
            "--all",
            "--follow-process-family",
            "--timeout-ms", "12000",
            "--interval-ms", "350",
            "--capture-before",
            "--capture-after",
            "--artifact-directory", @"C:\Artifacts"
        });

        global::DesktopManager.Cli.LaunchAndWaitCommandOptions options = global::DesktopManager.Cli.ProcessCommands.CreateStartAndWaitOptions(arguments);

        Assert.AreEqual("notepad.exe", options.FilePath);
        Assert.AreEqual("file.txt", options.Arguments);
        Assert.AreEqual(@"C:\Work", options.WorkingDirectory);
        Assert.AreEqual(1500, options.WaitForInputIdleMilliseconds);
        Assert.AreEqual(5000, options.LaunchWaitForWindowMilliseconds);
        Assert.AreEqual(250, options.LaunchWaitForWindowIntervalMilliseconds);
        Assert.AreEqual("*Launcher*", options.LaunchWindowTitlePattern);
        Assert.AreEqual("LauncherWindow", options.LaunchWindowClassNamePattern);
        Assert.AreEqual("*Editor*", options.WindowTitlePattern);
        Assert.AreEqual("EditorWindow", options.WindowClassNamePattern);
        Assert.IsTrue(options.IncludeHidden);
        Assert.IsTrue(options.IncludeEmpty);
        Assert.IsTrue(options.All);
        Assert.IsTrue(options.FollowProcessFamily);
        Assert.AreEqual(12000, options.TimeoutMilliseconds);
        Assert.AreEqual(350, options.IntervalMilliseconds);
        Assert.IsNotNull(options.ArtifactOptions);
        Assert.IsTrue(options.ArtifactOptions.CaptureBefore);
        Assert.IsTrue(options.ArtifactOptions.CaptureAfter);
        Assert.AreEqual(@"C:\Artifacts", options.ArtifactOptions.ArtifactDirectory);
    }

    [TestMethod]
    /// <summary>
    /// Ensures start-and-wait options use default timeout and interval values when unset.
    /// </summary>
    public void CreateStartAndWaitOptions_UsesDefaultTimeoutAndInterval() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "process",
            "start-and-wait",
            "notepad.exe"
        });

        global::DesktopManager.Cli.LaunchAndWaitCommandOptions options = global::DesktopManager.Cli.ProcessCommands.CreateStartAndWaitOptions(arguments);

        Assert.AreEqual("notepad.exe", options.FilePath);
        Assert.AreEqual(10000, options.TimeoutMilliseconds);
        Assert.AreEqual(200, options.IntervalMilliseconds);
        Assert.IsFalse(options.IncludeHidden);
        Assert.IsFalse(options.IncludeEmpty);
        Assert.IsFalse(options.All);
        Assert.IsFalse(options.FollowProcessFamily);
        Assert.IsNull(options.ArtifactOptions);
    }
}
#endif
