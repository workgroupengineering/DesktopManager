using System.Linq;
using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Waits for a desktop window control to appear.</summary>
/// <para type="synopsis">Waits for a matching Win32 or UI Automation control to appear.</para>
/// <example>
///   <code>Wait-DesktopWindowControl -Name "*Notepad*" -ClassName "RichEditD2DPT" -TimeoutMs 5000</code>
/// </example>
/// <example>
///   <code>Wait-DesktopWindowControl -ActiveWindow -UiAutomation -ControlType Button -TextPattern "Show sidebar" -TimeoutMs 5000</code>
/// </example>
[Cmdlet(VerbsLifecycle.Wait, "DesktopWindowControl")]
public sealed class CmdletWaitDesktopWindowControl : PSCmdlet {
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
    /// <para type="description">Timeout in milliseconds. Zero waits indefinitely.</para>
    /// </summary>
    [Parameter]
    public int TimeoutMs { get; set; } = 10000;

    /// <summary>
    /// <para type="description">Polling interval in milliseconds.</para>
    /// </summary>
    [Parameter]
    public int IntervalMs { get; set; } = 100;

    /// <summary>
    /// <para type="description">Return all matching controls instead of only the first one.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter All { get; set; }

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
            UseUiAutomation = UiAutomation,
            IncludeUiAutomation = IncludeUiAutomation
        };

        DesktopControlWaitResult result = automation.WaitForControls(windowOptions, controlOptions, TimeoutMs, IntervalMs, allWindows: true, allControls: All);
        WriteObject(result.Controls.Select(target => target.Control), true);
    }
}
