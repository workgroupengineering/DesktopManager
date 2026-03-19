using System;
using System.Linq;

namespace DesktopManager.Cli;

internal static class SnapshotCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "save" => Save(arguments),
            "restore" => Restore(arguments),
            "list" => List(arguments),
            _ => throw new CommandLineException($"Unknown snapshot command '{action}'.")
        };
    }

    private static int Save(CommandLineArguments arguments) {
        return WritePathResult(arguments, DesktopOperations.SaveSnapshot(arguments.GetRequiredCommandPart(2, "snapshot name")));
    }

    private static int Restore(CommandLineArguments arguments) {
        return WritePathResult(
            arguments,
            DesktopOperations.RestoreSnapshot(arguments.GetRequiredCommandPart(2, "snapshot name"), arguments.GetBoolFlag("validate")));
    }

    private static int List(CommandLineArguments arguments) {
        var names = DesktopOperations.ListSnapshots();
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(names);
            return 0;
        }

        if (names.Count == 0) {
            Console.WriteLine("No named snapshots found.");
            return 0;
        }

        foreach (string name in names.OrderBy(value => value)) {
            Console.WriteLine(name);
        }
        return 0;
    }

    private static int WritePathResult(CommandLineArguments arguments, NamedStateResult payload) {
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(payload);
            return 0;
        }

        Console.WriteLine($"{payload.Action}: {payload.Name}");
        Console.WriteLine(payload.Path);
        if (!string.IsNullOrWhiteSpace(payload.Scope)) {
            Console.WriteLine($"scope: {payload.Scope}");
        }
        return 0;
    }
}
