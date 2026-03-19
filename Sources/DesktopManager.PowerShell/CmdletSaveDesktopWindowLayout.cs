using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Saves the current desktop window layout to a file.</summary>
/// <para type="synopsis">Saves the current desktop window layout.</para>
[Cmdlet(VerbsData.Save, "DesktopWindowLayout", SupportsShouldProcess = true)]
public sealed class CmdletSaveDesktopWindowLayout : PSCmdlet {
    /// <summary>
    /// <para type="description">Path where the layout should be stored.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Path { get; set; }

    /// <summary>
    /// Begin processing.
    /// </summary>
    protected override void BeginProcessing() {
        var automation = new DesktopAutomationService();
        if (ShouldProcess($"Layout file '{Path}'", "Save desktop window layout")) {
            automation.SaveLayout(Path);
            WriteObject(Path);
        }
    }
}
