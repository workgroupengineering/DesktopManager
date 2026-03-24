#if NET8_0_OR_GREATER
using System.IO;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI hosted-session diagnostic text output.
/// </summary>
public class DiagnosticCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures summary-only output writes the preferred summary text.
    /// </summary>
    public void WriteSummary_WritesSummaryText() {
        var result = new global::DesktopManager.Cli.HostedSessionDiagnosticResult {
            SummaryText = "summary from sidecar"
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.DiagnosticCommands.WriteSummary(result, writer);

        Assert.AreEqual(0, exitCode);
        Assert.AreEqual("summary from sidecar" + System.Environment.NewLine, writer.ToString());
    }

    [TestMethod]
    /// <summary>
    /// Ensures full output includes artifact metadata, retry-history details, and status fields.
    /// </summary>
    public void WriteDiagnosticResult_WritesRichDiagnosticSummary() {
        var result = new global::DesktopManager.Cli.HostedSessionDiagnosticResult {
            ArtifactPath = @"C:\Artifacts\HostedSessionTyping\sample.json",
            SummaryPath = @"C:\Artifacts\HostedSessionTyping\sample.summary.txt",
            SummaryText = "summary from sidecar",
            Reason = "Interactive session stayed noisy.",
            CreatedUtc = "2026-03-24T08:00:00Z",
            RetryHistoryCategory = "browser-electron",
            RetryHistorySummary = "same external foreground interruption repeated",
            RetryHistoryExternalCount = 2,
            RetryHistoryDistinctFingerprintCount = 1,
            PolicyReport = "category='browser-electron'",
            WindowTitle = "DesktopManager Test App",
            StatusText = "Waiting for focus"
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.DiagnosticCommands.WriteDiagnosticResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, @"C:\Artifacts\HostedSessionTyping\sample.json");
        StringAssert.Contains(output, @"- SummaryPath: C:\Artifacts\HostedSessionTyping\sample.summary.txt");
        StringAssert.Contains(output, "- Summary: summary from sidecar");
        StringAssert.Contains(output, "- Reason: Interactive session stayed noisy.");
        StringAssert.Contains(output, "- CreatedUtc: 2026-03-24T08:00:00Z");
        StringAssert.Contains(output, "- RetryHistoryCategory: browser-electron");
        StringAssert.Contains(output, "- RetryHistorySummary: same external foreground interruption repeated");
        StringAssert.Contains(output, "- RetryHistoryExternalCount: 2");
        StringAssert.Contains(output, "- RetryHistoryDistinctFingerprintCount: 1");
        StringAssert.Contains(output, "- PolicyReport: category='browser-electron'");
        StringAssert.Contains(output, "- WindowTitle: DesktopManager Test App");
        StringAssert.Contains(output, "- StatusText: Waiting for focus");
    }
}
#endif
