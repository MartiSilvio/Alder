using System.Collections.Concurrent;

namespace CsEval.Test.Core;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ThreadSafetyTests(CompilationMode mode)
{
    [Test]
    public void ParallelForEach_WithCreateChild_EvaluatesCorrectly()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("multiplier", 2L);

        var items = Enumerable.Range(1, 100).ToList();
        var results = new ConcurrentBag<long>();

        Parallel.ForEach(items, item =>
        {
            var child = engine.CreateChild();
            child.SetVariable("item", (long)item);
            var result = child.Evaluate("item * multiplier");
            results.Add((long)result!);
        });

        var expected = items.Select(i => (long)i * 2).OrderBy(x => x).ToList();
        var actual = results.OrderBy(x => x).ToList();

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ParallelForEach_WithCreateChild_ChildIsolation()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("shared", 100L);

        var items = Enumerable.Range(1, 50).ToList();
        var results = new ConcurrentBag<(int Item, long Result)>();

        Parallel.ForEach(items, item =>
        {
            var child = engine.CreateChild();
            child.SetVariable("local", (long)item * 10);
            var result = child.Evaluate("shared + local");
            results.Add((item, (long)result!));
        });

        foreach (var (item, result) in results)
        {
            Assert.That(result, Is.EqualTo(100L + item * 10), $"Item {item} should be {100 + item * 10}");
        }
    }

    [Test]
    public void ParallelForEach_ChildDoesNotAffectParent()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", 10L);

        var items = Enumerable.Range(1, 20).ToList();

        Parallel.ForEach(items, item =>
        {
            var child = engine.CreateChild();
            child.SetVariable("y", (long)item);
            child.Evaluate("x + y");
        });

        // Parent should still have x = 10 and no y variable
        var parentResult = engine.Evaluate("x");
        Assert.That(parentResult, Is.EqualTo(10));

        // y should not be defined in parent
        Assert.Throws<CsEvalException>(() => engine.Evaluate("y"));
    }

    [Test]
    public void ParallelForEach_WithLinqExpression()
    {
        var engine = TestEngineFactory.Create(mode);

        var datasets = Enumerable.Range(0, 20)
            .Select(i => Enumerable.Range(1, 10).Select(n => (long)(n + i * 10)).ToList())
            .ToList();

        var results = new ConcurrentBag<(int Index, long Sum)>();

        Parallel.ForEach(datasets.Select((d, i) => (Data: d, Index: i)), tuple =>
        {
            var child = engine.CreateChild();
            child.SetVariable("items", tuple.Data);
            var result = child.Evaluate("items.Sum()");
            results.Add((tuple.Index, Convert.ToInt64(result)));
        });

        foreach (var (index, sum) in results)
        {
            var expected = datasets[index].Sum();
            Assert.That(sum, Is.EqualTo(expected), $"Dataset {index} sum mismatch");
        }
    }

    [Test]
    public void ParallelForEach_WithComplexExpression()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("threshold", 5L);

        var items = Enumerable.Range(1, 50).ToList();
        var results = new ConcurrentBag<(int Input, int Count)>();

        Parallel.ForEach(items, item =>
        {
            var child = engine.CreateChild();
            var data = Enumerable.Range(1, item).Select(n => (long)n).ToList();
            child.SetVariable("data", data);
            var result = child.Evaluate("data.Where(x => x > threshold).Count()");
            results.Add((item, Convert.ToInt32(result)));
        });

        foreach (var (input, count) in results)
        {
            var expected = Enumerable.Range(1, input).Count(n => n > 5);
            Assert.That(count, Is.EqualTo(expected), $"Input {input} filter count mismatch");
        }
    }

    [Test]
    public void ParallelForEach_WithPreParsedExpression()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("factor", 3L);

        var expression = engine.Parse("val * factor + offset");
        var items = Enumerable.Range(1, 100).ToList();
        var results = new ConcurrentBag<(int Val, int Offset, long Result)>();

        Parallel.ForEach(items, item =>
        {
            var child = engine.CreateChild();
            child.SetVariable("val", (long)item);
            child.SetVariable("offset", (long)(item % 10));
            var result = child.Evaluate(expression);
            results.Add((item, item % 10, (long)result!));
        });

        foreach (var (val, offset, result) in results)
        {
            var expected = val * 3L + offset;
            Assert.That(result, Is.EqualTo(expected), $"Val={val}, Offset={offset}");
        }
    }

    [Test]
    public void ParallelForEach_StressTest()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("baseVal", 1000L);

        var items = Enumerable.Range(1, 1000).ToList();
        var results = new ConcurrentBag<long>();

        Parallel.ForEach(items, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, item =>
        {
            var child = engine.CreateChild();
            child.SetVariable("n", (long)item);
            var result = child.Evaluate("baseVal + n * n");
            results.Add((long)result!);
        });

        Assert.That(results.Count, Is.EqualTo(1000));

        var expected = items.Select(i => 1000L + (long)i * i).OrderBy(x => x).ToList();
        var actual = results.OrderBy(x => x).ToList();
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ParallelForEach_WithStringInterpolation()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("prefix", "Item");

        var items = Enumerable.Range(1, 50).ToList();
        var results = new ConcurrentBag<(int Index, string Result)>();

        Parallel.ForEach(items, item =>
        {
            var child = engine.CreateChild();
            child.SetVariable("index", (long)item);
            var result = child.Evaluate("$\"{prefix}-{index}\"");
            results.Add((item, (string)result!));
        });

        foreach (var (index, result) in results)
        {
            Assert.That(result, Is.EqualTo($"Item-{index}"));
        }
    }

    [Test]
    public void ParallelForEach_WithAnonymousObjects()
    {
        var engine = TestEngineFactory.Create(mode);

        var items = Enumerable.Range(1, 50).ToList();
        var results = new ConcurrentBag<(int Input, IDictionary<string, object?> Result)>();

        Parallel.ForEach(items, item =>
        {
            var child = engine.CreateChild();
            child.SetVariable("id", (long)item);
            child.SetVariable("name", $"Item{item}");
            var result = child.Evaluate("new { Id = id, Name = name, Doubled = id * 2 }");
            results.Add((item, (IDictionary<string, object?>)result!));
        });

        foreach (var (input, result) in results)
        {
            Assert.That(result["Id"], Is.EqualTo((long)input));
            Assert.That(result["Name"], Is.EqualTo($"Item{input}"));
            Assert.That(result["Doubled"], Is.EqualTo((long)input * 2));
        }
    }

    [Test]
    public void ParallelForEach_ModuleAccessInChild()
    {
        var engine = TestEngineFactory.Create(mode);

        var items = Enumerable.Range(-50, 100).ToList();
        var results = new ConcurrentBag<(int Input, double Result)>();

        Parallel.ForEach(items, item =>
        {
            var child = engine.CreateChild();
            child.SetVariable("val", (double)item);
            var result = child.Evaluate("Math.Abs(val)");
            results.Add((item, (double)result!));
        });

        foreach (var (input, result) in results)
        {
            Assert.That(result, Is.EqualTo(Math.Abs(input)));
        }
    }

    [Test]
    public void ConcurrentSetVariable_DoesNotThrow()
    {
        var engine = TestEngineFactory.Create(mode);

        Assert.DoesNotThrow(() =>
        {
            Parallel.For(0, 200, i =>
            {
                engine.SetVariable($"var_{i}", (long)i);
            });
        });
    }

    [Test]
    public void ConcurrentEvaluate_AfterConfiguration_DoesNotThrow()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("baseVal", 100L);

        // Force context creation
        engine.Evaluate("baseVal");

        var results = new ConcurrentBag<long>();
        Assert.DoesNotThrow(() =>
        {
            Parallel.For(0, 200, i =>
            {
                var child = engine.CreateChild();
                child.SetVariable("n", (long)i);
                var result = child.Evaluate("baseVal + n");
                results.Add((long)result!);
            });
        });

        Assert.That(results.Count, Is.EqualTo(200));
    }
}
