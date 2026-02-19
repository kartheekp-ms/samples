using System.Globalization;
using System.Text.Json;
using CommandLine;
using CommandLine.Text;

namespace CommandLineParserDemo;

public enum GreetingLanguage
{
    English,
    Spanish,
    French,
    German
}

public enum GreetingStyle
{
    Standard,
    Friendly,
    Formal
}

public enum SumFormat
{
    Text,
    Json,
    Csv
}

public enum RoundingMode
{
    None,
    Round,
    Floor,
    Ceiling
}

public enum ConfigFormat
{
    Text,
    Json
}

[Verb("greet", false, new[] { "hello", "hi" }, HelpText = "Print localized greetings with advanced tone/format controls.")]
public sealed class GreetOptions
{
    [Value(0, Required = true, MetaName = "name", HelpText = "Name to greet.")]
    public string Name { get; set; } = string.Empty;

    [Option('t', "times", Default = 1, HelpText = "Number of greetings to print (1-20).")]
    public int Times { get; set; }

    [Option('l', "language", Default = GreetingLanguage.English, HelpText = "Greeting language: english, spanish, french, german.")]
    public GreetingLanguage Language { get; set; }

    [Option("style", Default = GreetingStyle.Standard, HelpText = "Greeting style: standard, friendly, formal.")]
    public GreetingStyle Style { get; set; }

    [Option("excited", SetName = "excited-tone", HelpText = "Use energetic punctuation (!).")]
    public bool Excited { get; set; }

    [Option("calm", SetName = "calm-tone", HelpText = "Use calm punctuation (...).")]
    public bool Calm { get; set; }

    [Option('T', "title", HelpText = "Optional title prefix (for example, Dr or Prof).")]
    public string? Title { get; set; }

    [Option('a', "aliases", Separator = ',', Max = 5, HelpText = "Comma-separated aliases to display after the greeting.")]
    public IEnumerable<string> Aliases { get; set; } = [];

    [Option('v', "verbose", FlagCounter = true, HelpText = "Increase diagnostics by repeating -v (for example -vv).")]
    public int Verbosity { get; set; }

    [Option("dry-run", HelpText = "Print normalized command line for this invocation.")]
    public bool DryRun { get; set; }

    [Option("internal-token", Hidden = true, HelpText = "Hidden option used for testing hidden help behavior.")]
    public string? InternalToken { get; set; }

    [Usage(ApplicationAlias = "commandline-parser-demo greet")]
    public static IEnumerable<Example> Examples =>
    [
        new("basic greeting", new GreetOptions { Name = "Mona" }),
        new("alias verb + style", new GreetOptions { Name = "Sam", Style = GreetingStyle.Friendly, Aliases = ["Sammy", "S"] }),
        new("verbose dry-run", new GreetOptions { Name = "Lee", Verbosity = 2, DryRun = true })
    ];
}

[Verb("sum", false, new[] { "add", "total" }, HelpText = "Sum decimal values with formatting, weighting, and rounding controls.")]
public sealed class SumOptions
{
    [Value(0, Required = true, Min = 1, Max = 20, MetaName = "numbers", HelpText = "One or more decimal values to sum.")]
    public IEnumerable<string> Numbers { get; set; } = [];

    [Option("absolute", HelpText = "Apply absolute value to each number before processing.")]
    public bool Absolute { get; set; }

    [Option("distinct", HelpText = "De-duplicate values before summing.")]
    public bool Distinct { get; set; }

    [Option("weights", Separator = ',', Min = 1, Max = 20, HelpText = "Optional comma-separated weights matching the number count.")]
    public IEnumerable<decimal> Weights { get; set; } = [];

    [Option('f', "format", Default = SumFormat.Text, HelpText = "Output format: text, json, csv.")]
    public SumFormat Format { get; set; }

    [Option('r', "round", Default = RoundingMode.None, HelpText = "Rounding mode: none, round, floor, ceiling.")]
    public RoundingMode Round { get; set; }

    [Option("precision", Default = 2, HelpText = "Number of decimal places used by round/floor/ceiling.")]
    public int Precision { get; set; }

    [Option("stats", SetName = "with-stats", HelpText = "Include count/min/max/average metadata.")]
    public bool IncludeStats { get; set; }

    [Option("compact", SetName = "compact-output", HelpText = "Emit compact output with minimal labels.")]
    public bool Compact { get; set; }

    [Option('v', "verbose", FlagCounter = true, HelpText = "Increase diagnostics by repeating -v.")]
    public int Verbosity { get; set; }

    [Option("dry-run", HelpText = "Print normalized command line for this invocation.")]
    public bool DryRun { get; set; }

    [Usage(ApplicationAlias = "commandline-parser-demo sum")]
    public static IEnumerable<Example> Examples =>
    [
        new("simple sum", new SumOptions { Numbers = ["1", "2", "3"] }),
        new("weighted json output", new SumOptions { Numbers = ["2.5", "3.5"], Weights = [2m, 1m], Format = SumFormat.Json, IncludeStats = true }),
        new("dash-dash negative values", new SumOptions { Numbers = ["-5", "-2"], Absolute = true })
    ];
}

[Verb("config", false, new[] { "cfg" }, HelpText = "Manipulate an in-memory key/value configuration map.")]
public sealed class ConfigOptions
{
    [Option("set", Group = "action", Separator = ';', HelpText = "Semicolon-separated key=value pairs to update.")]
    public IEnumerable<string> SetValues { get; set; } = [];

    [Option("get", Group = "action", HelpText = "Read a key value by name.")]
    public string? GetKey { get; set; }

    [Option("list", Group = "action", HelpText = "List all key/value pairs.")]
    public bool List { get; set; }

    [Option('f', "format", Default = ConfigFormat.Text, HelpText = "Output format: text or json.")]
    public ConfigFormat Format { get; set; }

    [Option('v', "verbose", FlagCounter = true, HelpText = "Increase diagnostics by repeating -v.")]
    public int Verbosity { get; set; }

    [Option("dry-run", HelpText = "Print normalized command line for this invocation.")]
    public bool DryRun { get; set; }

    [Usage(ApplicationAlias = "commandline-parser-demo config")]
    public static IEnumerable<Example> Examples =>
    [
        new("list values", new ConfigOptions { List = true }),
        new("set values", new ConfigOptions { SetValues = ["theme=light", "region=eu"] }),
        new("read one key", new ConfigOptions { GetKey = "theme" })
    ];
}

public sealed class CommandLineParserDemoApp
{
    private readonly TextWriter _output;
    private readonly TextWriter _error;
    private readonly Parser _parser;
    private readonly Dictionary<string, string> _config = new(StringComparer.OrdinalIgnoreCase)
    {
        ["theme"] = "dark",
        ["region"] = "us",
        ["timezone"] = "utc"
    };

    public CommandLineParserDemoApp(TextWriter? output = null, TextWriter? error = null)
    {
        _output = output ?? Console.Out;
        _error = error ?? Console.Error;
        _parser = new Parser(config =>
        {
            config.HelpWriter = null;
            config.CaseSensitive = false;
            config.CaseInsensitiveEnumValues = true;
            config.ParsingCulture = CultureInfo.InvariantCulture;
            config.AutoHelp = true;
            config.AutoVersion = true;
            config.EnableDashDash = true;
            config.GetoptMode = true;
            config.AllowMultiInstance = true;
            config.MaximumDisplayWidth = 120;
        });
    }

    public int Execute(string[] args)
    {
        var parseResult = _parser.ParseArguments<GreetOptions, SumOptions, ConfigOptions>(args);
        return parseResult.MapResult(
            (GreetOptions opts) => RunGreet(opts),
            (SumOptions opts) => RunSum(opts),
            (ConfigOptions opts) => RunConfig(opts),
            errs => HandleParseErrors(parseResult, errs));
    }

    private int RunGreet(GreetOptions opts)
    {
        if (opts.Times is < 1 or > 20)
        {
            _error.WriteLine("times must be between 1 and 20.");
            return 1;
        }

        if (opts.Verbosity > 0)
        {
            _error.WriteLine($"[verbose:greet] language={opts.Language} style={opts.Style} times={opts.Times}");
        }

        if (opts.DryRun)
        {
            _output.WriteLine($"[dry-run] {FormatCommandLine(opts)}");
        }

        var greetingLead = GetGreetingLead(opts.Language, opts.Style);
        var suffix = opts.Calm ? "..." : opts.Excited ? "!" : ".";
        var target = string.IsNullOrWhiteSpace(opts.Title) ? opts.Name : $"{opts.Title} {opts.Name}";

        for (var i = 0; i < opts.Times; i++)
        {
            _output.WriteLine($"{greetingLead}, {target}{suffix}");
        }

        var aliases = opts.Aliases.Where(static x => !string.IsNullOrWhiteSpace(x)).ToList();
        if (aliases.Count > 0)
        {
            _output.WriteLine($"Aliases: {string.Join(", ", aliases)}");
        }

        return 0;
    }

    private int RunSum(SumOptions opts)
    {
        if (opts.Precision is < 0 or > 6)
        {
            _error.WriteLine("precision must be between 0 and 6.");
            return 1;
        }

        if (opts.Verbosity > 0)
        {
            _error.WriteLine($"[verbose:sum] format={opts.Format} round={opts.Round} precision={opts.Precision}");
        }

        if (opts.DryRun)
        {
            _output.WriteLine($"[dry-run] {FormatCommandLine(opts)}");
        }

        var parsedValues = new List<decimal>();
        foreach (var token in opts.Numbers)
        {
            if (!decimal.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                _error.WriteLine($"Invalid number: {token}");
                return 1;
            }

            parsedValues.Add(opts.Absolute ? Math.Abs(parsed) : parsed);
        }

        if (opts.Distinct)
        {
            parsedValues = parsedValues.Distinct().ToList();
        }

        if (parsedValues.Count == 0)
        {
            _error.WriteLine("At least one number is required.");
            return 1;
        }

        var weights = opts.Weights.ToList();
        if (weights.Count > 0 && weights.Count != parsedValues.Count)
        {
            _error.WriteLine($"weights count ({weights.Count}) must match numbers count ({parsedValues.Count}).");
            return 1;
        }

        var aggregate = weights.Count > 0
            ? parsedValues.Zip(weights, (value, weight) => value * weight).Sum()
            : parsedValues.Sum();

        var sum = ApplyRounding(aggregate, opts.Round, opts.Precision);
        var min = parsedValues.Min();
        var max = parsedValues.Max();
        var avg = parsedValues.Average();

        switch (opts.Format)
        {
            case SumFormat.Json:
                if (opts.IncludeStats)
                {
                    _output.WriteLine(JsonSerializer.Serialize(new
                    {
                        sum,
                        count = parsedValues.Count,
                        min,
                        max,
                        avg
                    }));
                    return 0;
                }

                _output.WriteLine(JsonSerializer.Serialize(new { sum }));
                return 0;
            case SumFormat.Csv:
                _output.WriteLine("sum,count,min,max,avg");
                _output.WriteLine(string.Join(",",
                    FormatDecimal(sum),
                    parsedValues.Count.ToString(CultureInfo.InvariantCulture),
                    FormatDecimal(min),
                    FormatDecimal(max),
                    FormatDecimal(avg)));
                return 0;
            case SumFormat.Text:
            default:
                if (opts.Compact)
                {
                    _output.WriteLine(FormatDecimal(sum));
                    return 0;
                }

                _output.WriteLine($"Sum: {FormatDecimal(sum)}");
                if (opts.IncludeStats)
                {
                    _output.WriteLine($"Count: {parsedValues.Count}, Min: {FormatDecimal(min)}, Max: {FormatDecimal(max)}, Avg: {FormatDecimal(avg)}");
                }

                return 0;
        }
    }

    private int RunConfig(ConfigOptions opts)
    {
        if (opts.Verbosity > 0)
        {
            _error.WriteLine($"[verbose:config] format={opts.Format}");
        }

        if (opts.DryRun)
        {
            _output.WriteLine($"[dry-run] {FormatCommandLine(opts)}");
        }

        if (opts.SetValues.Any())
        {
            foreach (var pair in opts.SetValues)
            {
                var separatorIndex = pair.IndexOf('=', StringComparison.Ordinal);
                if (separatorIndex <= 0 || separatorIndex == pair.Length - 1)
                {
                    _error.WriteLine($"Invalid key=value pair: {pair}");
                    return 1;
                }

                var key = pair[..separatorIndex].Trim();
                var value = pair[(separatorIndex + 1)..].Trim();
                if (key.Length == 0 || value.Length == 0)
                {
                    _error.WriteLine($"Invalid key=value pair: {pair}");
                    return 1;
                }

                _config[key] = value;
            }

            return WriteConfigEntries(opts.Format, _config.OrderBy(static x => x.Key));
        }

        if (!string.IsNullOrWhiteSpace(opts.GetKey))
        {
            if (!_config.TryGetValue(opts.GetKey, out var value))
            {
                _error.WriteLine($"Config key not found: {opts.GetKey}");
                return 1;
            }

            return WriteConfigEntries(opts.Format, [new KeyValuePair<string, string>(opts.GetKey, value)]);
        }

        if (opts.List)
        {
            return WriteConfigEntries(opts.Format, _config.OrderBy(static x => x.Key));
        }

        _error.WriteLine("An action is required: --set, --get, or --list.");
        return 1;
    }

    private int HandleParseErrors(ParserResult<object> parseResult, IEnumerable<Error> errs)
    {
        var errors = errs.ToArray();
        if (errors.IsVersion())
        {
            var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
            _output.WriteLine($"CommandLineParserDemo {version}");
            return 0;
        }

        var helpText = BuildHelpText(parseResult);
        if (errors.IsHelp())
        {
            _output.Write(helpText);
            return 0;
        }

        _error.Write(helpText);
        return 1;
    }

    private string BuildHelpText(ParserResult<object> parseResult)
    {
        var helpText = HelpText.AutoBuild(parseResult, current =>
        {
            current.Heading = "CommandLineParserDemo - advanced CommandLineParser feature showcase";
            current.Copyright = "Samples repository";
            current.AdditionalNewLineAfterOption = false;
            current.AddPreOptionsLine("Demonstrates verbs, aliases, help/version, groups, set exclusivity, separators, dash-dash, and unparser support.");
            current.AddPostOptionsLine("Use '<verb> --help' for command-specific help.");
            return HelpText.DefaultParsingErrorsHandler(parseResult, current);
        }, maxDisplayWidth: 120);

        return helpText.ToString();
    }

    private string FormatCommandLine<T>(T options) where T : class =>
        _parser.FormatCommandLine(options, settings =>
        {
            settings.PreferShortName = false;
            settings.UseEqualToken = true;
            settings.SkipDefault = true;
        });

    private static string GetGreetingLead(GreetingLanguage language, GreetingStyle style) =>
        (language, style) switch
        {
            (GreetingLanguage.Spanish, GreetingStyle.Friendly) => "Buenas",
            (GreetingLanguage.Spanish, GreetingStyle.Formal) => "Saludos",
            (GreetingLanguage.Spanish, _) => "Hola",
            (GreetingLanguage.French, GreetingStyle.Friendly) => "Salut",
            (GreetingLanguage.French, GreetingStyle.Formal) => "Salutations",
            (GreetingLanguage.French, _) => "Bonjour",
            (GreetingLanguage.German, GreetingStyle.Friendly) => "Servus",
            (GreetingLanguage.German, GreetingStyle.Formal) => "Guten Tag",
            (GreetingLanguage.German, _) => "Hallo",
            (GreetingLanguage.English, GreetingStyle.Friendly) => "Hey",
            (GreetingLanguage.English, GreetingStyle.Formal) => "Greetings",
            _ => "Hello"
        };

    private static decimal ApplyRounding(decimal value, RoundingMode mode, int precision)
    {
        if (mode == RoundingMode.None)
        {
            return value;
        }

        var scale = (decimal)Math.Pow(10, precision);
        return mode switch
        {
            RoundingMode.Round => Math.Round(value, precision, MidpointRounding.AwayFromZero),
            RoundingMode.Floor => Math.Floor(value * scale) / scale,
            RoundingMode.Ceiling => Math.Ceiling(value * scale) / scale,
            _ => value
        };
    }

    private int WriteConfigEntries(ConfigFormat format, IEnumerable<KeyValuePair<string, string>> entries)
    {
        var orderedEntries = entries.OrderBy(static x => x.Key).ToList();
        if (format == ConfigFormat.Json)
        {
            _output.WriteLine(JsonSerializer.Serialize(orderedEntries.ToDictionary(static x => x.Key, static x => x.Value, StringComparer.OrdinalIgnoreCase)));
            return 0;
        }

        foreach (var (key, value) in orderedEntries)
        {
            _output.WriteLine($"{key}={value}");
        }

        return 0;
    }

    private static string FormatDecimal(decimal value) => value.ToString("0.################", CultureInfo.InvariantCulture);
}

public static class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandLineParserDemoApp();
        return app.Execute(args);
    }
}
