using System;
using System.Linq;
using System.Management.Automation;

namespace DesktopManager.PowerShell {
    /// <summary>Snaps a desktop window to a predefined position.</summary>
    /// <para type="synopsis">Snaps a desktop window to a predefined position.</para>
    /// <example>
    ///   <para>Snap Notepad to the left half of the screen</para>
    ///   <code>Set-DesktopWindowSnap -Name "*Notepad*" -Position Left</code>
    /// </example>
    [Cmdlet(VerbsCommon.Set, "DesktopWindowSnap", SupportsShouldProcess = true)]
    public class CmdletSetDesktopWindowSnap : PSCmdlet {
        /// <summary>
        /// <para type="description">The window title to snap. Supports wildcards.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; }

        /// <summary>
        /// <para type="description">The snap position.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
        public SnapPosition Position { get; set; }

        /// <summary>
        /// <para type="description">Re-query the snapped window and report the observed postcondition.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Verify { get; set; }

        /// <summary>
        /// <para type="description">Geometry verification tolerance in pixels for post-snap checks.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int VerificationTolerancePixels { get; set; } = 10;

        /// <summary>
        /// <para type="description">Return a structured mutation result object for each matching window.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru { get; set; }

        /// <summary>
        /// Snaps matching windows to the chosen position.
        /// </summary>
        protected override void BeginProcessing() {
            var automation = new DesktopAutomationService();
            var windows = automation.GetWindows(new WindowQueryOptions {
                TitlePattern = Name
            });
            foreach (var window in windows) {
                if (ShouldProcess($"Window '{window.Title}'", $"Snap {Position}")) {
                    DesktopWindowMutationRecord result = null;
                    try {
                        automation.SnapWindows(new WindowQueryOptions {
                            Handle = window.Handle,
                            IncludeHidden = true,
                            IncludeCloaked = true,
                            IncludeOwned = true,
                            IncludeEmptyTitles = true
                        }, Position);

                        if (Verify.IsPresent || PassThru.IsPresent) {
                            result = DesktopWindowMutationVerifier.Verify(
                                automation,
                                "snap",
                                window,
                                VerificationTolerancePixels);
                        }
                    } catch (Exception ex) {
                        WriteWarning($"Failed to snap window '{window.Title}': {ex.Message}");
                        if (Verify.IsPresent || PassThru.IsPresent) {
                            result = DesktopWindowMutationVerifier.CreateFailureRecord("snap", window, ex.Message, Verify.IsPresent, VerificationTolerancePixels);
                        }
                    }

                    if (result != null) {
                        WriteObject(result);
                    }
                }
            }
        }
    }
}
