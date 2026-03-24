using Alder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var config = McpServerConfig.Parse(args);

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

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
            "Alder is a C# expression evaluator. Use it to compute values, transform data, and execute " +
            "C# logic at runtime without a full compilation step. " +
            "The 'evaluate' tool executes expressions and returns results. " +
            "The 'validate' tool checks syntax and semantics without execution — use it to verify expressions before running them. " +
            "The 'evaluate_with_trace' tool shows step-by-step evaluation — use it only when the user needs to understand HOW a result was computed. " +
            "All tools accept standard C# expression syntax including arithmetic, string operations, LINQ, lambdas, " +
            "ternary/null-coalescing operators, pattern matching, and method calls on .NET types. " +
            "Expressions do NOT support class/struct/enum/record declarations, top-level using directives, or async/await. " +
            "Multi-statement expressions use semicolons and the last expression is the return value (e.g., 'var x = 5; x * 2'). " +
            "Variables can be injected as a JSON object to parameterize expressions. " +
            "When working with tool results, write down any important information you might need later in your response, " +
            "as the original tool result may be cleared later.";
    })
    .WithStdioServerTransport()
    .WithTools<AlderTools>();

await builder.Build().RunAsync();
