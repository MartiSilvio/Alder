using System.Globalization;
using System.Reflection;
using Alder;

namespace Alder.Mcp;

internal readonly record struct McpParseResult(McpServerConfig? Config, bool ShowHelp);

internal sealed class McpServerConfig
{
    internal const string DefaultSandboxPreset = "trusted";
    internal const long DefaultMaxStatements = 1_000_000;
    internal const long DefaultMaxLoopIterations = 1_000_000;
    internal static readonly TimeSpan DefaultMaxTimeout = TimeSpan.FromSeconds(10);

    public SandboxOptions Sandbox { get; init; } = SandboxOptions.Trusted();
    public string SandboxPreset { get; init; } = DefaultSandboxPreset;
    public LanguageMode LanguageMode { get; init; } = LanguageMode.Standard;
    public long? MaxStatements { get; init; } = DefaultMaxStatements;
    public long? MaxLoopIterations { get; init; } = DefaultMaxLoopIterations;
    public TimeSpan? MaxTimeout { get; init; } = DefaultMaxTimeout;
    public IReadOnlyList<string> ExtraNamespaces { get; init; } = [];
    public IReadOnlyList<string> ExtraAssemblies { get; init; } = [];

    public static McpParseResult ParseArguments(string[] args)
    {
        var sandboxPreset = DefaultSandboxPreset;
        var sandbox = SandboxOptions.Trusted();
        var languageMode = LanguageMode.Standard;
        long? maxStatements = DefaultMaxStatements;
        long? maxLoopIterations = DefaultMaxLoopIterations;
        TimeSpan? maxTimeout = DefaultMaxTimeout;
        var extraNamespaces = new List<string>();
        var extraAssemblies = new List<string>();

        ApplyEnvVars(ref sandboxPreset, ref sandbox, ref languageMode,
            ref maxStatements, ref maxLoopIterations, ref maxTimeout,
            extraNamespaces, extraAssemblies);

        if (ShouldShowHelp(args))
            return new McpParseResult(null, ShowHelp: true);

        ApplyCli(args, ref sandboxPreset, ref sandbox, ref languageMode,
            ref maxStatements, ref maxLoopIterations, ref maxTimeout,
            extraNamespaces, extraAssemblies);

        return new McpParseResult(new McpServerConfig
        {
            Sandbox = sandbox,
            SandboxPreset = sandboxPreset,
            LanguageMode = languageMode,
            MaxStatements = maxStatements,
            MaxLoopIterations = maxLoopIterations,
            MaxTimeout = maxTimeout,
            ExtraNamespaces = extraNamespaces.AsReadOnly(),
            ExtraAssemblies = extraAssemblies.AsReadOnly(),
        }, ShowHelp: false);
    }

    internal static IReadOnlyList<string> GetImplicitNamespaces() =>
        Runtime.TypeAssemblyIndex.GetDefaultImplicitNamespaces();

    private static bool ShouldShowHelp(string[] args) =>
        args.Any(arg => arg is "--help" or "-h");

    private static void ApplyEnvVars(
        ref string sandboxPreset, ref SandboxOptions sandbox,
        ref LanguageMode languageMode,
        ref long? maxStatements, ref long? maxLoopIterations, ref TimeSpan? maxTimeout,
        List<string> extraNamespaces, List<string> extraAssemblies)
    {
        if (Environment.GetEnvironmentVariable("ALDER_SANDBOX") is { Length: > 0 } envSandbox)
        {
            sandboxPreset = envSandbox.ToLowerInvariant();
            sandbox = ParseSandbox(envSandbox);
        }

        if (Environment.GetEnvironmentVariable("ALDER_LANGUAGE_MODE") is { Length: > 0 } envLangMode)
            languageMode = ParseLanguageMode(envLangMode);

        if (Environment.GetEnvironmentVariable("ALDER_MAX_STATEMENTS") is { Length: > 0 } envStatements)
            maxStatements = ParseLimit(envStatements, "ALDER_MAX_STATEMENTS");

        if (Environment.GetEnvironmentVariable("ALDER_MAX_LOOP_ITERATIONS") is { Length: > 0 } envIterations)
            maxLoopIterations = ParseLimit(envIterations, "ALDER_MAX_LOOP_ITERATIONS");

        if (Environment.GetEnvironmentVariable("ALDER_MAX_TIMEOUT") is { Length: > 0 } envTimeout)
            maxTimeout = ParseTimeout(envTimeout);

        if (Environment.GetEnvironmentVariable("ALDER_NAMESPACES") is { Length: > 0 } envNamespaces)
            extraNamespaces.AddRange(envNamespaces.Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (Environment.GetEnvironmentVariable("ALDER_ASSEMBLIES") is { Length: > 0 } envAssemblies)
        {
            foreach (var path in envAssemblies.Split(';',
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                extraAssemblies.Add(NormalizeAndValidateAssemblyPath(path));
            }
        }
    }

    private static void ApplyCli(
        string[] args,
        ref string sandboxPreset, ref SandboxOptions sandbox,
        ref LanguageMode languageMode,
        ref long? maxStatements, ref long? maxLoopIterations, ref TimeSpan? maxTimeout,
        List<string> extraNamespaces, List<string> extraAssemblies)
    {
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--help" or "-h":
                    break;

                case "--sandbox":
                    var sp = NextValue(args, ref i, "--sandbox");
                    sandboxPreset = sp.ToLowerInvariant();
                    sandbox = ParseSandbox(sp);
                    break;

                case "--language-mode":
                    languageMode = ParseLanguageMode(NextValue(args, ref i, "--language-mode"));
                    break;

                case "--max-statements":
                    maxStatements = ParseLimit(NextValue(args, ref i, "--max-statements"), "--max-statements");
                    break;

                case "--max-loop-iterations":
                    maxLoopIterations = ParseLimit(NextValue(args, ref i, "--max-loop-iterations"), "--max-loop-iterations");
                    break;

                case "--max-timeout":
                    maxTimeout = ParseTimeout(NextValue(args, ref i, "--max-timeout"));
                    break;

                case "--namespace":
                    extraNamespaces.Add(NextValue(args, ref i, "--namespace"));
                    break;

                case "--assembly":
                    extraAssemblies.Add(NormalizeAndValidateAssemblyPath(NextValue(args, ref i, "--assembly")));
                    break;

                default:
                    throw new ArgumentException($"Unknown option '{args[i]}'. Run 'alder-mcp --help' for usage.");
            }
        }
    }

    private static string NormalizeAndValidateAssemblyPath(string pathValue)
    {
        var path = Path.GetFullPath(pathValue);
        if (!File.Exists(path))
            throw new ArgumentException($"Assembly file not found: '{path}'.");

        try
        {
            _ = AssemblyName.GetAssemblyName(path);
        }
        catch (Exception ex) when (ex is BadImageFormatException or FileLoadException)
        {
            throw new ArgumentException($"Assembly file is not loadable: '{path}'. {ex.Message}", ex);
        }

        return path;
    }

    private static string NextValue(string[] args, ref int i, string argName)
    {
        i++;
        if (i >= args.Length)
            throw new ArgumentException($"'{argName}' requires a value.");
        var value = args[i];
        if (value.StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException($"'{argName}' requires a value but got '{value}'.");
        return value;
    }

    private static SandboxOptions ParseSandbox(string value) => value.ToLowerInvariant() switch
    {
        "trusted" => SandboxOptions.Trusted(),
        "safe" => SandboxOptions.Safe(),
        "strict" => SandboxOptions.Strict(),
        _ => throw new ArgumentException(
            $"Invalid sandbox preset '{value}'. Expected: trusted, safe, or strict.")
    };

    private static LanguageMode ParseLanguageMode(string value) => value.ToLowerInvariant() switch
    {
        "standard" => LanguageMode.Standard,
        "extended" => LanguageMode.Extended,
        _ => throw new ArgumentException(
            $"Invalid language mode '{value}'. Expected: standard or extended.")
    };

    private static long? ParseLimit(string value, string argName)
    {
        if (value.Equals("unlimited", StringComparison.OrdinalIgnoreCase))
            return null;
        if (long.TryParse(value, out var result) && result > 0)
            return result;
        throw new ArgumentException(
            $"Invalid value '{value}' for {argName}. Expected a positive integer or 'unlimited'.");
    }

    private static TimeSpan? ParseTimeout(string value)
    {
        if (value.Equals("unlimited", StringComparison.OrdinalIgnoreCase))
            return null;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds > 0)
            return TimeSpan.FromSeconds(seconds);
        throw new ArgumentException(
            $"Invalid value '{value}' for --max-timeout. Expected a positive number of seconds or 'unlimited'.");
    }

    public static void PrintHelp(TextWriter writer)
    {
        writer.WriteLine($"""
            Alder MCP Server: C# expression evaluation for AI agents

            Usage: alder-mcp [options]

            Options:
              --sandbox <preset>            Security preset: trusted, safe, or strict (default: {DefaultSandboxPreset})
                                              trusted  = all operations allowed
                                              safe     = property access and assignment; no method calls or construction
                                              strict   = read-only property access only

              --language-mode <mode>        C# language mode: standard or extended (default: standard)
                                              standard = standard C# expression semantics
                                              extended = superset with additional operators, builtins, and sugar

              --max-statements <n>          Maximum statements per evaluation, or 'unlimited' (default: {DefaultMaxStatements})
              --max-loop-iterations <n>     Maximum iterations per loop, or 'unlimited' (default: {DefaultMaxLoopIterations})
              --max-timeout <seconds>       Wall-clock timeout in seconds, or 'unlimited' (default: {DefaultMaxTimeout.TotalSeconds:0})

              --namespace <name>            Add a .NET namespace to implicit imports (repeatable)
              --assembly <path>             Load an external assembly and expose its types (repeatable)

              -h, --help                    Show this help message

            Environment variables (overridden by CLI args):
              ALDER_SANDBOX                 Same values as --sandbox
              ALDER_LANGUAGE_MODE           Same values as --language-mode
              ALDER_MAX_STATEMENTS          Same values as --max-statements
              ALDER_MAX_LOOP_ITERATIONS     Same values as --max-loop-iterations
              ALDER_MAX_TIMEOUT             Same values as --max-timeout
              ALDER_NAMESPACES              Comma-separated namespaces
              ALDER_ASSEMBLIES              Semicolon-separated assembly paths
            """);
    }
}
