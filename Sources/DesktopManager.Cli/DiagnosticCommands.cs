using System;
using System.IO;

namespace DesktopManager.Cli;

internal static class DiagnosticCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "hosted-session" => HostedSession(arguments),
            _ => throw new CommandLineException($"Unknown diagnostic command '{action}'.")
        };
    }

    private static int HostedSession(CommandLineArguments arguments) {
        string resolvedArtifactPath = ResolveArtifactPath(arguments);
        HostedSessionDiagnosticResult result = HostedSessionDiagnosticReader.Load(resolvedArtifactPath);

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        if (arguments.GetBoolFlag("summary-only")) {
            return WriteSummary(result, Console.Out);
        }

        return WriteDiagnosticResult(result, Console.Out);
    }

    private static string ResolveArtifactPath(CommandLineArguments arguments) {
        string artifactPath = arguments.GetOption("artifact") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(artifactPath)) {
            return artifactPath;
        }

        string artifactDirectory = arguments.GetOption("artifact-directory") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(artifactDirectory)) {
            return HostedSessionDiagnosticReader.FindLatestArtifactPath(artifactDirectory);
        }

        string repositoryRoot = arguments.GetOption("repository-root") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(repositoryRoot)) {
            return HostedSessionDiagnosticReader.FindLatestArtifactPath(
                HostedSessionDiagnosticReader.GetHostedSessionArtifactDirectory(repositoryRoot));
        }

        if (HostedSessionDiagnosticReader.TryFindRepositoryRoot(Environment.CurrentDirectory, out string currentRepositoryRoot)) {
            return HostedSessionDiagnosticReader.FindLatestArtifactPath(
                HostedSessionDiagnosticReader.GetHostedSessionArtifactDirectory(currentRepositoryRoot));
        }

        throw new CommandLineException(
            "Could not resolve the DesktopManager repository root from the current location. Specify --repository-root, --artifact-directory, or --artifact.");
    }

    internal static int WriteSummary(HostedSessionDiagnosticResult result, TextWriter writer) {
        writer.WriteLine(result.SummaryText);
        return 0;
    }

    internal static int WriteDiagnosticResult(HostedSessionDiagnosticResult result, TextWriter writer) {
        writer.WriteLine(result.ArtifactPath);
        if (!string.IsNullOrWhiteSpace(result.SummaryPath)) {
            writer.WriteLine($"- SummaryPath: {result.SummaryPath}");
        }
        writer.WriteLine($"- Summary: {result.SummaryText}");
        if (!string.IsNullOrWhiteSpace(result.Reason)) {
            writer.WriteLine($"- Reason: {result.Reason}");
        }
        if (!string.IsNullOrWhiteSpace(result.CreatedUtc)) {
            writer.WriteLine($"- CreatedUtc: {result.CreatedUtc}");
        }
        writer.WriteLine($"- RetryHistoryCategory: {result.RetryHistoryCategory}");
        writer.WriteLine($"- RetryHistorySummary: {result.RetryHistorySummary}");
        writer.WriteLine($"- RetryHistoryExternalCount: {result.RetryHistoryExternalCount}");
        writer.WriteLine($"- RetryHistoryDistinctFingerprintCount: {result.RetryHistoryDistinctFingerprintCount}");
        if (!string.IsNullOrWhiteSpace(result.PolicyReport)) {
            writer.WriteLine($"- PolicyReport: {result.PolicyReport}");
        }
        if (!string.IsNullOrWhiteSpace(result.WindowTitle)) {
            writer.WriteLine($"- WindowTitle: {result.WindowTitle}");
        }
        if (!string.IsNullOrWhiteSpace(result.StatusText)) {
            writer.WriteLine($"- StatusText: {result.StatusText}");
        }
        return 0;
    }
}
