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

    /// <inheritdoc />
    protected override void BeginProcessing() {
        if (ShouldProcess(Control.Text ?? Control.ClassName, "Send keys")) {
            KeyboardInputService.SendToControl(Control, Keys);
        }
    }
}
