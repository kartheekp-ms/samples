using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Globalization;

namespace CommandLineLegacy;

/// <summary>
/// Custom type for configuration file paths with validation.
/// </summary>
public record ConfigPath(string Path, bool Exists);

/// <summary>
/// Custom output format enum parsed from string.
/// </summary>
public enum OutputFormat { Text, Json, Xml }

/// <summary>
/// Extension methods for building commands - spread across the codebase.
/// </summary>
public static class GreetCommandExtensions
{
    public static Command AddGreetCommand(this RootCommand root, Option<bool> verboseOption)
    {
        var greetCommand = new Command("greet", "Print a greeting.");

        var nameArgument = new Argument<string>("name", "Name to greet.");
        nameArgument.AddValidator(result =>
        {
            var value = result.GetValueForArgument(nameArgument);
            if (string.IsNullOrWhiteSpace(value))
            {
                result.ErrorMessage = "Name cannot be empty or whitespace.";
            }
            else if (value.Length < 2)
            {
                result.ErrorMessage = "Name must be at least 2 characters.";
            }
        });

        var timesOption = new Option<int>(new[] { "-t", "--times" }, () => 1, "Number of greetings to print.");
        timesOption.AddValidator(result =>
        {
            var value = result.GetValueForOption(timesOption);
            if (value < 1 || value > 100)
            {
                result.ErrorMessage = "Times must be between 1 and 100.";
            }
        });

        var excitedOption = new Option<bool>("--excited", "Add an exclamation point.");

        // Global option passed down
        greetCommand.AddOption(verboseOption);
        greetCommand.AddArgument(nameArgument);
        greetCommand.AddOption(timesOption);
        greetCommand.AddOption(excitedOption);

        // Async handler with CancellationToken
        greetCommand.SetHandler(async (InvocationContext context) =>
        {
            var cancellationToken = context.GetCancellationToken();
            var name = context.ParseResult.GetValueForArgument(nameArgument);
            var count = context.ParseResult.GetValueForOption(timesOption);
            var excited = context.ParseResult.GetValueForOption(excitedOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var suffix = excited ? "!" : ".";

            if (verbose)
            {
                context.Console.Out.Write($"[VERBOSE] Greeting {name} {count} time(s)" + Environment.NewLine);
            }

            for (var i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(10, cancellationToken); // Simulate async work
                context.Console.Out.Write($"Hello, {name}{suffix}" + Environment.NewLine);
            }
        });

        root.AddCommand(greetCommand);
        return greetCommand;
    }
}

/// <summary>
/// Extension methods for sum command.
/// </summary>
public static class SumCommandExtensions
{
    public static Command AddSumCommand(this RootCommand root, Option<bool> verboseOption)
    {
        var sumCommand = new Command("sum", "Sum integer values.");

        var numbersArgument = new Argument<string[]>("numbers", "Numbers to sum.");

        var absoluteOption = new Option<bool>("--absolute", "Use absolute values.");

        // Custom parser for OutputFormat enum
        var formatOption = new Option<OutputFormat>(
            new[] { "-f", "--format" },
            parseArgument: result =>
            {
                var value = result.Tokens.SingleOrDefault()?.Value ?? "text";
                if (Enum.TryParse<OutputFormat>(value, ignoreCase: true, out var format))
                {
                    return format;
                }
                result.ErrorMessage = $"Invalid format: {value}. Use text, json, or xml.";
                return OutputFormat.Text;
            },
            isDefault: true,
            description: "Output format: text, json, or xml.");

        // Nested subcommand
        var statsSubCommand = new Command("stats", "Show statistics about the sum.");
        var showMedianOption = new Option<bool>("--median", "Include median in stats.");
        statsSubCommand.AddOption(showMedianOption);
        statsSubCommand.AddOption(verboseOption);

        statsSubCommand.SetHandler((InvocationContext context) =>
        {
            var nums = context.ParseResult.GetValueForArgument(numbersArgument);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

            if (nums is null || nums.Length == 0)
            {
                context.Console.Error.Write("No numbers provided for stats." + Environment.NewLine);
                context.ExitCode = 1;
                return;
            }

            var parsed = nums.Select(n => int.Parse(n, CultureInfo.InvariantCulture)).ToList();
            var sum = parsed.Sum();
            var avg = parsed.Average();
            var showMedian = context.ParseResult.GetValueForOption(showMedianOption);

            if (verbose)
            {
                context.Console.Out.Write($"[VERBOSE] Computing stats for {parsed.Count} numbers" + Environment.NewLine);
            }

            context.Console.Out.Write($"Sum: {sum}, Avg: {avg:F2}" + Environment.NewLine);

            if (showMedian)
            {
                var sorted = parsed.OrderBy(x => x).ToList();
                var mid = sorted.Count / 2;
                var median = sorted.Count % 2 == 0
                    ? (sorted[mid - 1] + sorted[mid]) / 2.0
                    : sorted[mid];
                context.Console.Out.Write($"Median: {median:F2}" + Environment.NewLine);
            }
        });

        sumCommand.AddArgument(numbersArgument);
        sumCommand.AddOption(absoluteOption);
        sumCommand.AddOption(formatOption);
        sumCommand.AddOption(verboseOption);
        sumCommand.AddCommand(statsSubCommand);

        sumCommand.SetHandler((InvocationContext context) =>
        {
            var numbers = context.ParseResult.GetValueForArgument(numbersArgument);
            var absolute = context.ParseResult.GetValueForOption(absoluteOption);
            var format = context.ParseResult.GetValueForOption(formatOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

            if (numbers is null || numbers.Length == 0)
            {
                context.Console.Error.Write("At least one number is required." + Environment.NewLine);
                context.ExitCode = 1;
                return;
            }

            if (verbose)
            {
                context.Console.Out.Write($"[VERBOSE] Processing {numbers.Length} numbers with format {format}" + Environment.NewLine);
            }

            var values = new List<int>(numbers.Length);
            foreach (var value in numbers)
            {
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    context.Console.Error.Write($"Invalid number: {value}" + Environment.NewLine);
                    context.ExitCode = 1;
                    return;
                }

                if (absolute)
                {
                    parsed = Math.Abs(parsed);
                }

                values.Add(parsed);
            }

            var sum = values.Sum();

            switch (format)
            {
                case OutputFormat.Json:
                    context.Console.Out.Write($@"{{""sum"":{sum}}}" + Environment.NewLine);
                    break;
                case OutputFormat.Xml:
                    context.Console.Out.Write($"<result><sum>{sum}</sum></result>" + Environment.NewLine);
                    break;
                case OutputFormat.Text:
                default:
                    context.Console.Out.Write(sum.ToString(CultureInfo.InvariantCulture) + Environment.NewLine);
                    break;
            }
        });

        root.AddCommand(sumCommand);
        return sumCommand;
    }
}

/// <summary>
/// Config command with custom type parsing.
/// </summary>
public static class ConfigCommandExtensions
{
    public static Command AddConfigCommand(this RootCommand root)
    {
        var configCommand = new Command("config", "Manage configuration.");

        // Custom type parser for ConfigPath
        var pathArgument = new Argument<ConfigPath>(
            "path",
            parse: result =>
            {
                var path = result.Tokens.SingleOrDefault()?.Value ?? "";
                var exists = File.Exists(path);
                return new ConfigPath(path, exists);
            },
            description: "Path to configuration file.");

        var validateOption = new Option<bool>("--validate", "Validate the config file exists.");
        validateOption.AddValidator(result =>
        {
            // Cross-option validation would happen here
        });

        configCommand.AddArgument(pathArgument);
        configCommand.AddOption(validateOption);

        configCommand.SetHandler((InvocationContext context) =>
        {
            var configPath = context.ParseResult.GetValueForArgument(pathArgument);
            var validate = context.ParseResult.GetValueForOption(validateOption);

            if (validate && !configPath.Exists)
            {
                context.Console.Error.Write($"Config file not found: {configPath.Path}" + Environment.NewLine);
                context.ExitCode = 1;
                return;
            }

            context.Console.Out.Write($"Config path: {configPath.Path} (exists: {configPath.Exists})" + Environment.NewLine);
        });

        root.AddCommand(configCommand);
        return configCommand;
    }
}

public static class CommandLineLegacyApp
{
    /// <summary>
    /// Builds the command tree using CommandLineBuilder with middleware.
    /// </summary>
    public static Parser Build(IConsole? console = null)
    {
        var rootCommand = new RootCommand("Sample command line app using System.CommandLine beta4.");

        // Global option shared across all commands
        var verboseOption = new Option<bool>(new[] { "-v", "--verbose" }, "Enable verbose output.");
        rootCommand.AddGlobalOption(verboseOption);

        // Build commands using extension methods (spread across files)
        rootCommand.AddGreetCommand(verboseOption);
        rootCommand.AddSumCommand(verboseOption);
        rootCommand.AddConfigCommand();

        // Use CommandLineBuilder with middleware chain
        var builder = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseExceptionHandler((exception, context) =>
            {
                context.Console.Error.Write($"[ERROR] {exception.Message}" + Environment.NewLine);
                context.ExitCode = 1;
            })
            .CancelOnProcessTermination();

        // Add custom middleware
        builder.AddMiddleware(async (context, next) =>
        {
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            if (verbose)
            {
                context.Console.Out.Write($"[VERBOSE] Command: {context.ParseResult.CommandResult.Command.Name}" + Environment.NewLine);
            }
            await next(context);
        }, MiddlewareOrder.Configuration);

        return builder.Build();
    }
}

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var parser = CommandLineLegacyApp.Build();
        return await parser.InvokeAsync(args);
    }
}
