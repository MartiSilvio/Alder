using System.Reflection;
using Alder;
using Alder.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

McpParseResult parsed;
try
{
    parsed = McpServerConfig.ParseArguments(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine($"alder-mcp: {ex.Message}");
    Console.Error.WriteLine("Run 'alder-mcp --help' for usage.");
    return 1;
}

if (parsed.ShowHelp)
{
    McpServerConfig.PrintHelp(Console.Out);
    return 0;
}

var config = parsed.Config!;
Assembly[] loadedAssemblies;
try
{
    loadedAssemblies = config.ExtraAssemblies.Select(Assembly.LoadFrom).ToArray();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"alder-mcp: Failed to load configured assemblies. {ex.Message}");
    return 1;
}

// Pass an empty argument list because the host does not parse Alder CLI arguments.
var builder = Host.CreateApplicationBuilder([]);
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton(config);

builder.Services.AddSingleton(_ => new AlderEngine(o =>
{
    o.Sandbox = config.Sandbox;
    o.LanguageMode = config.LanguageMode;
    o.Constraints = new ExecutionConstraints
    {
        MaxStatements = config.MaxStatements,
        MaxLoopIterations = config.MaxLoopIterations,
        MaxTimeout = config.MaxTimeout,
    };
    foreach (var ns in config.ExtraNamespaces)
        o.Types.AddNamespace(ns);
    foreach (var assembly in loadedAssemblies)
        o.Types.AddAssembly(assembly);
}));

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "alder",
            Version = typeof(AlderEngine).Assembly.GetName().Version?.ToString() ?? "0.1.0",
        };
        options.ServerInstructions =
            "Alder is a C# runtime engine. Use 'evaluate' as the primary execution tool. " +
            "Use 'validate' only when the caller needs preflight diagnostics without execution, and pass the same variables payload you would send to 'evaluate'. " +
            "Use 'evaluate_with_trace' only when the user needs step-by-step evaluation details. " +
            "Call 'get_info' to check the active sandbox, language mode, execution limits, and available namespaces. " +
            $"Pre-imported: {string.Join(", ", McpServerConfig.GetImplicitNamespaces())}. " +
            "Crypto (SHA256, Aes, RSA, etc.) requires full qualification: System.Security.Cryptography.SHA256.HashData(...) " +
            "or --namespace System.Security.Cryptography for short names.";
    })
    .WithStdioServerTransport()
    .WithTools<AlderTools>();

await builder.Build().RunAsync();
return 0;
