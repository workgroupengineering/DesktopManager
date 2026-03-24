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

    /// <summary>
    /// <para type="description">Re-query the control after the click and report the observed postcondition.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Verify { get; set; }

    /// <summary>
    /// <para type="description">Return a structured mutation result object for the clicked control.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        if (ShouldProcess(Control.Text ?? Control.ClassName, $"Click {Button}")) {
            var automation = new DesktopAutomationService();
            try {
                automation.ClickControl(Control, Button);
                if (Verify.IsPresent || PassThru.IsPresent) {
                    WriteObject(DesktopControlMutationVerifier.Verify(automation, "click", Control));
                }
            } catch (Exception ex) {
                if (!Verify.IsPresent && !PassThru.IsPresent) {
                    throw;
                }

                WriteWarning($"Failed to click control '{Control.Text ?? Control.ClassName}': {ex.Message}");
                WriteObject(DesktopControlMutationVerifier.CreateFailureRecord("click", Control, ex.Message, Verify.IsPresent));
            }
        }
    }
}
