using System.IO.Pipelines;
using System.Text.Json;

namespace PipeJsonDemo;

public static class PipeJsonSerializer
{
    public static async ValueTask WriteJsonToPipeAsync<T>(PipeWriter writer, T value, JsonSerializerOptions? options = null)
    {
        options ??= new JsonSerializerOptions { WriteIndented = false };

        await using (var jsonWriter = new Utf8JsonWriter(writer))
        {
            JsonSerializer.Serialize(jsonWriter, value, options);
            await jsonWriter.FlushAsync();
        }

        await writer.FlushAsync();
        await writer.CompleteAsync();
    }

    public static async ValueTask<T?> ReadJsonFromPipeAsync<T>(PipeReader reader, JsonSerializerOptions? options = null)
    {
        options ??= new JsonSerializerOptions();

        using var stream = reader.AsStream();
        var result = await JsonSerializer.DeserializeAsync<T>(stream, options);

        await reader.CompleteAsync();
        return result;
    }
}

public record Person(string Name, int Age);
