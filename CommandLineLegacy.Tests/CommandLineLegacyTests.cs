using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;

namespace CommandLineLegacy.Tests;

public class CommandLineLegacyTests
{
    [Fact]
    public async Task Greet_WithOptions_PrintsExpectedOutput()
    {
        var console = new TestConsole();
        var parser = CommandLineLegacyApp.Build(console);

        var exitCode = await parser.InvokeAsync(new[] { "greet", "Mona", "--times", "2", "--excited" }, console);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, console.Error.ToString()?.Trim() ?? string.Empty);
        Assert.Equal("Hello, Mona!" + Environment.NewLine + "Hello, Mona!", console.Out.ToString()?.Trim());
    }

    [Fact]
    public async Task Greet_DefaultsToSingleGreeting()
    {
        var console = new TestConsole();
        var parser = CommandLineLegacyApp.Build(console);

        var exitCode = await parser.InvokeAsync(new[] { "greet", "Sam" }, console);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, console.Error.ToString()?.Trim() ?? string.Empty);
        Assert.Equal("Hello, Sam.", console.Out.ToString()?.Trim());
    }

    [Fact]
    public async Task Greet_WithVerbose_PrintsVerboseOutput()
    {
        var console = new TestConsole();
        var parser = CommandLineLegacyApp.Build(console);

        var exitCode = await parser.InvokeAsync(new[] { "greet", "Sam", "--verbose" }, console);

        Assert.Equal(0, exitCode);
        Assert.Contains("[VERBOSE]", console.Out.ToString());
        Assert.Contains("Hello, Sam.", console.Out.ToString());
    }

    [Fact]
    public async Task Greet_NameTooShort_ReturnsValidationError()
    {
        var console = new TestConsole();
        var parser = CommandLineLegacyApp.Build(console);

        var exitCode = await parser.InvokeAsync(new[] { "greet", "X" }, console);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("at least 2 characters", console.Error.ToString());
    }

    [Fact]
    public async Task Greet_TimesOutOfRange_ReturnsValidationError()
    {
        var console = new TestConsole();
        var parser = CommandLineLegacyApp.Build(console);

        var exitCode = await parser.InvokeAsync(new[] { "greet", "Sam", "--times", "999" }, console);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("between 1 and 100", console.Error.ToString());
    }

    [Fact]
    public async Task Sum_WithAbsoluteAndJsonFormat_PrintsExpectedOutput()
    {
        var console = new TestConsole();
        var parser = CommandLineLegacyApp.Build(console);

        var exitCode = await parser.InvokeAsync(new[] { "sum", "1", "2", "3", "--absolute", "--format", "json" }, console);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, console.Error.ToString()?.Trim() ?? string.Empty);
        Assert.Equal("{\"sum\":6}", console.Out.ToString()?.Trim());
    }

    [Fact]
    public async Task Sum_WithXmlFormat_PrintsXmlOutput()
    {
        var console = new TestConsole();
        var parser = CommandLineLegacyApp.Build(console);

        var exitCode = await parser.InvokeAsync(new[] { "sum", "1", "2", "3", "--format", "xml" }, console);

        Assert.Equal(0, exitCode);
        Assert.Equal("<result><sum>6</sum></result>", console.Out.ToString()?.Trim());
    }

    [Fact]
    public async Task Sum_InvalidNumber_ReturnsError()
    {
        var console = new TestConsole();
        var parser = CommandLineLegacyApp.Build(console);

        var exitCode = await parser.InvokeAsync(new[] { "sum", "1", "oops" }, console);

        Assert.Equal(1, exitCode);
        Assert.Contains("Invalid number: oops", console.Error.ToString());
    }

    [Fact]
    public async Task Sum_InvalidFormat_ReturnsError()
    {
        var console = new TestConsole();
        var parser = CommandLineLegacyApp.Build(console);

        var exitCode = await parser.InvokeAsync(new[] { "sum", "1", "2", "--format", "csv" }, console);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("Invalid format: csv", console.Error.ToString());
    }

    [Fact]
    public async Task Sum_Stats_PrintsStatistics()
    {
        var console = new TestConsole();
        var parser = CommandLineLegacyApp.Build(console);

        var exitCode = await parser.InvokeAsync(new[] { "sum", "1", "2", "3", "stats", "--median" }, console);

        Assert.Equal(0, exitCode);
        Assert.Contains("Sum: 6", console.Out.ToString());
        Assert.Contains("Median:", console.Out.ToString());
    }

    [Fact]
    public async Task Config_ValidPath_PrintsPath()
    {
        var console = new TestConsole();
        var parser = CommandLineLegacyApp.Build(console);

        var exitCode = await parser.InvokeAsync(new[] { "config", "somefile.json" }, console);

        Assert.Equal(0, exitCode);
        Assert.Contains("Config path: somefile.json", console.Out.ToString());
    }

    [Fact]
    public async Task Config_ValidateNonExistent_ReturnsError()
    {
        var console = new TestConsole();
        var parser = CommandLineLegacyApp.Build(console);

        var exitCode = await parser.InvokeAsync(new[] { "config", "nonexistent.json", "--validate" }, console);

        Assert.Equal(1, exitCode);
        Assert.Contains("Config file not found", console.Error.ToString());
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
