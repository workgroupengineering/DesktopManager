using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Linq;

namespace DesktopManager.PowerShell;

/// <summary>Scrolls at a point relative to a desktop window.</summary>
/// <para type="synopsis">Simulates mouse-wheel scrolling at a point relative to a matching desktop window.</para>
/// <example>
///   <code>Invoke-DesktopWindowScroll -Name "*Notepad*" -X 200 -Y 200 -Delta -120 -ClientArea</code>
/// </example>
[Cmdlet(VerbsLifecycle.Invoke, "DesktopWindowScroll", SupportsShouldProcess = true)]
public sealed class CmdletInvokeDesktopWindowScroll : PSCmdlet {
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
    /// <para type="description">Horizontal coordinate relative to the target window.</para>
    /// </summary>
    [Parameter]
    public int? X { get; set; }

    /// <summary>
    /// <para type="description">Vertical coordinate relative to the target window.</para>
    /// </summary>
    [Parameter]
    public int? Y { get; set; }

    /// <summary>
    /// <para type="description">Horizontal coordinate ratio from 0 to 1 relative to the target bounds.</para>
    /// </summary>
    [Parameter]
    public double? XRatio { get; set; }

    /// <summary>
    /// <para type="description">Vertical coordinate ratio from 0 to 1 relative to the target bounds.</para>
    /// </summary>
    [Parameter]
    public double? YRatio { get; set; }

    /// <summary>
    /// <para type="description">Saved reusable target name to scroll at instead of supplying coordinates directly.</para>
    /// </summary>
    [Parameter]
    public string TargetName { get; set; }

    /// <summary>
    /// <para type="description">Scroll delta. Positive values scroll up.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public int Delta { get; set; }

    /// <summary>
    /// <para type="description">Activate the window before scrolling.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Activate { get; set; }

    /// <summary>
    /// <para type="description">Interpret the supplied coordinates relative to the client area instead of the outer window bounds.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter ClientArea { get; set; }

    /// <summary>
    /// <para type="description">Re-query the target window after the scroll action and report the observed postcondition.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Verify { get; set; }

    /// <summary>
    /// <para type="description">Geometry verification tolerance in pixels.</para>
    /// </summary>
    [Parameter]
    public int VerificationTolerancePixels { get; set; } = 10;

    /// <summary>
    /// <para type="description">Return a structured mutation result object for the scrolled window.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        var automation = new DesktopAutomationService();
        var options = new WindowQueryOptions {
            TitlePattern = Name,
            ActiveWindow = ActiveWindow,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = ActiveWindow ? true : null
        };

        string targetText = !string.IsNullOrWhiteSpace(TargetName)
            ? $"target '{TargetName}'"
            : X.HasValue && Y.HasValue
                ? $"{X},{Y}"
                : $"{XRatio},{YRatio}";
        if (ShouldProcess(ActiveWindow ? "active window" : Name, $"Scroll {Delta} at {targetText}")) {
            WindowInfo requestedWindow = automation.GetWindows(options).FirstOrDefault();
            try {
                IReadOnlyList<WindowInfo> windows = !string.IsNullOrWhiteSpace(TargetName)
                    ? automation.ScrollWindowTarget(options, TargetName, Delta, Activate, all: false)
                    : automation.ScrollWindowPoint(options, X, Y, XRatio, YRatio, Delta, Activate, ClientArea, all: false);
                if (!Verify.IsPresent && !PassThru.IsPresent) {
                    WriteObject(windows, true);
                    return;
                }

                WriteMutationResult(automation, windows, requestedWindow);
            } catch (Exception ex) {
                if (!Verify.IsPresent && !PassThru.IsPresent) {
                    throw;
                }

                WriteWarning($"Failed to scroll window '{requestedWindow?.Title ?? Name}': {ex.Message}");
                if (requestedWindow != null) {
                    WriteObject(DesktopWindowMutationVerifier.CreateFailureRecord("scroll", requestedWindow, ex.Message, Verify.IsPresent, VerificationTolerancePixels));
                }
            }
        }
    }

    private void WriteMutationResult(DesktopAutomationService automation, IReadOnlyList<WindowInfo> windows, WindowInfo requestedWindow) {
        if (windows.Count == 0 && requestedWindow != null) {
            WriteObject(DesktopWindowMutationVerifier.Verify(
                automation,
                "scroll",
                requestedWindow,
                VerificationTolerancePixels,
                requireForeground: Activate.IsPresent));
            return;
        }

        foreach (WindowInfo window in windows) {
            WriteObject(DesktopWindowMutationVerifier.Verify(
                automation,
                "scroll",
                window,
                VerificationTolerancePixels,
                requireForeground: Activate.IsPresent));
        }
    }
}
