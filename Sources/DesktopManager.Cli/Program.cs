namespace DesktopManager.Cli;

internal static class Program {
    [STAThread]
    private static int Main(string[] args) {
        return CliApplication.Run(args);
    }
}
