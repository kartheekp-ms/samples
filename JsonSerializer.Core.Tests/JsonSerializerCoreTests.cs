using System.Text.Json;

namespace JsonSerializer.Core.Tests;

public class JsonSerializerCoreTests
{
    [Fact]
    public void Serialize_Deserialize_RoundTrip()
    {
        var input = new Person("Alice", 30);

        var json = JsonSerializerCore.Serialize(input);
        var output = JsonSerializerCore.Deserialize<Person>(json);

        Assert.NotNull(output);
        Assert.Equal(input.Name, output.Name);
        Assert.Equal(input.Age, output.Age);
    }

    [Fact]
    public void Serialize_Deserialize_UsesSafeDefaults()
    {
        var input = new UnsafePayload("<script>alert('x')</script>");

        var json = JsonSerializerCore.Serialize(input);

        Assert.Contains("\\u003Cscript\\u003E", json);
    }

    [Fact]
    public void SerializeToUtf8Bytes_Deserialize_RoundTrip()
    {
        var input = new Person("Bob", 22);

        var bytes = JsonSerializerCore.SerializeToUtf8Bytes(input);
        var output = JsonSerializerCore.Deserialize<Person>(bytes);

        Assert.NotNull(output);
        Assert.Equal(input.Name, output.Name);
        Assert.Equal(input.Age, output.Age);
    }

    [Fact]
    public void DefaultSerializerOptions_UsesGeneralDefaults()
    {
        var options = JsonSerializerCore.DefaultSerializerOptions;

        Assert.NotNull(options);
        Assert.Null(options.PropertyNamingPolicy);
        Assert.False(options.WriteIndented);
    }
}

public sealed record UnsafePayload(string Html);
