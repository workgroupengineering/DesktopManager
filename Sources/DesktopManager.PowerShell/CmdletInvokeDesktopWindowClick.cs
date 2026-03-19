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
    [Parameter]
    public int? X { get; set; }

    /// <summary>
    /// <para type="description">Vertical coordinate relative to the window bounds.</para>
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
    /// <para type="description">Saved reusable target name to click instead of supplying coordinates directly.</para>
    /// </summary>
    [Parameter]
    public string TargetName { get; set; }

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

        string targetText = !string.IsNullOrWhiteSpace(TargetName)
            ? $"target '{TargetName}'"
            : X.HasValue && Y.HasValue
                ? $"{X},{Y}"
                : $"{XRatio},{YRatio}";
        if (ShouldProcess(ActiveWindow ? "active window" : Name, $"Click point {targetText}")) {
            if (!string.IsNullOrWhiteSpace(TargetName)) {
                WriteObject(automation.ClickWindowTarget(options, TargetName, Button, Activate, all: false), true);
            } else {
                WriteObject(automation.ClickWindowPoint(options, X, Y, XRatio, YRatio, Button, Activate, ClientArea, all: false), true);
            }
        }
    }
}
