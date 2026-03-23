using System;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace DesktopManager.Tests;

[TestClass]
public class DesktopManagerTestAppSessionTests {
    [TestMethod]
    public void BuildStatusArtifact_IncludesStructuredRetryHistoryReportAndStatus() {
        var status = new DesktopManagerTestAppStatus {
            WindowTitle = "DesktopManager-TestApp-hosted-symbol-matrix-abc",
            LastObservedForegroundTitle = "Microsoft Edge",
            LastObservedForegroundClass = "Chrome_WidgetWin_1",
            StatusText = "waiting"
        };
        var retryHistoryReport = new HostedSessionRetryHistoryReport {
            CategoryHint = "browser-electron",
            Summary = "Repeated browser-electron foreground interruption observed 2 time(s): title='Microsoft Edge' class='Chrome_WidgetWin_1'",
            ExternalCount = 2,
            DistinctFingerprintCount = 1
        };

        HostedSessionDiagnosticArtifact artifact = DesktopManagerTestAppSession.BuildStatusArtifact(
            "Hosted-session typing inconclusive after foreground loss",
            status,
            retryHistoryReport,
            "category='browser-electron', abortThreshold=2, fingerprint='title=''Microsoft Edge'' class=''Chrome_WidgetWin_1'''");

        Assert.AreEqual(1, artifact.FormatVersion);
        Assert.AreEqual("Hosted-session typing inconclusive after foreground loss", artifact.Reason);
        Assert.AreEqual("browser-electron", artifact.RetryHistoryReport.CategoryHint);
        Assert.AreEqual(2, artifact.RetryHistoryReport.ExternalCount);
        Assert.AreEqual(1, artifact.RetryHistoryReport.DistinctFingerprintCount);
        Assert.AreEqual("DesktopManager-TestApp-hosted-symbol-matrix-abc", artifact.Status.WindowTitle);
        StringAssert.Contains(artifact.PolicyReport, "category='browser-electron'");
        StringAssert.Contains(artifact.Summary, "RetryHistoryReport: category='browser-electron', externalCount=2, distinctFingerprintCount=1");
    }

    [TestMethod]
    public void BuildStatusArtifact_SerializesRetryHistoryReportIntoJson() {
        var status = new DesktopManagerTestAppStatus {
            WindowTitle = "DesktopManager-TestApp-hosted-symbol-matrix-abc",
            StatusText = "waiting"
        };
        var retryHistoryReport = new HostedSessionRetryHistoryReport {
            CategoryHint = "mixed",
            Summary = "Mixed foreground interruptions observed across retries: browser-electron x1, unknown x1.",
            ExternalCount = 2,
            DistinctFingerprintCount = 2
        };

        HostedSessionDiagnosticArtifact artifact = DesktopManagerTestAppSession.BuildStatusArtifact(
            "Hosted-session typing inconclusive after foreground loss",
            status,
            retryHistoryReport,
            "category='unknown', abortThreshold=3, fingerprint=''");
        string json = JsonSerializer.Serialize(artifact);

        StringAssert.Contains(json, "\"RetryHistoryReport\":");
        StringAssert.Contains(json, "\"CategoryHint\":\"mixed\"");
        StringAssert.Contains(json, "\"ExternalCount\":2");
        StringAssert.Contains(json, "\"DistinctFingerprintCount\":2");
        StringAssert.Contains(json, "\"Status\":");
    }

    [TestMethod]
    public void GetArtifactSetPaths_ReturnsJsonAndCompanionSummaryFiles() {
        string directory = CreateTemporaryArtifactDirectory();
        try {
            string jsonPath = Path.Combine(directory, "20260322-140000000-sample.json");
            string summaryPath = Path.Combine(directory, "20260322-140000000-sample.browser-electron.summary.txt");
            File.WriteAllText(jsonPath, "{}");
            File.WriteAllText(summaryPath, "summary");

            string[] paths = DesktopManagerTestAppSession.GetArtifactSetPaths(jsonPath);

            CollectionAssert.AreEquivalent(new[] { jsonPath, summaryPath }, paths);
        } finally {
            TryDeleteDirectory(directory);
        }
    }

    [TestMethod]
    public void PruneHostedSessionArtifacts_RemovesOlderArtifactSetsAndKeepsNewest() {
        string directory = CreateTemporaryArtifactDirectory();
        try {
            string oldestJsonPath = CreateArtifactSet(directory, "20260322-140000000-oldest", "oldest");
            Thread.Sleep(20);
            string middleJsonPath = CreateArtifactSet(directory, "20260322-140000001-middle", "middle");
            Thread.Sleep(20);
            string newestJsonPath = CreateArtifactSet(directory, "20260322-140000002-newest", "newest");

            DesktopManagerTestAppSession.PruneHostedSessionArtifacts(directory, 2);

            Assert.IsFalse(File.Exists(oldestJsonPath));
            Assert.IsFalse(File.Exists(DesktopManagerTestAppSession.GetSummaryArtifactPath(oldestJsonPath, "oldest")));
            Assert.IsTrue(File.Exists(middleJsonPath));
            Assert.IsTrue(File.Exists(DesktopManagerTestAppSession.GetSummaryArtifactPath(middleJsonPath, "middle")));
            Assert.IsTrue(File.Exists(newestJsonPath));
            Assert.IsTrue(File.Exists(DesktopManagerTestAppSession.GetSummaryArtifactPath(newestJsonPath, "newest")));
        } finally {
            TryDeleteDirectory(directory);
        }
    }

    [TestMethod]
    public void PruneHostedSessionArtifacts_WithMixedSummaryNames_RemovesOnlyMatchingArtifactSet() {
        string directory = CreateTemporaryArtifactDirectory();
        try {
            string oldestJsonPath = CreateArtifactSet(directory, "20260322-140000000-oldest", "browser-electron");
            string oldestExtraSummaryPath = Path.Combine(directory, "20260322-140000000-oldest.mixed.summary.txt");
            File.WriteAllText(oldestExtraSummaryPath, "alternate summary");
            Thread.Sleep(20);
            string newestJsonPath = CreateArtifactSet(directory, "20260322-140000001-newest", "unknown");
            string unrelatedPath = Path.Combine(directory, "notes.txt");
            File.WriteAllText(unrelatedPath, "keep me");

            DesktopManagerTestAppSession.PruneHostedSessionArtifacts(directory, 1);

            Assert.IsFalse(File.Exists(oldestJsonPath));
            Assert.IsFalse(File.Exists(DesktopManagerTestAppSession.GetSummaryArtifactPath(oldestJsonPath, "browser-electron")));
            Assert.IsFalse(File.Exists(oldestExtraSummaryPath));
            Assert.IsTrue(File.Exists(newestJsonPath));
            Assert.IsTrue(File.Exists(DesktopManagerTestAppSession.GetSummaryArtifactPath(newestJsonPath, "unknown")));
            Assert.IsTrue(File.Exists(unrelatedPath));
        } finally {
            TryDeleteDirectory(directory);
        }
    }

    private static string CreateArtifactSet(string directory, string stem, string categoryHint) {
        string jsonPath = Path.Combine(directory, stem + ".json");
        File.WriteAllText(jsonPath, "{}");
        File.WriteAllText(DesktopManagerTestAppSession.GetSummaryArtifactPath(jsonPath, categoryHint), "summary");
        return jsonPath;
    }

    private static string CreateTemporaryArtifactDirectory() {
        string directory = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", "HostedSessionArtifacts", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void TryDeleteDirectory(string directory) {
        try {
            if (Directory.Exists(directory)) {
                Directory.Delete(directory, recursive: true);
            }
        } catch {
            // Ignore cleanup failures for temporary test files.
        }
    }
}
