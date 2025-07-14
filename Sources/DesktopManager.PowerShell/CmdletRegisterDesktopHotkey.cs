using System;
using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Registers a global desktop hotkey.</summary>
/// <para type="synopsis">Registers a global desktop hotkey.</para>
/// <para type="description">Registers a system wide hotkey and runs the provided script block when triggered.</para>
/// <example>
///   <code>Register-DesktopHotkey -Key F12 -Modifiers Control,Alt -Action { "Hotkey" }</code>
/// </example>
[Cmdlet(VerbsLifecycle.Register, "DesktopHotkey", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletRegisterDesktopHotkey : PSCmdlet {
    /// <summary>
    /// <para type="description">Virtual key to register.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public VirtualKey Key { get; set; }

    /// <summary>
    /// <para type="description">Modifier keys for the hotkey.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public HotkeyModifiers Modifiers { get; set; }

    /// <summary>
    /// <para type="description">Script block executed when the hotkey fires.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public ScriptBlock Action { get; set; }

    /// <summary>
    /// <para type="description">Return the hotkey identifier.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        if (ShouldProcess($"Hotkey {Modifiers}+{Key}", "Register")) {
            int id = HotkeyService.Instance.RegisterHotkey(Modifiers, Key, () => Action.Invoke());
            if (PassThru.IsPresent) {
                WriteObject(id);
            }
        }
    }
}
