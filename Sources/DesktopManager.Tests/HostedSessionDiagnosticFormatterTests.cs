namespace DesktopManager.Tests;

[TestClass]
public class HostedSessionDiagnosticFormatterTests {
    [TestMethod]
    public void Summarize_WithExternalForegroundHistory_ReturnsExternalInterruptionSummary() {
        var status = new DesktopManagerTestAppStatus {
            WindowTitle = "DesktopManager-TestApp-hosted-symbol-matrix-abc",
            ForegroundHistory = new List<string> {
                "2026-03-22T13:55:00.0000000Z [foreground] 0x123 'DesktopManager-TestApp-hosted-symbol-matrix-abc' class='WindowsForms10.Window.8.app.0.bb8560_r3_ad1'",
                "2026-03-22T13:55:01.0000000Z [foreground] 0x999 'Microsoft Edge' class='Chrome_WidgetWin_1'"
            }
        };

        string summary = HostedSessionDiagnosticFormatter.Summarize(status);

        StringAssert.Contains(summary, "External foreground interruption observed:");
        StringAssert.Contains(summary, "Microsoft Edge");
    }

    [TestMethod]
    public void Summarize_WithoutExternalForegroundButWithRecoveries_ReturnsRecoverySummary() {
        var status = new DesktopManagerTestAppStatus {
            WindowTitle = "DesktopManager-TestApp-hosted-symbol-matrix-abc",
            ForegroundHoldRecoveryCount = 3,
            ForegroundHistory = new List<string> {
                "2026-03-22T13:55:00.0000000Z [foreground] 0x123 'DesktopManager-TestApp-hosted-symbol-matrix-abc' class='WindowsForms10.Window.8.app.0.bb8560_r3_ad1'"
            }
        };

        string summary = HostedSessionDiagnosticFormatter.Summarize(status);

        StringAssert.Contains(summary, "recovered focus 3 time(s)");
    }

    [TestMethod]
    public void Summarize_WithoutExternalForegroundOrRecoveries_ReturnsHoldSummary() {
        var status = new DesktopManagerTestAppStatus {
            WindowTitle = "DesktopManager-TestApp-hosted-symbol-matrix-abc",
            ForegroundHoldRequestCount = 1,
            ForegroundHistory = new List<string> {
                "2026-03-22T13:55:00.0000000Z [foreground] 0x123 'DesktopManager-TestApp-hosted-symbol-matrix-abc' class='WindowsForms10.Window.8.app.0.bb8560_r3_ad1'"
            }
        };

        string summary = HostedSessionDiagnosticFormatter.Summarize(status);

        StringAssert.Contains(summary, "No external foreground interruption recorded");
    }

    [TestMethod]
    public void GetLatestExternalForegroundFingerprint_WithRepeatedExternalCulprit_IgnoresTimestampAndHandleNoise() {
        var status = new DesktopManagerTestAppStatus {
            WindowTitle = "DesktopManager-TestApp-hosted-symbol-matrix-abc",
            ForegroundHistory = new List<string> {
                "2026-03-22T13:55:00.0000000Z [foreground] 0x123 'DesktopManager-TestApp-hosted-symbol-matrix-abc' class='WindowsForms10.Window.8.app.0.bb8560_r3_ad1'",
                "2026-03-22T13:55:01.0000000Z [foreground] 0x999 'Microsoft Edge' class='Chrome_WidgetWin_1'",
                "2026-03-22T13:55:02.0000000Z [foreground] 0xABC 'Microsoft Edge' class='Chrome_WidgetWin_1'"
            }
        };

        string fingerprint = HostedSessionDiagnosticFormatter.GetLatestExternalForegroundFingerprint(status);

        Assert.AreEqual("title='Microsoft Edge' class='Chrome_WidgetWin_1'", fingerprint);
    }

    [TestMethod]
    public void GetLatestExternalForegroundFingerprint_WithoutExternalForeground_ReturnsEmptyString() {
        var status = new DesktopManagerTestAppStatus {
            WindowTitle = "DesktopManager-TestApp-hosted-symbol-matrix-abc",
            ForegroundHistory = new List<string> {
                "2026-03-22T13:55:00.0000000Z [foreground] 0x123 'DesktopManager-TestApp-hosted-symbol-matrix-abc' class='WindowsForms10.Window.8.app.0.bb8560_r3_ad1'"
            }
        };

        string fingerprint = HostedSessionDiagnosticFormatter.GetLatestExternalForegroundFingerprint(status);

        Assert.AreEqual(string.Empty, fingerprint);
    }

    [TestMethod]
    public void GetRepeatedExternalForegroundAbortThreshold_WithBrowserLikeExternalCulprit_ReturnsShortThreshold() {
        var status = new DesktopManagerTestAppStatus {
            WindowTitle = "DesktopManager-TestApp-hosted-symbol-matrix-abc",
            ForegroundHistory = new List<string> {
                "2026-03-22T13:55:01.0000000Z [foreground] 0x999 'Microsoft Edge' class='Chrome_WidgetWin_1'"
            }
        };

        int threshold = HostedSessionDiagnosticFormatter.GetRepeatedExternalForegroundAbortThreshold(status);

        Assert.AreEqual(2, threshold);
    }

    [TestMethod]
    public void GetRepeatedExternalForegroundAbortThreshold_WithUnknownExternalCulprit_ReturnsDefaultThreshold() {
        var status = new DesktopManagerTestAppStatus {
            WindowTitle = "DesktopManager-TestApp-hosted-symbol-matrix-abc",
            ForegroundHistory = new List<string> {
                "2026-03-22T13:55:01.0000000Z [foreground] 0x999 'Untitled - Notepad' class='Notepad'"
            }
        };

        int threshold = HostedSessionDiagnosticFormatter.GetRepeatedExternalForegroundAbortThreshold(status);

        Assert.AreEqual(3, threshold);
    }

    [TestMethod]
    public void CreateExternalForegroundReport_WithBrowserLikeCulprit_ReportsBrowserElectronCategory() {
        var status = new DesktopManagerTestAppStatus {
            WindowTitle = "DesktopManager-TestApp-hosted-symbol-matrix-abc",
            ForegroundHistory = new List<string> {
                "2026-03-22T13:55:01.0000000Z [foreground] 0x999 'Microsoft Edge' class='Chrome_WidgetWin_1'"
            }
        };

        HostedSessionExternalForegroundReport report = HostedSessionDiagnosticFormatter.CreateExternalForegroundReport(status);

        Assert.IsTrue(report.HasExternalForeground);
        Assert.AreEqual("browser-electron", report.Category);
        Assert.AreEqual(2, report.AbortThreshold);
        StringAssert.Contains(report.ToPolicyReport(), "category='browser-electron'");
    }

    [TestMethod]
    public void CreateExternalForegroundReport_WithoutExternalForeground_ReturnsNoneCategory() {
        var status = new DesktopManagerTestAppStatus {
            WindowTitle = "DesktopManager-TestApp-hosted-symbol-matrix-abc",
            ForegroundHistory = new List<string> {
                "2026-03-22T13:55:00.0000000Z [foreground] 0x123 'DesktopManager-TestApp-hosted-symbol-matrix-abc' class='WindowsForms10.Window.8.app.0.bb8560_r3_ad1'"
            }
        };

        HostedSessionExternalForegroundReport report = HostedSessionDiagnosticFormatter.CreateExternalForegroundReport(status);

        Assert.IsFalse(report.HasExternalForeground);
        Assert.AreEqual("none", report.Category);
        Assert.AreEqual(3, report.AbortThreshold);
    }

    [TestMethod]
    public void SummarizeRetryHistory_WithRepeatedSameFingerprint_ReturnsRepeatedSummary() {
        var reports = new List<HostedSessionExternalForegroundReport> {
            new HostedSessionExternalForegroundReport {
                Category = "browser-electron",
                AbortThreshold = 2,
                Summary = "External foreground interruption observed: Edge",
                Fingerprint = "title='Microsoft Edge' class='Chrome_WidgetWin_1'",
                Entry = "entry-1"
            },
            new HostedSessionExternalForegroundReport {
                Category = "browser-electron",
                AbortThreshold = 2,
                Summary = "External foreground interruption observed: Edge",
                Fingerprint = "title='Microsoft Edge' class='Chrome_WidgetWin_1'",
                Entry = "entry-2"
            }
        };

        string summary = HostedSessionDiagnosticFormatter.SummarizeRetryHistory(reports);

        StringAssert.Contains(summary, "Repeated browser-electron foreground interruption observed 2 time(s)");
        StringAssert.Contains(summary, "Microsoft Edge");
    }

    [TestMethod]
    public void SummarizeRetryHistory_WithMixedCategories_ReturnsMixedSummary() {
        var reports = new List<HostedSessionExternalForegroundReport> {
            new HostedSessionExternalForegroundReport {
                Category = "browser-electron",
                AbortThreshold = 2,
                Summary = "External foreground interruption observed: Edge",
                Fingerprint = "title='Microsoft Edge' class='Chrome_WidgetWin_1'",
                Entry = "entry-1"
            },
            new HostedSessionExternalForegroundReport {
                Category = "unknown",
                AbortThreshold = 3,
                Summary = "External foreground interruption observed: Notepad",
                Fingerprint = "title='Untitled - Notepad' class='Notepad'",
                Entry = "entry-2"
            }
        };

        string summary = HostedSessionDiagnosticFormatter.SummarizeRetryHistory(reports);

        StringAssert.Contains(summary, "Mixed foreground interruptions observed across retries");
        StringAssert.Contains(summary, "browser-electron x1");
        StringAssert.Contains(summary, "unknown x1");
    }

    [TestMethod]
    public void BuildArtifactSummary_IncludesReasonRetryHistoryAndPolicy() {
        var status = new DesktopManagerTestAppStatus {
            LastObservedForegroundTitle = "Microsoft Edge",
            LastObservedForegroundClass = "Chrome_WidgetWin_1",
            ForegroundHoldRecoveryCount = 2,
            StatusText = "waiting"
        };

        string summary = HostedSessionDiagnosticFormatter.BuildArtifactSummary(
            "Hosted-session typing inconclusive after foreground loss",
            status,
            "Repeated browser-electron foreground interruption observed 2 time(s): title='Microsoft Edge' class='Chrome_WidgetWin_1'",
            "category='browser-electron', abortThreshold=2, fingerprint='title=''Microsoft Edge'' class=''Chrome_WidgetWin_1'''");

        StringAssert.Contains(summary, "Reason: Hosted-session typing inconclusive after foreground loss");
        StringAssert.Contains(summary, "RetryHistory: Repeated browser-electron foreground interruption observed 2 time(s)");
        StringAssert.Contains(summary, "Policy: category='browser-electron'");
        StringAssert.Contains(summary, "LastObservedForegroundTitle: Microsoft Edge");
    }

    [TestMethod]
    public void BuildArtifactSummary_WithRetryHistoryReport_IncludesRetryHistoryReportLine() {
        var status = new DesktopManagerTestAppStatus {
            LastObservedForegroundTitle = "Microsoft Edge",
            LastObservedForegroundClass = "Chrome_WidgetWin_1",
            ForegroundHoldRecoveryCount = 2,
            StatusText = "waiting"
        };
        var retryHistoryReport = new HostedSessionRetryHistoryReport {
            CategoryHint = "browser-electron",
            Summary = "Repeated browser-electron foreground interruption observed 2 time(s): title='Microsoft Edge' class='Chrome_WidgetWin_1'",
            ExternalCount = 2,
            DistinctFingerprintCount = 1
        };

        string summary = HostedSessionDiagnosticFormatter.BuildArtifactSummary(
            "Hosted-session typing inconclusive after foreground loss",
            status,
            retryHistoryReport,
            "category='browser-electron', abortThreshold=2, fingerprint='title=''Microsoft Edge'' class=''Chrome_WidgetWin_1'''");

        StringAssert.Contains(summary, "RetryHistoryReport: category='browser-electron', externalCount=2, distinctFingerprintCount=1");
    }

    [TestMethod]
    public void GetRetryHistoryCategoryHint_WithNoExternalReports_ReturnsNone() {
        string hint = HostedSessionDiagnosticFormatter.GetRetryHistoryCategoryHint(new[] {
            HostedSessionExternalForegroundReport.None
        });

        Assert.AreEqual("none", hint);
    }

    [TestMethod]
    public void GetRetryHistoryCategoryHint_WithSameCategoryReports_ReturnsCategory() {
        var reports = new[] {
            new HostedSessionExternalForegroundReport {
                Category = "browser-electron",
                AbortThreshold = 2,
                Summary = "External foreground interruption observed: Edge",
                Fingerprint = "title='Microsoft Edge' class='Chrome_WidgetWin_1'",
                Entry = "entry-1"
            },
            new HostedSessionExternalForegroundReport {
                Category = "browser-electron",
                AbortThreshold = 2,
                Summary = "External foreground interruption observed: ChatGPT",
                Fingerprint = "title='ChatGPT' class='Chrome_WidgetWin_1'",
                Entry = "entry-2"
            }
        };

        string hint = HostedSessionDiagnosticFormatter.GetRetryHistoryCategoryHint(reports);

        Assert.AreEqual("browser-electron", hint);
    }

    [TestMethod]
    public void GetRetryHistoryCategoryHint_WithMixedCategories_ReturnsMixed() {
        var reports = new[] {
            new HostedSessionExternalForegroundReport {
                Category = "browser-electron",
                AbortThreshold = 2,
                Summary = "External foreground interruption observed: Edge",
                Fingerprint = "title='Microsoft Edge' class='Chrome_WidgetWin_1'",
                Entry = "entry-1"
            },
            new HostedSessionExternalForegroundReport {
                Category = "unknown",
                AbortThreshold = 3,
                Summary = "External foreground interruption observed: Notepad",
                Fingerprint = "title='Untitled - Notepad' class='Notepad'",
                Entry = "entry-2"
            }
        };

        string hint = HostedSessionDiagnosticFormatter.GetRetryHistoryCategoryHint(reports);

        Assert.AreEqual("mixed", hint);
    }

    [TestMethod]
    public void DescribeRetryHistory_WithReport_ReturnsStructuredCounts() {
        var report = new HostedSessionRetryHistoryReport {
            CategoryHint = "mixed",
            Summary = "Mixed foreground interruptions observed across retries: browser-electron x1, unknown x1.",
            ExternalCount = 2,
            DistinctFingerprintCount = 2
        };

        string description = HostedSessionDiagnosticFormatter.DescribeRetryHistory(report);

        Assert.AreEqual("category='mixed', externalCount=2, distinctFingerprintCount=2", description);
    }

    [TestMethod]
    public void CreateRetryHistoryReport_WithNoisyInterleavedNoneEntries_IgnoresNonExternalReports() {
        var reports = new[] {
            HostedSessionExternalForegroundReport.None,
            new HostedSessionExternalForegroundReport {
                Category = "browser-electron",
                AbortThreshold = 2,
                Summary = "External foreground interruption observed: Edge",
                Fingerprint = "title='Microsoft Edge' class='Chrome_WidgetWin_1'",
                Entry = "entry-1"
            },
            HostedSessionExternalForegroundReport.None,
            new HostedSessionExternalForegroundReport {
                Category = "browser-electron",
                AbortThreshold = 2,
                Summary = "External foreground interruption observed: Edge",
                Fingerprint = "title='Microsoft Edge' class='Chrome_WidgetWin_1'",
                Entry = "entry-2"
            }
        };

        HostedSessionRetryHistoryReport report = HostedSessionDiagnosticFormatter.CreateRetryHistoryReport(reports);

        Assert.AreEqual("browser-electron", report.CategoryHint);
        Assert.AreEqual(2, report.ExternalCount);
        Assert.AreEqual(1, report.DistinctFingerprintCount);
        StringAssert.Contains(report.Summary, "Repeated browser-electron foreground interruption observed 2 time(s)");
    }

    [TestMethod]
    public void CreateRetryHistoryReport_WithSameCategoryDifferentFingerprints_ReturnsSameCategoryHint() {
        var reports = new[] {
            new HostedSessionExternalForegroundReport {
                Category = "browser-electron",
                AbortThreshold = 2,
                Summary = "External foreground interruption observed: Edge",
                Fingerprint = "title='Microsoft Edge' class='Chrome_WidgetWin_1'",
                Entry = "entry-1"
            },
            new HostedSessionExternalForegroundReport {
                Category = "browser-electron",
                AbortThreshold = 2,
                Summary = "External foreground interruption observed: Codex",
                Fingerprint = "title='Codex' class='Chrome_WidgetWin_1'",
                Entry = "entry-2"
            }
        };

        HostedSessionRetryHistoryReport report = HostedSessionDiagnosticFormatter.CreateRetryHistoryReport(reports);

        Assert.AreEqual("browser-electron", report.CategoryHint);
        Assert.AreEqual(2, report.ExternalCount);
        Assert.AreEqual(2, report.DistinctFingerprintCount);
        StringAssert.Contains(report.Summary, "Multiple browser-electron foreground interruptions observed 2 time(s) across 2 distinct fingerprints.");
    }
}
