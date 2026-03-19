using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DesktopManager.Cli;

internal static class NamedStorage {
    public static string GetLayoutPath(string name) {
        return GetNamedPath("layouts", name);
    }

    public static string GetSnapshotPath(string name) {
        return GetNamedPath("snapshots", name);
    }

    public static IReadOnlyList<string> ListNames(string category) {
        string directory = GetCategoryDirectory(category);
        if (!Directory.Exists(directory)) {
            return Array.Empty<string>();
        }

        return Directory.GetFiles(directory, "*.json")
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string GetNamedPath(string category, string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new CommandLineException("A name is required.");
        }

        string sanitized = SanitizeName(name);
        string directory = GetCategoryDirectory(category);
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, sanitized + ".json");
    }

    private static string GetCategoryDirectory(string category) {
        string root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DesktopManager");
        return Path.Combine(root, category);
    }

    private static string SanitizeName(string name) {
        char[] invalid = Path.GetInvalidFileNameChars();
        var buffer = new List<char>(name.Length);
        foreach (char character in name.Trim()) {
            if (Array.IndexOf(invalid, character) >= 0) {
                continue;
            }

            buffer.Add(character);
        }

        string sanitized = new string(buffer.ToArray());
        if (string.IsNullOrWhiteSpace(sanitized)) {
            throw new CommandLineException($"The name '{name}' does not produce a valid file name.");
        }

        return sanitized;
    }
}
