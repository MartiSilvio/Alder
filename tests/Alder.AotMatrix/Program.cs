using Alder;
using Alder.AotMatrix;

if (args.Length > 0 && args[0] == "--single" && args.Length > 1)
    return TestSingle.Run(args[1]);
if (args.Length > 0 && args[0] == "--diag")
    return TestSingle.Run("--diag");
if (args.Length > 0 && args[0] == "--check-factories")
    return TestSingle.Run("--check-factories");

Console.WriteLine("AOT Matrix starting...");
Console.Out.Flush();

try
{
    Console.WriteLine("Creating engine...");
    Console.Out.Flush();
    var testEngine = new AlderEngine();
    Console.WriteLine("Engine created OK");
    Console.Out.Flush();
    var testResult = testEngine.Evaluate("1 + 1");
    Console.WriteLine($"Quick eval: {testResult}");
    Console.Out.Flush();
}
catch (Exception ex)
{
    Console.WriteLine($"CRASH: {ex.GetType().Name}: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Console.Out.Flush();
}

Console.WriteLine("Starting file scan...");
Console.Out.Flush();

var testDataDir = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "TestData", "ValidExpressions");

if (!Directory.Exists(testDataDir))
{
    Console.Error.WriteLine($"TestData directory not found: {testDataDir}");
    Console.Error.WriteLine("Usage: Alder.AotMatrix [path-to-ValidExpressions]");
    return 1;
}

var files = Directory.GetFiles(testDataDir, "*.csx", SearchOption.AllDirectories)
    .Where(f => !f.EndsWith(".roslyn.csx", StringComparison.OrdinalIgnoreCase))
    .OrderBy(f => f)
    .ToArray();

int pass = 0, fail = 0;
var failures = new List<FailureRecord>();

foreach (var file in files)
{
    var expr = File.ReadAllText(file).Trim();
    var relPath = Path.GetRelativePath(testDataDir, file);
    Console.Write($"  {relPath}... ");
    Console.Out.Flush();

    Exception? threadEx = null;
    object? threadResult = null;
    var evalThread = new Thread(() =>
    {
        try
        {
            var engine = new AlderEngine(new AlderOptions
            {
                LanguageMode = LanguageMode.Extended,
                Constraints = new ExecutionConstraints { MaxStatements = 100_000 }
            });
            threadResult = engine.Evaluate(expr);
        }
        catch (Exception ex)
        {
            threadEx = ex;
        }
    }, 16 * 1024 * 1024);
    evalThread.Start();
    evalThread.Join();

    if (threadEx == null)
    {
        Console.WriteLine("OK");
        pass++;
    }
    else
    {
        var ex = threadEx;
        var errorType = ex.GetType().Name;
        var message = ex.Message.Length > 120 ? ex.Message[..120] + "..." : ex.Message;
        Console.WriteLine($"FAIL: {errorType}");
        failures.Add(new FailureRecord(relPath, errorType, message));
        fail++;
    }
}

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  Alder AOT Compatibility Matrix                            ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine($"  Total expressions:  {files.Length}");
Console.WriteLine($"  Pass:               {pass}");
Console.WriteLine($"  Fail:               {fail}");
Console.WriteLine($"  Pass rate:          {(double)pass / files.Length:P1}");
Console.WriteLine();

var grouped = failures
    .GroupBy(f => f.ErrorType)
    .OrderByDescending(g => g.Count())
    .ToArray();

Console.WriteLine("  Failure breakdown:");
foreach (var g in grouped)
    Console.WriteLine($"    {g.Key,-35} {g.Count(),5}");

Console.WriteLine();

var byCategory = failures
    .GroupBy(f =>
    {
        var dir = Path.GetDirectoryName(f.File)?.Replace(Path.DirectorySeparatorChar, '/') ?? "";
        return dir.Contains('/') ? dir[..dir.IndexOf('/')] : dir;
    })
    .OrderByDescending(g => g.Count())
    .Take(15)
    .ToArray();

Console.WriteLine("  Top failing categories:");
foreach (var g in byCategory)
    Console.WriteLine($"    {g.Key,-35} {g.Count(),5}");

var reportDir = Environment.GetEnvironmentVariable("AOT_REPORT_DIR") ?? Directory.GetCurrentDirectory();
var artifactsDir = Path.Combine(reportDir, "artifacts", "aot-matrix");
Directory.CreateDirectory(artifactsDir);
var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
var reportPath = Path.Combine(artifactsDir, $"aot-matrix_{timestamp}.tsv");
var lines = new List<string> { "File\tErrorType\tMessage" };
lines.AddRange(failures.Select(f => $"{f.File}\t{f.ErrorType}\t{f.Message}"));
File.WriteAllLines(reportPath, lines);
Console.WriteLine($"\n  Full report: {reportPath}");

return fail > 0 ? 1 : 0;

namespace Alder.AotMatrix
{
    record FailureRecord(string File, string ErrorType, string Message);
}
