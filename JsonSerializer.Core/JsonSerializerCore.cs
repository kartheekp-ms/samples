using System.Text.Json;
using SystemTextJsonSerializer = System.Text.Json.JsonSerializer;

namespace JsonSerializer.Core;

public static class JsonSerializerCore
{
    private static readonly JsonSerializerOptions DefaultOptions = new(JsonSerializerDefaults.General)
    {
        WriteIndented = false
    };

    public static JsonSerializerOptions DefaultSerializerOptions => DefaultOptions;

    public static string Serialize<T>(T value, JsonSerializerOptions? options = null)
    {
        return SystemTextJsonSerializer.Serialize(value, options ?? DefaultOptions);
    }

    public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null)
    {
        return SystemTextJsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
    }

    public static byte[] SerializeToUtf8Bytes<T>(T value, JsonSerializerOptions? options = null)
    {
        return SystemTextJsonSerializer.SerializeToUtf8Bytes(value, options ?? DefaultOptions);
    }

    public static T? Deserialize<T>(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? options = null)
    {
        return SystemTextJsonSerializer.Deserialize<T>(utf8Json, options ?? DefaultOptions);
    }
}

public sealed record Person(string Name, int Age);
