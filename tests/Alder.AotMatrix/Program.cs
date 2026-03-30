using Alder;
using Alder.AotMatrix;

if (args.Length > 0 && args[0] == "--single" && args.Length > 1)
    return TestSingle.Run(args[1]);
if (args.Length > 0 && args[0] == "--diag")
    return TestSingle.Run("--diag");
if (args.Length > 0 && args[0] == "--check-tuple")
    return TestSingle.Run("--check-tuple");
if (args.Length > 0 && args[0] == "--check-factories")
    return TestSingle.Run("--check-factories");
if (args.Length > 0 && args[0] == "--check-enum")
    return TestSingle.Run("--check-enum");

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

var selfPath = Environment.ProcessPath!;

foreach (var file in files)
{
    var relPath = Path.GetRelativePath(testDataDir, file);
    Console.Write($"  {relPath}... ");
    Console.Out.Flush();

    var psi = new System.Diagnostics.ProcessStartInfo(selfPath, $"--single \"{file}\"")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    using var proc = System.Diagnostics.Process.Start(psi)!;
    proc.WaitForExit(30_000);

    if (!proc.HasExited)
    {
        proc.Kill();
        Console.WriteLine("FAIL: Timeout");
        failures.Add(new FailureRecord(relPath, "Timeout", "Expression exceeded 30s"));
        fail++;
    }
    else if (proc.ExitCode == 0)
    {
        Console.WriteLine("OK");
        pass++;
    }
    else
    {
        var stderr = proc.StandardError.ReadToEnd().Trim();
        var stdout = proc.StandardOutput.ReadToEnd().Trim();
        var output = string.IsNullOrEmpty(stderr) ? stdout : stderr;
        var errorType = proc.ExitCode == 139 ? "StackOverflow(SIGSEGV)" : "RuntimeError";
        var messageLine = output.Split('\n').FirstOrDefault(l => l.StartsWith("Message:")) ?? output.Split('\n').LastOrDefault() ?? "";
        var message = messageLine.Length > 120 ? messageLine[..120] + "..." : messageLine;
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
