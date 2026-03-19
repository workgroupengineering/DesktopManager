using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Clicks a point relative to a desktop window.</summary>
/// <para type="synopsis">Clicks a point relative to a matching desktop window.</para>
/// <example>
///   <code>Invoke-DesktopWindowClick -Name "*Notepad*" -X 200 -Y 200</code>
/// </example>
[Cmdlet(VerbsLifecycle.Invoke, "DesktopWindowClick", SupportsShouldProcess = true)]
public sealed class CmdletInvokeDesktopWindowClick : PSCmdlet {
    /// <summary>
    /// <para type="description">Title of the window to match. Supports wildcards.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByName")]
    public string Name { get; set; } = "*";

    /// <summary>
    /// <para type="description">Use the current foreground window instead of matching by name.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "ActiveWindow")]
    public SwitchParameter ActiveWindow { get; set; }

    /// <summary>
    /// <para type="description">Horizontal coordinate relative to the window bounds.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public int X { get; set; }

    /// <summary>
    /// <para type="description">Vertical coordinate relative to the window bounds.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public int Y { get; set; }

    /// <summary>
    /// <para type="description">Mouse button to use for the click.</para>
    /// </summary>
    [Parameter]
    public MouseButton Button { get; set; } = MouseButton.Left;

    /// <summary>
    /// <para type="description">Activate the window before clicking.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Activate { get; set; }

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

        if (ShouldProcess(ActiveWindow ? "active window" : Name, $"Click point {X},{Y}")) {
            WriteObject(automation.ClickWindowPoint(options, X, Y, Button, Activate, all: false), true);
        }
    }
}
