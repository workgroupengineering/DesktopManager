using System.Management.Automation;
using System.Text.RegularExpressions;

namespace DesktopManager.PowerShell;

/// <summary>Tests whether a desktop window exists or is currently active.</summary>
/// <para type="synopsis">Tests desktop window presence and active-window matching.</para>
/// <example>
///   <code>Test-DesktopWindow -Name "*Notepad*"</code>
/// </example>
/// <example>
///   <code>Test-DesktopWindow -ActiveWindow -Name "Codex"</code>
/// </example>
[Cmdlet(VerbsDiagnostic.Test, "DesktopWindow")]
public sealed class CmdletTestDesktopWindow : PSCmdlet {
    /// <summary>
    /// <para type="description">Filter windows by title. Supports wildcards.</para>
    /// </summary>
    [Parameter(Position = 0)]
    public string Name { get; set; } = "*";

    /// <summary>
    /// <para type="description">Filter windows by process name. Supports wildcards.</para>
    /// </summary>
    [Parameter]
    public string ProcessName { get; set; } = "*";

    /// <summary>
    /// <para type="description">Filter windows by class name. Supports wildcards.</para>
    /// </summary>
    [Parameter]
    public string ClassName { get; set; } = "*";

    /// <summary>
    /// <para type="description">Filter windows by title regular expression.</para>
    /// </summary>
    [Parameter]
    public Regex Regex { get; set; }

    /// <summary>
    /// <para type="description">Filter windows by process identifier.</para>
    /// </summary>
    [Parameter]
    public int ProcessId { get; set; }

    /// <summary>
    /// <para type="description">Check whether the current foreground window matches the selector.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter ActiveWindow { get; set; }

    /// <summary>
    /// <para type="description">Include hidden windows in the lookup.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter IncludeHidden { get; set; }

    /// <summary>
    /// <para type="description">Include DWM-cloaked windows in the lookup.</para>
    /// </summary>
    [Parameter]
    public bool IncludeCloaked { get; set; } = true;

    /// <summary>
    /// <para type="description">Include owned windows in the lookup.</para>
    /// </summary>
    [Parameter]
    public bool IncludeOwned { get; set; } = true;

    /// <inheritdoc />
    protected override void BeginProcessing() {
        var automation = new DesktopAutomationService();
        var options = new WindowQueryOptions {
            TitlePattern = Name,
            ProcessNamePattern = ProcessName,
            ClassNamePattern = ClassName,
            TitleRegex = Regex,
            ProcessId = ProcessId,
            IncludeHidden = IncludeHidden,
            IncludeCloaked = IncludeCloaked,
            IncludeOwned = IncludeOwned,
            IncludeEmptyTitles = ActiveWindow ? true : null
        };

        bool result = ActiveWindow
            ? automation.ActiveWindowMatches(options)
            : automation.WindowExists(options);
        WriteObject(result);
    }
}
