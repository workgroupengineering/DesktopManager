using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Clicks a specific window control.</summary>
/// <para type="synopsis">Invokes a click on a window control.</para>
/// <example>
///   <code>Invoke-DesktopControlClick -Control $ctrl</code>
/// </example>
[Cmdlet(VerbsLifecycle.Invoke, "DesktopControlClick", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletInvokeDesktopControlClick : PSCmdlet {
    /// <summary>
    /// <para type="description">Control to click.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public WindowControlInfo Control { get; set; } = null!;

    /// <summary>
    /// <para type="description">Mouse button to use. Defaults to Left.</para>
    /// </summary>
    [Parameter]
    public MouseButton Button { get; set; } = MouseButton.Left;

    /// <inheritdoc />
    protected override void BeginProcessing() {
        if (ShouldProcess(Control.Text ?? Control.ClassName, $"Click {Button}")) {
            var automation = new DesktopAutomationService();
            automation.ClickControl(Control, Button);
        }
    }
}
