using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Sends keys to a window control without activating it.</summary>
/// <para type="synopsis">Sends virtual key presses directly to a window control.</para>
/// <example>
///   <code>Send-DesktopControlKey -Control $ctrl -Keys @([DesktopManager.VirtualKey]::VK_A)</code>
/// </example>
[Cmdlet(VerbsCommunications.Send, "DesktopControlKey", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletSendDesktopControlKey : PSCmdlet {
    /// <summary>
    /// <para type="description">Control that will receive the keys.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public WindowControlInfo Control { get; set; } = null!;

    /// <summary>
    /// <para type="description">Keys to send.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 1)]
    public VirtualKey[] Keys { get; set; } = Array.Empty<VirtualKey>();

    /// <summary>
    /// <para type="description">Bring the parent window to the foreground before UI Automation key fallback.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter EnsureForeground { get; set; }

    /// <summary>
    /// <para type="description">Explicitly allow focused foreground input fallback for zero-handle UI Automation controls.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter AllowForegroundInput { get; set; }

    /// <summary>
    /// <para type="description">Re-query the control after sending keys and report the observed postcondition.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Verify { get; set; }

    /// <summary>
    /// <para type="description">Return a structured mutation result object for the targeted control.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        if (ShouldProcess(Control.Text ?? Control.ClassName, "Send keys")) {
            var automation = new DesktopAutomationService();
            try {
                automation.SendControlKeys(Control, Keys, EnsureForeground, AllowForegroundInput);
                if (Verify.IsPresent || PassThru.IsPresent) {
                    WriteObject(DesktopControlMutationVerifier.Verify(
                        automation,
                        "send-keys",
                        Control,
                        requireForeground: EnsureForeground.IsPresent));
                }
            } catch (Exception ex) {
                if (!Verify.IsPresent && !PassThru.IsPresent) {
                    throw;
                }

                WriteWarning($"Failed to send keys to control '{Control.Text ?? Control.ClassName}': {ex.Message}");
                WriteObject(DesktopControlMutationVerifier.CreateFailureRecord("send-keys", Control, ex.Message, Verify.IsPresent));
            }
        }
    }
}
