using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Saves or updates a reusable control target.</summary>
/// <para type="synopsis">Persists a named DesktopManager control target.</para>
/// <example>
///   <code>Set-DesktopControlTarget -Name "edge-address" -ControlType "Edit" -BackgroundText -UiAutomation</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopControlTarget", SupportsShouldProcess = true)]
public sealed class CmdletSetDesktopControlTarget : PSCmdlet {
    /// <summary>
    /// <para type="description">Saved control target name.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name { get; set; }

    /// <summary>
    /// <para type="description">Optional target description.</para>
    /// </summary>
    [Parameter]
    public string Description { get; set; }

    /// <summary>
    /// <para type="description">Control class filter. Supports wildcards.</para>
    /// </summary>
    [Parameter]
    public string ClassName { get; set; } = "*";

    /// <summary>
    /// <para type="description">Control text filter. Supports wildcards.</para>
    /// </summary>
    [Parameter]
    public string TextPattern { get; set; } = "*";

    /// <summary>
    /// <para type="description">Control value filter. Supports wildcards.</para>
    /// </summary>
    [Parameter]
    public string ValuePattern { get; set; } = "*";

    /// <summary>
    /// <para type="description">Exact control identifier.</para>
    /// </summary>
    [Parameter]
    public int? Id { get; set; }

    /// <summary>
    /// <para type="description">Exact control handle in decimal or hexadecimal format.</para>
    /// </summary>
    [Parameter]
    public string Handle { get; set; }

    /// <summary>
    /// <para type="description">UI Automation automation identifier filter. Supports wildcards.</para>
    /// </summary>
    [Parameter]
    public string AutomationId { get; set; } = "*";

    /// <summary>
    /// <para type="description">UI Automation control type filter. Supports wildcards.</para>
    /// </summary>
    [Parameter]
    public string ControlType { get; set; } = "*";

    /// <summary>
    /// <para type="description">UI Automation framework identifier filter. Supports wildcards.</para>
    /// </summary>
    [Parameter]
    public string FrameworkId { get; set; } = "*";

    /// <summary>
    /// <para type="description">Require the control to be enabled.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Enabled { get; set; }

    /// <summary>
    /// <para type="description">Require the control to be disabled.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Disabled { get; set; }

    /// <summary>
    /// <para type="description">Require the control to accept keyboard focus.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Focusable { get; set; }

    /// <summary>
    /// <para type="description">Require the control to not accept keyboard focus.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter NotFocusable { get; set; }

    /// <summary>
    /// <para type="description">Require controls that support background-safe click or invoke actions.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter BackgroundClick { get; set; }

    /// <summary>
    /// <para type="description">Require controls that support background-safe text updates.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter BackgroundText { get; set; }

    /// <summary>
    /// <para type="description">Require controls that support background-safe key delivery.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter BackgroundKeys { get; set; }

    /// <summary>
    /// <para type="description">Require controls that support explicit foreground input fallback.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter ForegroundFallback { get; set; }

    /// <summary>
    /// <para type="description">Use UI Automation for control discovery.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter UiAutomation { get; set; }

    /// <summary>
    /// <para type="description">Combine Win32 and UI Automation control discovery.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter IncludeUiAutomation { get; set; }

    /// <summary>
    /// <para type="description">Bring the target window to the foreground before UI Automation discovery.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter EnsureForeground { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        var automation = new DesktopAutomationService();
        var definition = new DesktopControlTargetDefinition {
            Description = Description,
            ClassNamePattern = ClassName,
            TextPattern = TextPattern,
            ValuePattern = ValuePattern,
            Id = Id,
            Handle = Handle,
            AutomationIdPattern = AutomationId,
            ControlTypePattern = ControlType,
            FrameworkIdPattern = FrameworkId,
            IsEnabled = Enabled ? true : Disabled ? false : null,
            IsKeyboardFocusable = Focusable ? true : NotFocusable ? false : null,
            SupportsBackgroundClick = BackgroundClick ? true : null,
            SupportsBackgroundText = BackgroundText ? true : null,
            SupportsBackgroundKeys = BackgroundKeys ? true : null,
            SupportsForegroundInputFallback = ForegroundFallback ? true : null,
            UseUiAutomation = UiAutomation,
            IncludeUiAutomation = IncludeUiAutomation,
            EnsureForegroundWindow = EnsureForeground
        };

        if (ShouldProcess(Name, "Save reusable control target")) {
            WriteObject(automation.SaveControlTarget(Name, definition));
        }
    }
}
