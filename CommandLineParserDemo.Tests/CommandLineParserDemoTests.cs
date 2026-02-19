namespace CommandLineParserDemo.Tests;

public class CommandLineParserDemoTests
{
    [Fact]
    public void Greet_WithOptions_PrintsExpectedOutput()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["greet", "Mona", "--times", "2", "--excited"]);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, error.ToString());
        Assert.Equal("Hello, Mona!" + Environment.NewLine + "Hello, Mona!" + Environment.NewLine, output.ToString());
    }

    [Fact]
    public void Greet_DefaultsToSingleGreeting()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["greet", "Sam"]);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, error.ToString());
        Assert.Equal("Hello, Sam." + Environment.NewLine, output.ToString());
    }

    [Fact]
    public void Greet_WithAliasVerbAndAliases_PrintsExpectedOutput()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["hello", "Ana", "--style", "friendly", "--aliases", "Anita,A"]);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, error.ToString());
        Assert.Equal("Hey, Ana." + Environment.NewLine + "Aliases: Anita, A" + Environment.NewLine, output.ToString());
    }

    [Fact]
    public void Greet_MutuallyExclusivePunctuation_ReturnsError()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["greet", "Mona", "--excited", "--calm"]);

        Assert.Equal(1, exitCode);
        Assert.Contains("not compatible", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, output.ToString());
    }

    [Fact]
    public void Greet_WithCalmTitleAndLanguage_PrintsExpectedOutput()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["greet", "Mona", "--language", "german", "--style", "formal", "--title", "Dr", "--calm"]);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, error.ToString());
        Assert.Equal("Guten Tag, Dr Mona..." + Environment.NewLine, output.ToString());
    }

    [Fact]
    public void Greet_TimesOutOfRange_ReturnsError()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["greet", "Sam", "--times", "0"]);

        Assert.Equal(1, exitCode);
        Assert.Contains("times must be between 1 and 20.", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, output.ToString());
    }

    [Fact]
    public void Greet_DryRunAndVerbose_PrintsDiagnostics()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["greet", "Lee", "--dry-run", "-v"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("[verbose:greet]", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[dry-run]", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hello, Lee.", output.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sum_WithAbsoluteDistinctAndJsonStats_PrintsExpectedOutput()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["sum", "-1", "2", "-1", "--absolute", "--distinct", "--format", "json", "--stats"]);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, error.ToString());
        Assert.Contains("\"sum\":3", output.ToString());
        Assert.Contains("\"count\":2", output.ToString());
    }

    [Fact]
    public void Sum_WithDashDash_AllowsNegativeValues()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["sum", "--absolute", "--", "-1", "-2"]);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, error.ToString());
        Assert.Equal("Sum: 3" + Environment.NewLine, output.ToString());
    }

    [Fact]
    public void Sum_WeightsCountMismatch_ReturnsError()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["sum", "1", "2", "3", "--weights", "1,2"]);

        Assert.Equal(1, exitCode);
        Assert.Contains("weights count", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, output.ToString());
    }

    [Fact]
    public void Sum_WeightedRoundedCsv_PrintsExpectedOutput()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["add", "1.25", "2.75", "--weights", "2,1", "--round", "round", "--precision", "1", "--format", "csv"]);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, error.ToString());
        Assert.Equal("sum,count,min,max,avg" + Environment.NewLine + "5.3,2,1.25,2.75,2" + Environment.NewLine, output.ToString());
    }

    [Fact]
    public void Sum_InvalidNumber_ReturnsError()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["sum", "1", "oops"]);

        Assert.Equal(1, exitCode);
        Assert.Contains("Invalid number: oops", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, output.ToString());
    }

    [Fact]
    public void Sum_PrecisionOutOfRange_ReturnsError()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["sum", "1", "--precision", "8"]);

        Assert.Equal(1, exitCode);
        Assert.Contains("precision must be between 0 and 6", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, output.ToString());
    }

    [Fact]
    public void Sum_CompactText_PrintsOnlyValue()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["total", "1", "2", "--compact"]);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, error.ToString());
        Assert.Equal("3" + Environment.NewLine, output.ToString());
    }

    [Fact]
    public void Sum_DryRunAndVerbose_PrintsDiagnostics()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["sum", "1", "2", "--dry-run", "-v"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("[verbose:sum]", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[dry-run]", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sum: 3", output.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sum_JsonWithoutStats_PrintsSumOnly()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["sum", "1", "2", "--format", "json"]);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, error.ToString());
        Assert.Contains("\"sum\":3", output.ToString());
        Assert.DoesNotContain("\"count\":", output.ToString());
    }

    [Fact]
    public void Sum_StatsAndCompactTogether_ReturnsError()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["sum", "1", "2", "--stats", "--compact"]);

        Assert.Equal(1, exitCode);
        Assert.Contains("not compatible", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, output.ToString());
    }

    [Fact]
    public void Config_SetThenGet_UsesGroupActionAndSeparator()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var setExitCode = app.Execute(["config", "--set", "theme=light;region=eu"]);
        var getExitCode = app.Execute(["config", "--get", "theme"]);

        Assert.Equal(0, setExitCode);
        Assert.Equal(0, getExitCode);
        Assert.Equal(string.Empty, error.ToString());
        Assert.Contains("theme=light", output.ToString());
    }

    [Fact]
    public void Config_ListAsJson_PrintsDefaultEntries()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["cfg", "--list", "--format", "json"]);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, error.ToString());
        Assert.Contains("\"theme\":\"dark\"", output.ToString());
        Assert.Contains("\"region\":\"us\"", output.ToString());
        Assert.Contains("\"timezone\":\"utc\"", output.ToString());
    }

    [Fact]
    public void Config_GetMissingKey_ReturnsError()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["config", "--get", "missing"]);

        Assert.Equal(1, exitCode);
        Assert.Contains("Config key not found", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, output.ToString());
    }

    [Fact]
    public void Config_SetInvalidPair_ReturnsError()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["config", "--set", "invalid-pair"]);

        Assert.Equal(1, exitCode);
        Assert.Contains("Invalid key=value pair", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, output.ToString());
    }

    [Fact]
    public void Config_DryRunAndVerbose_PrintsDiagnostics()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["config", "--list", "--dry-run", "-v"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("[verbose:config]", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[dry-run]", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("theme=dark", output.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Config_WithoutAction_ReturnsGroupError()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["config"]);

        Assert.Equal(1, exitCode);
        Assert.Contains("group", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, output.ToString());
    }

    [Fact]
    public void GlobalHelp_ReturnsSuccess()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("advanced CommandLineParser feature showcase", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public void Version_ReturnsSuccess()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["--version"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("CommandLineParserDemo", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public void UnknownOption_ReturnsError()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var app = new CommandLineParserDemoApp(output, error);

        var exitCode = app.Execute(["sum", "1", "--nope"]);

        Assert.Equal(1, exitCode);
        Assert.Contains("option", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, output.ToString());
    }
}
