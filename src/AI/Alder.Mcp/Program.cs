using System.Reflection;
using Alder;
using Alder.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var config = McpServerConfig.Parse(args);

// Pass [] — the host does not parse Alder's CLI args.
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
    foreach (var path in config.ExtraAssemblies)
        o.Types.AddAssembly(Assembly.LoadFrom(path));
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
            "Alder is a C# runtime engine. Call 'validate' before 'evaluate' on untrusted input. " +
            "Use 'evaluate_with_trace' only when the user needs step-by-step evaluation details. " +
            "Call 'get_info' to check the active sandbox, language mode, execution limits, and available namespaces. " +
            "Pre-imported: System, System.Collections.Generic, System.Linq, System.Threading.Tasks, " +
            "System.Text, System.Text.RegularExpressions, System.Text.Json, System.Numerics, System.Globalization. " +
            "Crypto (SHA256, Aes, RSA, etc.) requires full qualification: System.Security.Cryptography.SHA256.HashData(...) " +
            "or --namespace System.Security.Cryptography for short names.";
    })
    .WithStdioServerTransport()
    .WithTools<AlderTools>();

await builder.Build().RunAsync();
