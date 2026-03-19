using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Scrolls at a point relative to a desktop window.</summary>
/// <para type="synopsis">Simulates mouse-wheel scrolling at a point relative to a matching desktop window.</para>
/// <example>
///   <code>Invoke-DesktopWindowScroll -Name "*Notepad*" -X 200 -Y 200 -Delta -120 -ClientArea</code>
/// </example>
[Cmdlet(VerbsLifecycle.Invoke, "DesktopWindowScroll", SupportsShouldProcess = true)]
public sealed class CmdletInvokeDesktopWindowScroll : PSCmdlet {
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
    /// <para type="description">Horizontal coordinate relative to the target window.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public int X { get; set; }

    /// <summary>
    /// <para type="description">Vertical coordinate relative to the target window.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public int Y { get; set; }

    /// <summary>
    /// <para type="description">Scroll delta. Positive values scroll up.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public int Delta { get; set; }

    /// <summary>
    /// <para type="description">Activate the window before scrolling.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Activate { get; set; }

    /// <summary>
    /// <para type="description">Interpret the supplied coordinates relative to the client area instead of the outer window bounds.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter ClientArea { get; set; }

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

        if (ShouldProcess(ActiveWindow ? "active window" : Name, $"Scroll {Delta} at {X},{Y}")) {
            WriteObject(automation.ScrollWindowPoint(options, X, Y, Delta, Activate, ClientArea, all: false), true);
        }
    }
}
