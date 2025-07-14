using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Unregisters a global desktop hotkey.</summary>
/// <para type="synopsis">Unregisters a global desktop hotkey.</para>
/// <para type="description">Removes a hotkey previously registered with Register-DesktopHotkey.</para>
/// <example>
///   <code>Unregister-DesktopHotkey -Id 1</code>
/// </example>
[Cmdlet(VerbsLifecycle.Unregister, "DesktopHotkey", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletUnregisterDesktopHotkey : PSCmdlet {
    /// <summary>
    /// <para type="description">Identifier of the hotkey to remove.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public int Id { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        if (ShouldProcess(Id.ToString(), "Unregister")) {
            HotkeyService.Instance.UnregisterHotkey(Id);
        }
    }
}
