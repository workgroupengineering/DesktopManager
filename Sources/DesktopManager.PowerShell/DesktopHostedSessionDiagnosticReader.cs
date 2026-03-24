using System.IO;
using System.Text.Json;

#nullable enable

namespace DesktopManager.PowerShell;

internal static class DesktopHostedSessionDiagnosticReader {
    public static DesktopHostedSessionDiagnosticRecord Load(string artifactPath) {
        if (string.IsNullOrWhiteSpace(artifactPath)) {
            throw new ArgumentException("ArtifactPath cannot be null or empty.", nameof(artifactPath));
        }

        if (!File.Exists(artifactPath)) {
            throw new FileNotFoundException("Hosted-session diagnostic artifact was not found.", artifactPath);
        }

        DesktopHostedSessionDiagnosticArtifact artifact = LoadArtifact(artifactPath);
        string? summaryPath = FindPreferredSummaryArtifactPath(artifactPath);
        string summaryText = !string.IsNullOrWhiteSpace(summaryPath) && File.Exists(summaryPath)
            ? File.ReadAllText(summaryPath)
            : BuildSummaryText(artifact);

        return new DesktopHostedSessionDiagnosticRecord {
            ArtifactPath = artifactPath,
            SummaryPath = summaryPath ?? string.Empty,
            SummaryText = summaryText,
            Reason = artifact.Reason,
            CreatedUtc = artifact.CreatedUtc,
            RetryHistoryCategory = artifact.RetryHistoryReport.CategoryHint,
            RetryHistorySummary = artifact.RetryHistoryReport.Summary,
            RetryHistoryExternalCount = artifact.RetryHistoryReport.ExternalCount,
            RetryHistoryDistinctFingerprintCount = artifact.RetryHistoryReport.DistinctFingerprintCount,
            PolicyReport = artifact.PolicyReport,
            WindowTitle = artifact.Status.WindowTitle,
            StatusText = artifact.Status.StatusText
        };
    }

    public static DesktopHostedSessionDiagnosticRecord LoadLatest(string artifactDirectory) {
        return Load(FindLatestArtifactPath(artifactDirectory));
    }

    public static string FindLatestArtifactPath(string artifactDirectory) {
        if (string.IsNullOrWhiteSpace(artifactDirectory)) {
            throw new ArgumentException("ArtifactDirectory cannot be null or empty.", nameof(artifactDirectory));
        }

        if (!Directory.Exists(artifactDirectory)) {
            throw new DirectoryNotFoundException("Hosted-session diagnostic artifact directory was not found.");
        }

        string[] artifactPaths = Directory.GetFiles(artifactDirectory, "*.json", SearchOption.TopDirectoryOnly);
        if (artifactPaths.Length == 0) {
            throw new FileNotFoundException("No hosted-session diagnostic artifacts were found.", artifactDirectory);
        }

        Array.Sort(artifactPaths, CompareArtifactPathsByLastWriteDescending);
        return artifactPaths[0];
    }

    public static string GetHostedSessionArtifactDirectory(string repositoryRoot) {
        if (string.IsNullOrWhiteSpace(repositoryRoot)) {
            throw new ArgumentException("RepositoryRoot cannot be null or empty.", nameof(repositoryRoot));
        }

        return Path.Combine(repositoryRoot, "Artifacts", "HostedSessionTyping");
    }

    public static bool TryFindRepositoryRoot(string startDirectory, out string repositoryRoot) {
        repositoryRoot = string.Empty;
        if (string.IsNullOrWhiteSpace(startDirectory)) {
            return false;
        }

        DirectoryInfo? current = new(startDirectory);
        while (current != null) {
            if (File.Exists(Path.Combine(current.FullName, "DesktopManager.sln")) ||
                Directory.Exists(Path.Combine(current.FullName, "Artifacts", "HostedSessionTyping"))) {
                repositoryRoot = current.FullName;
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    public static bool TryLoadLatestFromRepositoryRoot(string repositoryRoot, out DesktopHostedSessionDiagnosticRecord record, out string error) {
        record = new DesktopHostedSessionDiagnosticRecord();
        error = string.Empty;

        try {
            record = LoadLatest(GetHostedSessionArtifactDirectory(repositoryRoot));
            return true;
        } catch (Exception exception) when (
            exception is ArgumentException ||
            exception is DirectoryNotFoundException ||
            exception is FileNotFoundException ||
            exception is IOException ||
            exception is InvalidOperationException ||
            exception is JsonException) {
            error = exception.Message;
            return false;
        }
    }

    private static DesktopHostedSessionDiagnosticArtifact LoadArtifact(string artifactPath) {
        string json = File.ReadAllText(artifactPath);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        if (root.TryGetProperty("Status", out _)) {
            DesktopHostedSessionDiagnosticArtifact? artifact = JsonSerializer.Deserialize<DesktopHostedSessionDiagnosticArtifact>(json);
            if (artifact == null) {
                throw new InvalidOperationException("Hosted-session diagnostic artifact could not be deserialized.");
            }

            return Normalize(artifact);
        }

        if (root.TryGetProperty("WindowTitle", out _)) {
            DesktopHostedSessionStatus? status = JsonSerializer.Deserialize<DesktopHostedSessionStatus>(json);
            if (status == null) {
                throw new InvalidOperationException("Hosted-session legacy diagnostic artifact could not be deserialized.");
            }

            return CreateLegacyArtifact(status);
        }

        throw new InvalidOperationException("Hosted-session diagnostic artifact could not be deserialized.");
    }

    private static DesktopHostedSessionDiagnosticArtifact Normalize(DesktopHostedSessionDiagnosticArtifact artifact) {
        artifact.Reason ??= string.Empty;
        artifact.CreatedUtc ??= string.Empty;
        artifact.Summary ??= string.Empty;
        artifact.PolicyReport ??= string.Empty;
        artifact.RetryHistoryReport ??= DesktopHostedSessionRetryHistoryReport.None;
        artifact.RetryHistoryReport.CategoryHint ??= string.Empty;
        artifact.RetryHistoryReport.Summary ??= string.Empty;
        artifact.Status ??= new DesktopHostedSessionStatus();
        artifact.Status.WindowTitle ??= string.Empty;
        artifact.Status.LastObservedForegroundTitle ??= string.Empty;
        artifact.Status.LastObservedForegroundClass ??= string.Empty;
        artifact.Status.StatusText ??= string.Empty;
        artifact.Status.ForegroundHistory ??= [];
        return artifact;
    }

    private static DesktopHostedSessionDiagnosticArtifact CreateLegacyArtifact(DesktopHostedSessionStatus status) {
        string categoryHint;
        if (!string.IsNullOrWhiteSpace(status.LastObservedForegroundClass) &&
            (status.LastObservedForegroundClass.Contains("Chrome_WidgetWin_1", StringComparison.OrdinalIgnoreCase) ||
             status.LastObservedForegroundClass.Contains("Chrome_RenderWidgetHostHWND", StringComparison.OrdinalIgnoreCase) ||
             status.LastObservedForegroundClass.Contains("MozillaWindowClass", StringComparison.OrdinalIgnoreCase) ||
             status.LastObservedForegroundClass.Contains("ApplicationFrameWindow", StringComparison.OrdinalIgnoreCase))) {
            categoryHint = "browser-electron";
        } else if (!string.IsNullOrWhiteSpace(status.LastObservedForegroundTitle)) {
            categoryHint = "unknown";
        } else {
            categoryHint = "none";
        }

        return Normalize(new DesktopHostedSessionDiagnosticArtifact {
            FormatVersion = 0,
            Reason = "Legacy hosted-session diagnostic artifact",
            CreatedUtc = string.Empty,
            Summary = BuildSummaryText(status.WindowTitle, status.StatusText, categoryHint, 0, 0, $"category='{categoryHint}'"),
            PolicyReport = $"category='{categoryHint}'",
            RetryHistoryReport = new DesktopHostedSessionRetryHistoryReport {
                CategoryHint = categoryHint,
                Summary = categoryHint == "none" ? "no retained external foreground interruption" : $"external foreground interruption observed ({categoryHint})",
                ExternalCount = categoryHint == "none" ? 0 : 1,
                DistinctFingerprintCount = categoryHint == "none" ? 0 : 1
            },
            Status = status
        });
    }

    private static string BuildSummaryText(DesktopHostedSessionDiagnosticArtifact artifact) {
        if (!string.IsNullOrWhiteSpace(artifact.Summary)) {
            return artifact.Summary;
        }

        return BuildSummaryText(
            artifact.Status.WindowTitle,
            artifact.Status.StatusText,
            artifact.RetryHistoryReport.CategoryHint,
            artifact.RetryHistoryReport.ExternalCount,
            artifact.RetryHistoryReport.DistinctFingerprintCount,
            artifact.PolicyReport);
    }

    private static string BuildSummaryText(string windowTitle, string statusText, string categoryHint, int externalCount, int distinctFingerprintCount, string policyReport) {
        return
            "category='" + categoryHint + "', " +
            "externalCount=" + externalCount + ", " +
            "distinctFingerprintCount=" + distinctFingerprintCount + ", " +
            "policy='" + policyReport + "', " +
            "windowTitle='" + windowTitle + "', " +
            "statusText='" + statusText + "'";
    }

    private static string? FindPreferredSummaryArtifactPath(string artifactPath) {
        string directory = Path.GetDirectoryName(artifactPath) ?? string.Empty;
        string stem = Path.GetFileNameWithoutExtension(artifactPath);
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(stem) || !Directory.Exists(directory)) {
            return null;
        }

        string[] summaryPaths = Directory.GetFiles(directory, stem + "*.summary.txt", SearchOption.TopDirectoryOnly);
        if (summaryPaths.Length == 0) {
            return null;
        }

        Array.Sort(summaryPaths, CompareSummaryPaths);
        return summaryPaths[0];
    }

    private static int CompareSummaryPaths(string left, string right) {
        string leftFileName = Path.GetFileName(left);
        string rightFileName = Path.GetFileName(right);
        int leftSegmentCount = CountSegments(leftFileName);
        int rightSegmentCount = CountSegments(rightFileName);
        int comparison = leftSegmentCount.CompareTo(rightSegmentCount);
        if (comparison != 0) {
            return comparison;
        }

        return string.Compare(leftFileName, rightFileName, StringComparison.OrdinalIgnoreCase);
    }

    private static int CountSegments(string fileName) {
        if (string.IsNullOrWhiteSpace(fileName)) {
            return int.MaxValue;
        }

        int count = 0;
        foreach (char character in fileName) {
            if (character == '.') {
                count++;
            }
        }

        return count;
    }

    private static int CompareArtifactPathsByLastWriteDescending(string left, string right) {
        DateTime leftWriteTime = File.Exists(left) ? File.GetLastWriteTimeUtc(left) : DateTime.MinValue;
        DateTime rightWriteTime = File.Exists(right) ? File.GetLastWriteTimeUtc(right) : DateTime.MinValue;
        int comparison = rightWriteTime.CompareTo(leftWriteTime);
        if (comparison != 0) {
            return comparison;
        }

        return string.Compare(right, left, StringComparison.OrdinalIgnoreCase);
    }
}
