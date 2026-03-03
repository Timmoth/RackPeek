using RackPeek.Domain;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace Shared.Rcl;

public class ConsoleEmulator : IConsoleEmulator
{
    public ConsoleEmulator(IServiceProvider provider)
    {
        var registrar = new TypeRegistrar(provider);
        App = new CommandApp(registrar);
        CliBootstrap.BuildApp(App);
    }

    public CommandApp App { get; }

    public async Task<string> Execute(string input)
    {
        var testConsole = new TestConsole();
        testConsole.Width(120);

        AnsiConsole.Console = testConsole;
        App.Configure(c => c.Settings.Console = testConsole);

        await App.RunAsync(ParseArguments(input));

        return testConsole.Output;
    }

    internal static string[] ParseArguments(string input)
    {
        var args = new List<string>();
        var current = new System.Text.StringBuilder();
        char? quote = null;

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];

            if (quote.HasValue)
            {
                if (c == quote.Value)
                    quote = null;
                else
                    current.Append(c);
            }
            else if (c is '"' or '\'')
            {
                quote = c;
            }
            else if (c == ' ')
            {
                if (current.Length > 0)
                {
                    args.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            args.Add(current.ToString());

        return args.ToArray();
    }
}

public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceProvider _provider;

    public TypeRegistrar(IServiceProvider provider)
    {
        _provider = provider;
    }

    public void Register(Type service, Type implementation)
    {
        // DO NOTHING — services must already be registered
    }

    public void RegisterInstance(Type service, object implementation)
    {
        // DO NOTHING
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        // DO NOTHING
    }

    public ITypeResolver Build()
    {
        return new TypeResolver(_provider);
    }
}

public sealed class TypeResolver : ITypeResolver
{
    private readonly IServiceProvider _provider;

    public TypeResolver(IServiceProvider provider)
    {
        _provider = provider;
    }

    public object? Resolve(Type? type)
    {
        return type == null ? null : _provider.GetService(type);
    }
}