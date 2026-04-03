using Alder;
using Alder.AotMatrix;

if (args.Length > 0 && args[0] == "--single" && args.Length > 1)
    return await TestSingle.Run(args[1]);
if (args.Length > 0 && args[0] == "--diag")
    return await TestSingle.Run("--diag");
if (args.Length > 0 && args[0] == "--check-tuple")
    return await TestSingle.Run("--check-tuple");
if (args.Length > 0 && args[0] == "--check-factories")
    return await TestSingle.Run("--check-factories");
if (args.Length > 0 && args[0] == "--check-enum")
    return await TestSingle.Run("--check-enum");

Console.WriteLine("AOT Matrix starting...");
Console.Out.Flush();

try
{
    Console.WriteLine("Creating engine...");
    Console.Out.Flush();
    var testEngine = new AlderEngine();
    Console.WriteLine("Engine created OK");
    Console.Out.Flush();
    var syncResult = testEngine.Evaluate("1 + 1");
    Console.WriteLine($"Sync eval:  {syncResult}");
    Console.Out.Flush();
    var asyncResult = await testEngine.EvaluateAsync("1 + 1");
    Console.WriteLine($"Async eval: {asyncResult}");
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

var testDataBase = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "TestData");

var syncDir = Path.Combine(testDataBase, "ValidExpressions");
var asyncDir = Path.Combine(testDataBase, "ValidAsyncExpressions");

if (!Directory.Exists(syncDir))
{
    Console.Error.WriteLine($"TestData directory not found: {syncDir}");
    Console.Error.WriteLine("Usage: Alder.AotMatrix [path-to-TestData]");
    return 1;
}

var syncFiles = Directory.Exists(syncDir)
    ? Directory.GetFiles(syncDir, "*.csx", SearchOption.AllDirectories)
    : Array.Empty<string>();
var asyncFiles = Directory.Exists(asyncDir)
    ? Directory.GetFiles(asyncDir, "*.csx", SearchOption.AllDirectories)
    : Array.Empty<string>();

var files = syncFiles.Concat(asyncFiles)
    .Where(f => !f.EndsWith(".roslyn.csx", StringComparison.OrdinalIgnoreCase))
    .OrderBy(f => f)
    .ToArray();

int syncPass = 0, syncFail = 0, asyncPass = 0, asyncFail = 0;
var failures = new List<FailureRecord>();

var selfPath = Environment.ProcessPath!;

foreach (var file in files)
{
    var relPath = Path.GetRelativePath(testDataBase, file);
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
        failures.Add(new FailureRecord(relPath, "Sync", "Timeout", "Expression exceeded 30s"));
        failures.Add(new FailureRecord(relPath, "Async", "Timeout", "Expression exceeded 30s"));
        syncFail++;
        asyncFail++;
        continue;
    }

    var stdout = proc.StandardOutput.ReadToEnd();
    var stderr = proc.StandardError.ReadToEnd().Trim();
    var outputLines = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    var syncLine = outputLines.FirstOrDefault(l => l.StartsWith("SYNC:"));
    var asyncLine = outputLines.FirstOrDefault(l => l.StartsWith("ASYNC:"));

    if (proc.ExitCode == 139)
    {
        Console.WriteLine("FAIL: SIGSEGV");
        failures.Add(new FailureRecord(relPath, "Sync", "SIGSEGV", ""));
        failures.Add(new FailureRecord(relPath, "Async", "SIGSEGV", ""));
        syncFail++;
        asyncFail++;
        continue;
    }

    var syncSkipped = syncLine != null && syncLine.StartsWith("SYNC:SKIP:");
    var syncOk = syncLine != null && syncLine.StartsWith("SYNC:PASS:");
    var asyncOk = asyncLine != null && asyncLine.StartsWith("ASYNC:PASS:");

    if (syncSkipped) { /* async-only expression, don't count sync */ }
    else if (syncOk) syncPass++; else syncFail++;
    if (asyncOk) asyncPass++; else asyncFail++;

    var syncResult = syncSkipped || syncOk;
    if (syncResult && asyncOk)
    {
        Console.WriteLine(syncSkipped ? "OK (async-only)" : "OK");
    }
    else
    {
        var parts = new List<string>();
        if (!syncResult) parts.Add("sync");
        if (!asyncOk) parts.Add("async");
        Console.WriteLine($"FAIL: {string.Join("+", parts)}");
    }

    if (!syncResult && !syncSkipped)
    {
        var msg = syncLine?["SYNC:FAIL:".Length..] ?? stderr;
        if (msg.Length > 120) msg = msg[..120] + "...";
        failures.Add(new FailureRecord(relPath, "Sync", "RuntimeError", msg));
    }

    if (!asyncOk)
    {
        var msg = asyncLine?["ASYNC:FAIL:".Length..] ?? stderr;
        if (msg.Length > 120) msg = msg[..120] + "...";
        failures.Add(new FailureRecord(relPath, "Async", "RuntimeError", msg));
    }
}

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  Alder AOT Compatibility Matrix                            ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine($"  Total expressions:  {files.Length}");
Console.WriteLine();
Console.WriteLine($"  Sync  Pass:         {syncPass}");
Console.WriteLine($"  Sync  Fail:         {syncFail}");
Console.WriteLine($"  Sync  Pass rate:    {(double)syncPass / files.Length:P1}");
Console.WriteLine();
Console.WriteLine($"  Async Pass:         {asyncPass}");
Console.WriteLine($"  Async Fail:         {asyncFail}");
Console.WriteLine($"  Async Pass rate:    {(double)asyncPass / files.Length:P1}");
Console.WriteLine();

foreach (var mode in new[] { "Sync", "Async" })
{
    var modeFailures = failures.Where(f => f.Mode == mode).ToArray();
    if (modeFailures.Length == 0) continue;

    Console.WriteLine($"  {mode} failure breakdown:");
    var grouped = modeFailures
        .GroupBy(f => f.ErrorType)
        .OrderByDescending(g => g.Count());
    foreach (var g in grouped)
        Console.WriteLine($"    {g.Key,-35} {g.Count(),5}");
    Console.WriteLine();

    var byCategory = modeFailures
        .GroupBy(f =>
        {
            var dir = Path.GetDirectoryName(f.File)?.Replace(Path.DirectorySeparatorChar, '/') ?? "";
            return dir.Contains('/') ? dir[..dir.IndexOf('/')] : dir;
        })
        .OrderByDescending(g => g.Count())
        .Take(15);

    Console.WriteLine($"  {mode} top failing categories:");
    foreach (var g in byCategory)
        Console.WriteLine($"    {g.Key,-35} {g.Count(),5}");
    Console.WriteLine();
}

var reportDir = Environment.GetEnvironmentVariable("AOT_REPORT_DIR") ?? Directory.GetCurrentDirectory();
var artifactsDir = Path.Combine(reportDir, "artifacts", "aot-matrix");
Directory.CreateDirectory(artifactsDir);
var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
var reportPath = Path.Combine(artifactsDir, $"aot-matrix_{timestamp}.tsv");
var lines = new List<string> { "File\tMode\tErrorType\tMessage" };
lines.AddRange(failures.Select(f => $"{f.File}\t{f.Mode}\t{f.ErrorType}\t{f.Message}"));
File.WriteAllLines(reportPath, lines);
Console.WriteLine($"  Full report: {reportPath}");

return (syncFail > 0 || asyncFail > 0) ? 1 : 0;

namespace Alder.AotMatrix
{
    record FailureRecord(string File, string Mode, string ErrorType, string Message);
}
