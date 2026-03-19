using System;

namespace DesktopManager.Cli;

internal sealed class CommandLineException : Exception {
    public CommandLineException(string message) : base(message) {
    }
}
