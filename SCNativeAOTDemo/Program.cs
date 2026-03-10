using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Globalization;

namespace SCNativeAOTDemo;

/// <summary>
/// Priority level for tasks.
/// </summary>
public enum Priority { Low, Medium, High, Critical }

/// <summary>
/// Custom type representing a date range.
/// </summary>
public record DateRange(DateTime Start, DateTime End);

/// <summary>
/// Output format for enum-info command.
/// </summary>
public enum OutputFormat { Text, Csv, Json }

#region Model Classes

public class EchoModel
{
    public string Message { get; set; } = "";
    public int Repeat { get; set; } = 1;
    public bool Uppercase { get; set; }
    public bool Reverse { get; set; }
    public bool Verbose { get; set; }
}

public class CalcModel
{
    public string[] Numbers { get; set; } = Array.Empty<string>();
    public string Operation { get; set; } = "sum";
    public int Precision { get; set; } = 2;
    public bool Verbose { get; set; }
}

public class CalcStatsModel
{
    public string[] Numbers { get; set; } = Array.Empty<string>();
    public bool Percentiles { get; set; }
    public bool Verbose { get; set; }
}

public class TaskModel
{
    public string Title { get; set; } = "";
    public string Priority { get; set; } = "medium";
    public string? Due { get; set; }
    public string[]? Tags { get; set; }
    public bool Verbose { get; set; }
}

public class EnumInfoModel
{
    public string EnumName { get; set; } = "Priority";
    public string Format { get; set; } = "Text";
    public bool ShowAll { get; set; }
    public bool Verbose { get; set; }
}

#endregion

/// <summary>
/// AOT-safe helpers for type conversion and enum introspection.
/// </summary>
public static class AotSafeHelpers
{
    /// <summary>
    /// Creates a DateRange from a "start..end" formatted string.
    /// </summary>
    public static DateRange? CreateDateRange(string? value)
    {
        if (value is null) return null;

        var parts = value.Split("..");
        if (parts.Length != 2) return null;

        if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) ||
            !DateTime.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            return null;
        }

        return new DateRange(start, end);
    }

    /// <summary>
    /// Gets enum names and integer values for a compile-time known enum type.
    /// </summary>
    public static List<(string Name, int Value)> GetEnumValues<TEnum>() where TEnum : struct, Enum
    {
        var names = Enum.GetNames<TEnum>();
        var values = Enum.GetValues<TEnum>();
        var result = new List<(string Name, int Value)>();
        for (var i = 0; i < names.Length; i++)
        {
            result.Add((names[i], Convert.ToInt32(values[i])));
        }
        return result;
    }
}

public static class EchoCommandExtensions
{
    public static Command AddEchoCommand(this RootCommand root, Option<bool> verboseOption)
    {
        var echoCommand = new Command("echo", "Echo a message to the console.");

        var messageArgument = new Argument<string>("message", "The message to echo.");
        messageArgument.AddValidator(result =>
        {
            var value = result.GetValueForArgument(messageArgument);
            if (string.IsNullOrWhiteSpace(value))
            {
                result.ErrorMessage = "Message cannot be empty or whitespace.";
            }
        });

        var repeatOption = new Option<string>(new[] { "-r", "--repeat" }, () => "1", "Number of times to repeat the message.");
        repeatOption.AddValidator(result =>
        {
            var value = result.GetValueForOption(repeatOption);
            if (value is null) return;

            if (!int.TryParse(value, out var intValue) || intValue < 1 || intValue > 50)
            {
                result.ErrorMessage = "Repeat count must be between 1 and 50.";
            }
        });

        var uppercaseOption = new Option<bool>(new[] { "-u", "--uppercase" }, "Convert message to uppercase.");
        var reverseOption = new Option<bool>("--reverse", "Reverse the message.");

        echoCommand.AddArgument(messageArgument);
        echoCommand.AddOption(repeatOption);
        echoCommand.AddOption(uppercaseOption);
        echoCommand.AddOption(reverseOption);
        echoCommand.AddOption(verboseOption);

        echoCommand.SetHandler(async (InvocationContext context) =>
        {
            var cancellationToken = context.GetCancellationToken();

            var model = new EchoModel
            {
                Message = context.ParseResult.GetValueForArgument(messageArgument),
                Repeat = int.Parse(context.ParseResult.GetValueForOption(repeatOption)!, CultureInfo.InvariantCulture),
                Uppercase = context.ParseResult.GetValueForOption(uppercaseOption),
                Reverse = context.ParseResult.GetValueForOption(reverseOption),
                Verbose = context.ParseResult.GetValueForOption(verboseOption)
            };

            if (model.Uppercase)
            {
                model.Message = model.Message.ToUpperInvariant();
            }

            if (model.Reverse)
            {
                var chars = model.Message.ToCharArray();
                Array.Reverse(chars);
                model.Message = new string(chars);
            }

            if (model.Verbose)
            {
                context.Console.Out.Write($"[VERBOSE] Echoing message {model.Repeat} time(s)" + Environment.NewLine);
            }

            for (var i = 0; i < model.Repeat; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(10, cancellationToken);
                context.Console.Out.Write(model.Message + Environment.NewLine);
            }
        });

        root.AddCommand(echoCommand);
        return echoCommand;
    }
}

public static class CalcCommandExtensions
{
    public static Command AddCalcCommand(this RootCommand root, Option<bool> verboseOption)
    {
        var calcCommand = new Command("calc", "Perform arithmetic on a list of numbers.");

        var numbersArgument = new Argument<string[]>("numbers", "Numbers to operate on.");

        var operationOption = new Option<string>(
            new[] { "-o", "--operation" },
            () => "sum",
            "Operation: sum, product, avg, min, max.");
        operationOption.AddValidator(result =>
        {
            var value = result.GetValueForOption(operationOption);
            string[] validOperations = ["sum", "product", "avg", "min", "max"];
            if (value is not null && !validOperations.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                result.ErrorMessage = $"Invalid operation: {value}. Use sum, product, avg, min, or max.";
            }
        });

        var precisionOption = new Option<string>(new[] { "-p", "--precision" }, () => "2", "Decimal places for output.");
        precisionOption.AddValidator(result =>
        {
            var value = result.GetValueForOption(precisionOption);
            if (value is null) return;

            if (!int.TryParse(value, out var intValue) || intValue < 0 || intValue > 10)
            {
                result.ErrorMessage = "Precision must be between 0 and 10.";
            }
        });

        // Nested subcommand
        var statsSubCommand = new Command("stats", "Show detailed statistics for the numbers.");
        var percentilesOption = new Option<bool>("--percentiles", "Include 25th and 75th percentiles.");
        statsSubCommand.AddOption(percentilesOption);
        statsSubCommand.AddOption(verboseOption);

        statsSubCommand.SetHandler((InvocationContext context) =>
        {
            var model = new CalcStatsModel
            {
                Numbers = context.ParseResult.GetValueForArgument(numbersArgument),
                Percentiles = context.ParseResult.GetValueForOption(percentilesOption),
                Verbose = context.ParseResult.GetValueForOption(verboseOption)
            };

            if (model.Numbers is null || model.Numbers.Length == 0)
            {
                context.Console.Error.Write("No numbers provided for stats." + Environment.NewLine);
                context.ExitCode = 1;
                return;
            }

            var parsed = new List<double>();
            foreach (var n in model.Numbers)
            {
                try
                {
                    var val = Convert.ToDouble(n, CultureInfo.InvariantCulture);
                    parsed.Add(val);
                }
                catch
                {
                    context.Console.Error.Write($"Invalid number: {n}" + Environment.NewLine);
                    context.ExitCode = 1;
                    return;
                }
            }

            if (model.Verbose)
            {
                context.Console.Out.Write($"[VERBOSE] Computing stats for {parsed.Count} numbers" + Environment.NewLine);
            }

            var sorted = parsed.OrderBy(x => x).ToList();
            context.Console.Out.Write($"Count: {sorted.Count}" + Environment.NewLine);
            context.Console.Out.Write($"Sum: {sorted.Sum():F2}" + Environment.NewLine);
            context.Console.Out.Write($"Avg: {sorted.Average():F2}" + Environment.NewLine);
            context.Console.Out.Write($"Min: {sorted.First():F2}" + Environment.NewLine);
            context.Console.Out.Write($"Max: {sorted.Last():F2}" + Environment.NewLine);

            if (model.Percentiles)
            {
                context.Console.Out.Write($"P25: {Percentile(sorted, 25):F2}" + Environment.NewLine);
                context.Console.Out.Write($"P75: {Percentile(sorted, 75):F2}" + Environment.NewLine);
            }
        });

        calcCommand.AddArgument(numbersArgument);
        calcCommand.AddOption(operationOption);
        calcCommand.AddOption(precisionOption);
        calcCommand.AddOption(verboseOption);
        calcCommand.AddCommand(statsSubCommand);

        calcCommand.SetHandler((InvocationContext context) =>
        {
            var model = new CalcModel
            {
                Numbers = context.ParseResult.GetValueForArgument(numbersArgument),
                Operation = context.ParseResult.GetValueForOption(operationOption) ?? "sum",
                Precision = int.Parse(context.ParseResult.GetValueForOption(precisionOption)!, CultureInfo.InvariantCulture),
                Verbose = context.ParseResult.GetValueForOption(verboseOption)
            };

            if (model.Numbers is null || model.Numbers.Length == 0)
            {
                context.Console.Error.Write("At least one number is required." + Environment.NewLine);
                context.ExitCode = 1;
                return;
            }

            if (model.Verbose)
            {
                context.Console.Out.Write($"[VERBOSE] Operation: {model.Operation}, Precision: {model.Precision}" + Environment.NewLine);
            }

            var values = new List<double>();
            foreach (var n in model.Numbers)
            {
                if (!double.TryParse(n, NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
                {
                    context.Console.Error.Write($"Invalid number: {n}" + Environment.NewLine);
                    context.ExitCode = 1;
                    return;
                }
                values.Add(val);
            }

            var format = $"F{model.Precision}";
            double result = model.Operation.ToLowerInvariant() switch
            {
                "sum" => values.Sum(),
                "product" => values.Aggregate(1.0, (acc, v) => acc * v),
                "avg" => values.Average(),
                "min" => values.Min(),
                "max" => values.Max(),
                _ => throw new InvalidOperationException($"Unknown operation: {model.Operation}")
            };

            context.Console.Out.Write(result.ToString(format, CultureInfo.InvariantCulture) + Environment.NewLine);
        });

        root.AddCommand(calcCommand);
        return calcCommand;
    }

    private static double Percentile(List<double> sorted, double percentile)
    {
        var index = (percentile / 100.0) * (sorted.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        if (lower == upper)
            return sorted[lower];
        return sorted[lower] + (index - lower) * (sorted[upper] - sorted[lower]);
    }
}

public static class TaskCommandExtensions
{
    public static Command AddTaskCommand(this RootCommand root, Option<bool> verboseOption)
    {
        var taskCommand = new Command("task", "Manage tasks with priorities.");

        var titleArgument = new Argument<string>("title", "Task title.");
        titleArgument.AddValidator(result =>
        {
            var value = result.GetValueForArgument(titleArgument);

            if (string.IsNullOrWhiteSpace(value))
            {
                result.ErrorMessage = "Title cannot be empty.";
                return;
            }

            if (value.Length > 100)
            {
                result.ErrorMessage = "Title must be 100 characters or fewer.";
            }
        });

        var priorityOption = new Option<string>(
            new[] { "-p", "--priority" },
            () => "medium",
            "Task priority: low, medium, high, or critical.");
        priorityOption.AddValidator(result =>
        {
            var value = result.GetValueForOption(priorityOption);
            if (value is null) return;

            if (!Enum.GetNames<Priority>().Any(n => n.Equals(value, StringComparison.OrdinalIgnoreCase)))
            {
                result.ErrorMessage = $"Invalid priority: {value}. Use low, medium, high, or critical.";
            }
        });

        var dueOption = new Option<string?>(
            "--due",
            description: "Due date range (e.g. 2025-01-01..2025-12-31).");
        dueOption.AddValidator(result =>
        {
            var value = result.GetValueForOption(dueOption);
            if (value is null) return;

            var parts = value.Split("..");
            if (parts.Length != 2 ||
                !DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) ||
                !DateTime.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
            {
                result.ErrorMessage = $"Invalid date range: {value}. Use format: yyyy-MM-dd..yyyy-MM-dd";
                return;
            }

            if (end < start)
            {
                result.ErrorMessage = "End date must be after start date.";
            }
        });

        var tagsOption = new Option<string[]>(new[] { "--tags" }, "Comma-separated tags.")
        {
            AllowMultipleArgumentsPerToken = true
        };

        taskCommand.AddArgument(titleArgument);
        taskCommand.AddOption(priorityOption);
        taskCommand.AddOption(dueOption);
        taskCommand.AddOption(tagsOption);
        taskCommand.AddOption(verboseOption);

        taskCommand.SetHandler((InvocationContext context) =>
        {
            var model = new TaskModel
            {
                Title = context.ParseResult.GetValueForArgument(titleArgument),
                Priority = context.ParseResult.GetValueForOption(priorityOption) ?? "medium",
                Due = context.ParseResult.GetValueForOption(dueOption),
                Tags = context.ParseResult.GetValueForOption(tagsOption),
                Verbose = context.ParseResult.GetValueForOption(verboseOption)
            };

            var displayPriority = Enum.Parse<Priority>(model.Priority, ignoreCase: true);

            if (model.Verbose)
            {
                context.Console.Out.Write($"[VERBOSE] Creating task with priority {displayPriority}" + Environment.NewLine);
            }

            context.Console.Out.Write($"Task: {model.Title}" + Environment.NewLine);
            context.Console.Out.Write($"Priority: {displayPriority}" + Environment.NewLine);

            if (model.Due is not null)
            {
                var dateRange = AotSafeHelpers.CreateDateRange(model.Due)!;
                context.Console.Out.Write($"Due: {dateRange.Start:yyyy-MM-dd} to {dateRange.End:yyyy-MM-dd}" + Environment.NewLine);
            }

            if (model.Tags is not null && model.Tags.Length > 0)
            {
                context.Console.Out.Write($"Tags: {string.Join(", ", model.Tags)}" + Environment.NewLine);
            }
        });

        root.AddCommand(taskCommand);
        return taskCommand;
    }
}

public static class EnumInfoCommandExtensions
{
    public static Command AddEnumInfoCommand(this RootCommand root, Option<bool> verboseOption)
    {
        var enumInfoCommand = new Command("enum-info", "Display enum metadata.");

        var enumNameArgument = new Argument<string>(
            "enum-name",
            () => "Priority",
            "Name of the enum to inspect: Priority or OutputFormat.");

        var formatOption = new Option<string>(
            new[] { "-f", "--format" },
            () => "Text",
            "Output format.");

        var showAllOption = new Option<bool>("--show-all", "Show values for all known enums.");

        enumInfoCommand.AddArgument(enumNameArgument);
        enumInfoCommand.AddOption(formatOption);
        enumInfoCommand.AddOption(showAllOption);
        enumInfoCommand.AddOption(verboseOption);

        enumInfoCommand.SetHandler((InvocationContext context) =>
        {
            var model = new EnumInfoModel
            {
                EnumName = context.ParseResult.GetValueForArgument(enumNameArgument),
                Format = context.ParseResult.GetValueForOption(formatOption) ?? "Text",
                ShowAll = context.ParseResult.GetValueForOption(showAllOption),
                Verbose = context.ParseResult.GetValueForOption(verboseOption)
            };

            var format = Enum.Parse<OutputFormat>(model.Format, ignoreCase: true);

            if (model.Verbose)
            {
                context.Console.Out.Write($"[VERBOSE] Inspecting enum: {model.EnumName}, Format: {format}" + Environment.NewLine);
            }

            if (model.ShowAll)
            {
                PrintEnumValues<Priority>(context, "Priority", format);
                PrintEnumValues<OutputFormat>(context, "OutputFormat", format);
                return;
            }

            switch (model.EnumName.ToLowerInvariant())
            {
                case "priority":
                    PrintEnumValues<Priority>(context, "Priority", format);
                    break;
                case "outputformat":
                    PrintEnumValues<OutputFormat>(context, "OutputFormat", format);
                    break;
                default:
                    context.Console.Error.Write($"Unknown enum: {model.EnumName}. Use Priority or OutputFormat." + Environment.NewLine);
                    context.ExitCode = 1;
                    break;
            }
        });

        root.AddCommand(enumInfoCommand);
        return enumInfoCommand;
    }

    /// <summary>
    /// Prints enum values using compile-time generic type resolution.
    /// </summary>
    private static void PrintEnumValues<TEnum>(InvocationContext context, string displayName, OutputFormat format) where TEnum : struct, Enum
    {
        var values = AotSafeHelpers.GetEnumValues<TEnum>();

        switch (format)
        {
            case OutputFormat.Csv:
                context.Console.Out.Write($"Enum,Name,Value" + Environment.NewLine);
                foreach (var (name, value) in values)
                {
                    context.Console.Out.Write($"{displayName},{name},{value}" + Environment.NewLine);
                }
                break;

            case OutputFormat.Json:
                context.Console.Out.Write("{" + Environment.NewLine);
                context.Console.Out.Write($"  \"enum\": \"{displayName}\"," + Environment.NewLine);
                context.Console.Out.Write($"  \"values\": [" + Environment.NewLine);
                for (var i = 0; i < values.Count; i++)
                {
                    var comma = i < values.Count - 1 ? "," : "";
                    context.Console.Out.Write($"    {{ \"name\": \"{values[i].Name}\", \"value\": {values[i].Value} }}{comma}" + Environment.NewLine);
                }
                context.Console.Out.Write("  ]" + Environment.NewLine);
                context.Console.Out.Write("}" + Environment.NewLine);
                break;

            default:
                context.Console.Out.Write($"{displayName}:" + Environment.NewLine);
                foreach (var (name, value) in values)
                {
                    context.Console.Out.Write($"  {name} = {value}" + Environment.NewLine);
                }
                break;
        }
    }
}

public static class SCNativeAOTDemoApp
{
    /// <summary>
    /// Builds the command tree using CommandLineBuilder with middleware.
    /// </summary>
    public static Parser Build(IConsole? console = null)
    {
        var rootCommand = new RootCommand("Sample command line app using System.CommandLine beta4.");

        var verboseOption = new Option<bool>(new[] { "-v", "--verbose" }, "Enable verbose output.");
        rootCommand.AddGlobalOption(verboseOption);

        rootCommand.AddEchoCommand(verboseOption);
        rootCommand.AddCalcCommand(verboseOption);
        rootCommand.AddTaskCommand(verboseOption);
        rootCommand.AddEnumInfoCommand(verboseOption);

        var builder = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseExceptionHandler((exception, context) =>
            {
                context.Console.Error.Write($"[ERROR] {exception.Message}" + Environment.NewLine);
                context.ExitCode = 1;
            })
            .CancelOnProcessTermination();

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
        var parser = SCNativeAOTDemoApp.Build();
        return await parser.InvokeAsync(args);
    }
}
