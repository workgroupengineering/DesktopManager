using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Windows.Forms.Integration;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsTextBox = System.Windows.Forms.TextBox;
using WpfKey = System.Windows.Input.Key;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace DesktopManager.TestApp;

internal sealed class MainForm : Form {
    private const int ForegroundHistoryLimit = 12;
    private const string DragPayloadText = "desktopmanager-drag-payload";

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    private readonly string _baseTitle;
    private readonly bool _useCommandBarSurface;
    private readonly string? _statusFilePath;
    private readonly string? _commandFilePath;
    private readonly WinFormsTextBox _editorTextBox;
    private readonly ElementHost _commandBarHost;
    private readonly WpfTextBox _commandBarTextBox;
    private readonly WinFormsLabel _statusLabel;
    private readonly Panel _dragSourcePanel;
    private readonly WinFormsLabel _dragSourceLabel;
    private readonly Panel _dropTargetPanel;
    private readonly WinFormsLabel _dropTargetLabel;
    private SecondaryFocusForm? _secondaryForm;
    private System.Windows.Forms.Timer? _statusTimer;
    private DateTime _foregroundHoldUntilUtc;
    private bool _foregroundHoldUseCommandBar;
    private int _foregroundHoldRequestCount;
    private int _foregroundHoldRecoveryCount;
    private long _lastObservedForegroundHandle;
    private string _lastObservedForegroundTitle = string.Empty;
    private string _lastObservedForegroundClass = string.Empty;
    private string _lastObservedForegroundChangedUtc = string.Empty;
    private string _lastCommand = string.Empty;
    private Point _dragSourceMouseDownLocation = Point.Empty;
    private string _droppedText = string.Empty;
    private string _dragDropStatus = "Drag source ready.";
    private int _dragDropCount;
    private readonly List<string> _foregroundHistory = [];

    public MainForm(TestAppOptions options) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        _baseTitle = options.Title;
        _useCommandBarSurface = string.Equals(options.Surface, "commandbar", StringComparison.OrdinalIgnoreCase);
        _statusFilePath = options.StatusFilePath;
        _commandFilePath = options.CommandFilePath;
        Text = options.Title;
        Name = "DesktopManagerMcpTestApp";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 960;
        Height = 720;
        MinimumSize = new Size(640, 480);

        var titleLabel = new WinFormsLabel {
            AutoSize = true,
            Text = "DesktopManager MCP Test App",
            Font = new Font(SystemFonts.MessageBoxFont ?? Font, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        };

        var hintLabel = new WinFormsLabel {
            AutoSize = true,
            Text = "This window is used by automated end-to-end tests.",
            Margin = new Padding(0, 0, 0, 12)
        };

        _statusLabel = new WinFormsLabel {
            AutoSize = true,
            Name = "StatusLabel",
            AccessibleName = "StatusLabel",
            Text = _useCommandBarSurface
                ? "Type a value into the command bar and press Enter."
                : "Editor surface ready.",
            Margin = new Padding(0, 0, 0, 12)
        };

        _editorTextBox = new WinFormsTextBox {
            Name = "EditorTextBox",
            AccessibleName = "Editor",
            Multiline = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Dock = DockStyle.Fill,
            Text = options.InitialText
        };

        _commandBarTextBox = new WpfTextBox {
            Name = "CommandBarTextBox",
            Text = options.InitialText,
            MinWidth = 480,
            MinHeight = 30
        };
        _commandBarTextBox.KeyDown += CommandBarTextBox_KeyDown;

        var commandBarPanel = new System.Windows.Controls.StackPanel {
            Orientation = System.Windows.Controls.Orientation.Horizontal
        };
        commandBarPanel.Children.Add(new System.Windows.Controls.Label {
            Content = "Command",
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            Padding = new System.Windows.Thickness(0, 0, 8, 0)
        });
        commandBarPanel.Children.Add(_commandBarTextBox);

        _commandBarHost = new ElementHost {
            Name = "CommandBarHost",
            Dock = DockStyle.Top,
            Height = 42,
            Child = commandBarPanel,
            Visible = _useCommandBarSurface
        };

        var closeButton = new Button {
            Name = "CloseButton",
            Text = "Close",
            AutoSize = true,
            Anchor = AnchorStyles.Right
        };
        closeButton.Click += (_, _) => Close();

        _dragSourceLabel = new WinFormsLabel {
            Name = "DragSourceLabel",
            AccessibleName = "DragSource",
            AutoSize = false,
            Text = "Drag Source\r\nHold and drag me",
            TextAlign = ContentAlignment.MiddleCenter,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(229, 240, 255),
            Dock = DockStyle.Fill,
            Enabled = false
        };

        _dragSourcePanel = new Panel {
            Name = "DragSourcePanel",
            AccessibleName = "DragSource",
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(229, 240, 255),
            Margin = new Padding(0, 0, 12, 0),
            Width = 220,
            Height = 88
        };
        _dragSourcePanel.Controls.Add(_dragSourceLabel);
        _dragSourcePanel.MouseDown += DragSourceLabel_MouseDown;
        _dragSourcePanel.MouseMove += DragSourceLabel_MouseMove;
        _dragSourcePanel.MouseUp += DragSourceLabel_MouseUp;

        _dropTargetLabel = new WinFormsLabel {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = "Drop Target\r\nAwaiting payload",
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(239, 245, 231),
            Enabled = false
        };

        _dropTargetPanel = new Panel {
            Name = "DropTargetPanel",
            AccessibleName = "DropTarget",
            AllowDrop = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(239, 245, 231),
            Margin = new Padding(0),
            Width = 220,
            Height = 88
        };
        _dropTargetPanel.Controls.Add(_dropTargetLabel);
        _dropTargetPanel.DragEnter += DropTargetPanel_DragEnter;
        _dropTargetPanel.DragLeave += DropTargetPanel_DragLeave;
        _dropTargetPanel.DragDrop += DropTargetPanel_DragDrop;

        var dragDropPanel = new FlowLayoutPanel {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 12)
        };
        dragDropPanel.Controls.Add(_dragSourcePanel);
        dragDropPanel.Controls.Add(_dropTargetPanel);

        var buttonPanel = new FlowLayoutPanel {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 12, 0, 0)
        };
        buttonPanel.Controls.Add(closeButton);

        var contentPanel = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(16)
        };
        contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        contentPanel.Controls.Add(titleLabel, 0, 0);
        contentPanel.Controls.Add(hintLabel, 0, 1);
        contentPanel.Controls.Add(_statusLabel, 0, 2);
        contentPanel.Controls.Add(dragDropPanel, 0, 3);
        contentPanel.Controls.Add(_editorTextBox, 0, 4);

        Controls.Add(_commandBarHost);
        Controls.Add(contentPanel);
        Controls.Add(buttonPanel);

        _editorTextBox.TextChanged += (_, _) => WriteStatusSnapshot();
        _commandBarTextBox.TextChanged += (_, _) => WriteStatusSnapshot();
        Activated += (_, _) => WriteStatusSnapshot();
        Deactivate += (_, _) => WriteStatusSnapshot();
        Shown += (_, _) => {
            FocusSurface(_useCommandBarSurface);

            var activationTimer = new System.Windows.Forms.Timer {
                Interval = 250
            };
            int activationAttempts = 0;
            activationTimer.Tick += (_, _) => {
                activationAttempts++;
                FocusSurface(_useCommandBarSurface);
                if (ContainsFocus || activationAttempts >= 4) {
                    activationTimer.Stop();
                    activationTimer.Dispose();
                }
            };
            activationTimer.Start();
            StartStatusChannel();
            WriteStatusSnapshot();
        };
        FormClosed += (_, _) => {
            _statusTimer?.Stop();
            _statusTimer?.Dispose();
            _statusTimer = null;
            WriteStatusSnapshot();
        };
    }

    private void CommandBarTextBox_KeyDown(object? sender, WpfKeyEventArgs e) {
        if (e.Key != WpfKey.Return) {
            return;
        }

        string command = _commandBarTextBox.Text.Trim();
        _statusLabel.Text = string.IsNullOrWhiteSpace(command)
            ? "Accepted empty command."
            : "Accepted command: " + command;
        Text = string.IsNullOrWhiteSpace(command)
            ? _baseTitle + " - Accepted"
            : _baseTitle + " - Accepted - " + command;
        e.Handled = true;
    }

    private void FocusSurface(bool useCommandBarSurface) {
        TopMost = true;
        BringToFront();
        Activate();
        BringWindowToTop(Handle);
        SetForegroundWindow(Handle);
        if (!IsForegroundHoldActive()) {
            TopMost = false;
        }

        if (useCommandBarSurface) {
            _commandBarTextBox.Focus();
            _commandBarTextBox.Select(_commandBarTextBox.Text.Length, 0);
            return;
        }

        _editorTextBox.Focus();
        _editorTextBox.SelectionStart = _editorTextBox.TextLength;
        _editorTextBox.SelectionLength = 0;
        WriteStatusSnapshot();
    }

    private void StartStatusChannel() {
        if (string.IsNullOrWhiteSpace(_statusFilePath) && string.IsNullOrWhiteSpace(_commandFilePath)) {
            return;
        }

        _statusTimer = new System.Windows.Forms.Timer {
            Interval = 100
        };
        _statusTimer.Tick += (_, _) => {
            ProcessCommandFile();
            MaintainForegroundHold();
            WriteStatusSnapshot();
        };
        _statusTimer.Start();
    }

    private void ProcessCommandFile() {
        if (string.IsNullOrWhiteSpace(_commandFilePath) || !File.Exists(_commandFilePath)) {
            return;
        }

        string command;
        try {
            command = File.ReadAllText(_commandFilePath).Trim();
            File.Delete(_commandFilePath);
        } catch {
            return;
        }

        _lastCommand = command;
        AddForegroundHistoryEntry("command", command);

        if (string.Equals(command, "focus-editor", StringComparison.OrdinalIgnoreCase)) {
            FocusSurface(useCommandBarSurface: false);
            return;
        }

        if (string.Equals(command, "focus-commandbar", StringComparison.OrdinalIgnoreCase)) {
            FocusSurface(useCommandBarSurface: true);
            return;
        }

        if (string.Equals(command, "focus-secondary", StringComparison.OrdinalIgnoreCase)) {
            EnsureSecondaryWindow();
            _secondaryForm?.FocusSecondaryWindow();
            return;
        }

        if (command.StartsWith("hold-editor-foreground:", StringComparison.OrdinalIgnoreCase)) {
            if (TryParseDuration(command, "hold-editor-foreground:", out int editorDurationMilliseconds)) {
                StartForegroundHold(useCommandBarSurface: false, editorDurationMilliseconds);
            }

            return;
        }

        if (command.StartsWith("hold-commandbar-foreground:", StringComparison.OrdinalIgnoreCase)) {
            if (TryParseDuration(command, "hold-commandbar-foreground:", out int commandBarDurationMilliseconds)) {
                StartForegroundHold(useCommandBarSurface: true, commandBarDurationMilliseconds);
            }

            return;
        }

        if (string.Equals(command, "stop-foreground-hold", StringComparison.OrdinalIgnoreCase)) {
            StopForegroundHold();
        }
    }

    private void WriteStatusSnapshot() {
        if (string.IsNullOrWhiteSpace(_statusFilePath)) {
            return;
        }

        try {
            UpdateForegroundDiagnostics();
            string? directory = Path.GetDirectoryName(_statusFilePath);
            if (!string.IsNullOrWhiteSpace(directory)) {
                Directory.CreateDirectory(directory);
            }

            var snapshot = new TestAppStatusSnapshot {
                ProcessId = Environment.ProcessId,
                WindowHandle = Handle.ToInt64(),
                EditorHandle = _editorTextBox.IsHandleCreated ? _editorTextBox.Handle.ToInt64() : 0,
                SecondaryWindowHandle = _secondaryForm != null && !_secondaryForm.IsDisposed && _secondaryForm.IsHandleCreated ? _secondaryForm.Handle.ToInt64() : 0,
                WindowTitle = Text,
                ActiveSurface = GetActiveSurface(),
                ContainsFocus = ContainsFocus,
                IsForegroundWindow = GetForegroundWindow() == Handle,
                SecondaryIsForegroundWindow = _secondaryForm != null && !_secondaryForm.IsDisposed && _secondaryForm.IsHandleCreated && GetForegroundWindow() == _secondaryForm.Handle,
                ForegroundHoldActive = IsForegroundHoldActive(),
                ForegroundHoldSurface = _foregroundHoldUseCommandBar ? "commandbar" : "editor",
                ForegroundHoldRequestCount = _foregroundHoldRequestCount,
                ForegroundHoldRecoveryCount = _foregroundHoldRecoveryCount,
                LastObservedForegroundHandle = _lastObservedForegroundHandle,
                LastObservedForegroundTitle = _lastObservedForegroundTitle,
                LastObservedForegroundClass = _lastObservedForegroundClass,
                LastObservedForegroundChangedUtc = _lastObservedForegroundChangedUtc,
                LastCommand = _lastCommand,
                ForegroundHistory = new List<string>(_foregroundHistory),
                EditorText = _editorTextBox.Text,
                SecondaryText = _secondaryForm != null && !_secondaryForm.IsDisposed ? _secondaryForm.CurrentText : string.Empty,
                CommandBarText = _commandBarTextBox.Text,
                StatusText = _statusLabel.Text,
                DragPayload = DragPayloadText,
                DroppedText = _droppedText,
                DragDropCount = _dragDropCount,
                DragDropStatus = _dragDropStatus,
                EditorBounds = GetScreenBounds(_editorTextBox),
                DragSourceBounds = GetScreenBounds(_dragSourcePanel),
                DropTargetBounds = GetScreenBounds(_dropTargetPanel)
            };

            string json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions {
                WriteIndented = true
            });
            File.WriteAllText(_statusFilePath, json);
        } catch {
            // Best-effort diagnostics only.
        }
    }

    private string GetActiveSurface() {
        if (_secondaryForm != null && !_secondaryForm.IsDisposed && _secondaryForm.ContainsFocus) {
            return "secondary";
        }

        if (_commandBarHost.Visible && _commandBarTextBox.IsKeyboardFocused) {
            return "commandbar";
        }

        if (_editorTextBox.Focused) {
            return "editor";
        }

        return _useCommandBarSurface ? "commandbar" : "editor";
    }

    private void EnsureSecondaryWindow() {
        if (_secondaryForm != null && !_secondaryForm.IsDisposed) {
            return;
        }

        _secondaryForm = new SecondaryFocusForm(_baseTitle, WriteStatusSnapshot);
        _secondaryForm.Show(this);
    }

    private void MaintainForegroundHold() {
        if (!IsForegroundHoldActive()) {
            if (TopMost) {
                StopForegroundHold();
            }

            return;
        }

        bool needsFocus = _foregroundHoldUseCommandBar
            ? !_commandBarTextBox.IsKeyboardFocused || GetForegroundWindow() != Handle
            : !_editorTextBox.Focused || GetForegroundWindow() != Handle;
        if (!needsFocus) {
            return;
        }

        _foregroundHoldRecoveryCount++;
        AddForegroundHistoryEntry("hold-recover", _foregroundHoldUseCommandBar ? "commandbar" : "editor");
        FocusSurface(_foregroundHoldUseCommandBar);
    }

    private void StartForegroundHold(bool useCommandBarSurface, int durationMilliseconds) {
        if (durationMilliseconds <= 0) {
            return;
        }

        _foregroundHoldUseCommandBar = useCommandBarSurface;
        _foregroundHoldUntilUtc = DateTime.UtcNow.AddMilliseconds(durationMilliseconds);
        _foregroundHoldRequestCount++;
        AddForegroundHistoryEntry(
            "hold-start",
            (_foregroundHoldUseCommandBar ? "commandbar" : "editor") + " durationMs=" + durationMilliseconds);
        TopMost = true;
        FocusSurface(useCommandBarSurface);
    }

    private bool IsForegroundHoldActive() {
        return _foregroundHoldUntilUtc > DateTime.UtcNow;
    }

    private void StopForegroundHold() {
        if (_foregroundHoldUntilUtc != DateTime.MinValue) {
            AddForegroundHistoryEntry("hold-stop", _foregroundHoldUseCommandBar ? "commandbar" : "editor");
        }

        _foregroundHoldUntilUtc = DateTime.MinValue;
        TopMost = false;
    }

    private void UpdateForegroundDiagnostics() {
        IntPtr foregroundHandle = GetForegroundWindow();
        long handleValue = foregroundHandle.ToInt64();
        if (_lastObservedForegroundHandle == handleValue) {
            return;
        }

        _lastObservedForegroundHandle = handleValue;
        _lastObservedForegroundTitle = ReadWindowText(foregroundHandle);
        _lastObservedForegroundClass = ReadWindowClassName(foregroundHandle);
        _lastObservedForegroundChangedUtc = DateTime.UtcNow.ToString("O");
        AddForegroundHistoryEntry(
            "foreground",
            $"0x{_lastObservedForegroundHandle:X} '{_lastObservedForegroundTitle}' class='{_lastObservedForegroundClass}'");
    }

    private static string ReadWindowText(IntPtr handle) {
        if (handle == IntPtr.Zero) {
            return string.Empty;
        }

        var builder = new StringBuilder(512);
        return GetWindowText(handle, builder, builder.Capacity) > 0 ? builder.ToString() : string.Empty;
    }

    private static string ReadWindowClassName(IntPtr handle) {
        if (handle == IntPtr.Zero) {
            return string.Empty;
        }

        var builder = new StringBuilder(256);
        return GetClassName(handle, builder, builder.Capacity) > 0 ? builder.ToString() : string.Empty;
    }

    private static bool TryParseDuration(string command, string prefix, out int durationMilliseconds) {
        durationMilliseconds = 0;
        if (!command.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        string rawDuration = command.Substring(prefix.Length).Trim();
        return int.TryParse(rawDuration, out durationMilliseconds) && durationMilliseconds > 0;
    }

    private void AddForegroundHistoryEntry(string category, string detail) {
        string entry = DateTime.UtcNow.ToString("O") + " [" + category + "] " + detail;
        _foregroundHistory.Add(entry);
        if (_foregroundHistory.Count > ForegroundHistoryLimit) {
            _foregroundHistory.RemoveAt(0);
        }
    }

    private void DragSourceLabel_MouseDown(object? sender, MouseEventArgs e) {
        if (e.Button != MouseButtons.Left) {
            return;
        }

        _dragSourceMouseDownLocation = e.Location;
        _dragSourcePanel.Capture = true;
        _dragDropStatus = "Drag armed from source.";
        WriteStatusSnapshot();
    }

    private void DragSourceLabel_MouseMove(object? sender, MouseEventArgs e) {
        if ((e.Button & MouseButtons.Left) != MouseButtons.Left) {
            return;
        }

        Size dragSize = SystemInformation.DragSize;
        Rectangle dragBounds = new(
            _dragSourceMouseDownLocation.X - dragSize.Width / 2,
            _dragSourceMouseDownLocation.Y - dragSize.Height / 2,
            dragSize.Width,
            dragSize.Height);
        if (dragBounds.Contains(e.Location)) {
            return;
        }

        _dragDropStatus = "Dragging payload.";
        WriteStatusSnapshot();
        _dragSourcePanel.Capture = false;
        DragDropEffects effect = _dragSourcePanel.DoDragDrop(DragPayloadText, DragDropEffects.Copy);
        _dragDropStatus = effect == DragDropEffects.None ? "Drag canceled." : "Drag completed.";
        WriteStatusSnapshot();
    }

    private void DragSourceLabel_MouseUp(object? sender, MouseEventArgs e) {
        _dragSourcePanel.Capture = false;
    }

    private void DropTargetPanel_DragEnter(object? sender, DragEventArgs e) {
        if (e.Data?.GetDataPresent(DataFormats.UnicodeText) == true || e.Data?.GetDataPresent(DataFormats.Text) == true) {
            e.Effect = DragDropEffects.Copy;
            _dragDropStatus = "Drop target armed.";
            _dropTargetPanel.BackColor = Color.FromArgb(214, 235, 204);
            _dropTargetLabel.BackColor = _dropTargetPanel.BackColor;
            WriteStatusSnapshot();
            return;
        }

        e.Effect = DragDropEffects.None;
    }

    private void DropTargetPanel_DragLeave(object? sender, EventArgs e) {
        _dropTargetPanel.BackColor = Color.FromArgb(239, 245, 231);
        _dropTargetLabel.BackColor = _dropTargetPanel.BackColor;
        _dragDropStatus = "Drag left drop target.";
        WriteStatusSnapshot();
    }

    private void DropTargetPanel_DragDrop(object? sender, DragEventArgs e) {
        string droppedText = e.Data?.GetData(DataFormats.UnicodeText)?.ToString()
            ?? e.Data?.GetData(DataFormats.Text)?.ToString()
            ?? string.Empty;
        _droppedText = droppedText;
        _dragDropCount++;
        _dragDropStatus = string.IsNullOrWhiteSpace(droppedText) ? "Drop completed with empty payload." : "Drop completed.";
        _dropTargetPanel.BackColor = Color.FromArgb(198, 227, 184);
        _dropTargetLabel.BackColor = _dropTargetPanel.BackColor;
        _dropTargetLabel.Text = string.IsNullOrWhiteSpace(droppedText)
            ? "Drop Target\r\nReceived empty payload"
            : "Drop Target\r\nReceived: " + droppedText;
        _statusLabel.Text = string.IsNullOrWhiteSpace(droppedText)
            ? "Drop completed with empty payload."
            : "Dropped payload: " + droppedText;
        WriteStatusSnapshot();
    }

    private static TestAppControlBounds GetScreenBounds(Control control) {
        if (!control.IsHandleCreated) {
            return new TestAppControlBounds();
        }

        Rectangle screenBounds = control.RectangleToScreen(control.ClientRectangle);
        return new TestAppControlBounds {
            Left = screenBounds.Left,
            Top = screenBounds.Top,
            Width = screenBounds.Width,
            Height = screenBounds.Height
        };
    }
}
