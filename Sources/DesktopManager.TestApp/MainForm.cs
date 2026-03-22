using System.Drawing;
using System.Windows.Forms.Integration;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsTextBox = System.Windows.Forms.TextBox;
using WpfKey = System.Windows.Input.Key;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace DesktopManager.TestApp;

internal sealed class MainForm : Form {
    private readonly string _baseTitle;
    private readonly WinFormsTextBox _editorTextBox;
    private readonly ElementHost _commandBarHost;
    private readonly WpfTextBox _commandBarTextBox;
    private readonly WinFormsLabel _statusLabel;

    public MainForm(TestAppOptions options) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        _baseTitle = options.Title;
        Text = options.Title;
        Name = "DesktopManagerMcpTestApp";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 960;
        Height = 720;
        MinimumSize = new Size(640, 480);
        bool useCommandBarSurface = string.Equals(options.Surface, "commandbar", StringComparison.OrdinalIgnoreCase);

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
            Text = useCommandBarSurface
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
            Visible = useCommandBarSurface
        };

        var closeButton = new Button {
            Name = "CloseButton",
            Text = "Close",
            AutoSize = true,
            Anchor = AnchorStyles.Right
        };
        closeButton.Click += (_, _) => Close();

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
            RowCount = 3,
            Padding = new Padding(16)
        };
        contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        contentPanel.Controls.Add(titleLabel, 0, 0);
        contentPanel.Controls.Add(hintLabel, 0, 1);
        contentPanel.Controls.Add(_statusLabel, 0, 2);
        contentPanel.Controls.Add(_editorTextBox, 0, 3);

        Controls.Add(_commandBarHost);
        Controls.Add(contentPanel);
        Controls.Add(buttonPanel);

        Shown += (_, _) => {
            if (useCommandBarSurface) {
                _commandBarTextBox.Focus();
                _commandBarTextBox.Select(_commandBarTextBox.Text.Length, 0);
            } else {
                _editorTextBox.Focus();
                _editorTextBox.SelectionStart = _editorTextBox.TextLength;
                _editorTextBox.SelectionLength = 0;
            }
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
}
