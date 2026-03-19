using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Saves or updates a reusable window-relative target.</summary>
/// <para type="synopsis">Persists a named DesktopManager window target.</para>
/// <example>
///   <code>Set-DesktopWindowTarget -Name "editor-center" -XRatio 0.5 -YRatio 0.5 -ClientArea</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopWindowTarget", SupportsShouldProcess = true)]
public sealed class CmdletSetDesktopWindowTarget : PSCmdlet {
    /// <summary>
    /// <para type="description">Saved target name.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name { get; set; }

    /// <summary>
    /// <para type="description">Optional target description.</para>
    /// </summary>
    [Parameter]
    public string Description { get; set; }

    /// <summary>
    /// <para type="description">Horizontal coordinate relative to the target bounds.</para>
    /// </summary>
    [Parameter]
    public int? X { get; set; }

    /// <summary>
    /// <para type="description">Vertical coordinate relative to the target bounds.</para>
    /// </summary>
    [Parameter]
    public int? Y { get; set; }

    /// <summary>
    /// <para type="description">Horizontal coordinate ratio from 0 to 1 relative to the target bounds.</para>
    /// </summary>
    [Parameter]
    public double? XRatio { get; set; }

    /// <summary>
    /// <para type="description">Vertical coordinate ratio from 0 to 1 relative to the target bounds.</para>
    /// </summary>
    [Parameter]
    public double? YRatio { get; set; }

    /// <summary>
    /// <para type="description">Interpret the target relative to the window client area instead of the outer bounds.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter ClientArea { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        var automation = new DesktopAutomationService();
        var definition = new DesktopWindowTargetDefinition {
            Description = Description,
            X = X,
            Y = Y,
            XRatio = XRatio,
            YRatio = YRatio,
            ClientArea = ClientArea
        };

        if (ShouldProcess(Name, "Save reusable window target")) {
            WriteObject(automation.SaveWindowTarget(Name, definition));
        }
    }
}
