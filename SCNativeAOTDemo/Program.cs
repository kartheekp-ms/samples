using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Globalization;
using System.Reflection;

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

#region Validation Attributes

[AttributeUsage(AttributeTargets.Property)]
public class ValidRangeAttribute : Attribute
{
    public int Min { get; }
    public int Max { get; }
    public ValidRangeAttribute(int min, int max) { Min = min; Max = max; }
}

[AttributeUsage(AttributeTargets.Property)]
public class ValidValuesAttribute : Attribute
{
    public string[] Values { get; }
    public ValidValuesAttribute(params string[] values) { Values = values; }
}

[AttributeUsage(AttributeTargets.Property)]
public class RequiredFieldAttribute : Attribute
{
    public string ErrorMessage { get; }
    public RequiredFieldAttribute(string errorMessage) { ErrorMessage = errorMessage; }
}

[AttributeUsage(AttributeTargets.Property)]
public class MaxFieldLengthAttribute : Attribute
{
    public int MaxLength { get; }
    public string ErrorMessage { get; }
    public MaxFieldLengthAttribute(int maxLength, string errorMessage) { MaxLength = maxLength; ErrorMessage = errorMessage; }
}

[AttributeUsage(AttributeTargets.Property)]
public class DynamicEnumTypeAttribute : Attribute
{
    public string EnumTypeName { get; }
    public DynamicEnumTypeAttribute(string enumTypeName) { EnumTypeName = enumTypeName; }
}

#endregion

#region Model Classes

public class EchoModel
{
    [RequiredField("Message cannot be empty or whitespace.")]
    public string Message { get; set; } = "";

    [ValidRange(1, 50)]
    public int Repeat { get; set; } = 1;

    public bool Uppercase { get; set; }
    public bool Reverse { get; set; }
    public bool Verbose { get; set; }
}

public class CalcModel
{
    public string[] Numbers { get; set; } = Array.Empty<string>();

    [ValidValues("sum", "product", "avg", "min", "max")]
    public string Operation { get; set; } = "sum";

    [ValidRange(0, 10)]
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
    [RequiredField("Title cannot be empty.")]
    [MaxFieldLength(100, "Title must be 100 characters or fewer.")]
    public string Title { get; set; } = "";

    [DynamicEnumType("SCNativeAOTDemo.Priority")]
    public string Priority { get; set; } = "medium";

    public string? Due { get; set; }
    public string[]? Tags { get; set; }
    public bool Verbose { get; set; }
}

public class EnumInfoModel
{
    public string EnumName { get; set; } = "Priority";

    [DynamicEnumType("SCNativeAOTDemo.OutputFormat")]
    public string Format { get; set; } = "Text";

    public bool ShowAll { get; set; }
    public bool Verbose { get; set; }
}

#endregion

/// <summary>
/// Helper for type conversion and model binding.
/// </summary>
public static class ReflectionBinder
{
    /// <summary>
    /// Converts a string value to a target type.
    /// </summary>
    public static object? DynamicConvert(string? value, string targetTypeName)
    {
        if (value is null) return null;

        var targetType = Type.GetType(targetTypeName);
        if (targetType is null) return value;

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, value, ignoreCase: true);
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Sets a property on a model object.
    /// </summary>
    public static void SetProperty<T>(T model, string propertyName, object? value)
    {
        var property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        property?.SetValue(model, value);
    }

    /// <summary>
    /// Reads a validation attribute from a model property.
    /// </summary>
    public static TAttr? GetValidationAttribute<TModel, TAttr>(string propertyName) where TAttr : Attribute
    {
        var property = typeof(TModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return property?.GetCustomAttribute<TAttr>();
    }

    /// <summary>
    /// Creates a DateRange from a string value.
    /// </summary>
    public static object? CreateDateRange(string? value)
    {
        if (value is null) return null;

        var parts = value.Split("..");
        if (parts.Length != 2) return null;

        if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) ||
            !DateTime.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            return null;
        }

        var dateRangeType = Type.GetType("SCNativeAOTDemo.DateRange")!;
        var ctor = dateRangeType.GetConstructors().First();
        return ctor.Invoke(new object[] { start, end });
    }

    /// <summary>
    /// Resolves enum values by type name.
    /// </summary>
    public static (Array values, string[] names) GetEnumInfo(string enumTypeName)
    {
        var enumType = Type.GetType(enumTypeName)
            ?? throw new InvalidOperationException($"Cannot resolve type: {enumTypeName}");
        return (Enum.GetValues(enumType), Enum.GetNames(enumType));
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
            var attr = ReflectionBinder.GetValidationAttribute<EchoModel, RequiredFieldAttribute>("Message");
            if (attr is not null && string.IsNullOrWhiteSpace(value))
            {
                result.ErrorMessage = attr.ErrorMessage;
            }
        });

        var repeatOption = new Option<string>(new[] { "-r", "--repeat" }, () => "1", "Number of times to repeat the message.");
        repeatOption.AddValidator(result =>
        {
            var value = result.GetValueForOption(repeatOption);
            if (value is null) return;

            var rangeAttr = ReflectionBinder.GetValidationAttribute<EchoModel, ValidRangeAttribute>("Repeat");
            if (rangeAttr is null) return;

            if (!int.TryParse(value, out var intValue) || intValue < rangeAttr.Min || intValue > rangeAttr.Max)
            {
                result.ErrorMessage = $"Repeat count must be between {rangeAttr.Min} and {rangeAttr.Max}.";
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

            var model = new EchoModel();
            ReflectionBinder.SetProperty(model, "Message", context.ParseResult.GetValueForArgument(messageArgument));

            var repeatStr = context.ParseResult.GetValueForOption(repeatOption);
            ReflectionBinder.SetProperty(model, "Repeat", (int)ReflectionBinder.DynamicConvert(repeatStr, "System.Int32")!);

            ReflectionBinder.SetProperty(model, "Uppercase", context.ParseResult.GetValueForOption(uppercaseOption));
            ReflectionBinder.SetProperty(model, "Reverse", context.ParseResult.GetValueForOption(reverseOption));
            ReflectionBinder.SetProperty(model, "Verbose", context.ParseResult.GetValueForOption(verboseOption));

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
            var attr = ReflectionBinder.GetValidationAttribute<CalcModel, ValidValuesAttribute>("Operation");
            if (attr is not null && value is not null && !attr.Values.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                result.ErrorMessage = $"Invalid operation: {value}. Use sum, product, avg, min, or max.";
            }
        });

        var precisionOption = new Option<string>(new[] { "-p", "--precision" }, () => "2", "Decimal places for output.");
        precisionOption.AddValidator(result =>
        {
            var value = result.GetValueForOption(precisionOption);
            if (value is null) return;

            var rangeAttr = ReflectionBinder.GetValidationAttribute<CalcModel, ValidRangeAttribute>("Precision");
            if (rangeAttr is null) return;

            if (!int.TryParse(value, out var intValue) || intValue < rangeAttr.Min || intValue > rangeAttr.Max)
            {
                result.ErrorMessage = $"Precision must be between {rangeAttr.Min} and {rangeAttr.Max}.";
            }
        });

        // Nested subcommand
        var statsSubCommand = new Command("stats", "Show detailed statistics for the numbers.");
        var percentilesOption = new Option<bool>("--percentiles", "Include 25th and 75th percentiles.");
        statsSubCommand.AddOption(percentilesOption);
        statsSubCommand.AddOption(verboseOption);

        statsSubCommand.SetHandler((InvocationContext context) =>
        {
            var model = new CalcStatsModel();
            ReflectionBinder.SetProperty(model, "Numbers", context.ParseResult.GetValueForArgument(numbersArgument));
            ReflectionBinder.SetProperty(model, "Percentiles", context.ParseResult.GetValueForOption(percentilesOption));
            ReflectionBinder.SetProperty(model, "Verbose", context.ParseResult.GetValueForOption(verboseOption));

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
                    var val = (double)ReflectionBinder.DynamicConvert(n, "System.Double")!;
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
            var model = new CalcModel();
            ReflectionBinder.SetProperty(model, "Numbers", context.ParseResult.GetValueForArgument(numbersArgument));
            ReflectionBinder.SetProperty(model, "Operation", context.ParseResult.GetValueForOption(operationOption) ?? "sum");

            var precisionStr = context.ParseResult.GetValueForOption(precisionOption);
            ReflectionBinder.SetProperty(model, "Precision", (int)ReflectionBinder.DynamicConvert(precisionStr, "System.Int32")!);

            ReflectionBinder.SetProperty(model, "Verbose", context.ParseResult.GetValueForOption(verboseOption));

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

            var requiredAttr = ReflectionBinder.GetValidationAttribute<TaskModel, RequiredFieldAttribute>("Title");
            if (requiredAttr is not null && string.IsNullOrWhiteSpace(value))
            {
                result.ErrorMessage = requiredAttr.ErrorMessage;
                return;
            }

            var maxLenAttr = ReflectionBinder.GetValidationAttribute<TaskModel, MaxFieldLengthAttribute>("Title");
            if (maxLenAttr is not null && value.Length > maxLenAttr.MaxLength)
            {
                result.ErrorMessage = maxLenAttr.ErrorMessage;
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

            var enumAttr = ReflectionBinder.GetValidationAttribute<TaskModel, DynamicEnumTypeAttribute>("Priority");
            if (enumAttr is not null)
            {
                var enumType = Type.GetType(enumAttr.EnumTypeName);
                if (enumType is not null && !Enum.GetNames(enumType).Any(n => n.Equals(value, StringComparison.OrdinalIgnoreCase)))
                {
                    result.ErrorMessage = $"Invalid priority: {value}. Use low, medium, high, or critical.";
                }
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
            var model = new TaskModel();
            ReflectionBinder.SetProperty(model, "Title", context.ParseResult.GetValueForArgument(titleArgument));
            ReflectionBinder.SetProperty(model, "Priority", context.ParseResult.GetValueForOption(priorityOption) ?? "medium");
            ReflectionBinder.SetProperty(model, "Due", context.ParseResult.GetValueForOption(dueOption));
            ReflectionBinder.SetProperty(model, "Tags", context.ParseResult.GetValueForOption(tagsOption));
            ReflectionBinder.SetProperty(model, "Verbose", context.ParseResult.GetValueForOption(verboseOption));

            var enumAttr = ReflectionBinder.GetValidationAttribute<TaskModel, DynamicEnumTypeAttribute>("Priority");
            var displayPriority = ReflectionBinder.DynamicConvert(model.Priority, enumAttr!.EnumTypeName);

            if (model.Verbose)
            {
                context.Console.Out.Write($"[VERBOSE] Creating task with priority {displayPriority}" + Environment.NewLine);
            }

            context.Console.Out.Write($"Task: {model.Title}" + Environment.NewLine);
            context.Console.Out.Write($"Priority: {displayPriority}" + Environment.NewLine);

            if (model.Due is not null)
            {
                var dateRange = (DateRange)ReflectionBinder.CreateDateRange(model.Due)!;
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
            var model = new EnumInfoModel();
            ReflectionBinder.SetProperty(model, "EnumName", context.ParseResult.GetValueForArgument(enumNameArgument));
            ReflectionBinder.SetProperty(model, "Format", context.ParseResult.GetValueForOption(formatOption) ?? "Text");
            ReflectionBinder.SetProperty(model, "ShowAll", context.ParseResult.GetValueForOption(showAllOption));
            ReflectionBinder.SetProperty(model, "Verbose", context.ParseResult.GetValueForOption(verboseOption));

            var enumAttr = ReflectionBinder.GetValidationAttribute<EnumInfoModel, DynamicEnumTypeAttribute>("Format");
            var format = (OutputFormat)ReflectionBinder.DynamicConvert(model.Format, enumAttr!.EnumTypeName)!;

            if (model.Verbose)
            {
                context.Console.Out.Write($"[VERBOSE] Inspecting enum: {model.EnumName}, Format: {format}" + Environment.NewLine);
            }

            if (model.ShowAll)
            {
                PrintEnumValuesDynamic(context, "SCNativeAOTDemo.Priority", "Priority", format);
                PrintEnumValuesDynamic(context, "SCNativeAOTDemo.OutputFormat", "OutputFormat", format);
                return;
            }

            switch (model.EnumName.ToLowerInvariant())
            {
                case "priority":
                    PrintEnumValuesDynamic(context, "SCNativeAOTDemo.Priority", "Priority", format);
                    break;
                case "outputformat":
                    PrintEnumValuesDynamic(context, "SCNativeAOTDemo.OutputFormat", "OutputFormat", format);
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
    /// Prints enum values for the given type name.
    /// </summary>
    private static void PrintEnumValuesDynamic(InvocationContext context, string enumTypeName, string displayName, OutputFormat format)
    {
        var (valuesArray, names) = ReflectionBinder.GetEnumInfo(enumTypeName);
        var values = new List<(string Name, int Value)>();
        for (var i = 0; i < names.Length; i++)
        {
            values.Add((names[i], Convert.ToInt32(valuesArray.GetValue(i))));
        }

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
