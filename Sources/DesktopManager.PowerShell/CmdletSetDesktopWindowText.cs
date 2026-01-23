using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Sets text in a desktop window.</summary>
/// <para type="synopsis">Pastes or types text into a desktop window.</para>
/// <example>
///   <code>Set-DesktopWindowText -Name "Notepad" -Text "Hello"</code>
/// </example>
/// <example>
///   <code>Set-DesktopWindowText -Name "Notepad" -Text "Hello" -Type</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopWindowText", DefaultParameterSetName = "Paste", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletSetDesktopWindowText : PSCmdlet {
    /// <summary>
    /// <para type="description">Window title to match. Supports wildcards.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name { get; set; }

    /// <summary>
    /// <para type="description">Text to paste or type.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 1)]
    public string Text { get; set; }

    /// <summary>
    /// <para type="description">Use the clipboard paste method.</para>
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "Paste")]
    public SwitchParameter Paste { get; set; }

    /// <summary>
    /// <para type="description">Simulate typing the text.</para>
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "Type")]
    public SwitchParameter Type { get; set; }

    /// <summary>
    /// <para type="description">Delay in milliseconds between characters when typing.</para>
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "Type")]
    public int Delay { get; set; } = 0;

    /// <summary>
    /// <para type="description">Use WM_CHAR messages instead of SendInput when typing.</para>
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "Type")]
    public SwitchParameter UseMessage { get; set; }

    /// <summary>
    /// <para type="description">Number of clipboard open retries.</para>
    /// </summary>
    [Parameter]
    public int ClipboardRetryCount { get; set; } = 5;

    /// <summary>
    /// <para type="description">Delay between clipboard retries in milliseconds.</para>
    /// </summary>
    [Parameter]
    public int ClipboardRetryDelayMilliseconds { get; set; } = 50;

    /// <summary>
    /// <para type="description">Number of activation retries.</para>
    /// </summary>
    [Parameter]
    public int ActivationRetryCount { get; set; } = 3;

    /// <summary>
    /// <para type="description">Delay between activation retries in milliseconds.</para>
    /// </summary>
    [Parameter]
    public int ActivationRetryDelayMilliseconds { get; set; } = 100;

    /// <summary>
    /// <para type="description">Number of input retries.</para>
    /// </summary>
    [Parameter]
    public int InputRetryCount { get; set; } = 2;

    /// <summary>
    /// <para type="description">Do not activate the window before sending input.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter NoActivate { get; set; }

    /// <summary>
    /// <para type="description">Restore focus to the previously active window.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter RestoreFocus { get; set; }

    /// <summary>
    /// <para type="description">Preserve and restore clipboard text.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PreserveClipboard { get; set; }

    /// <summary>
    /// <para type="description">Enable safe mode (no activation, preserve clipboard).</para>
    /// </summary>
    [Parameter]
    public SwitchParameter SafeMode { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        var manager = new WindowManager();
        var windows = manager.GetWindows(Name);

        foreach (var window in windows) {
            string action = ParameterSetName == "Type" ? "Type text" : "Paste text";
            if (ShouldProcess(window.Title, action)) {
                var options = new WindowInputOptions {
                    ActivateWindow = !NoActivate,
                    RestoreFocus = RestoreFocus,
                    PreserveClipboard = PreserveClipboard,
                    ClipboardRetryCount = ClipboardRetryCount,
                    ClipboardRetryDelayMilliseconds = ClipboardRetryDelayMilliseconds,
                    ActivationRetryCount = ActivationRetryCount,
                    ActivationRetryDelayMilliseconds = ActivationRetryDelayMilliseconds,
                    InputRetryCount = InputRetryCount,
                    KeyDelayMilliseconds = Delay,
                    UseSendInput = !UseMessage
                };

                if (SafeMode) {
                    options.ActivateWindow = false;
                    options.RestoreFocus = false;
                    options.PreserveClipboard = true;
                }

                if (ParameterSetName == "Type" && !options.ActivateWindow && options.UseSendInput) {
                    options.UseSendInput = false;
                    WriteVerbose("NoActivate is set; using WM_CHAR to avoid typing into the foreground window.");
                }

                if (ParameterSetName == "Type") {
                    manager.TypeText(window, Text, options);
                } else {
                    manager.PasteText(window, Text, options);
                }
            }
        }
    }
}

