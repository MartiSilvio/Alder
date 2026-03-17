using CsEval;
using CsEval.AotMatrix;

var testDataDir = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "TestData", "ValidExpressions");

if (!Directory.Exists(testDataDir))
{
    Console.Error.WriteLine($"TestData directory not found: {testDataDir}");
    Console.Error.WriteLine("Usage: CsEval.AotMatrix [path-to-ValidExpressions]");
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

    try
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = CompilationMode.Interpreted,
            LanguageMode = LanguageMode.Extended,
            Constraints = new ExecutionConstraints { MaxStatements = 100_000 }
        });
        engine.Evaluate(expr);
        pass++;
    }
    catch (Exception ex)
    {
        var errorType = ex.GetType().Name;
        var message = ex.Message.Length > 120 ? ex.Message[..120] + "..." : ex.Message;
        failures.Add(new FailureRecord(relPath, errorType, message));
        fail++;
    }
}

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  CsEval AOT Compatibility Matrix                            ║");
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

namespace CsEval.AotMatrix
{
    record FailureRecord(string File, string ErrorType, string Message);
}
