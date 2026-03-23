namespace DesktopManager.TestApp;

internal sealed class TestAppOptions {
    private const string DefaultTitle = "DesktopManager-McpTestApp";
    private const string DefaultInitialText = "seed";
    private const string DefaultSurface = "editor";

    private TestAppOptions(string title, string initialText, string surface, string? statusFilePath, string? commandFilePath) {
        Title = title;
        InitialText = initialText;
        Surface = surface;
        StatusFilePath = statusFilePath;
        CommandFilePath = commandFilePath;
    }

    public string Title { get; }

    public string InitialText { get; }

    public string Surface { get; }

    public string? StatusFilePath { get; }

    public string? CommandFilePath { get; }

    public static TestAppOptions Parse(string[] args) {
        string title = DefaultTitle;
        string initialText = DefaultInitialText;
        string surface = DefaultSurface;
        string? statusFilePath = null;
        string? commandFilePath = null;

        for (int index = 0; index < args.Length; index++) {
            string argument = args[index];
            if (string.Equals(argument, "--title", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length) {
                title = args[++index];
                continue;
            }

            if (string.Equals(argument, "--text", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length) {
                initialText = args[++index];
                continue;
            }

            if (string.Equals(argument, "--surface", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length) {
                surface = args[++index];
                continue;
            }

            if (string.Equals(argument, "--status-file", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length) {
                statusFilePath = args[++index];
                continue;
            }

            if (string.Equals(argument, "--command-file", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length) {
                commandFilePath = args[++index];
            }
        }

        return new TestAppOptions(title, initialText, surface, statusFilePath, commandFilePath);
    }
}
