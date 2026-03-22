#if NET8_0_OR_GREATER
using System.IO;
using System.Collections.Generic;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI snapshot text output.
/// </summary>
public class SnapshotCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures path output writes the action, path, and scope when available.
    /// </summary>
    public void WritePathResult_WritesActionPathAndScope() {
        var payload = new global::DesktopManager.Cli.NamedStateResult {
            Action = "restore-snapshot",
            Name = "Coding",
            Path = @"C:\Snapshots\Coding.snapshot.json",
            Scope = "windows"
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.SnapshotCommands.WritePathResult(payload, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "restore-snapshot: Coding");
        StringAssert.Contains(output, "C:\\Snapshots\\Coding.snapshot.json");
        StringAssert.Contains(output, "scope: windows");
    }

    [TestMethod]
    /// <summary>
    /// Ensures path output omits the scope line when no scope is present.
    /// </summary>
    public void WritePathResult_OmitsScopeWhenUnset() {
        var payload = new global::DesktopManager.Cli.NamedStateResult {
            Action = "save-snapshot",
            Name = "Coding",
            Path = @"C:\Snapshots\Coding.snapshot.json"
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.SnapshotCommands.WritePathResult(payload, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "save-snapshot: Coding");
        Assert.IsFalse(output.Contains("scope:", System.StringComparison.Ordinal));
    }

    [TestMethod]
    /// <summary>
    /// Ensures list output sorts names and shows the empty-state message when no snapshots are present.
    /// </summary>
    public void WriteSnapshotNames_SortsNamesAndSupportsEmptyState() {
        using var populatedWriter = new StringWriter();

        int populatedExitCode = global::DesktopManager.Cli.SnapshotCommands.WriteSnapshotNames(
            new List<string> { "zeta", "Alpha", "beta" },
            populatedWriter);

        string populatedOutput = populatedWriter.ToString();
        int alphaIndex = populatedOutput.IndexOf("Alpha", System.StringComparison.Ordinal);
        int betaIndex = populatedOutput.IndexOf("beta", System.StringComparison.Ordinal);
        int zetaIndex = populatedOutput.IndexOf("zeta", System.StringComparison.Ordinal);

        Assert.AreEqual(0, populatedExitCode);
        Assert.IsTrue(alphaIndex >= 0);
        Assert.IsTrue(betaIndex > alphaIndex);
        Assert.IsTrue(zetaIndex > betaIndex);

        using var emptyWriter = new StringWriter();

        int emptyExitCode = global::DesktopManager.Cli.SnapshotCommands.WriteSnapshotNames(Array.Empty<string>(), emptyWriter);

        Assert.AreEqual(0, emptyExitCode);
        StringAssert.Contains(emptyWriter.ToString(), "No named snapshots found.");
    }
}
#endif
