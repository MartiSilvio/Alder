using System.Diagnostics;
using Alder.Parsing;

namespace Alder.Test.Performance;

[TestFixture]
[Explicit]
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
            var parser = ExpressionParser.CreateForSubExpression(tokens);
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
            var parser = ExpressionParser.CreateForSubExpression(tokens);
            parser.Parse();
        }
        sw.Stop();

        ReportResult("Parse complex expression", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_Evaluate_Arithmetic()
    {
        const string expression = "1 + 2 * 3 - 4 / 2";
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());

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
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
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
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
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
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());

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
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

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
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

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
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("name", "John")
            .SetVariable("age", 30);

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
        var engine = new AlderEngine(AlderOptions.Default with { LanguageMode = LanguageMode.Extended });

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
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());

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
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
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
        var parentEngine = new AlderEngine(AlderOptions.Default.UseCompiler())
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
            var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
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

        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
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

    [Test]
    public void Benchmark_MetadataProvider_TypedObjectPropertyAccess()
    {
        // This test stresses the TypeMetadataProvider by accessing properties on typed objects
        const string expression = "person.FirstName + \" \" + person.LastName + \" (\" + person.Age + \")\"";
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("person", new Person { FirstName = "John", LastName = "Doe", Age = 30 });

        Warmup(() => engine.Evaluate(expression));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expression);
        }
        sw.Stop();

        ReportResult("Typed object property access (3 props)", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_ReflectionCache_ManyTypedObjects()
    {
        // Access properties across MANY different types to stress the cache
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("person", new Person { FirstName = "John", LastName = "Doe", Age = 30 })
            .SetVariable("order", new Order { Id = 1, Total = 99.99m, Status = "Pending" })
            .SetVariable("product", new Product { Name = "Widget", Price = 19.99, InStock = true });

        const string expression = "person.FirstName + \" ordered \" + product.Name + \" for $\" + order.Total";

        Warmup(() => engine.Evaluate(expression));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expression);
        }
        sw.Stop();

        ReportResult("Multiple typed objects (3 types, 4 props)", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_ReflectionCache_ObjectMerge()
    {
        // Object merging uses GetProperties heavily
        const string expression = "person + new { FullName = person.FirstName + \" \" + person.LastName, IsAdult = person.Age >= 18 }";
        var engine = new AlderEngine(AlderOptions.Default with { LanguageMode = LanguageMode.Extended })
            .SetVariable("person", new Person { FirstName = "John", LastName = "Doe", Age = 30 });

        Warmup(() => engine.Evaluate(expression));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expression);
        }
        sw.Stop();

        ReportResult("Object merge (typed + anonymous)", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_ReflectionCache_SpreadOperator()
    {
        // Spread also uses GetProperties
        const string expression = "new { ..person, Email = \"john@example.com\" }";
        var engine = new AlderEngine(AlderOptions.Default with { LanguageMode = LanguageMode.Extended })
            .SetVariable("person", new Person { FirstName = "John", LastName = "Doe", Age = 30 });

        Warmup(() => engine.Evaluate(expression));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expression);
        }
        sw.Stop();

        ReportResult("Spread typed object", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_ReflectionCache_MethodCallOnTypedObject()
    {
        // Calling methods on typed objects uses GetMethods
        const string expression = "text.ToUpper()";
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("text", "hello world");

        Warmup(() => engine.Evaluate(expression));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expression);
        }
        sw.Stop();

        ReportResult("Method call on string", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_ReflectionCache_HeavyReflection()
    {
        // Combine all reflection-heavy operations in one expression
        const string expression = @"
            {
                var merged = person + new { FullName = person.FirstName + "" "" + person.LastName };
                var summary = $""{merged.FullName} ({person.Age}) - {order.Status}: ${order.Total}"";
                return new { ..merged, Summary = summary, Product = product.Name };
            }
        ";

        var engine = new AlderEngine(AlderOptions.Default with { LanguageMode = LanguageMode.Extended })
            .SetVariable("person", new Person { FirstName = "John", LastName = "Doe", Age = 30 })
            .SetVariable("order", new Order { Id = 1, Total = 99.99m, Status = "Complete" })
            .SetVariable("product", new Product { Name = "Widget", Price = 19.99, InStock = true });

        Warmup(() => engine.Evaluate(expression));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expression);
        }
        sw.Stop();

        ReportResult("Heavy reflection (merge + spread + props)", sw.ElapsedMilliseconds);
    }

    #region IL Compilation Benchmarks

    [Test]
    public void Benchmark_CompiledExpression_SimpleArithmetic()
    {
        const string expression = "1 + 2 * 3";

        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.ParseAndCompile(expression);

        // Verify it's compiled
        Assert.That(expr.IsCompiled, Is.True);

        Warmup(() => engine.Evaluate(expr));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expr);
        }
        sw.Stop();

        ReportResult("Simple arithmetic (IL compiled)", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_CompiledExpression_WithVariables()
    {
        const string expression = "x + y * z";

        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("x", 10L)
            .SetVariable("y", 20L)
            .SetVariable("z", 30L);
        var expr = engine.ParseAndCompile(expression);

        Assert.That(expr.IsCompiled, Is.True);

        Warmup(() => engine.Evaluate(expr));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expr);
        }
        sw.Stop();

        ReportResult("With variables (IL compiled)", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_CompiledExpression_Ternary()
    {
        const string expression = "x > 5 ? x * 2 : x + 10";

        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("x", 10L);
        var expr = engine.ParseAndCompile(expression);

        Assert.That(expr.IsCompiled, Is.True);

        Warmup(() => engine.Evaluate(expr));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expr);
        }
        sw.Stop();

        ReportResult("Ternary (IL compiled)", sw.ElapsedMilliseconds);
    }

    [Test]
    public void Benchmark_CompiledExpression_PropertyAccess()
    {
        const string expression = "person.Name";

        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("person", new Person { FirstName = "John", LastName = "Doe", Age = 30 });
        var expr = engine.ParseAndCompile(expression);

        Assert.That(expr.IsCompiled, Is.True);

        Warmup(() => engine.Evaluate(expr));

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < BenchmarkIterations; i++)
        {
            engine.Evaluate(expr);
        }
        sw.Stop();

        ReportResult("Property access (IL compiled)", sw.ElapsedMilliseconds);
    }

    #endregion

    public class TestModule
    {
        public long Add(long a, long b) => a + b;
        public long Multiply(long a, long b) => a * b;
    }

    public class Person
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Name => $"{FirstName} {LastName}";
        public int Age { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = "";
    }

    public class Product
    {
        public string Name { get; set; } = "";
        public double Price { get; set; }
        public bool InStock { get; set; }
    }
}
