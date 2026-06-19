using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Alder;
using Alder.Parity;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

// Three-way value-parity harness.
//
// For every corpus expression it computes the result three ways — Roslyn (the
// oracle, via CSharpScript), Alder-JIT (in-process), and Alder-AOT (forked to
// the published NativeAOT worker) — captures value + runtime type, and fails if
// any disagree. A CSV report is written under artifacts/parity/ (git-ignored).
//
// Roslyn is best-effort: expressions it cannot compile (Alder Extended-mode
// syntax) are marked N/A and excluded from the Roslyn axis. The Alder-JIT vs
// Alder-AOT axis is always required to match — that is the real target.

var options = Args.Parse(args);
if (options is null)
{
    Args.PrintUsage();
    return 2;
}

if (!File.Exists(options.BinPath))
{
    Console.Error.WriteLine($"AOT worker binary not found: {options.BinPath}");
    Console.Error.WriteLine("Publish it first (scripts/parity-matrix.sh does this), or pass --bin.");
    return 3;
}

if (!Directory.Exists(options.TestDataDir))
{
    Console.Error.WriteLine($"TestData directory not found: {options.TestDataDir}");
    return 3;
}

var files = Harness.Discover(options.TestDataDir, options.Filter, options.Limit);
if (files.Count == 0)
{
    Console.Error.WriteLine("No expressions matched.");
    return 3;
}

Console.WriteLine($"[parity] worker:   {options.BinPath}");
Console.WriteLine($"[parity] corpus:   {files.Count} expressions");
Console.WriteLine($"[parity] output:   {options.OutPath}");
Console.WriteLine();

Directory.CreateDirectory(Path.GetDirectoryName(options.OutPath)!);

var rows = new List<Row>(files.Count);
var mismatches = 0;
var processed = 0;

foreach (var file in files)
{
    var relPath = Path.GetRelativePath(options.TestDataDir, file).Replace(Path.DirectorySeparatorChar, '/');
    var isAsync = relPath.StartsWith("ValidAsyncExpressions", StringComparison.Ordinal);
    var (alderExpr, roslynExpr) = Harness.LoadExpressionPair(file);

    var roslyn = await Harness.EvaluateRoslyn(roslynExpr);
    var jit = Harness.EvaluateAlderJit(alderExpr, isAsync);
    var aot = Harness.RunAot(options.BinPath, file, options.TimeoutMs);

    var row = Harness.Compare(relPath, alderExpr, roslyn, jit, aot);
    rows.Add(row);

    if (row.IsFailure)
    {
        mismatches++;
        Console.WriteLine($"  MISMATCH  {relPath}");
        Console.WriteLine($"            roslyn={row.RoslynType}:{Trim(row.RoslynValue)}  alder={row.AlderType}:{Trim(row.AlderValue)}  aot={row.AotType}:{Trim(row.AotValue)}  [{row.Status}]");
    }

    if (++processed % 250 == 0)
        Console.WriteLine($"  ... {processed}/{files.Count} ({mismatches} mismatches)");
}

Harness.WriteCsv(options.OutPath, rows);

Console.WriteLine();
Console.WriteLine("╔════════════════════════════════════════════════╗");
Console.WriteLine("║  Alder three-way parity (Roslyn · JIT · AOT)   ║");
Console.WriteLine("╚════════════════════════════════════════════════╝");
Console.WriteLine($"  Expressions:  {rows.Count}");
Console.WriteLine($"  Matches:      {rows.Count - mismatches}");
Console.WriteLine($"  Mismatches:   {mismatches}");
Console.WriteLine($"  Roslyn N/A:   {rows.Count(r => r.RoslynNa)}");
Console.WriteLine($"  Report:       {options.OutPath}");
Console.WriteLine();

return mismatches > 0 ? 1 : 0;

static string Trim(string s) => s.Length > 60 ? s[..60] + "…" : s;

// ────────────────────────────────────────────────────────────────────────────

static class Harness
{
    private static readonly ScriptOptions RoslynOptions = ScriptOptions.Default
        .AddReferences(typeof(object).Assembly, typeof(Enumerable).Assembly)
        .AddImports("System", "System.Collections.Generic", "System.Linq", "System.Threading.Tasks")
        .WithLanguageVersion(LanguageVersion.CSharp12);

    public static List<string> Discover(string testDataDir, string? filter, int limit)
    {
        var files = new List<string>();
        foreach (var sub in new[] { "ValidExpressions", "ValidAsyncExpressions" })
        {
            var dir = Path.Combine(testDataDir, sub);
            if (!Directory.Exists(dir)) continue;
            foreach (var f in Directory.GetFiles(dir, "*.csx", SearchOption.AllDirectories))
            {
                if (f.EndsWith(".roslyn.csx", StringComparison.OrdinalIgnoreCase)) continue;
                if (f.EndsWith(".ignore.csx", StringComparison.OrdinalIgnoreCase)) continue;
                files.Add(f);
            }
        }
        files.Sort(StringComparer.Ordinal);

        if (!string.IsNullOrEmpty(filter))
            files = files.Where(f => f.Replace('\\', '/').Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
        if (limit > 0 && files.Count > limit)
            files = files.Take(limit).ToList();
        return files;
    }

    public static (string AlderExpr, string RoslynExpr) LoadExpressionPair(string csxPath)
    {
        var alderExpr = File.ReadAllText(csxPath).Trim();
        var sibling = csxPath[..^".csx".Length] + ".roslyn.csx";
        var roslynExpr = File.Exists(sibling) ? File.ReadAllText(sibling).Trim() : alderExpr;
        return (alderExpr, roslynExpr);
    }

    public static async Task<Outcome> EvaluateRoslyn(string expr)
    {
        try
        {
            var value = await CSharpScript.EvaluateAsync<object>(expr, RoslynOptions);
            return Outcome.Ok(value);
        }
        catch (CompilationErrorException)
        {
            return Outcome.Na(); // Alder Extended-mode syntax Roslyn can't compile.
        }
        catch (Exception ex)
        {
            return Outcome.Err(ex);
        }
    }

    public static Outcome EvaluateAlderJit(string expr, bool isAsync)
    {
        try
        {
            var engine = new AlderEngine(new AlderOptions
            {
                LanguageMode = LanguageMode.Extended,
                Constraints = new ExecutionConstraints { MaxStatements = 500_000 }
            });
            var value = isAsync
                ? engine.EvaluateAsync(expr).GetAwaiter().GetResult()
                : engine.Evaluate(expr);
            return Outcome.Ok(value);
        }
        catch (Exception ex)
        {
            return Outcome.Err(ex);
        }
    }

    public static AotResult RunAot(string binPath, string file, int timeoutMs)
    {
        var isDll = binPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
        var psi = new ProcessStartInfo(isDll ? "dotnet" : binPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        if (isDll) psi.ArgumentList.Add(binPath);
        psi.ArgumentList.Add("--canonical");
        psi.ArgumentList.Add(file);

        using var proc = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        if (!proc.WaitForExit(timeoutMs))
        {
            try { proc.Kill(entireProcessTree: true); } catch { /* already gone */ }
            return new AotResult(AotKind.Timeout, "TIMEOUT", "");
        }
        var stdout = stdoutTask.GetAwaiter().GetResult();

        if (proc.ExitCode == 139)
            return new AotResult(AotKind.Crash, "CRASH", "SIGSEGV");

        var line = stdout.Split('\n').FirstOrDefault(l => l.StartsWith("CANON\t", StringComparison.Ordinal));
        if (line is null)
            return new AotResult(AotKind.Crash, "CRASH", $"no canonical output (exit {proc.ExitCode})");

        var parts = line.TrimEnd('\r').Split('\t');
        var status = parts.Length > 1 ? parts[1] : "ERR";
        var type = parts.Length > 2 ? CanonicalValue.UnescapeField(parts[2]) : "";
        var value = parts.Length > 3 ? CanonicalValue.UnescapeField(parts[3]) : "";
        return new AotResult(status == "OK" ? AotKind.Ok : AotKind.Err, type, value);
    }

    public static Row Compare(string path, string expr, Outcome roslyn, Outcome jit, AotResult aot)
    {
        var (roslynType, roslynValue) = DisplayRoslyn(roslyn);
        var (alderType, alderValue) = DisplayAlder(jit);
        var (aotType, aotValue) = DisplayAot(aot);

        var reasons = new List<string>();

        // Axis 1 — Alder-JIT vs Alder-AOT (both Alder; authoritative AOT-parity axis).
        if (jit.Kind == OutcomeKind.Ok && aot.Kind == AotKind.Ok)
        {
            if (CanonicalValue.TypeOf(jit.Value) != aot.Type) reasons.Add("JIT_AOT_TYPE");
            else if (CanonicalValue.Render(jit.Value) != aot.Value) reasons.Add("JIT_AOT_VALUE");
        }
        else if (jit.Kind == OutcomeKind.Err && aot.Kind == AotKind.Err)
        {
            if ((jit.Error!.GetType().FullName ?? "") != aot.Type) reasons.Add("JIT_AOT_ERRTYPE");
        }
        else
        {
            reasons.Add("JIT_AOT_STATUS"); // one returned; the other threw / crashed / timed out
        }

        // Axis 2 — Roslyn vs Alder-JIT (oracle; skipped when Roslyn can't compile).
        if (roslyn.Kind != OutcomeKind.Na)
        {
            if (roslyn.Kind == OutcomeKind.Ok && jit.Kind == OutcomeKind.Ok)
            {
                if (!ResultsEqual(roslyn.Value, jit.Value)) reasons.Add("ROSLYN_VALUE");
            }
            else if (roslyn.Kind == OutcomeKind.Err && jit.Kind == OutcomeKind.Err)
            {
                // both threw — cross-engine exception types differ legitimately, accept.
            }
            else
            {
                reasons.Add("ROSLYN_STATUS");
            }
        }

        var status = reasons.Count == 0
            ? (roslyn.Kind == OutcomeKind.Na ? "MATCH (roslyn n/a)" : "MATCH")
            : "MISMATCH:" + string.Join("+", reasons);

        return new Row(path, expr, roslynType, roslynValue, alderType, alderValue, aotType, aotValue,
            status, reasons.Count > 0, roslyn.Kind == OutcomeKind.Na);
    }

    private static (string Type, string Value) DisplayRoslyn(Outcome o) => o.Kind switch
    {
        OutcomeKind.Na => ("N/A", "N/A"),
        OutcomeKind.Ok => (CanonicalValue.TypeOf(o.Value), CanonicalValue.Render(o.Value)),
        _ => (o.Error!.GetType().FullName ?? "Error", "ERR: " + o.Error!.Message),
    };

    private static (string Type, string Value) DisplayAlder(Outcome o) => o.Kind switch
    {
        OutcomeKind.Ok => (CanonicalValue.TypeOf(o.Value), CanonicalValue.Render(o.Value)),
        _ => (o.Error!.GetType().FullName ?? "Error", "ERR: " + o.Error!.Message),
    };

    private static (string Type, string Value) DisplayAot(AotResult a) => a.Kind switch
    {
        AotKind.Ok => (a.Type, a.Value),
        AotKind.Err => (a.Type, "ERR: " + a.Value),
        _ => (a.Type, a.Value),
    };

    public static void WriteCsv(string path, List<Row> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Path,Expression,RoslynType,RoslynValue,AlderType,AlderValue,AotType,AotValue,Status");
        foreach (var r in rows)
            sb.Append(Csv(r.Path)).Append(',').Append(Csv(r.Expr)).Append(',')
              .Append(Csv(r.RoslynType)).Append(',').Append(Csv(r.RoslynValue)).Append(',')
              .Append(Csv(r.AlderType)).Append(',').Append(Csv(r.AlderValue)).Append(',')
              .Append(Csv(r.AotType)).Append(',').Append(Csv(r.AotValue)).Append(',')
              .Append(Csv(r.Status)).Append('\n');
        File.WriteAllText(path, sb.ToString());
    }

    private static string Csv(string field)
    {
        if (field.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return field;
        return '"' + field.Replace("\"", "\"\"") + '"';
    }

    // ── Structural value equality ──
    // Object projections (anonymous types / dictionaries) are read by the shared StructuralParity
    // helper and compared property-by-property via canonical render; everything else is compared by
    // value AND runtime type.

    private static bool ResultsEqual(object? expected, object? actual)
    {
        if (StructuralParity.TryReadStructuralParityProperties(expected, actual, out var ep, out var ap))
            return StructuralEqual(ap, ep);
        // Value AND runtime type. Canonical render gives deep, nesting-safe value
        // equality for collections (plain Equals is reference equality on arrays),
        // and TypeOf catches int-vs-long / boxing differences.
        return CanonicalValue.TypeOf(expected) == CanonicalValue.TypeOf(actual)
            && CanonicalValue.Render(expected) == CanonicalValue.Render(actual);
    }

    private static bool StructuralEqual(
        IReadOnlyDictionary<string, object?> actual,
        IReadOnlyDictionary<string, object?> expected)
    {
        if (actual.Count != expected.Count) return false;
        foreach (var (name, expectedValue) in expected)
        {
            if (!actual.TryGetValue(name, out var actualValue)) return false;
            if (CanonicalValue.Render(actualValue) != CanonicalValue.Render(expectedValue)) return false;
        }
        return true;
    }
}

enum OutcomeKind { Ok, Err, Na }

sealed record Outcome(OutcomeKind Kind, object? Value, Exception? Error)
{
    public static Outcome Ok(object? v) => new(OutcomeKind.Ok, v, null);
    public static Outcome Err(Exception e) => new(OutcomeKind.Err, null, e);
    public static Outcome Na() => new(OutcomeKind.Na, null, null);
}

enum AotKind { Ok, Err, Crash, Timeout }

sealed record AotResult(AotKind Kind, string Type, string Value);

sealed record Row(
    string Path, string Expr,
    string RoslynType, string RoslynValue,
    string AlderType, string AlderValue,
    string AotType, string AotValue,
    string Status, bool IsFailure, bool RoslynNa);

sealed class Options
{
    public required string BinPath { get; init; }
    public required string TestDataDir { get; init; }
    public required string OutPath { get; init; }
    public string? Filter { get; init; }
    public int Limit { get; init; }
    public int TimeoutMs { get; init; } = 30_000;
}

static class Args
{
    public static Options? Parse(string[] args)
    {
        string? bin = null, testData = null, outPath = null, filter = null;
        var limit = 0;
        var timeoutMs = 30_000;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--bin": bin = Next(args, ref i); break;
                case "--testdata": testData = Next(args, ref i); break;
                case "--out": outPath = Next(args, ref i); break;
                case "--filter": filter = Next(args, ref i); break;
                case "--limit": int.TryParse(Next(args, ref i), out limit); break;
                case "--timeout": int.TryParse(Next(args, ref i), out timeoutMs); break;
                default:
                    Console.Error.WriteLine($"Unknown argument: {args[i]}");
                    return null;
            }
        }

        if (bin is null || testData is null) return null;

        outPath ??= Path.Combine("artifacts", "parity",
            $"parity_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv");

        return new Options
        {
            BinPath = Path.GetFullPath(bin),
            TestDataDir = Path.GetFullPath(testData),
            OutPath = Path.GetFullPath(outPath),
            Filter = filter,
            Limit = limit,
            TimeoutMs = timeoutMs,
        };
    }

    private static string? Next(string[] args, ref int i) => ++i < args.Length ? args[i] : null;

    public static void PrintUsage()
    {
        Console.Error.WriteLine(
            "Usage: Alder.ParityHarness --bin <aot-worker> --testdata <dir> [--out <csv>] [--filter <substr>] [--limit <n>] [--timeout <ms>]");
    }
}
