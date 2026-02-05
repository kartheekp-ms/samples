using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;

namespace CommandLineLegacy;

public static class CommandLineLegacyApp
{
    public static RootCommand Build()
    {
        var rootCommand = new RootCommand("Sample command line app using System.CommandLine beta4.");

        // Greet command
        var greetCommand = new Command("greet", "Print a greeting.");
        var nameArgument = new Argument<string>("name", "Name to greet.");
        var timesOption = new Option<int>(new[] { "-t", "--times" }, () => 1, "Number of greetings to print.");
        var excitedOption = new Option<bool>("--excited", "Add an exclamation point.");

        greetCommand.AddArgument(nameArgument);
        greetCommand.AddOption(timesOption);
        greetCommand.AddOption(excitedOption);

        greetCommand.SetHandler((InvocationContext context) =>
        {
            var name = context.ParseResult.GetValueForArgument(nameArgument);
            var count = context.ParseResult.GetValueForOption(timesOption);
            var excited = context.ParseResult.GetValueForOption(excitedOption);
            var suffix = excited ? "!" : ".";

            for (var i = 0; i < count; i++)
            {
                context.Console.Out.Write($"Hello, {name}{suffix}" + Environment.NewLine);
            }
        });

        rootCommand.AddCommand(greetCommand);

        // Sum command
        var sumCommand = new Command("sum", "Sum integer values.");
        var numbersArgument = new Argument<string[]>("numbers", "Numbers to sum.");
        var absoluteOption = new Option<bool>("--absolute", "Use absolute values.");
        var formatOption = new Option<string>(new[] { "-f", "--format" }, () => "text", "Output format: text or json.");

        sumCommand.AddArgument(numbersArgument);
        sumCommand.AddOption(absoluteOption);
        sumCommand.AddOption(formatOption);

        sumCommand.SetHandler((InvocationContext context) =>
        {
            var numbers = context.ParseResult.GetValueForArgument(numbersArgument);
            var absolute = context.ParseResult.GetValueForOption(absoluteOption);
            var formatValue = context.ParseResult.GetValueForOption(formatOption) ?? "text";

            if (numbers is null || numbers.Length == 0)
            {
                context.Console.Error.Write("At least one number is required." + Environment.NewLine);
                context.ExitCode = 1;
                return;
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

            if (string.Equals(formatValue, "json", StringComparison.OrdinalIgnoreCase))
            {
                context.Console.Out.Write($@"{{""sum"":{sum}}}" + Environment.NewLine);
                return;
            }

            if (string.Equals(formatValue, "text", StringComparison.OrdinalIgnoreCase))
            {
                context.Console.Out.Write(sum.ToString(CultureInfo.InvariantCulture) + Environment.NewLine);
                return;
            }

            context.Console.Error.Write($"Unknown format: {formatValue}" + Environment.NewLine);
            context.ExitCode = 1;
        });

        rootCommand.AddCommand(sumCommand);

        return rootCommand;
    }
}

public static class Program
{
    public static int Main(string[] args)
    {
        var app = CommandLineLegacyApp.Build();
        return app.Invoke(args);
    }
}
