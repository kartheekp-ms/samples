using System.CommandLine;
using System.CommandLine.IO;

namespace CommandLineLegacy.Tests;

public class CommandLineLegacyTests
{
    [Fact]
    public void Greet_WithOptions_PrintsExpectedOutput()
    {
        var app = CommandLineLegacyApp.Build();
        var console = new TestConsole();

        var exitCode = app.Invoke(new[] { "greet", "Mona", "--times", "2", "--excited" }, console);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, console.Error.ToString()?.Trim() ?? string.Empty);
        Assert.Equal("Hello, Mona!" + Environment.NewLine + "Hello, Mona!", console.Out.ToString()?.Trim());
    }

    [Fact]
    public void Greet_DefaultsToSingleGreeting()
    {
        var app = CommandLineLegacyApp.Build();
        var console = new TestConsole();

        var exitCode = app.Invoke(new[] { "greet", "Sam" }, console);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, console.Error.ToString()?.Trim() ?? string.Empty);
        Assert.Equal("Hello, Sam.", console.Out.ToString()?.Trim());
    }

    [Fact]
    public void Sum_WithAbsoluteAndJsonFormat_PrintsExpectedOutput()
    {
        var app = CommandLineLegacyApp.Build();
        var console = new TestConsole();

        var exitCode = app.Invoke(new[] { "sum", "1", "2", "3", "--absolute", "--format", "json" }, console);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, console.Error.ToString()?.Trim() ?? string.Empty);
        Assert.Equal("{\"sum\":6}", console.Out.ToString()?.Trim());
    }

    [Fact]
    public void Sum_InvalidNumber_ReturnsError()
    {
        var app = CommandLineLegacyApp.Build();
        var console = new TestConsole();

        var exitCode = app.Invoke(new[] { "sum", "1", "oops" }, console);

        Assert.Equal(1, exitCode);
        Assert.Contains("Invalid number: oops", console.Error.ToString());
    }
}

public class TestConsole : IConsole
{
    public TestConsole()
    {
        Out = new TestStreamWriter();
        Error = new TestStreamWriter();
    }

    public IStandardStreamWriter Out { get; }
    public bool IsOutputRedirected => true;

    public IStandardStreamWriter Error { get; }
    public bool IsErrorRedirected => true;

    public bool IsInputRedirected => false;
}

public class TestStreamWriter : IStandardStreamWriter
{
    private readonly StringWriter _writer = new();

    public void Write(string? value) => _writer.Write(value);

    public override string ToString() => _writer.ToString();
}
