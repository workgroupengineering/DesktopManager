using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Drags between two points relative to a desktop window.</summary>
/// <para type="synopsis">Simulates dragging between two points relative to a matching desktop window.</para>
/// <example>
///   <code>Invoke-DesktopWindowDrag -Name "*Notepad*" -StartX 200 -StartY 200 -EndX 500 -EndY 200 -ClientArea</code>
/// </example>
[Cmdlet(VerbsLifecycle.Invoke, "DesktopWindowDrag", SupportsShouldProcess = true)]
public sealed class CmdletInvokeDesktopWindowDrag : PSCmdlet {
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
    /// <para type="description">Horizontal starting coordinate relative to the target window.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public int StartX { get; set; }

    /// <summary>
    /// <para type="description">Vertical starting coordinate relative to the target window.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public int StartY { get; set; }

    /// <summary>
    /// <para type="description">Horizontal ending coordinate relative to the target window.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public int EndX { get; set; }

    /// <summary>
    /// <para type="description">Vertical ending coordinate relative to the target window.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public int EndY { get; set; }

    /// <summary>
    /// <para type="description">Mouse button to hold during the drag.</para>
    /// </summary>
    [Parameter]
    public MouseButton Button { get; set; } = MouseButton.Left;

    /// <summary>
    /// <para type="description">Delay in milliseconds between drag steps.</para>
    /// </summary>
    [Parameter]
    public int StepDelayMilliseconds { get; set; }

    /// <summary>
    /// <para type="description">Activate the window before dragging.</para>
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

        if (ShouldProcess(ActiveWindow ? "active window" : Name, $"Drag from {StartX},{StartY} to {EndX},{EndY}")) {
            WriteObject(automation.DragWindowPoints(options, StartX, StartY, EndX, EndY, Button, StepDelayMilliseconds, Activate, ClientArea, all: false), true);
        }
    }
}
