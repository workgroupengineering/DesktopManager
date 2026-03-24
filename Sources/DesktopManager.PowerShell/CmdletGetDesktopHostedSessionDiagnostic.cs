using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Gets the latest hosted-session diagnostic artifact or a specific hosted-session artifact.</summary>
/// <para type="synopsis">Reads hosted-session typing diagnostics from the DesktopManager artifact folder.</para>
/// <para>Prefers the companion summary file when one exists and falls back to the JSON diagnostic artifact otherwise.</para>
/// <example>
///   <code>Get-DesktopHostedSessionDiagnostic</code>
/// </example>
/// <example>
///   <code>Get-DesktopHostedSessionDiagnostic -SummaryOnly</code>
/// </example>
/// <example>
///   <code>Get-DesktopHostedSessionDiagnostic -RepositoryRoot C:\Support\GitHub\DesktopManager</code>
/// </example>
/// <example>
///   <code>Get-DesktopHostedSessionDiagnostic -ArtifactPath C:\Support\GitHub\DesktopManager\Artifacts\HostedSessionTyping\sample.json</code>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopHostedSessionDiagnostic")]
public sealed class CmdletGetDesktopHostedSessionDiagnostic : PSCmdlet {
    /// <summary>
    /// <para type="description">Specific hosted-session JSON artifact to read.</para>
    /// </summary>
    [Parameter(Position = 0)]
    public string ArtifactPath { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Directory containing hosted-session JSON artifacts and summary companion files.</para>
    /// </summary>
    [Parameter]
    public string ArtifactDirectory { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Repository root used to resolve Artifacts\HostedSessionTyping when ArtifactPath is not supplied.</para>
    /// </summary>
    [Parameter]
    public string RepositoryRoot { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Returns only the resolved summary text instead of the structured diagnostic record.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter SummaryOnly { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        string resolvedArtifactPath;

        if (!string.IsNullOrWhiteSpace(ArtifactPath)) {
            resolvedArtifactPath = ArtifactPath;
        } else {
            string artifactDirectory = ResolveArtifactDirectory();
            resolvedArtifactPath = DesktopHostedSessionDiagnosticReader.FindLatestArtifactPath(artifactDirectory);
        }

        DesktopHostedSessionDiagnosticRecord record = DesktopHostedSessionDiagnosticReader.Load(resolvedArtifactPath);
        if (SummaryOnly) {
            WriteObject(record.SummaryText);
            return;
        }

        WriteObject(record);
    }

    private string ResolveArtifactDirectory() {
        if (!string.IsNullOrWhiteSpace(ArtifactDirectory)) {
            return ArtifactDirectory;
        }

        if (!string.IsNullOrWhiteSpace(RepositoryRoot)) {
            return DesktopHostedSessionDiagnosticReader.GetHostedSessionArtifactDirectory(RepositoryRoot);
        }

        string currentDirectory = SessionState.Path.CurrentFileSystemLocation?.Path ?? string.Empty;
        if (DesktopHostedSessionDiagnosticReader.TryFindRepositoryRoot(currentDirectory, out string repositoryRoot)) {
            return DesktopHostedSessionDiagnosticReader.GetHostedSessionArtifactDirectory(repositoryRoot);
        }

        throw new PSInvalidOperationException(
            "Could not resolve the DesktopManager repository root from the current location. Specify -RepositoryRoot, -ArtifactDirectory, or -ArtifactPath.");
    }
}
