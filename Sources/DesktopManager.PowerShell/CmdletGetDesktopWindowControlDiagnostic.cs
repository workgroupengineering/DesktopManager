using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Gets shared diagnostics for desktop window control discovery.</summary>
/// <para type="synopsis">Collects Win32 and UI Automation discovery diagnostics for a desktop window.</para>
/// <example>
///   <code>Get-DesktopWindowControlDiagnostic -Name "*Codex*" -UiAutomation -EnsureForeground</code>
/// </example>
/// <example>
///   <code>Get-DesktopWindowControlDiagnostic -Name "*Codex*" -ControlTargetName "codex-sidebar-toggle" -ActionProbe</code>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopWindowControlDiagnostic")]
public sealed class CmdletGetDesktopWindowControlDiagnostic : PSCmdlet {
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
    /// <para type="description">Filter controls by their value. Supports wildcards.</para>
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
    /// <para type="description">Filter by whether the control is enabled.</para>
    /// </summary>
    [Parameter]
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// <para type="description">Filter by whether the control can receive keyboard focus.</para>
    /// </summary>
    [Parameter]
    public bool? IsKeyboardFocusable { get; set; }

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

    /// <summary>
    /// <para type="description">Use a saved control target definition instead of ad-hoc control selector parameters.</para>
    /// </summary>
    [Parameter]
    public string ControlTargetName { get; set; }

    /// <summary>
    /// <para type="description">Maximum number of sample controls to include in each diagnostic result.</para>
    /// </summary>
    [Parameter]
    public int SampleLimit { get; set; } = 10;

    /// <summary>
    /// <para type="description">Include a read-only UI Automation action-resolution probe for the first matched UIA control.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter ActionProbe { get; set; }

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
            IsEnabled = IsEnabled,
            IsKeyboardFocusable = IsKeyboardFocusable,
            EnsureForegroundWindow = EnsureForeground,
            UseUiAutomation = UiAutomation,
            IncludeUiAutomation = IncludeUiAutomation
        };

        WriteObject(string.IsNullOrWhiteSpace(ControlTargetName)
            ? automation.GetControlDiagnostics(windowOptions, controlOptions, allWindows: true, sampleLimit: SampleLimit, includeActionProbe: ActionProbe)
            : automation.GetControlTargetDiagnostics(windowOptions, ControlTargetName, allWindows: true, sampleLimit: SampleLimit, includeActionProbe: ActionProbe), true);
    }
}
