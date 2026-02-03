using System.IO.Pipelines;

namespace PipeJsonDemo.Tests;

public class PipeJsonSerializerTests
{
    [Fact]
    public async Task RoundTrip_Person_Success()
    {
        var pipe = new Pipe();
        var input = new Person("Alice", 30);

        await PipeJsonSerializer.WriteJsonToPipeAsync(pipe.Writer, input);
        var output = await PipeJsonSerializer.ReadJsonFromPipeAsync<Person>(pipe.Reader);

        Assert.NotNull(output);
        Assert.Equal("Alice", output.Name);
        Assert.Equal(30, output.Age);
    }

    [Fact]
    public async Task Serialize_NullValue_Success()
    {
        var pipe = new Pipe();
        Person? input = null;

        await PipeJsonSerializer.WriteJsonToPipeAsync(pipe.Writer, input);
        var output = await PipeJsonSerializer.ReadJsonFromPipeAsync<Person>(pipe.Reader);

        Assert.Null(output);
    }

    [Fact]
    public async Task RoundTrip_ComplexObject_Success()
    {
        var pipe = new Pipe();
        var input = new TestData
        {
            Id = 42,
            Name = "Test",
            Tags = new[] { "tag1", "tag2" },
            Nested = new Person("Bob", 25)
        };

        await PipeJsonSerializer.WriteJsonToPipeAsync(pipe.Writer, input);
        var output = await PipeJsonSerializer.ReadJsonFromPipeAsync<TestData>(pipe.Reader);

        Assert.NotNull(output);
        Assert.Equal(42, output.Id);
        Assert.Equal("Test", output.Name);
        Assert.Equal(2, output.Tags?.Length);
        Assert.NotNull(output.Nested);
        Assert.Equal("Bob", output.Nested.Name);
    }

    [Fact]
    public async Task RoundTrip_EmptyString_Success()
    {
        var pipe = new Pipe();
        var input = new Person("", 0);

        await PipeJsonSerializer.WriteJsonToPipeAsync(pipe.Writer, input);
        var output = await PipeJsonSerializer.ReadJsonFromPipeAsync<Person>(pipe.Reader);

        Assert.NotNull(output);
        Assert.Equal("", output.Name);
        Assert.Equal(0, output.Age);
    }
}

public class TestData
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string[]? Tags { get; set; }
    public Person? Nested { get; set; }
}
