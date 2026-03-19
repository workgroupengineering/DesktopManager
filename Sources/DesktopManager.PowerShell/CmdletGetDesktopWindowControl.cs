using System.Linq;
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
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByName")]
    public string Name { get; set; } = "*";

    /// <summary>
    /// <para type="description">Use the current foreground window instead of matching by name.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "ActiveWindow")]
    public SwitchParameter ActiveWindow { get; set; }

    /// <summary>
    /// <para type="description">Filter controls by class name. Supports wildcards.</para>
    /// </summary>
    [Parameter]
    public string ClassName { get; set; } = "*";

    /// <summary>
    /// <para type="description">Filter controls by visible text. Supports wildcards.</para>
    /// </summary>
    [Parameter]
    public string TextPattern { get; set; } = "*";

    /// <summary>
    /// <para type="description">Filter controls by current value. Supports wildcards.</para>
    /// </summary>
    [Parameter]
    public string ValuePattern { get; set; } = "*";

    /// <summary>
    /// <para type="description">Filter controls by control identifier.</para>
    /// </summary>
    [Parameter]
    public int? Id { get; set; }

    /// <summary>
    /// <para type="description">Filter UI Automation controls by automation identifier. Supports wildcards.</para>
    /// </summary>
    [Parameter]
    public string AutomationId { get; set; } = "*";

    /// <summary>
    /// <para type="description">Filter UI Automation controls by control type. Supports wildcards.</para>
    /// </summary>
    [Parameter]
    public string ControlType { get; set; } = "*";

    /// <summary>
    /// <para type="description">Filter UI Automation controls by framework identifier. Supports wildcards.</para>
    /// </summary>
    [Parameter]
    public string FrameworkId { get; set; } = "*";

    /// <summary>
    /// <para type="description">Optional saved control target name to resolve instead of ad-hoc selectors.</para>
    /// </summary>
    [Parameter]
    public string ControlTargetName { get; set; }

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
        var windowOptions = new WindowQueryOptions {
            TitlePattern = Name,
            ActiveWindow = ActiveWindow,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = ActiveWindow ? true : null
        };
        var controlOptions = new WindowControlQueryOptions {
            ClassNamePattern = ClassName,
            TextPattern = TextPattern,
            ValuePattern = ValuePattern,
            Id = Id,
            AutomationIdPattern = AutomationId,
            ControlTypePattern = ControlType,
            FrameworkIdPattern = FrameworkId,
            IsEnabled = Enabled ? true : Disabled ? false : null,
            IsKeyboardFocusable = Focusable ? true : NotFocusable ? false : null,
            SupportsBackgroundClick = BackgroundClick ? true : null,
            SupportsBackgroundText = BackgroundText ? true : null,
            SupportsBackgroundKeys = BackgroundKeys ? true : null,
            SupportsForegroundInputFallback = ForegroundFallback ? true : null,
            EnsureForegroundWindow = EnsureForeground,
            UseUiAutomation = UiAutomation,
            IncludeUiAutomation = IncludeUiAutomation
        };
        var controls = string.IsNullOrWhiteSpace(ControlTargetName)
            ? automation.GetControls(windowOptions, controlOptions, allWindows: true, allControls: true)
            : automation.GetControlTargets(windowOptions, ControlTargetName, allWindows: true, allControls: true);
        WriteObject(controls.Select(target => target.Control), true);
    }
}
