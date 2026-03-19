using System.Text.Json;

namespace DesktopManager.Cli;

internal static class JsonUtilities {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = true
    };

    public static string Serialize(object value) {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    public static JsonSerializerOptions SerializerOptions => JsonOptions;
}
