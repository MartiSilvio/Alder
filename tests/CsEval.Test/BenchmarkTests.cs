using System.Diagnostics;
using CsEval.Evaluation;
using CsEval.Parsing;
using NUnit.Framework;

namespace CsEval.Test;

[TestFixture]
public class BenchmarkTests
{
    private const int WarmupIterations = 100;
    private const int BenchmarkIterations = 10000;

    [Test]
    public void Benchmark_Parse_SimpleExpression()
    {
        const string expression = "1 + 2 * 3";

        Warmup(() => new Lexer(expression).Tokenize());

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            var lexer = new Lexer(expression);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            parser.Parse();
        }
        sw.Stop();

        ReportResult("Parse simple expression", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_Parse_ComplexExpression()
    {
        const string expression = "items.Where((x) => x.Price > 10).Select((x) => x.Name + \" - $\" + x.Price)";

        Warmup(() => new Lexer(expression).Tokenize());

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            var lexer = new Lexer(expression);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            parser.Parse();
        }
        sw.Stop();

        ReportResult("Parse complex expression", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_Evaluate_Arithmetic()
    {
        const string expression = "1 + 2 * 3 - 4 / 2";
        var engine = new CsEvalEngine();

        Warmup(() => engine.Evaluate(expression));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expression);
        }
        sw.Stop();

        ReportResult("Evaluate arithmetic", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_Evaluate_WithVariables()
    {
        const string expression = "x + y * z";
        var engine = new CsEvalEngine()
            .SetVariable("x", 10L)
            .SetVariable("y", 20L)
            .SetVariable("z", 30L);

        Warmup(() => engine.Evaluate(expression));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expression);
        }
        sw.Stop();

        ReportResult("Evaluate with variables", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_Evaluate_MemberAccess()
    {
        const string expression = "user.Name";
        var engine = new CsEvalEngine()
            .SetVariable("user", new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 30 });

        Warmup(() => engine.Evaluate(expression));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expression);
        }
        sw.Stop();

        ReportResult("Evaluate member access", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_Evaluate_MethodCall()
    {
        const string expression = "Math.Sqrt(16)";
        var engine = new CsEvalEngine();

        Warmup(() => engine.Evaluate(expression));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expression);
        }
        sw.Stop();

        ReportResult("Evaluate method call", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_Evaluate_Lambda()
    {
        const string expression = "numbers.Where((x) => x > 2)";
        var engine = new CsEvalEngine()
            .SetVariable("numbers", new List<object?> { 1L, 2L, 3L, 4L, 5L });

        Warmup(() => engine.Evaluate(expression));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expression);
        }
        sw.Stop();

        ReportResult("Evaluate lambda", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_Evaluate_ChainedLambda()
    {
        const string expression = "numbers.Where((x) => x > 2).Select((x) => x * 2)";
        var engine = new CsEvalEngine()
            .SetVariable("numbers", new List<object?> { 1L, 2L, 3L, 4L, 5L });

        Warmup(() => engine.Evaluate(expression));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expression);
        }
        sw.Stop();

        ReportResult("Evaluate chained lambda", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_Evaluate_Interpolation()
    {
        const string expression = "$\"Hello {name}, you are {age} years old\"";
        var engine = new CsEvalEngine()
            .SetVariable("name", "John")
            .SetVariable("age", 30L);

        Warmup(() => engine.Evaluate(expression));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expression);
        }
        sw.Stop();

        ReportResult("Evaluate interpolation", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_Evaluate_ArrayLiteral()
    {
        const string expression = "[1, 2, 3, 4, 5]";
        var engine = new CsEvalEngine();

        Warmup(() => engine.Evaluate(expression));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expression);
        }
        sw.Stop();

        ReportResult("Evaluate array literal", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_Evaluate_AnonymousObject()
    {
        const string expression = "new { Name = \"John\", Age = 30 }";
        var engine = new CsEvalEngine();

        Warmup(() => engine.Evaluate(expression));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expression);
        }
        sw.Stop();

        ReportResult("Evaluate anonymous object", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_PreParsed_VsOnTheFly()
    {
        const string expression = "items.Where((x) => x.Price > 10).Select((x) => x.Name)";
        var engine = new CsEvalEngine()
            .SetVariable("items", CreateItems());

        var preParsed = engine.Parse(expression);

        Warmup(() => engine.Evaluate(expression));

        var swParse = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expression);
        }
        swParse.Stop();

        var swPreParsed = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(preParsed);
        }
        swPreParsed.Stop();

        ReportResult("Evaluate on-the-fly", swParse.ElapsedMilliseconds);
        ReportResult("Evaluate pre-parsed", swPreParsed.ElapsedMilliseconds);

        var improvement = (double)(swParse.ElapsedMilliseconds - swPreParsed.ElapsedMilliseconds) / swParse.ElapsedMilliseconds * 100;
        TestContext.WriteLine($"Pre-parsing improvement: {improvement:F1}%");
    }

    [Test]
    public void Benchmark_ChildEngine_VsNewEngine()
    {
        const string expression = "x + y";
        var parentEngine = new CsEvalEngine()
            .SetVariable("x", 10L)
            .SetVariable("y", 20L);

        Warmup(() => parentEngine.CreateChild().Evaluate(expression));

        var swChild = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            var child = parentEngine.CreateChild();
            child.Evaluate(expression);
        }
        swChild.Stop();

        var swNew = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            var engine = new CsEvalEngine()
                .SetVariable("x", 10L)
                .SetVariable("y", 20L);
            engine.Evaluate(expression);
        }
        swNew.Stop();

        ReportResult("CreateChild + Evaluate", swChild.ElapsedMilliseconds);
        ReportResult("New engine + Evaluate", swNew.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_ModuleMethod_Call()
    {
        const string expression = "MyModule.Add(x, y)";

        var engine = new CsEvalEngine()
            .RegisterModule<TestModule>("MyModule")
            .SetVariable("x", 10L)
            .SetVariable("y", 20L);

        Warmup(() => engine.Evaluate(expression));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expression);
        }
        sw.Stop();

        ReportResult("Module method call", sw.ElapsedMilliseconds);
    }

    private static void Warmup(Action action)
    {
        for (var i = 0; i < WarmupIterations; i++)
        {
            action();
        }
    }

    private static void ReportResult(string name, long elapsedMs)
    {
        var opsPerSecond = BenchmarkIterations * 1000.0 / elapsedMs;
        var avgMicroseconds = elapsedMs * 1000.0 / BenchmarkIterations;
        TestContext.WriteLine($"{name}: {elapsedMs}ms total, {opsPerSecond:N0} ops/sec, {avgMicroseconds:F2}μs/op");
    }

    private static List<Dictionary<string, object?>> CreateItems()
    {
        return
        [
            new Dictionary<string, object?> { ["Name"] = "Apple", ["Price"] = 1.5 },
            new Dictionary<string, object?> { ["Name"] = "Banana", ["Price"] = 0.75 },
            new Dictionary<string, object?> { ["Name"] = "Orange", ["Price"] = 15.0 },
            new Dictionary<string, object?> { ["Name"] = "Mango", ["Price"] = 3.0 },
            new Dictionary<string, object?> { ["Name"] = "Grape", ["Price"] = 12.0 }
        ];
    }

    public class TestModule
    {
        public long Add(long a, long b) => a + b;
        public long Multiply(long a, long b) => a * b;
    }
}
