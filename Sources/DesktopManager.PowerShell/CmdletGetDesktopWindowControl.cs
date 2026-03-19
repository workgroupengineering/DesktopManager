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
    /// <para type="description">Use UI Automation for control discovery.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter UiAutomation { get; set; }

    /// <summary>
    /// <para type="description">Combine Win32 and UI Automation control discovery.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter IncludeUiAutomation { get; set; }

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
            Id = Id,
            AutomationIdPattern = AutomationId,
            ControlTypePattern = ControlType,
            FrameworkIdPattern = FrameworkId,
            UseUiAutomation = UiAutomation,
            IncludeUiAutomation = IncludeUiAutomation
        };
        var controls = automation.GetControls(windowOptions, controlOptions, allWindows: true, allControls: true);
        WriteObject(controls.Select(target => target.Control), true);
    }
}
