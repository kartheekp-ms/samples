using System.Globalization;
using CommandLine;

namespace CommandLineParserDemo;

[Verb("greet", HelpText = "Print a greeting.")]
public class GreetOptions
{
    [Value(0, Required = true, MetaName = "name", HelpText = "Name to greet.")]
    public string Name { get; set; } = string.Empty;

    [Option('t', "times", Default = 1, HelpText = "Number of greetings to print.")]
    public int Times { get; set; }

    [Option("excited", HelpText = "Add an exclamation point.")]
    public bool Excited { get; set; }
}

[Verb("sum", HelpText = "Sum integer values.")]
public class SumOptions
{
    [Value(0, Required = true, MetaName = "numbers", HelpText = "Numbers to sum.")]
    public IEnumerable<string> Numbers { get; set; } = [];

    [Option("absolute", HelpText = "Use absolute values.")]
    public bool Absolute { get; set; }

    [Option('f', "format", Default = "text", HelpText = "Output format: text or json.")]
    public string Format { get; set; } = "text";
}

public class CommandLineParserDemoApp
{
    private readonly TextWriter _output;
    private readonly TextWriter _error;

    public CommandLineParserDemoApp(TextWriter? output = null, TextWriter? error = null)
    {
        _output = output ?? Console.Out;
        _error = error ?? Console.Error;
    }

    public int Execute(string[] args)
    {
        var parser = new Parser(config => config.HelpWriter = _error);
        return parser.ParseArguments<GreetOptions, SumOptions>(args)
            .MapResult(
                (GreetOptions opts) => RunGreet(opts),
                (SumOptions opts) => RunSum(opts),
                _ => 1);
    }

    private int RunGreet(GreetOptions opts)
    {
        var suffix = opts.Excited ? "!" : ".";
        for (var i = 0; i < opts.Times; i++)
        {
            _output.WriteLine($"Hello, {opts.Name}{suffix}");
        }
        return 0;
    }

    private int RunSum(SumOptions opts)
    {
        var numbersList = opts.Numbers.ToList();
        if (numbersList.Count == 0)
        {
            _error.WriteLine("At least one number is required.");
            return 1;
        }

        var values = new List<int>(numbersList.Count);
        foreach (var value in numbersList)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                _error.WriteLine($"Invalid number: {value}");
                return 1;
            }

            if (opts.Absolute)
            {
                parsed = Math.Abs(parsed);
            }

            values.Add(parsed);
        }

        var sum = values.Sum();

        if (string.Equals(opts.Format, "json", StringComparison.OrdinalIgnoreCase))
        {
            _output.WriteLine($@"{{""sum"":{sum}}}");
            return 0;
        }

        if (string.Equals(opts.Format, "text", StringComparison.OrdinalIgnoreCase))
        {
            _output.WriteLine(sum.ToString(CultureInfo.InvariantCulture));
            return 0;
        }

        _error.WriteLine($"Unknown format: {opts.Format}");
        return 1;
    }
}

public static class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandLineParserDemoApp();
        return app.Execute(args);
    }
}
