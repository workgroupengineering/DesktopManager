using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace DesktopManager.Cli;

internal sealed class McpServer {
    private const string ProtocolVersion = "2025-06-18";
    private static readonly byte[] HeaderSeparator = Encoding.ASCII.GetBytes("\r\n\r\n");

    public int Run() {
        using Stream input = Console.OpenStandardInput();
        using Stream output = Console.OpenStandardOutput();

        while (true) {
            string? payload = ReadMessage(input);
            if (payload == null) {
                return 0;
            }

            using JsonDocument document = JsonDocument.Parse(payload);
            JsonElement root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array) {
                foreach (JsonElement item in root.EnumerateArray()) {
                    ProcessMessage(item, output);
                }
            } else {
                ProcessMessage(root, output);
            }
        }
    }

    private static void ProcessMessage(JsonElement message, Stream output) {
        if (!message.TryGetProperty("method", out JsonElement methodElement)) {
            return;
        }

        string? method = methodElement.GetString();
        bool hasId = message.TryGetProperty("id", out JsonElement idElement);
        JsonElement parameters = message.TryGetProperty("params", out JsonElement paramsElement) ? paramsElement : default;

        switch (method) {
            case "initialize":
                WriteSuccess(output, idElement, new {
                    protocolVersion = ProtocolVersion,
                    capabilities = new {
                        prompts = new { listChanged = false },
                        resources = new { subscribe = false, listChanged = false },
                        tools = new { listChanged = false }
                    },
                    serverInfo = new {
                        name = "DesktopManager.Cli",
                        version = "0.1.0"
                    },
                    instructions = "Use read-only inspection tools before mutating the desktop. Layouts and snapshots are stored per-user. Snapshots currently store window layout state only."
                });
                return;
            case "notifications/initialized":
                return;
            case "ping":
                WriteSuccess(output, idElement, new { });
                return;
            case "tools/list":
                WriteSuccess(output, idElement, new {
                    tools = McpCatalog.GetTools()
                });
                return;
            case "tools/call":
                HandleToolCall(output, idElement, parameters);
                return;
            case "resources/list":
                WriteSuccess(output, idElement, new {
                    resources = McpCatalog.GetResources()
                });
                return;
            case "resources/templates/list":
                WriteSuccess(output, idElement, new {
                    resourceTemplates = Array.Empty<object>()
                });
                return;
            case "resources/read":
                HandleResourceRead(output, idElement, parameters);
                return;
            case "prompts/list":
                WriteSuccess(output, idElement, new {
                    prompts = McpCatalog.GetPrompts()
                });
                return;
            case "prompts/get":
                HandlePromptGet(output, idElement, parameters);
                return;
            default:
                if (hasId) {
                    WriteError(output, idElement, -32601, $"Method '{method}' is not supported.");
                }
                return;
        }
    }

    private static void HandleToolCall(Stream output, JsonElement id, JsonElement parameters) {
        if (!parameters.TryGetProperty("name", out JsonElement nameElement)) {
            WriteError(output, id, -32602, "Tool calls require a tool name.");
            return;
        }

        string name = nameElement.GetString() ?? string.Empty;
        JsonElement arguments = parameters.TryGetProperty("arguments", out JsonElement argsElement) ? argsElement : default;

        bool success = McpCatalog.TryCallTool(name, arguments, out object result, out string? error);
        WriteSuccess(output, id, new {
            content = new[] {
                new {
                    type = "text",
                    text = JsonUtilities.Serialize(result)
                }
            },
            structuredContent = result,
            isError = !success,
            message = error
        });
    }

    private static void HandleResourceRead(Stream output, JsonElement id, JsonElement parameters) {
        if (!parameters.TryGetProperty("uri", out JsonElement uriElement)) {
            WriteError(output, id, -32602, "Resource reads require a uri.");
            return;
        }

        try {
            string uri = uriElement.GetString() ?? string.Empty;
            object content = McpCatalog.ReadResource(uri);
            WriteSuccess(output, id, new {
                contents = new[] {
                    new {
                        uri,
                        mimeType = "application/json",
                        text = JsonUtilities.Serialize(content)
                    }
                }
            });
        } catch (CommandLineException ex) {
            WriteError(output, id, -32602, ex.Message);
        }
    }

    private static void HandlePromptGet(Stream output, JsonElement id, JsonElement parameters) {
        if (!parameters.TryGetProperty("name", out JsonElement nameElement)) {
            WriteError(output, id, -32602, "Prompt requests require a name.");
            return;
        }

        JsonElement arguments = parameters.TryGetProperty("arguments", out JsonElement argsElement) ? argsElement : default;
        try {
            WriteSuccess(output, id, McpCatalog.GetPrompt(nameElement.GetString() ?? string.Empty, arguments));
        } catch (CommandLineException ex) {
            WriteError(output, id, -32602, ex.Message);
        }
    }

    private static string? ReadMessage(Stream input) {
        var headerBuffer = new List<byte>();
        while (true) {
            int value = input.ReadByte();
            if (value == -1) {
                return headerBuffer.Count == 0 ? null : throw new EndOfStreamException("Unexpected end of stream while reading MCP headers.");
            }

            headerBuffer.Add((byte)value);
            if (EndsWith(headerBuffer, HeaderSeparator)) {
                break;
            }
        }

        string headers = Encoding.ASCII.GetString(headerBuffer.ToArray());
        int contentLength = 0;
        foreach (string line in headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)) {
            if (!line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            string valueText = line.Substring("Content-Length:".Length).Trim();
            if (!int.TryParse(valueText, out contentLength) || contentLength < 0) {
                throw new InvalidDataException("Invalid Content-Length header.");
            }
        }

        if (contentLength == 0) {
            throw new InvalidDataException("Missing Content-Length header.");
        }

        byte[] payload = new byte[contentLength];
        int offset = 0;
        while (offset < payload.Length) {
            int read = input.Read(payload, offset, payload.Length - offset);
            if (read <= 0) {
                throw new EndOfStreamException("Unexpected end of stream while reading an MCP message body.");
            }

            offset += read;
        }

        return Encoding.UTF8.GetString(payload);
    }

    private static void WriteSuccess(Stream output, JsonElement id, object result) {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new {
            jsonrpc = "2.0",
            id = JsonSerializer.Deserialize<object>(id.GetRawText(), JsonUtilities.SerializerOptions),
            result
        }, JsonUtilities.SerializerOptions);
        WriteMessage(output, payload);
    }

    private static void WriteError(Stream output, JsonElement id, int code, string message) {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new {
            jsonrpc = "2.0",
            id = JsonSerializer.Deserialize<object>(id.GetRawText(), JsonUtilities.SerializerOptions),
            error = new {
                code,
                message
            }
        }, JsonUtilities.SerializerOptions);
        WriteMessage(output, payload);
    }

    private static void WriteMessage(Stream output, byte[] payload) {
        byte[] header = Encoding.ASCII.GetBytes($"Content-Length: {payload.Length}\r\n\r\n");
        output.Write(header, 0, header.Length);
        output.Write(payload, 0, payload.Length);
        output.Flush();
    }

    private static bool EndsWith(List<byte> source, byte[] suffix) {
        if (source.Count < suffix.Length) {
            return false;
        }

        for (int index = 0; index < suffix.Length; index++) {
            if (source[source.Count - suffix.Length + index] != suffix[index]) {
                return false;
            }
        }

        return true;
    }
}
