using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Sets the check state of a button control.</summary>
/// <para type="synopsis">Checks or unchecks a button control.</para>
/// <example>
///   <code>Set-DesktopControlCheck -Control $ctrl -Check $true</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopControlCheck", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletSetDesktopControlCheck : PSCmdlet {
    /// <summary>
    /// <para type="description">Control to modify.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public WindowControlInfo Control { get; set; } = null!;

    /// <summary>
    /// <para type="description">Desired check state.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 1)]
    public bool Check { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        string action = Check ? "Check" : "Uncheck";
        if (ShouldProcess(Control.Text ?? Control.ClassName, action)) {
            WindowControlService.SetCheckState(Control, Check);
        }
    }
}
