using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Gets outer-window and client-area geometry for desktop windows.</summary>
/// <para type="synopsis">Returns window and client-area geometry for matching desktop windows.</para>
/// <example>
///   <code>Get-DesktopWindowGeometry -Name "*Notepad*"</code>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopWindowGeometry")]
public sealed class CmdletGetDesktopWindowGeometry : PSCmdlet {
    /// <summary>
    /// <para type="description">Title of the window to match. Supports wildcards.</para>
    /// </summary>
    [Parameter(Position = 0)]
    public string Name { get; set; } = "*";

    /// <summary>
    /// <para type="description">Use the current foreground window instead of matching by name.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter ActiveWindow { get; set; }

    /// <summary>
    /// <para type="description">Return geometry for all matching windows instead of only the first.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter All { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        var automation = new DesktopAutomationService();
        var options = new WindowQueryOptions {
            TitlePattern = Name,
            ActiveWindow = ActiveWindow,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = ActiveWindow ? true : null
        };

        WriteObject(automation.GetWindowGeometry(options, All), true);
    }
}
