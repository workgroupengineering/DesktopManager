using System.Runtime.InteropServices;
using WinFormsTextBox = System.Windows.Forms.TextBox;

namespace DesktopManager.TestApp;

internal sealed class SecondaryFocusForm : Form {
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    private readonly Action _statusCallback;
    private readonly WinFormsTextBox _textBox;

    public SecondaryFocusForm(string title, Action statusCallback) {
        _statusCallback = statusCallback ?? throw new ArgumentNullException(nameof(statusCallback));

        Text = title + " - Secondary";
        Name = "DesktopManagerSecondaryFocusWindow";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 480;
        Height = 200;

        _textBox = new WinFormsTextBox {
            Name = "SecondaryTextBox",
            Multiline = true,
            Dock = DockStyle.Fill
        };
        _textBox.TextChanged += (_, _) => _statusCallback();

        var label = new Label {
            Text = "Secondary focus helper",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8)
        };

        Controls.Add(_textBox);
        Controls.Add(label);

        Activated += (_, _) => _statusCallback();
        Deactivate += (_, _) => _statusCallback();
        Shown += (_, _) => {
            FocusSecondaryWindow();
            _statusCallback();
        };
        FormClosed += (_, _) => _statusCallback();
    }

    public string CurrentText => _textBox.Text;

    public void FocusSecondaryWindow() {
        TopMost = true;
        Show();
        BringToFront();
        Activate();
        BringWindowToTop(Handle);
        SetForegroundWindow(Handle);
        _textBox.Focus();
        _textBox.SelectionStart = _textBox.TextLength;
        _textBox.SelectionLength = 0;
        TopMost = false;
        _statusCallback();
    }
}
