#if NET8_0_OR_GREATER
using System.Text.Json;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for the hosted-session diagnostic PowerShell reader and cmdlet surface.
/// </summary>
public class PowerShellHostedSessionDiagnosticTests {
    [TestMethod]
    /// <summary>
    /// Ensures the PowerShell reader prefers the companion summary file when one exists.
    /// </summary>
    public void HostedSessionDiagnosticReader_PrefersSummaryArtifact() {
        string repositoryRoot = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", Guid.NewGuid().ToString("N"));
        string artifactDirectory = Path.Combine(repositoryRoot, "Artifacts", "HostedSessionTyping");
        Directory.CreateDirectory(artifactDirectory);
        File.WriteAllText(Path.Combine(repositoryRoot, "DesktopManager.sln"), string.Empty);

        string artifactPath = Path.Combine(artifactDirectory, "20260324-101500000-sample.json");
        string summaryPath = Path.Combine(artifactDirectory, "20260324-101500000-sample.browser-electron.summary.txt");

        File.WriteAllText(artifactPath, JsonSerializer.Serialize(new {
            FormatVersion = 1,
            Reason = "Hosted-session sample",
            CreatedUtc = "2026-03-24T10:15:00Z",
            Summary = "json summary",
            PolicyReport = "category='browser-electron'",
            RetryHistoryReport = new {
                CategoryHint = "browser-electron",
                Summary = "same culprit repeated",
                ExternalCount = 2,
                DistinctFingerprintCount = 1
            },
            Status = new {
                WindowTitle = "Remote session",
                StatusText = "foreground contention"
            }
        }));
        File.WriteAllText(summaryPath, "preferred summary");

        global::DesktopManager.PowerShell.DesktopHostedSessionDiagnosticRecord record =
            global::DesktopManager.PowerShell.DesktopHostedSessionDiagnosticReader.LoadLatest(
                global::DesktopManager.PowerShell.DesktopHostedSessionDiagnosticReader.GetHostedSessionArtifactDirectory(repositoryRoot));

        Assert.AreEqual(artifactPath, record.ArtifactPath);
        Assert.AreEqual(summaryPath, record.SummaryPath);
        Assert.AreEqual("preferred summary", record.SummaryText);
        Assert.AreEqual("browser-electron", record.RetryHistoryCategory);
    }

    [TestMethod]
    /// <summary>
    /// Ensures repository-root discovery finds the repo marker from a nested working directory.
    /// </summary>
    public void HostedSessionDiagnosticReader_FindsRepositoryRootFromNestedDirectory() {
        string repositoryRoot = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", Guid.NewGuid().ToString("N"));
        string nestedDirectory = Path.Combine(repositoryRoot, "Sources", "DesktopManager.PowerShell");
        Directory.CreateDirectory(nestedDirectory);
        File.WriteAllText(Path.Combine(repositoryRoot, "DesktopManager.sln"), string.Empty);

        bool found = global::DesktopManager.PowerShell.DesktopHostedSessionDiagnosticReader.TryFindRepositoryRoot(
            nestedDirectory,
            out string resolvedRepositoryRoot);

        Assert.IsTrue(found);
        Assert.AreEqual(repositoryRoot, resolvedRepositoryRoot);
    }

    [TestMethod]
    /// <summary>
    /// Ensures the hosted-session diagnostic cmdlet exposes the expected operator parameters.
    /// </summary>
    public void HostedSessionDiagnosticCmdlet_ExposesExpectedParameters() {
        Type? cmdletType = Type.GetType(
            "DesktopManager.PowerShell.CmdletGetDesktopHostedSessionDiagnostic, DesktopManager.PowerShell",
            throwOnError: true);

        Assert.IsNotNull(cmdletType);
        Assert.IsNotNull(cmdletType.GetProperty("ArtifactPath"));
        Assert.IsNotNull(cmdletType.GetProperty("ArtifactDirectory"));
        Assert.IsNotNull(cmdletType.GetProperty("RepositoryRoot"));
        Assert.IsNotNull(cmdletType.GetProperty("SummaryOnly"));
    }
}
#endif
