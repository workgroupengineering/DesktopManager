using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Gets controls of a desktop window.</summary>
/// <para type="synopsis">Enumerates child controls of a desktop window.</para>
/// <example>
///   <code>Get-DesktopWindowControl -Name "*Notepad*"</code>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopWindowControl")]
public sealed class CmdletGetDesktopWindowControl : PSCmdlet {
    /// <summary>
    /// <para type="description">Title of the window to match. Supports wildcards.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        var manager = new WindowManager();
        var windows = manager.GetWindows(Name);
        var enumerator = new ControlEnumerator();
        foreach (var window in windows) {
            var controls = enumerator.EnumerateControls(window.Handle);
            WriteObject(controls, true);
        }
    }
}
