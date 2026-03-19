using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Gets saved reusable control targets, or resolves one against live windows.</summary>
/// <para type="synopsis">Returns saved DesktopManager control targets.</para>
/// <example>
///   <code>Get-DesktopControlTarget</code>
/// </example>
/// <example>
///   <code>Get-DesktopControlTarget -Name "edge-address"</code>
/// </example>
/// <example>
///   <code>Get-DesktopControlTarget -Name "edge-address" -Resolve -WindowName "*Edge*"</code>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopControlTarget")]
public sealed class CmdletGetDesktopControlTarget : PSCmdlet {
    /// <summary>
    /// <para type="description">Optional saved control target name. When omitted, all saved target names are returned.</para>
    /// </summary>
    [Parameter(Position = 0)]
    public string Name { get; set; }

    /// <summary>
    /// <para type="description">Resolve the saved target against one or more live windows instead of returning only the saved definition.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Resolve { get; set; }

    /// <summary>
    /// <para type="description">Window title filter to use when resolving the target. Supports wildcards.</para>
    /// </summary>
    [Parameter]
    public string WindowName { get; set; } = "*";

    /// <summary>
    /// <para type="description">Use the current foreground window when resolving the target.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter ActiveWindow { get; set; }

    /// <summary>
    /// <para type="description">Return resolved controls for all matching windows.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter AllWindows { get; set; }

    /// <summary>
    /// <para type="description">Return all matching controls instead of only the first resolved control.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter AllControls { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        var automation = new DesktopAutomationService();
        if (string.IsNullOrWhiteSpace(Name)) {
            WriteObject(automation.ListControlTargets(), true);
            return;
        }

        if (!Resolve) {
            WriteObject(automation.GetControlTarget(Name));
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
        WriteObject(automation.ResolveControlTargets(options, Name, AllWindows, AllControls), true);
    }
}
