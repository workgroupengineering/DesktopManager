namespace DesktopManager.TestApp;

internal sealed class TestAppOptions {
    private const string DefaultTitle = "DesktopManager-McpTestApp";
    private const string DefaultInitialText = "seed";
    private const string DefaultSurface = "editor";

    private TestAppOptions(string title, string initialText, string surface) {
        Title = title;
        InitialText = initialText;
        Surface = surface;
    }

    public string Title { get; }

    public string InitialText { get; }

    public string Surface { get; }

    public static TestAppOptions Parse(string[] args) {
        string title = DefaultTitle;
        string initialText = DefaultInitialText;
        string surface = DefaultSurface;

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
            }
        }

        return new TestAppOptions(title, initialText, surface);
    }
}
