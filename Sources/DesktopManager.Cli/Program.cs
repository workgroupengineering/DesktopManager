namespace DesktopManager.Cli;

internal static class Program {
    [STAThread]
    private static int Main(string[] args) {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA) {
            return CliApplication.Run(args);
        }

        int exitCode = 1;
        Exception? failure = null;
        Thread thread = new Thread(() => {
            try {
                exitCode = CliApplication.Run(args);
            } catch (Exception ex) {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure != null) {
            throw failure;
        }

        return exitCode;
    }
}
