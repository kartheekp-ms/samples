using System.IO.Pipelines;
using System.Text.Json;

namespace PipeJsonDemo;

public static class PipeJsonSerializer
{
    public static async ValueTask WriteJsonToPipeAsync<T>(PipeWriter writer, T value, JsonSerializerOptions? options = null)
    {
        options ??= new JsonSerializerOptions { WriteIndented = false };

        // PipeWriter implements IBufferWriter<byte>, so Utf8JsonWriter can write directly into it.
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

        // DeserializeAsync works over a Stream; PipeReader can be viewed as one.
        using var stream = reader.AsStream(leaveOpen: true);
        var result = await JsonSerializer.DeserializeAsync<T>(stream, options);

        await reader.CompleteAsync();
        return result;
    }
}

public record Person(string Name, int Age);
