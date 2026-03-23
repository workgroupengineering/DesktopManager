using System;

namespace DesktopManager.Tests;

internal static class HostedSessionDiagnosticFormatter {
    private const int PersistentExternalForegroundAbortThreshold = 2;
    private const int DefaultExternalForegroundAbortThreshold = 3;

    public static string Summarize(DesktopManagerTestAppStatus status) {
        if (status == null) {
            throw new ArgumentNullException(nameof(status));
        }

        HostedSessionExternalForegroundReport report = CreateExternalForegroundReport(status);
        if (report.HasExternalForeground) {
            return "External foreground interruption observed: " + report.Entry;
        }

        if (status.ForegroundHoldRecoveryCount > 0) {
            return "Foreground hold recovered focus " + status.ForegroundHoldRecoveryCount + " time(s) without a retained external foreground entry.";
        }

        if (status.ForegroundHoldRequestCount > 0) {
            return "No external foreground interruption recorded; hold requested " + status.ForegroundHoldRequestCount + " time(s).";
        }

        return "No hosted-session hold or external foreground interruption was recorded.";
    }

    internal static string BuildArtifactSummary(string reason, DesktopManagerTestAppStatus status, string retryHistorySummary, string policyReport) {
        return BuildArtifactSummary(
            reason,
            status,
            new HostedSessionRetryHistoryReport {
                CategoryHint = "unknown",
                Summary = retryHistorySummary,
                ExternalCount = 0,
                DistinctFingerprintCount = 0
            },
            policyReport);
    }

    internal static string BuildArtifactSummary(string reason, DesktopManagerTestAppStatus status, HostedSessionRetryHistoryReport retryHistoryReport, string policyReport) {
        if (string.IsNullOrWhiteSpace(reason)) {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(reason));
        }

        if (status == null) {
            throw new ArgumentNullException(nameof(status));
        }

        if (retryHistoryReport == null) {
            throw new ArgumentNullException(nameof(retryHistoryReport));
        }

        if (string.IsNullOrWhiteSpace(retryHistoryReport.Summary)) {
            retryHistoryReport = new HostedSessionRetryHistoryReport {
                CategoryHint = string.IsNullOrWhiteSpace(retryHistoryReport.CategoryHint) ? "none" : retryHistoryReport.CategoryHint,
                Summary = "No retry history was captured for this diagnostic.",
                ExternalCount = retryHistoryReport.ExternalCount,
                DistinctFingerprintCount = retryHistoryReport.DistinctFingerprintCount
            };
        }

        if (string.IsNullOrWhiteSpace(policyReport)) {
            policyReport = CreateExternalForegroundReport(status).ToPolicyReport();
        }

        return string.Join(
            Environment.NewLine,
            new[] {
                "Reason: " + reason,
                "Summary: " + Summarize(status),
                "RetryHistory: " + retryHistoryReport.Summary,
                "RetryHistoryReport: " + DescribeRetryHistory(retryHistoryReport),
                "Policy: " + policyReport,
                "LastObservedForegroundTitle: " + status.LastObservedForegroundTitle,
                "LastObservedForegroundClass: " + status.LastObservedForegroundClass,
                "ForegroundHoldRecoveryCount: " + status.ForegroundHoldRecoveryCount,
                "StatusText: " + status.StatusText
            });
    }

    internal static string DescribeRetryHistory(HostedSessionRetryHistoryReport report) {
        if (report == null) {
            throw new ArgumentNullException(nameof(report));
        }

        return
            "category='" + report.CategoryHint + "', " +
            "externalCount=" + report.ExternalCount + ", " +
            "distinctFingerprintCount=" + report.DistinctFingerprintCount;
    }

    internal static string GetRetryHistoryCategoryHint(IReadOnlyList<HostedSessionExternalForegroundReport> reports) {
        return CreateRetryHistoryReport(reports).CategoryHint;
    }

    internal static string SummarizeRetryHistory(IReadOnlyList<HostedSessionExternalForegroundReport> reports) {
        return CreateRetryHistoryReport(reports).Summary;
    }

    internal static HostedSessionRetryHistoryReport CreateRetryHistoryReport(IReadOnlyList<HostedSessionExternalForegroundReport> reports) {
        if (reports == null) {
            throw new ArgumentNullException(nameof(reports));
        }

        var externalReports = new List<HostedSessionExternalForegroundReport>();
        foreach (HostedSessionExternalForegroundReport report in reports) {
            if (report != null && report.HasExternalForeground) {
                externalReports.Add(report);
            }
        }

        if (externalReports.Count == 0) {
            return HostedSessionRetryHistoryReport.None;
        }

        var categoryCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var fingerprintCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (HostedSessionExternalForegroundReport report in externalReports) {
            if (!categoryCounts.ContainsKey(report.Category)) {
                categoryCounts[report.Category] = 0;
            }

            categoryCounts[report.Category]++;

            if (!fingerprintCounts.ContainsKey(report.Fingerprint)) {
                fingerprintCounts[report.Fingerprint] = 0;
            }

            fingerprintCounts[report.Fingerprint]++;
        }

        if (fingerprintCounts.Count == 1) {
            HostedSessionExternalForegroundReport firstReport = externalReports[0];
            return new HostedSessionRetryHistoryReport {
                CategoryHint = firstReport.Category,
                ExternalCount = externalReports.Count,
                DistinctFingerprintCount = 1,
                Summary =
                    "Repeated " +
                    firstReport.Category +
                    " foreground interruption observed " +
                    externalReports.Count +
                    " time(s): " +
                    firstReport.Fingerprint
            };
        }

        if (categoryCounts.Count == 1) {
            HostedSessionExternalForegroundReport firstReport = externalReports[0];
            return new HostedSessionRetryHistoryReport {
                CategoryHint = firstReport.Category,
                ExternalCount = externalReports.Count,
                DistinctFingerprintCount = fingerprintCounts.Count,
                Summary =
                    "Multiple " +
                    firstReport.Category +
                    " foreground interruptions observed " +
                    externalReports.Count +
                    " time(s) across " +
                    fingerprintCounts.Count +
                    " distinct fingerprints."
            };
        }

        var parts = new List<string>();
        foreach (KeyValuePair<string, int> entry in categoryCounts) {
            parts.Add(entry.Key + " x" + entry.Value);
        }

        return new HostedSessionRetryHistoryReport {
            CategoryHint = "mixed",
            ExternalCount = externalReports.Count,
            DistinctFingerprintCount = fingerprintCounts.Count,
            Summary = "Mixed foreground interruptions observed across retries: " + string.Join(", ", parts) + "."
        };
    }

    internal static int GetRepeatedExternalForegroundAbortThreshold(DesktopManagerTestAppStatus status) {
        if (status == null) {
            throw new ArgumentNullException(nameof(status));
        }

        return CreateExternalForegroundReport(status).AbortThreshold;
    }

    internal static string GetLatestExternalForegroundEntry(DesktopManagerTestAppStatus status) {
        if (status == null) {
            throw new ArgumentNullException(nameof(status));
        }

        for (int index = status.ForegroundHistory.Count - 1; index >= 0; index--) {
            string entry = status.ForegroundHistory[index];
            if (!entry.Contains("[foreground]", StringComparison.Ordinal)) {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(status.WindowTitle) && entry.Contains(status.WindowTitle, StringComparison.Ordinal)) {
                continue;
            }

            return entry;
        }

        return string.Empty;
    }

    internal static string GetLatestExternalForegroundFingerprint(DesktopManagerTestAppStatus status) {
        return CreateExternalForegroundReport(status).Fingerprint;
    }

    internal static HostedSessionExternalForegroundReport CreateExternalForegroundReport(DesktopManagerTestAppStatus status) {
        if (status == null) {
            throw new ArgumentNullException(nameof(status));
        }

        string entry = GetLatestExternalForegroundEntry(status);
        if (string.IsNullOrWhiteSpace(entry)) {
            return HostedSessionExternalForegroundReport.None;
        }

        string title = ExtractQuotedValue(entry, 0);
        string className = ExtractNamedValue(entry, "class='");
        string fingerprint = string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(className)
            ? entry
            : "title='" + title + "' class='" + className + "'";
        string category = GetExternalForegroundCategory(title, className);
        int abortThreshold = string.Equals(category, "browser-electron", StringComparison.Ordinal)
            ? PersistentExternalForegroundAbortThreshold
            : DefaultExternalForegroundAbortThreshold;

        return new HostedSessionExternalForegroundReport {
            Category = category,
            AbortThreshold = abortThreshold,
            Summary = "External foreground interruption observed: " + entry,
            Fingerprint = fingerprint,
            Entry = entry
        };
    }

    private static string ExtractQuotedValue(string entry, int startIndex) {
        int openQuoteIndex = entry.IndexOf('\'', startIndex);
        if (openQuoteIndex < 0) {
            return string.Empty;
        }

        int closeQuoteIndex = entry.IndexOf('\'', openQuoteIndex + 1);
        if (closeQuoteIndex < 0) {
            return string.Empty;
        }

        return entry.Substring(openQuoteIndex + 1, closeQuoteIndex - openQuoteIndex - 1);
    }

    private static string ExtractNamedValue(string entry, string marker) {
        int markerIndex = entry.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0) {
            return string.Empty;
        }

        return ExtractQuotedValue(entry, markerIndex + marker.Length - 1);
    }

    private static string GetExternalForegroundCategory(string title, string className) {
        if (className.Equals("Chrome_WidgetWin_1", StringComparison.OrdinalIgnoreCase) ||
            className.Equals("Chrome_RenderWidgetHostHWND", StringComparison.OrdinalIgnoreCase) ||
            className.Equals("MozillaWindowClass", StringComparison.OrdinalIgnoreCase) ||
            className.Equals("ApplicationFrameWindow", StringComparison.OrdinalIgnoreCase)) {
            return "browser-electron";
        }

        if (title.Contains("Microsoft Edge", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("Codex", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("ChatGPT", StringComparison.OrdinalIgnoreCase)) {
            return "browser-electron";
        }

        return "unknown";
    }
}
