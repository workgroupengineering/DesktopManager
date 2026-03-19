using System.Management.Automation;
using System.Text.RegularExpressions;

namespace DesktopManager.PowerShell {
    /// <summary>Gets information about desktop windows.</summary>
    /// <para type="synopsis">Gets information about desktop windows.</para>
    /// <para type="description">Retrieves information about desktop windows. Supports filters for title, process, class, visibility, state, topmost, and Z-order.</para>
    /// <example>
    ///   <para>Get all visible windows</para>
    ///   <code>Get-DesktopWindow</code>
    /// </example>
    /// <example>
    ///   <para>Get windows with "Notepad" in the title</para>
    ///   <code>Get-DesktopWindow -Name "*Notepad*"</code>
    /// </example>
    [Cmdlet(VerbsCommon.Get, "DesktopWindow")]
    public class CmdletGetDesktopWindow : PSCmdlet {
        /// <summary>
        /// <para type="description">Filter windows by title. Supports wildcards.</para>
        /// </summary>
        [Parameter(Position = 0)]
        public string Name { get; set; } = "*";

        /// <summary>
        /// <para type="description">Filter windows by process name. Supports wildcards.</para>
        /// </summary>
        [Parameter]
        public string ProcessName { get; set; } = "*";

        /// <summary>
        /// <para type="description">Filter windows by window class name. Supports wildcards.</para>
        /// </summary>
        [Parameter]
        public string ClassName { get; set; } = "*";

        /// <summary>
        /// <para type="description">Filter window titles using a regular expression.</para>
        /// </summary>
        [Parameter]
        public Regex Regex { get; set; }

        /// <summary>
        /// <para type="description">Filter windows by process ID.</para>
        /// </summary>
        [Parameter]
        public int ProcessId { get; set; } = 0;

        /// <summary>
        /// <para type="description">Return only the current foreground window.</para>
        /// </summary>
        [Parameter]
        public SwitchParameter ActiveWindow { get; set; }

        /// <summary>
        /// <para type="description">Include hidden windows in the results.</para>
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeHidden { get; set; }

        /// <summary>
        /// <para type="description">Include DWM-cloaked windows in the results.</para>
        /// </summary>
        [Parameter]
        public bool IncludeCloaked { get; set; } = true;

        /// <summary>
        /// <para type="description">Include owned windows in the results.</para>
        /// </summary>
        [Parameter]
        public bool IncludeOwned { get; set; } = true;

        /// <summary>
        /// <para type="description">Filter windows by visibility. Use $true for visible or $false for hidden.</para>
        /// </summary>
        [Parameter]
        public bool? IsVisible { get; set; }

        /// <summary>
        /// <para type="description">Filter windows by state (Normal, Minimize, Maximize).</para>
        /// </summary>
        [Parameter]
        public WindowState? State { get; set; }

        /// <summary>
        /// <para type="description">Filter windows by topmost state.</para>
        /// </summary>
        [Parameter]
        public bool? IsTopMost { get; set; }

        /// <summary>
        /// <para type="description">Minimum Z-order index (0 is top-most).</para>
        /// </summary>
        [Parameter]
        public int? ZOrderMin { get; set; }

        /// <summary>
        /// <para type="description">Maximum Z-order index (0 is top-most).</para>
        /// </summary>
        [Parameter]
        public int? ZOrderMax { get; set; }

        /// <summary>
        /// Retrieves and outputs matching windows.
        /// </summary>
        protected override void BeginProcessing() {
            var automation = new DesktopAutomationService();
            var options = new WindowQueryOptions {
                TitlePattern = Name,
                ProcessNamePattern = ProcessName,
                ClassNamePattern = ClassName,
                TitleRegex = Regex,
                ActiveWindow = ActiveWindow,
                ProcessId = ProcessId,
                IncludeHidden = IncludeHidden,
                IncludeCloaked = IncludeCloaked,
                IncludeOwned = IncludeOwned,
                IncludeEmptyTitles = ActiveWindow ? true : null,
                IsVisible = IsVisible,
                State = State,
                IsTopMost = IsTopMost,
                ZOrderMin = ZOrderMin,
                ZOrderMax = ZOrderMax
            };
            var windows = automation.GetWindows(options);
            WriteObject(windows, true);
        }
    }
}
