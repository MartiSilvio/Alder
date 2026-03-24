using Alder;

internal sealed class McpServerConfig
{
    public SandboxOptions Sandbox { get; init; } = SandboxOptions.Trusted();
    public LanguageMode LanguageMode { get; init; } = LanguageMode.Standard;
    public long? MaxStatements { get; init; } = 100_000_000;
    public long? MaxLoopIterations { get; init; } = 100_000_000;
    public TimeSpan? MaxTimeout { get; init; } = TimeSpan.FromSeconds(30);

    public static McpServerConfig Parse(string[] args)
    {
        var sandbox = SandboxOptions.Trusted();
        var languageMode = LanguageMode.Standard;
        long? maxStatements = 100_000_000;
        long? maxLoopIterations = 100_000_000;
        TimeSpan? maxTimeout = TimeSpan.FromSeconds(30);

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--help" or "-h":
                    PrintHelp();
                    Environment.Exit(0);
                    break;

                case "--sandbox" when TryNextArg(args, i, out var value):
                    sandbox = ParseSandbox(value);
                    i++;
                    break;

                case "--language-mode" when TryNextArg(args, i, out var value):
                    languageMode = ParseLanguageMode(value);
                    i++;
                    break;

                case "--max-statements" when TryNextArg(args, i, out var value):
                    maxStatements = ParseLimit(value, "--max-statements");
                    i++;
                    break;

                case "--max-loop-iterations" when TryNextArg(args, i, out var value):
                    maxLoopIterations = ParseLimit(value, "--max-loop-iterations");
                    i++;
                    break;

                case "--max-timeout" when TryNextArg(args, i, out var value):
                    maxTimeout = ParseTimeout(value);
                    i++;
                    break;
            }
        }

        return new McpServerConfig
        {
            Sandbox = sandbox,
            LanguageMode = languageMode,
            MaxStatements = maxStatements,
            MaxLoopIterations = maxLoopIterations,
            MaxTimeout = maxTimeout,
        };
    }

    static bool TryNextArg(string[] args, int current, out string value)
    {
        if (current + 1 < args.Length)
        {
            value = args[current + 1];
            return true;
        }
        value = "";
        return false;
    }

    static SandboxOptions ParseSandbox(string value) => value.ToLowerInvariant() switch
    {
        "trusted" => SandboxOptions.Trusted(),
        "safe" => SandboxOptions.Safe(),
        "strict" => SandboxOptions.Strict(),
        _ => throw new ArgumentException(
            $"Invalid sandbox preset '{value}'. Expected: trusted, safe, or strict.")
    };

    static LanguageMode ParseLanguageMode(string value) => value.ToLowerInvariant() switch
    {
        "standard" => LanguageMode.Standard,
        "extended" => LanguageMode.Extended,
        _ => throw new ArgumentException(
            $"Invalid language mode '{value}'. Expected: standard or extended.")
    };

    static long? ParseLimit(string value, string argName)
    {
        if (value.Equals("unlimited", StringComparison.OrdinalIgnoreCase))
            return null;

        if (long.TryParse(value, out var result) && result > 0)
            return result;

        throw new ArgumentException(
            $"Invalid value '{value}' for {argName}. Expected a positive integer or 'unlimited'.");
    }

    static TimeSpan? ParseTimeout(string value)
    {
        if (value.Equals("unlimited", StringComparison.OrdinalIgnoreCase))
            return null;

        if (double.TryParse(value, out var seconds) && seconds > 0)
            return TimeSpan.FromSeconds(seconds);

        throw new ArgumentException(
            $"Invalid value '{value}' for --max-timeout. Expected a positive number of seconds or 'unlimited'.");
    }

    static void PrintHelp()
    {
        Console.Error.WriteLine("""
            Alder MCP Server — C# expression evaluation for AI agents

            Usage: alder-mcp [options]

            Options:
              --sandbox <preset>            Security preset: trusted, safe, or strict (default: trusted)
                                              trusted  — all operations allowed
                                              safe     — property access and assignment, no method calls or construction
                                              strict   — read-only property access only

              --language-mode <mode>        C# language mode: standard or extended (default: standard)
                                              standard — standard C# expression semantics
                                              extended — superset with additional operators, builtins, and sugar

              --max-statements <n>          Maximum statements per evaluation (default: 100000000)
              --max-loop-iterations <n>     Maximum iterations per loop (default: 100000000)
              --max-timeout <seconds>       Wall-clock timeout in seconds (default: 30)

              Use 'unlimited' for --max-statements, --max-loop-iterations, or --max-timeout to remove the limit.

              -h, --help                    Show this help message

            Example MCP client configuration:
              {
                "mcpServers": {
                  "alder": {
                    "command": "alder-mcp",
                    "args": ["--sandbox", "safe", "--max-timeout", "10"]
                  }
                }
              }
            """);
    }
}
