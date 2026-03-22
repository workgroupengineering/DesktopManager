namespace DesktopManager.TestApp;

internal static class Program {
    [STAThread]
    private static void Main(string[] args) {
        ApplicationConfiguration.Initialize();
        TestAppOptions options = TestAppOptions.Parse(args);
        Application.Run(new MainForm(options));
    }
}
