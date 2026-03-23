using System;
using System.IO;
using System.Text.Json;

namespace DesktopManager.Tests;

internal static class HostedSessionDiagnosticArtifactReader {
    public static HostedSessionDiagnosticArtifact Load(string artifactPath) {
        if (string.IsNullOrWhiteSpace(artifactPath)) {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(artifactPath));
        }

        if (!File.Exists(artifactPath)) {
            throw new FileNotFoundException("Hosted-session diagnostic artifact was not found.", artifactPath);
        }

        string json = File.ReadAllText(artifactPath);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        if (root.TryGetProperty("Status", out _)) {
            HostedSessionDiagnosticArtifact? artifact = JsonSerializer.Deserialize<HostedSessionDiagnosticArtifact>(json);
            if (artifact == null) {
                throw new InvalidOperationException("Hosted-session diagnostic artifact could not be deserialized.");
            }

            return Normalize(artifact);
        }

        if (root.TryGetProperty("WindowTitle", out _)) {
            DesktopManagerTestAppStatus? status = JsonSerializer.Deserialize<DesktopManagerTestAppStatus>(json);
            if (status == null) {
                throw new InvalidOperationException("Hosted-session legacy diagnostic artifact could not be deserialized.");
            }

            return CreateLegacyArtifact(status);
        }

        throw new InvalidOperationException("Hosted-session diagnostic artifact could not be deserialized.");
    }

    public static bool TryLoad(string artifactPath, out HostedSessionDiagnosticArtifact artifact, out string error) {
        artifact = new HostedSessionDiagnosticArtifact();
        error = string.Empty;

        try {
            artifact = Load(artifactPath);
            return true;
        } catch (Exception exception) when (
            exception is ArgumentException ||
            exception is FileNotFoundException ||
            exception is IOException ||
            exception is InvalidOperationException ||
            exception is JsonException) {
            error = exception.Message;
            return false;
        }
    }

    public static string Summarize(HostedSessionDiagnosticArtifact artifact) {
        if (artifact == null) {
            throw new ArgumentNullException(nameof(artifact));
        }

        HostedSessionRetryHistoryReport retryHistoryReport = artifact.RetryHistoryReport ?? HostedSessionRetryHistoryReport.None;
        DesktopManagerTestAppStatus status = artifact.Status ?? new DesktopManagerTestAppStatus();

        return
            "reason='" + artifact.Reason + "', " +
            HostedSessionDiagnosticFormatter.DescribeRetryHistory(retryHistoryReport) + ", " +
            "policy='" + artifact.PolicyReport + "', " +
            "windowTitle='" + status.WindowTitle + "', " +
            "statusText='" + status.StatusText + "'";
    }

    public static string ReadSummary(string artifactPath) {
        if (string.IsNullOrWhiteSpace(artifactPath)) {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(artifactPath));
        }

        string? summaryArtifactPath = FindPreferredSummaryArtifactPath(artifactPath);
        if (!string.IsNullOrWhiteSpace(summaryArtifactPath) && File.Exists(summaryArtifactPath)) {
            return File.ReadAllText(summaryArtifactPath);
        }

        return Summarize(Load(artifactPath));
    }

    public static bool TryReadSummary(string artifactPath, out string summary, out string error) {
        summary = string.Empty;
        error = string.Empty;

        try {
            summary = ReadSummary(artifactPath);
            return true;
        } catch (Exception exception) when (
            exception is ArgumentException ||
            exception is FileNotFoundException ||
            exception is IOException ||
            exception is InvalidOperationException ||
            exception is JsonException) {
            error = exception.Message;
            return false;
        }
    }

    public static string FindLatestArtifactPath(string artifactDirectory) {
        if (string.IsNullOrWhiteSpace(artifactDirectory)) {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(artifactDirectory));
        }

        if (!Directory.Exists(artifactDirectory)) {
            throw new DirectoryNotFoundException("Hosted-session diagnostic artifact directory was not found.");
        }

        string latestArtifactPath = FindLatestArtifactPathOrDefault(artifactDirectory) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(latestArtifactPath)) {
            throw new FileNotFoundException("No hosted-session diagnostic artifacts were found.", artifactDirectory);
        }

        return latestArtifactPath;
    }

    public static string GetHostedSessionArtifactDirectory(string repositoryRoot) {
        if (string.IsNullOrWhiteSpace(repositoryRoot)) {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(repositoryRoot));
        }

        return Path.Combine(repositoryRoot, "Artifacts", "HostedSessionTyping");
    }

    public static string FindLatestArtifactPathFromRepositoryRoot(string repositoryRoot) {
        return FindLatestArtifactPath(GetHostedSessionArtifactDirectory(repositoryRoot));
    }

    public static HostedSessionDiagnosticArtifact LoadLatest(string artifactDirectory) {
        return Load(FindLatestArtifactPath(artifactDirectory));
    }

    public static HostedSessionDiagnosticArtifact LoadLatestFromRepositoryRoot(string repositoryRoot) {
        return Load(FindLatestArtifactPathFromRepositoryRoot(repositoryRoot));
    }

    public static bool TryLoadLatest(string artifactDirectory, out HostedSessionDiagnosticArtifact artifact, out string artifactPath, out string error) {
        artifact = new HostedSessionDiagnosticArtifact();
        artifactPath = string.Empty;
        error = string.Empty;

        try {
            artifactPath = FindLatestArtifactPath(artifactDirectory);
            artifact = Load(artifactPath);
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

    public static bool TryLoadLatestFromRepositoryRoot(string repositoryRoot, out HostedSessionDiagnosticArtifact artifact, out string artifactPath, out string error) {
        artifact = new HostedSessionDiagnosticArtifact();
        artifactPath = string.Empty;
        error = string.Empty;

        try {
            return TryLoadLatest(GetHostedSessionArtifactDirectory(repositoryRoot), out artifact, out artifactPath, out error);
        } catch (Exception exception) when (exception is ArgumentException) {
            error = exception.Message;
            return false;
        }
    }

    public static bool TryReadLatestSummary(string artifactDirectory, out string artifactPath, out string summary, out string error) {
        artifactPath = string.Empty;
        summary = string.Empty;
        error = string.Empty;

        try {
            artifactPath = FindLatestArtifactPath(artifactDirectory);
            summary = ReadSummary(artifactPath);
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

    public static bool TryReadLatestSummaryFromRepositoryRoot(string repositoryRoot, out string artifactPath, out string summary, out string error) {
        string artifactDirectory = string.Empty;
        artifactPath = string.Empty;
        summary = string.Empty;
        error = string.Empty;

        try {
            artifactDirectory = GetHostedSessionArtifactDirectory(repositoryRoot);
            return TryReadLatestSummary(artifactDirectory, out artifactPath, out summary, out error);
        } catch (Exception exception) when (exception is ArgumentException) {
            error = exception.Message;
            return false;
        }
    }

    internal static string? FindPreferredSummaryArtifactPath(string artifactPath) {
        if (string.IsNullOrWhiteSpace(artifactPath)) {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(artifactPath));
        }

        if (!File.Exists(artifactPath)) {
            return null;
        }

        string[] artifactSetPaths = DesktopManagerTestAppSession.GetArtifactSetPaths(artifactPath);
        string? preferred = null;
        foreach (string candidatePath in artifactSetPaths) {
            if (!candidatePath.EndsWith(".summary.txt", StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            if (preferred == null) {
                preferred = candidatePath;
                continue;
            }

            preferred = CompareSummaryPaths(candidatePath, preferred) < 0 ? candidatePath : preferred;
        }

        return preferred;
    }

    internal static string? FindLatestArtifactPathOrDefault(string artifactDirectory) {
        if (string.IsNullOrWhiteSpace(artifactDirectory) || !Directory.Exists(artifactDirectory)) {
            return null;
        }

        string[] artifactPaths = Directory.GetFiles(artifactDirectory, "*.json", SearchOption.TopDirectoryOnly);
        if (artifactPaths.Length == 0) {
            return null;
        }

        Array.Sort(artifactPaths, CompareArtifactPathsByLastWriteDescending);
        return artifactPaths[0];
    }

    private static HostedSessionDiagnosticArtifact Normalize(HostedSessionDiagnosticArtifact artifact) {
        artifact.Reason ??= string.Empty;
        artifact.CreatedUtc ??= string.Empty;
        artifact.Summary ??= string.Empty;
        artifact.PolicyReport ??= string.Empty;
        artifact.RetryHistoryReport ??= HostedSessionRetryHistoryReport.None;
        artifact.RetryHistoryReport.CategoryHint ??= string.Empty;
        artifact.RetryHistoryReport.Summary ??= string.Empty;
        artifact.Status ??= new DesktopManagerTestAppStatus();
        artifact.Status.WindowTitle ??= string.Empty;
        artifact.Status.ActiveSurface ??= string.Empty;
        artifact.Status.ForegroundHoldSurface ??= string.Empty;
        artifact.Status.LastObservedForegroundTitle ??= string.Empty;
        artifact.Status.LastObservedForegroundClass ??= string.Empty;
        artifact.Status.LastObservedForegroundChangedUtc ??= string.Empty;
        artifact.Status.LastCommand ??= string.Empty;
        artifact.Status.ForegroundHistory ??= [];
        artifact.Status.EditorText ??= string.Empty;
        artifact.Status.SecondaryText ??= string.Empty;
        artifact.Status.CommandBarText ??= string.Empty;
        artifact.Status.StatusText ??= string.Empty;
        return artifact;
    }

    private static HostedSessionDiagnosticArtifact CreateLegacyArtifact(DesktopManagerTestAppStatus status) {
        HostedSessionExternalForegroundReport report = HostedSessionDiagnosticFormatter.CreateExternalForegroundReport(status);
        HostedSessionRetryHistoryReport retryHistoryReport = HostedSessionDiagnosticFormatter.CreateRetryHistoryReport(new[] { report });
        return Normalize(new HostedSessionDiagnosticArtifact {
            FormatVersion = 0,
            Reason = "Legacy hosted-session diagnostic artifact",
            CreatedUtc = string.Empty,
            Summary = HostedSessionDiagnosticFormatter.Summarize(status),
            PolicyReport = report.ToPolicyReport(),
            RetryHistoryReport = retryHistoryReport,
            Status = status
        });
    }

    private static int CompareSummaryPaths(string left, string right) {
        string leftFileName = Path.GetFileName(left);
        string rightFileName = Path.GetFileName(right);
        int leftSegmentCount = CountSummaryNameSegments(leftFileName);
        int rightSegmentCount = CountSummaryNameSegments(rightFileName);
        int comparison = leftSegmentCount.CompareTo(rightSegmentCount);
        if (comparison != 0) {
            return comparison;
        }

        return string.Compare(leftFileName, rightFileName, StringComparison.OrdinalIgnoreCase);
    }

    private static int CountSummaryNameSegments(string fileName) {
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
