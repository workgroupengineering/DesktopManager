using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Sets text on a specific window control.</summary>
/// <para type="synopsis">Writes text directly to a control without depending on foreground focus.</para>
/// <example>
///   <code>Set-DesktopControlText -Control $ctrl -Text "Hello world"</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopControlText", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletSetDesktopControlText : PSCmdlet {
    /// <summary>
    /// <para type="description">Control to update.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public WindowControlInfo Control { get; set; } = null!;

    /// <summary>
    /// <para type="description">Text to apply to the control.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 1)]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Bring the parent window to the foreground before UI Automation text fallback.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter EnsureForeground { get; set; }

    /// <summary>
    /// <para type="description">Explicitly allow focused foreground input fallback for zero-handle UI Automation controls.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter AllowForegroundInput { get; set; }

    /// <summary>
    /// <para type="description">Re-query the control after setting text and report the observed postcondition.</para>
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
        if (ShouldProcess(Control.Text ?? Control.ClassName, "Set text")) {
            var automation = new DesktopAutomationService();
            try {
                automation.SetControlText(Control, Text, EnsureForeground, AllowForegroundInput);
                if (Verify.IsPresent || PassThru.IsPresent) {
                    WriteObject(DesktopControlMutationVerifier.Verify(
                        automation,
                        "set-text",
                        Control,
                        expectedText: Text,
                        requireForeground: EnsureForeground.IsPresent));
                }
            } catch (Exception ex) {
                if (!Verify.IsPresent && !PassThru.IsPresent) {
                    throw;
                }

                WriteWarning($"Failed to set text on control '{Control.Text ?? Control.ClassName}': {ex.Message}");
                WriteObject(DesktopControlMutationVerifier.CreateFailureRecord("set-text", Control, ex.Message, Verify.IsPresent));
            }
        }
    }
}
