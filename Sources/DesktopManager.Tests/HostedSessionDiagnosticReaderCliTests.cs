#if NET8_0_OR_GREATER
using System.IO;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI hosted-session diagnostic reader behavior.
/// </summary>
public class HostedSessionDiagnosticReaderCliTests {
    [TestMethod]
    /// <summary>
    /// Ensures the CLI reader prefers the sidecar summary artifact and maps retry-history fields.
    /// </summary>
    public void Load_PrefersSummaryArtifactAndMapsFields() {
        string directory = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", "CliHostedSessionReader", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try {
            string artifactPath = Path.Combine(directory, "sample.json");
            File.WriteAllText(artifactPath, """
{"FormatVersion":1,"Reason":"Test artifact","CreatedUtc":"2026-03-24T08:00:00Z","Summary":"summary from json","PolicyReport":"category='mixed'","RetryHistoryReport":{"CategoryHint":"mixed","Summary":"mixed focus contention","ExternalCount":3,"DistinctFingerprintCount":2},"Status":{"WindowTitle":"DesktopManager Test App","StatusText":"Waiting for focus","ForegroundHistory":[]}}
""");
            File.WriteAllText(Path.Combine(directory, "sample.summary.txt"), "summary from sidecar");

            global::DesktopManager.Cli.HostedSessionDiagnosticResult result =
                global::DesktopManager.Cli.HostedSessionDiagnosticReader.Load(artifactPath);

            Assert.AreEqual(artifactPath, result.ArtifactPath);
            Assert.AreEqual("summary from sidecar", result.SummaryText);
            Assert.AreEqual("mixed", result.RetryHistoryCategory);
            Assert.AreEqual(3, result.RetryHistoryExternalCount);
            Assert.AreEqual(2, result.RetryHistoryDistinctFingerprintCount);
            Assert.AreEqual("DesktopManager Test App", result.WindowTitle);
        } finally {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures repository-root discovery resolves the artifact folder from nested directories.
    /// </summary>
    public void TryFindRepositoryRoot_FromNestedDirectory_ReturnsRepositoryRoot() {
        string repositoryRoot = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", "CliRepoRoot", Guid.NewGuid().ToString("N"));
        string nestedDirectory = Path.Combine(repositoryRoot, "Sources", "DesktopManager.Cli");
        Directory.CreateDirectory(nestedDirectory);
        File.WriteAllText(Path.Combine(repositoryRoot, "DesktopManager.sln"), string.Empty);

        try {
            bool found = global::DesktopManager.Cli.HostedSessionDiagnosticReader.TryFindRepositoryRoot(nestedDirectory, out string resolvedRoot);

            Assert.IsTrue(found);
            Assert.AreEqual(repositoryRoot, resolvedRoot);
        } finally {
            Directory.Delete(repositoryRoot, true);
        }
    }
}
#endif
