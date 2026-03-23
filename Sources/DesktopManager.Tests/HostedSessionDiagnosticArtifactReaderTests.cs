using System;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace DesktopManager.Tests;

[TestClass]
public class HostedSessionDiagnosticArtifactReaderTests {
    [TestMethod]
    public void Load_RoundTripsStructuredArtifact() {
        string artifactPath = CreateTemporaryArtifactPath();
        try {
            var artifact = new HostedSessionDiagnosticArtifact {
                Reason = "Hosted-session typing inconclusive after foreground loss",
                CreatedUtc = "2026-03-22T17:20:00.0000000Z",
                Summary = "summary",
                PolicyReport = "category='browser-electron', abortThreshold=2, fingerprint='title=''Microsoft Edge'' class=''Chrome_WidgetWin_1'''",
                RetryHistoryReport = new HostedSessionRetryHistoryReport {
                    CategoryHint = "browser-electron",
                    Summary = "Repeated browser-electron foreground interruption observed 2 time(s): title='Microsoft Edge' class='Chrome_WidgetWin_1'",
                    ExternalCount = 2,
                    DistinctFingerprintCount = 1
                },
                Status = new DesktopManagerTestAppStatus {
                    WindowTitle = "DesktopManager-TestApp-hosted-symbol-matrix-abc",
                    StatusText = "waiting"
                }
            };
            File.WriteAllText(artifactPath, JsonSerializer.Serialize(artifact));

            HostedSessionDiagnosticArtifact loaded = HostedSessionDiagnosticArtifactReader.Load(artifactPath);

            Assert.AreEqual("Hosted-session typing inconclusive after foreground loss", loaded.Reason);
            Assert.AreEqual("browser-electron", loaded.RetryHistoryReport.CategoryHint);
            Assert.AreEqual(2, loaded.RetryHistoryReport.ExternalCount);
            Assert.AreEqual("DesktopManager-TestApp-hosted-symbol-matrix-abc", loaded.Status.WindowTitle);
        } finally {
            TryDeleteFile(artifactPath);
        }
    }

    [TestMethod]
    public void TryLoad_WithMalformedJson_ReturnsFalseAndError() {
        string artifactPath = CreateTemporaryArtifactPath();
        try {
            File.WriteAllText(artifactPath, "{ not-json");

            bool loaded = HostedSessionDiagnosticArtifactReader.TryLoad(artifactPath, out HostedSessionDiagnosticArtifact artifact, out string error);

            Assert.IsFalse(loaded);
            Assert.AreEqual(string.Empty, artifact.Reason);
            Assert.IsFalse(string.IsNullOrWhiteSpace(error));
        } finally {
            TryDeleteFile(artifactPath);
        }
    }

    [TestMethod]
    public void Summarize_IncludesStructuredRetryHistoryAndStatusFields() {
        var artifact = new HostedSessionDiagnosticArtifact {
            Reason = "Hosted-session typing inconclusive after foreground loss",
            PolicyReport = "category='mixed', abortThreshold=3, fingerprint=''",
            RetryHistoryReport = new HostedSessionRetryHistoryReport {
                CategoryHint = "mixed",
                Summary = "Mixed foreground interruptions observed across retries: browser-electron x1, unknown x1.",
                ExternalCount = 2,
                DistinctFingerprintCount = 2
            },
            Status = new DesktopManagerTestAppStatus {
                WindowTitle = "DesktopManager-TestApp-hosted-symbol-matrix-abc",
                StatusText = "waiting"
            }
        };

        string summary = HostedSessionDiagnosticArtifactReader.Summarize(artifact);

        StringAssert.Contains(summary, "reason='Hosted-session typing inconclusive after foreground loss'");
        StringAssert.Contains(summary, "category='mixed', externalCount=2, distinctFingerprintCount=2");
        StringAssert.Contains(summary, "windowTitle='DesktopManager-TestApp-hosted-symbol-matrix-abc'");
        StringAssert.Contains(summary, "statusText='waiting'");
    }

    [TestMethod]
    public void Load_WithLegacyRawStatusArtifact_ReturnsCompatibilityArtifact() {
        string artifactPath = CreateTemporaryArtifactPath();
        try {
            File.WriteAllText(artifactPath, JsonSerializer.Serialize(new DesktopManagerTestAppStatus {
                WindowTitle = "DesktopManager-TestApp-hosted-symbol-matrix-legacy",
                StatusText = "waiting",
                LastObservedForegroundTitle = "Microsoft Edge",
                LastObservedForegroundClass = "Chrome_WidgetWin_1",
                ForegroundHistory = new List<string> {
                    "2026-03-22T18:10:00.0000000Z [foreground] 0x999 'Microsoft Edge' class='Chrome_WidgetWin_1'"
                }
            }));

            HostedSessionDiagnosticArtifact artifact = HostedSessionDiagnosticArtifactReader.Load(artifactPath);

            Assert.AreEqual(0, artifact.FormatVersion);
            Assert.AreEqual("Legacy hosted-session diagnostic artifact", artifact.Reason);
            Assert.AreEqual("DesktopManager-TestApp-hosted-symbol-matrix-legacy", artifact.Status.WindowTitle);
            Assert.AreEqual("browser-electron", artifact.RetryHistoryReport.CategoryHint);
            StringAssert.Contains(artifact.PolicyReport, "category='browser-electron'");
        } finally {
            TryDeleteFile(artifactPath);
        }
    }

    [TestMethod]
    public void ReadSummary_WithCompanionSummaryPrefersSummaryFile() {
        string artifactPath = CreateTemporaryArtifactPath();
        try {
            File.WriteAllText(artifactPath, JsonSerializer.Serialize(new HostedSessionDiagnosticArtifact {
                Reason = "reason",
                Status = new DesktopManagerTestAppStatus {
                    WindowTitle = "window"
                }
            }));
            string summaryPath = DesktopManagerTestAppSession.GetSummaryArtifactPath(artifactPath, "browser-electron");
            File.WriteAllText(summaryPath, "summary-file-text");

            string summary = HostedSessionDiagnosticArtifactReader.ReadSummary(artifactPath);

            Assert.AreEqual("summary-file-text", summary);
        } finally {
            TryDeleteFile(artifactPath);
        }
    }

    [TestMethod]
    public void ReadSummary_WithoutCompanionSummaryFallsBackToArtifactSummary() {
        string artifactPath = CreateTemporaryArtifactPath();
        try {
            File.WriteAllText(artifactPath, JsonSerializer.Serialize(new HostedSessionDiagnosticArtifact {
                Reason = "Hosted-session typing inconclusive after foreground loss",
                PolicyReport = "category='mixed', abortThreshold=3, fingerprint=''",
                RetryHistoryReport = new HostedSessionRetryHistoryReport {
                    CategoryHint = "mixed",
                    Summary = "Mixed foreground interruptions observed across retries: browser-electron x1, unknown x1.",
                    ExternalCount = 2,
                    DistinctFingerprintCount = 2
                },
                Status = new DesktopManagerTestAppStatus {
                    WindowTitle = "DesktopManager-TestApp-hosted-symbol-matrix-abc",
                    StatusText = "waiting"
                }
            }));

            string summary = HostedSessionDiagnosticArtifactReader.ReadSummary(artifactPath);

            StringAssert.Contains(summary, "reason='Hosted-session typing inconclusive after foreground loss'");
            StringAssert.Contains(summary, "category='mixed', externalCount=2, distinctFingerprintCount=2");
        } finally {
            TryDeleteFile(artifactPath);
        }
    }

    [TestMethod]
    public void FindPreferredSummaryArtifactPath_WithMultipleCompanionFiles_PrefersSimplerName() {
        string artifactPath = CreateTemporaryArtifactPath();
        try {
            File.WriteAllText(artifactPath, "{}");
            string preferredSummaryPath = Path.ChangeExtension(artifactPath, ".summary.txt");
            string alternateSummaryPath = DesktopManagerTestAppSession.GetSummaryArtifactPath(artifactPath, "browser-electron");
            File.WriteAllText(preferredSummaryPath, "preferred");
            File.WriteAllText(alternateSummaryPath, "alternate");

            string? resolvedSummaryPath = HostedSessionDiagnosticArtifactReader.FindPreferredSummaryArtifactPath(artifactPath);

            Assert.AreEqual(preferredSummaryPath, resolvedSummaryPath);
        } finally {
            TryDeleteFile(artifactPath);
        }
    }

    [TestMethod]
    public void FindLatestArtifactPathOrDefault_WithMultipleArtifacts_ReturnsNewestJson() {
        string directory = CreateTemporaryArtifactDirectory();
        try {
            string oldestArtifactPath = Path.Combine(directory, "20260322-172000000-oldest.json");
            File.WriteAllText(oldestArtifactPath, "{}");
            Thread.Sleep(20);
            string newestArtifactPath = Path.Combine(directory, "20260322-172000001-newest.json");
            File.WriteAllText(newestArtifactPath, "{}");

            string? latestArtifactPath = HostedSessionDiagnosticArtifactReader.FindLatestArtifactPathOrDefault(directory);

            Assert.AreEqual(newestArtifactPath, latestArtifactPath);
        } finally {
            TryDeleteDirectory(directory);
        }
    }

    [TestMethod]
    public void TryReadLatestSummary_WithNewestSummaryArtifact_ReturnsArtifactAndSummary() {
        string directory = CreateTemporaryArtifactDirectory();
        try {
            string oldestArtifactPath = Path.Combine(directory, "20260322-172000000-oldest.json");
            File.WriteAllText(oldestArtifactPath, JsonSerializer.Serialize(new HostedSessionDiagnosticArtifact {
                Reason = "oldest"
            }));
            File.WriteAllText(DesktopManagerTestAppSession.GetSummaryArtifactPath(oldestArtifactPath, "unknown"), "oldest-summary");
            Thread.Sleep(20);

            string newestArtifactPath = Path.Combine(directory, "20260322-172000001-newest.json");
            File.WriteAllText(newestArtifactPath, JsonSerializer.Serialize(new HostedSessionDiagnosticArtifact {
                Reason = "newest"
            }));
            File.WriteAllText(DesktopManagerTestAppSession.GetSummaryArtifactPath(newestArtifactPath, "browser-electron"), "newest-summary");

            bool loaded = HostedSessionDiagnosticArtifactReader.TryReadLatestSummary(directory, out string artifactPath, out string summary, out string error);

            Assert.IsTrue(loaded);
            Assert.AreEqual(newestArtifactPath, artifactPath);
            Assert.AreEqual("newest-summary", summary);
            Assert.AreEqual(string.Empty, error);
        } finally {
            TryDeleteDirectory(directory);
        }
    }

    [TestMethod]
    public void TryReadLatestSummary_WithoutArtifacts_ReturnsFalseAndError() {
        string directory = CreateTemporaryArtifactDirectory();
        try {
            bool loaded = HostedSessionDiagnosticArtifactReader.TryReadLatestSummary(directory, out string artifactPath, out string summary, out string error);

            Assert.IsFalse(loaded);
            Assert.AreEqual(string.Empty, artifactPath);
            Assert.AreEqual(string.Empty, summary);
            Assert.IsFalse(string.IsNullOrWhiteSpace(error));
        } finally {
            TryDeleteDirectory(directory);
        }
    }

    [TestMethod]
    public void GetHostedSessionArtifactDirectory_FromRepositoryRoot_ReturnsExpectedFolder() {
        string directory = HostedSessionDiagnosticArtifactReader.GetHostedSessionArtifactDirectory(@"C:\Repo\DesktopManager");

        Assert.AreEqual(@"C:\Repo\DesktopManager\Artifacts\HostedSessionTyping", directory);
    }

    [TestMethod]
    public void TryReadLatestSummaryFromRepositoryRoot_UsesHostedSessionArtifactFolder() {
        string repositoryRoot = CreateTemporaryArtifactDirectory();
        string artifactDirectory = HostedSessionDiagnosticArtifactReader.GetHostedSessionArtifactDirectory(repositoryRoot);
        Directory.CreateDirectory(artifactDirectory);

        try {
            string artifactPath = Path.Combine(artifactDirectory, "20260322-172000001-newest.json");
            File.WriteAllText(artifactPath, JsonSerializer.Serialize(new HostedSessionDiagnosticArtifact {
                Reason = "newest"
            }));
            File.WriteAllText(DesktopManagerTestAppSession.GetSummaryArtifactPath(artifactPath, "browser-electron"), "newest-summary");

            bool loaded = HostedSessionDiagnosticArtifactReader.TryReadLatestSummaryFromRepositoryRoot(repositoryRoot, out string resolvedArtifactPath, out string summary, out string error);

            Assert.IsTrue(loaded);
            Assert.AreEqual(artifactPath, resolvedArtifactPath);
            Assert.AreEqual("newest-summary", summary);
            Assert.AreEqual(string.Empty, error);
        } finally {
            TryDeleteDirectory(repositoryRoot);
        }
    }

    [TestMethod]
    public void LoadLatest_WithMultipleArtifacts_ReturnsNewestArtifactObject() {
        string directory = CreateTemporaryArtifactDirectory();
        try {
            string oldestArtifactPath = Path.Combine(directory, "20260322-172000000-oldest.json");
            File.WriteAllText(oldestArtifactPath, JsonSerializer.Serialize(new HostedSessionDiagnosticArtifact {
                Reason = "oldest"
            }));
            Thread.Sleep(20);

            string newestArtifactPath = Path.Combine(directory, "20260322-172000001-newest.json");
            File.WriteAllText(newestArtifactPath, JsonSerializer.Serialize(new HostedSessionDiagnosticArtifact {
                Reason = "newest",
                Status = new DesktopManagerTestAppStatus {
                    WindowTitle = "DesktopManager-TestApp-hosted-symbol-matrix-newest"
                }
            }));

            HostedSessionDiagnosticArtifact artifact = HostedSessionDiagnosticArtifactReader.LoadLatest(directory);

            Assert.AreEqual("newest", artifact.Reason);
            Assert.AreEqual("DesktopManager-TestApp-hosted-symbol-matrix-newest", artifact.Status.WindowTitle);
        } finally {
            TryDeleteDirectory(directory);
        }
    }

    [TestMethod]
    public void TryLoadLatestFromRepositoryRoot_ReturnsArtifactAndPath() {
        string repositoryRoot = CreateTemporaryArtifactDirectory();
        string artifactDirectory = HostedSessionDiagnosticArtifactReader.GetHostedSessionArtifactDirectory(repositoryRoot);
        Directory.CreateDirectory(artifactDirectory);

        try {
            string artifactPath = Path.Combine(artifactDirectory, "20260322-172000001-newest.json");
            File.WriteAllText(artifactPath, JsonSerializer.Serialize(new HostedSessionDiagnosticArtifact {
                Reason = "newest",
                Status = new DesktopManagerTestAppStatus {
                    WindowTitle = "DesktopManager-TestApp-hosted-symbol-matrix-newest"
                }
            }));

            bool loaded = HostedSessionDiagnosticArtifactReader.TryLoadLatestFromRepositoryRoot(repositoryRoot, out HostedSessionDiagnosticArtifact artifact, out string resolvedArtifactPath, out string error);

            Assert.IsTrue(loaded);
            Assert.AreEqual(artifactPath, resolvedArtifactPath);
            Assert.AreEqual("newest", artifact.Reason);
            Assert.AreEqual("DesktopManager-TestApp-hosted-symbol-matrix-newest", artifact.Status.WindowTitle);
            Assert.AreEqual(string.Empty, error);
        } finally {
            TryDeleteDirectory(repositoryRoot);
        }
    }

    private static string CreateTemporaryArtifactPath() {
        return Path.Combine(CreateTemporaryArtifactDirectory(), "artifact.json");
    }

    private static string CreateTemporaryArtifactDirectory() {
        string directory = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", "HostedSessionArtifacts", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void TryDeleteFile(string path) {
        try {
            string? directory = Path.GetDirectoryName(path);
            if (File.Exists(path)) {
                File.Delete(path);
            }

            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory)) {
                Directory.Delete(directory, recursive: true);
            }
        } catch {
            // Ignore cleanup failures for temporary test files.
        }
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
