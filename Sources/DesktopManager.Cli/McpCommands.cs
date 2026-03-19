namespace DesktopManager.Cli;

internal static class McpCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "serve" => Serve(arguments),
            _ => throw new CommandLineException($"Unknown mcp command '{action}'.")
        };
    }

    private static int Serve(CommandLineArguments arguments) {
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(new {
                status = "ready",
                protocolVersion = "2025-06-18",
                mode = "stdio"
            });
            return 0;
        }

        return new McpServer().Run();
    }
}
