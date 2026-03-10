namespace SCNativeAOTDemo.Tests;

public class EchoCommandTests
{
    [Fact]
    public async Task Echo_BasicMessage_PrintsMessage()
    {
        var result = await CliRunner.RunAsync("echo", "hello");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("hello", result.StdOut.Trim());
        Assert.Empty(result.StdErr.Trim());
    }

    [Fact]
    public async Task Echo_WithRepeat_PrintsMultipleTimes()
    {
        var result = await CliRunner.RunAsync("echo", "hi", "--repeat", "3");

        Assert.Equal(0, result.ExitCode);
        var lines = result.StdOut.Trim().Split(Environment.NewLine);
        Assert.Equal(3, lines.Length);
        Assert.All(lines, line => Assert.Equal("hi", line));
    }

    [Fact]
    public async Task Echo_WithUppercase_ConvertsToUpper()
    {
        var result = await CliRunner.RunAsync("echo", "hello world", "--uppercase");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("HELLO WORLD", result.StdOut.Trim());
    }

    [Fact]
    public async Task Echo_WithReverse_ReversesMessage()
    {
        var result = await CliRunner.RunAsync("echo", "abcde", "--reverse");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("edcba", result.StdOut.Trim());
    }

    [Fact]
    public async Task Echo_WithUppercaseAndReverse_AppliesBoth()
    {
        var result = await CliRunner.RunAsync("echo", "Hello", "--uppercase", "--reverse");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("OLLEH", result.StdOut.Trim());
    }

    [Fact]
    public async Task Echo_WithVerbose_PrintsVerboseOutput()
    {
        var result = await CliRunner.RunAsync("echo", "test", "--verbose");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("[VERBOSE]", result.StdOut);
        Assert.Contains("test", result.StdOut);
    }

    [Fact]
    public async Task Echo_RepeatOutOfRange_ReturnsError()
    {
        var result = await CliRunner.RunAsync("echo", "test", "--repeat", "999");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("between 1 and 50", result.StdErr);
    }
}

public class CalcCommandTests
{
    [Fact]
    public async Task Calc_Sum_ReturnsSum()
    {
        var result = await CliRunner.RunAsync("calc", "1", "2", "3");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("6.00", result.StdOut.Trim());
    }

    [Fact]
    public async Task Calc_Product_ReturnsProduct()
    {
        var result = await CliRunner.RunAsync("calc", "2", "3", "4", "--operation", "product");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("24.00", result.StdOut.Trim());
    }

    [Fact]
    public async Task Calc_Avg_ReturnsAverage()
    {
        var result = await CliRunner.RunAsync("calc", "10", "20", "30", "-o", "avg");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("20.00", result.StdOut.Trim());
    }

    [Fact]
    public async Task Calc_Min_ReturnsMinimum()
    {
        var result = await CliRunner.RunAsync("calc", "5", "1", "9", "-o", "min");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("1.00", result.StdOut.Trim());
    }

    [Fact]
    public async Task Calc_Max_ReturnsMaximum()
    {
        var result = await CliRunner.RunAsync("calc", "5", "1", "9", "-o", "max");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("9.00", result.StdOut.Trim());
    }

    [Fact]
    public async Task Calc_CustomPrecision_FormatsCorrectly()
    {
        var result = await CliRunner.RunAsync("calc", "1", "3", "-o", "avg", "--precision", "4");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("2.0000", result.StdOut.Trim());
    }

    [Fact]
    public async Task Calc_InvalidNumber_ReturnsError()
    {
        var result = await CliRunner.RunAsync("calc", "1", "oops");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Invalid number: oops", result.StdErr);
    }

    [Fact]
    public async Task Calc_InvalidOperation_ReturnsError()
    {
        var result = await CliRunner.RunAsync("calc", "1", "2", "--operation", "divide");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Invalid operation: divide", result.StdErr);
    }

    [Fact]
    public async Task Calc_PrecisionOutOfRange_ReturnsError()
    {
        var result = await CliRunner.RunAsync("calc", "1", "2", "--precision", "15");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("between 0 and 10", result.StdErr);
    }

    [Fact]
    public async Task Calc_Stats_PrintsStatistics()
    {
        var result = await CliRunner.RunAsync("calc", "1", "2", "3", "4", "5", "stats");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Count: 5", result.StdOut);
        Assert.Contains("Sum: 15.00", result.StdOut);
        Assert.Contains("Avg: 3.00", result.StdOut);
        Assert.Contains("Min: 1.00", result.StdOut);
        Assert.Contains("Max: 5.00", result.StdOut);
    }

    [Fact]
    public async Task Calc_Stats_WithPercentiles_PrintsPercentiles()
    {
        var result = await CliRunner.RunAsync("calc", "1", "2", "3", "4", "5", "stats", "--percentiles");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("P25:", result.StdOut);
        Assert.Contains("P75:", result.StdOut);
    }

    [Fact]
    public async Task Calc_WithVerbose_PrintsVerboseOutput()
    {
        var result = await CliRunner.RunAsync("calc", "1", "2", "--verbose");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("[VERBOSE]", result.StdOut);
    }
}

public class TaskCommandTests
{
    [Fact]
    public async Task Task_BasicTitle_PrintsTask()
    {
        var result = await CliRunner.RunAsync("task", "Write unit tests");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Task: Write unit tests", result.StdOut);
        Assert.Contains("Priority: Medium", result.StdOut);
    }

    [Fact]
    public async Task Task_WithPriority_PrintsPriority()
    {
        var result = await CliRunner.RunAsync("task", "Fix bug", "--priority", "critical");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Task: Fix bug", result.StdOut);
        Assert.Contains("Priority: Critical", result.StdOut);
    }

    [Fact]
    public async Task Task_WithDateRange_PrintsDueDate()
    {
        var result = await CliRunner.RunAsync("task", "Ship feature", "--due", "2025-01-01..2025-06-30");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Due: 2025-01-01 to 2025-06-30", result.StdOut);
    }

    [Fact]
    public async Task Task_WithTags_PrintsTags()
    {
        var result = await CliRunner.RunAsync("task", "Review PR", "--tags", "code-review", "urgent");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Tags: code-review, urgent", result.StdOut);
    }

    [Fact]
    public async Task Task_InvalidPriority_ReturnsError()
    {
        var result = await CliRunner.RunAsync("task", "Something", "--priority", "extreme");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Invalid priority: extreme", result.StdErr);
    }

    [Fact]
    public async Task Task_InvalidDateRange_ReturnsError()
    {
        var result = await CliRunner.RunAsync("task", "Something", "--due", "not-a-date");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Invalid date range", result.StdErr);
    }

    [Fact]
    public async Task Task_EndBeforeStart_ReturnsError()
    {
        var result = await CliRunner.RunAsync("task", "Something", "--due", "2025-12-31..2025-01-01");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("End date must be after start date", result.StdErr);
    }

    [Fact]
    public async Task Task_WithVerbose_PrintsVerboseOutput()
    {
        var result = await CliRunner.RunAsync("task", "Debug issue", "--verbose");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("[VERBOSE]", result.StdOut);
    }
}

public class RootCommandTests
{
    [Fact]
    public async Task NoArgs_ShowsUsageInfo()
    {
        var result = await CliRunner.RunAsync();

        // System.CommandLine shows usage/help info on stderr when no command is given
        var combined = result.StdOut + result.StdErr;
        Assert.Contains("echo", combined);
        Assert.Contains("calc", combined);
        Assert.Contains("task", combined);
        Assert.Contains("enum-info", combined);
    }

    [Fact]
    public async Task Help_ShowsCommands()
    {
        var result = await CliRunner.RunAsync("--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("echo", result.StdOut);
        Assert.Contains("calc", result.StdOut);
        Assert.Contains("task", result.StdOut);
        Assert.Contains("enum-info", result.StdOut);
    }

    [Fact]
    public async Task UnknownCommand_ReturnsError()
    {
        var result = await CliRunner.RunAsync("unknown");

        Assert.NotEqual(0, result.ExitCode);
    }
}

public class EnumInfoCommandTests
{
    [Fact]
    public async Task EnumInfo_DefaultEnum_PrintsPriority()
    {
        var result = await CliRunner.RunAsync("enum-info");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Priority:", result.StdOut);
        Assert.Contains("Low = 0", result.StdOut);
        Assert.Contains("Medium = 1", result.StdOut);
        Assert.Contains("High = 2", result.StdOut);
        Assert.Contains("Critical = 3", result.StdOut);
    }

    [Fact]
    public async Task EnumInfo_OutputFormat_PrintsOutputFormatValues()
    {
        var result = await CliRunner.RunAsync("enum-info", "OutputFormat");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("OutputFormat:", result.StdOut);
        Assert.Contains("Text = 0", result.StdOut);
        Assert.Contains("Csv = 1", result.StdOut);
        Assert.Contains("Json = 2", result.StdOut);
    }

    [Fact]
    public async Task EnumInfo_CsvFormat_PrintsCsv()
    {
        var result = await CliRunner.RunAsync("enum-info", "Priority", "--format", "Csv");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Enum,Name,Value", result.StdOut);
        Assert.Contains("Priority,Low,0", result.StdOut);
        Assert.Contains("Priority,Critical,3", result.StdOut);
    }

    [Fact]
    public async Task EnumInfo_JsonFormat_PrintsJson()
    {
        var result = await CliRunner.RunAsync("enum-info", "Priority", "--format", "Json");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("\"enum\": \"Priority\"", result.StdOut);
        Assert.Contains("\"name\": \"Low\"", result.StdOut);
    }

    [Fact]
    public async Task EnumInfo_ShowAll_PrintsBothEnums()
    {
        var result = await CliRunner.RunAsync("enum-info", "--show-all");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Priority:", result.StdOut);
        Assert.Contains("OutputFormat:", result.StdOut);
    }

    [Fact]
    public async Task EnumInfo_UnknownEnum_ReturnsError()
    {
        var result = await CliRunner.RunAsync("enum-info", "Bogus");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Unknown enum: Bogus", result.StdErr);
    }

    [Fact]
    public async Task EnumInfo_WithVerbose_PrintsVerboseOutput()
    {
        var result = await CliRunner.RunAsync("enum-info", "--verbose");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("[VERBOSE]", result.StdOut);
    }
}
