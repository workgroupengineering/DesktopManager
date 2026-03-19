using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Gets saved reusable window-relative targets, or resolves one against live windows.</summary>
/// <para type="synopsis">Returns saved DesktopManager window targets.</para>
/// <example>
///   <code>Get-DesktopWindowTarget</code>
/// </example>
/// <example>
///   <code>Get-DesktopWindowTarget -Name "editor-center"</code>
/// </example>
/// <example>
///   <code>Get-DesktopWindowTarget -Name "editor-center" -Resolve -WindowName "*Notepad*"</code>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopWindowTarget")]
public sealed class CmdletGetDesktopWindowTarget : PSCmdlet {
    /// <summary>
    /// <para type="description">Optional saved target name. When omitted, all saved target names are returned.</para>
    /// </summary>
    [Parameter(Position = 0)]
    public string Name { get; set; }

    /// <summary>
    /// <para type="description">Resolve the saved target against one or more live windows instead of returning only the saved definition.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Resolve { get; set; }

    /// <summary>
    /// <para type="description">Window title filter to use when resolving a target. Supports wildcards.</para>
    /// </summary>
    [Parameter]
    public string WindowName { get; set; } = "*";

    /// <summary>
    /// <para type="description">Use the current foreground window when resolving a target.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter ActiveWindow { get; set; }

    /// <summary>
    /// <para type="description">Return resolved points for all matching windows instead of only the first.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter All { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        var automation = new DesktopAutomationService();
        if (string.IsNullOrWhiteSpace(Name)) {
            WriteObject(automation.ListWindowTargets(), true);
            return;
        }

        if (!Resolve) {
            WriteObject(automation.GetWindowTarget(Name));
            return;
        }

        var options = new WindowQueryOptions {
            TitlePattern = WindowName,
            ActiveWindow = ActiveWindow,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = ActiveWindow ? true : null
        };
        WriteObject(automation.ResolveWindowTargets(options, Name, All), true);
    }
}
