#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI process and workflow criteria mapping.
/// </summary>
public class ProcessAndWorkflowCriteriaTests {
    [TestMethod]
    /// <summary>
    /// Ensures process artifact options return null when no capture flags or directory are provided.
    /// </summary>
    public void ProcessCreateArtifactOptions_ReturnsNullWhenNoArtifactsRequested() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "process",
            "start-and-wait",
            "notepad.exe"
        });

        global::DesktopManager.Cli.MutationArtifactOptions? options = global::DesktopManager.Cli.ProcessCommands.CreateArtifactOptions(arguments);

        Assert.IsNull(options);
    }

    [TestMethod]
    /// <summary>
    /// Ensures process artifact options map capture flags and artifact directory.
    /// </summary>
    public void ProcessCreateArtifactOptions_MapsCaptureFlagsAndDirectory() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "process",
            "start-and-wait",
            "notepad.exe",
            "--capture-before",
            "--capture-after",
            "--artifact-directory", @"C:\Artifacts"
        });

        global::DesktopManager.Cli.MutationArtifactOptions? options = global::DesktopManager.Cli.ProcessCommands.CreateArtifactOptions(arguments);

        Assert.IsNotNull(options);
        Assert.IsTrue(options.CaptureBefore);
        Assert.IsTrue(options.CaptureAfter);
        Assert.AreEqual(@"C:\Artifacts", options.ArtifactDirectory);
    }

    [TestMethod]
    /// <summary>
    /// Ensures workflow focus criteria map window selector flags and keep all=false.
    /// </summary>
    public void WorkflowCreateFocusCriteria_MapsSelectorFlags() {
        global::DesktopManager.Cli.CommandLineArguments arguments = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "workflow",
            "prepare-coding",
            "--title", "*Editor*",
            "--process", "DesktopManager.TestApp",
            "--class", "TestWindowClass",
            "--pid", "321",
            "--handle", "0x1234",
            "--active",
            "--include-hidden",
            "--exclude-cloaked",
            "--exclude-owned",
            "--include-empty"
        });

        global::DesktopManager.Cli.WindowSelectionCriteria criteria = global::DesktopManager.Cli.WorkflowCommands.CreateFocusCriteria(arguments);

        Assert.AreEqual("*Editor*", criteria.TitlePattern);
        Assert.AreEqual("DesktopManager.TestApp", criteria.ProcessNamePattern);
        Assert.AreEqual("TestWindowClass", criteria.ClassNamePattern);
        Assert.AreEqual(321, criteria.ProcessId);
        Assert.AreEqual("0x1234", criteria.Handle);
        Assert.IsTrue(criteria.Active);
        Assert.IsTrue(criteria.IncludeHidden);
        Assert.IsFalse(criteria.IncludeCloaked);
        Assert.IsFalse(criteria.IncludeOwned);
        Assert.IsTrue(criteria.IncludeEmptyTitles);
        Assert.IsFalse(criteria.All);
    }

    [TestMethod]
    /// <summary>
    /// Ensures workflow artifact options return null without flags and map requested captures when present.
    /// </summary>
    public void WorkflowCreateArtifactOptions_MapsRequestedCaptures() {
        global::DesktopManager.Cli.CommandLineArguments noArtifacts = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "workflow",
            "prepare-coding"
        });

        global::DesktopManager.Cli.MutationArtifactOptions? noArtifactOptions = global::DesktopManager.Cli.WorkflowCommands.CreateArtifactOptions(noArtifacts);

        Assert.IsNull(noArtifactOptions);

        global::DesktopManager.Cli.CommandLineArguments withArtifacts = global::DesktopManager.Cli.CommandLineArguments.Parse(new[] {
            "workflow",
            "prepare-coding",
            "--capture-after",
            "--artifact-directory", @"C:\WorkflowArtifacts"
        });

        global::DesktopManager.Cli.MutationArtifactOptions? artifactOptions = global::DesktopManager.Cli.WorkflowCommands.CreateArtifactOptions(withArtifacts);

        Assert.IsNotNull(artifactOptions);
        Assert.IsFalse(artifactOptions.CaptureBefore);
        Assert.IsTrue(artifactOptions.CaptureAfter);
        Assert.AreEqual(@"C:\WorkflowArtifacts", artifactOptions.ArtifactDirectory);
    }
}
#endif
