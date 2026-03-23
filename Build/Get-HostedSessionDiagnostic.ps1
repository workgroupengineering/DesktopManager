param(
    [string] $ArtifactDirectory = (Join-Path $PSScriptRoot '..\Artifacts\HostedSessionTyping'),
    [string] $ArtifactPath,
    [switch] $SummaryOnly,
    [switch] $AsJson
)

function Get-HostedSessionDiagnostic {
    <#
    .SYNOPSIS
    Reads the latest hosted-session diagnostic artifact or a specific artifact file.

    .DESCRIPTION
    Reads repo-local hosted-session typing diagnostics from Artifacts\HostedSessionTyping.
    Prefers the companion summary file when one exists and falls back to the JSON artifact content otherwise.

    .PARAMETER ArtifactDirectory
    The directory containing hosted-session diagnostic JSON artifacts and summary companion files.

    .PARAMETER ArtifactPath
    A specific hosted-session diagnostic JSON artifact path to read.

    .PARAMETER SummaryOnly
    Returns only the resolved summary text instead of a diagnostic object.

    .PARAMETER AsJson
    Serializes the resolved diagnostic object as JSON.

    .EXAMPLE
    .\Build\Get-HostedSessionDiagnostic.ps1

    .EXAMPLE
    .\Build\Get-HostedSessionDiagnostic.ps1 -SummaryOnly

    .EXAMPLE
    .\Build\Get-HostedSessionDiagnostic.ps1 -ArtifactPath "C:\Repo\DesktopManager\Artifacts\HostedSessionTyping\sample.json" -AsJson

    .NOTES
    The latest JSON artifact is selected by LastWriteTimeUtc when ArtifactPath is not supplied.
    #>
    [CmdletBinding()]
    param(
        [string] $ArtifactDirectory,
        [string] $ArtifactPath,
        [switch] $SummaryOnly,
        [switch] $AsJson
    )

    if ([string]::IsNullOrWhiteSpace($ArtifactPath)) {
        if ([string]::IsNullOrWhiteSpace($ArtifactDirectory)) {
            throw "ArtifactDirectory cannot be null or empty."
        }
        if (-not (Test-Path -LiteralPath $ArtifactDirectory -PathType Container)) {
            throw "Hosted-session diagnostic directory not found: $ArtifactDirectory"
        }

        $artifact = Get-ChildItem -LiteralPath $ArtifactDirectory -Filter '*.json' -File |
            Sort-Object -Property LastWriteTimeUtc, FullName -Descending |
            Select-Object -First 1
        if ($null -eq $artifact) {
            throw "No hosted-session diagnostic artifacts were found in: $ArtifactDirectory"
        }

        $ArtifactPath = $artifact.FullName
    } elseif (-not (Test-Path -LiteralPath $ArtifactPath -PathType Leaf)) {
        throw "Hosted-session diagnostic artifact not found: $ArtifactPath"
    }

    $artifactDirectoryPath = Split-Path -Path $ArtifactPath -Parent
    $artifactStem = [System.IO.Path]::GetFileNameWithoutExtension($ArtifactPath)
    $summaryCandidate = Get-ChildItem -LiteralPath $artifactDirectoryPath -Filter ($artifactStem + '*.summary.txt') -File |
        Sort-Object -Property @{ Expression = { ($_.Name -split '\.').Count } }, Name |
        Select-Object -First 1
    $summaryPath = $null
    $summaryText = $null

    if ($null -ne $summaryCandidate) {
        $summaryPath = $summaryCandidate.FullName
        $summaryText = Get-Content -LiteralPath $summaryPath -Raw
    }

    $artifactObject = Get-Content -LiteralPath $ArtifactPath -Raw | ConvertFrom-Json
    if ($null -eq $artifactObject) {
        throw "Hosted-session diagnostic artifact could not be deserialized: $ArtifactPath"
    }

    $reason = [string] $artifactObject.Reason
    $createdUtc = [string] $artifactObject.CreatedUtc
    $policyReport = [string] $artifactObject.PolicyReport
    $retryHistoryCategory = [string] $artifactObject.RetryHistoryReport.CategoryHint
    $retryHistorySummary = [string] $artifactObject.RetryHistoryReport.Summary
    $retryHistoryExternalCount = 0
    $retryHistoryDistinctFingerprintCount = 0
    $statusObject = $artifactObject.Status

    if ($artifactObject.PSObject.Properties.Match('WindowTitle').Count -gt 0 -and $artifactObject.PSObject.Properties.Match('Status').Count -eq 0) {
        $statusObject = $artifactObject
        $reason = 'Legacy hosted-session diagnostic artifact'
        $createdUtc = ''
        if ([string]::IsNullOrWhiteSpace($policyReport)) {
            if (-not [string]::IsNullOrWhiteSpace([string] $statusObject.LastObservedForegroundClass) -and
                [string] $statusObject.LastObservedForegroundClass -match 'Chrome_WidgetWin_1|Chrome_RenderWidgetHostHWND|MozillaWindowClass|ApplicationFrameWindow') {
                $policyReport = "category='browser-electron'"
                $retryHistoryCategory = 'browser-electron'
            } elseif (-not [string]::IsNullOrWhiteSpace([string] $statusObject.LastObservedForegroundTitle)) {
                $policyReport = "category='unknown'"
                $retryHistoryCategory = 'unknown'
            } else {
                $policyReport = "category='none'"
                $retryHistoryCategory = 'none'
            }
        }
    }

    if ($artifactObject.PSObject.Properties.Match('RetryHistoryReport').Count -gt 0 -and $null -ne $artifactObject.RetryHistoryReport) {
        $retryHistoryExternalCount = [int] $artifactObject.RetryHistoryReport.ExternalCount
        $retryHistoryDistinctFingerprintCount = [int] $artifactObject.RetryHistoryReport.DistinctFingerprintCount
    }

    if ([string]::IsNullOrWhiteSpace($summaryText)) {
        if (-not [string]::IsNullOrWhiteSpace([string] $artifactObject.Summary)) {
            $summaryText = [string] $artifactObject.Summary
        } else {
            $summaryText =
                "reason='" + $reason + "', " +
                "category='" + $retryHistoryCategory + "', " +
                "externalCount=" + $retryHistoryExternalCount + ", " +
                "distinctFingerprintCount=" + $retryHistoryDistinctFingerprintCount + ", " +
                "policy='" + $policyReport + "', " +
                "windowTitle='" + [string] $statusObject.WindowTitle + "', " +
                "statusText='" + [string] $statusObject.StatusText + "'"
        }
    }

    if ($SummaryOnly) {
        $summaryText
        return
    }

    $result = [pscustomobject] @{
        ArtifactPath                     = $ArtifactPath
        SummaryPath                      = $summaryPath
        SummaryText                      = $summaryText
        Reason                           = $reason
        CreatedUtc                       = $createdUtc
        RetryHistoryCategory             = $retryHistoryCategory
        RetryHistorySummary              = $retryHistorySummary
        RetryHistoryExternalCount        = $retryHistoryExternalCount
        RetryHistoryDistinctFingerprintCount = $retryHistoryDistinctFingerprintCount
        PolicyReport                     = $policyReport
        WindowTitle                      = [string] $statusObject.WindowTitle
        StatusText                       = [string] $statusObject.StatusText
    }

    if ($AsJson) {
        $result | ConvertTo-Json -Depth 5
        return
    }

    $result
}

Get-HostedSessionDiagnostic -ArtifactDirectory $ArtifactDirectory -ArtifactPath $ArtifactPath -SummaryOnly:$SummaryOnly -AsJson:$AsJson
