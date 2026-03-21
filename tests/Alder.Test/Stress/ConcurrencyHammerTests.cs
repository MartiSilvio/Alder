using System.Collections.Concurrent;
using Alder.Test._Infrastructure;

namespace Alder.Test.Stress;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ConcurrencyHammerTests(CompilationMode mode)
{
    private AlderEngine CreateEngine() => TestEngineFactory.Create(mode);

    [Test]
    public void ParallelChildren_ShouldBeSafe_IfParentIsReadOnly()
    {
        var engine = CreateEngine();
        engine.SetVariable("globalConfig", 123);

        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 };
        var exceptions = new ConcurrentBag<Exception>();

        Parallel.For(0, 1000, parallelOptions, i =>
        {
            try
            {
                var child = engine.CreateChild();
                child.SetVariable("local", i);
                var result = child.Evaluate("globalConfig + local");
                Assert.That(result, Is.EqualTo(123 + i));
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.That(exceptions, Is.Empty);
    }

    [Test]
    public void ParallelParsing_SameExpression_ShouldHitCacheSafe()
    {
        var engine = CreateEngine();
        const string expr = "1 + 1";

        Parallel.For(0, 10000, i =>
        {
            engine.Evaluate(expr);
        });
    }

    [Test]
    public void ParallelParsing_DifferentExpressions_ShouldStressCacheWraps()
    {
        var engine = CreateEngine();

        Parallel.For(0, 5000, i =>
        {
            engine.Evaluate($"{i} + {i}");
        });
    }

    [Test]
    [Explicit("Demonstrates expected unsafe behavior - Modifying parent while children read is NOT thread safe")]
    public void ModifyingParent_WhileChildrenRead_ShouldCrashOrCorrupt()
    {
        var engine = CreateEngine();

        var running = true;

        var writerTask = Task.Run(() =>
        {
            var i = 0;
            while (running)
            {
                engine.RegisterFunction($"func{i}", args => i);
                i++;
                if (i % 100 == 0) Thread.Sleep(1);
            }
        });

        var readerTask = Task.Run(() =>
        {
            Parallel.For(0, 1000, i =>
            {
                try
                {
                    var child = engine.CreateChild();
                    child.Evaluate("1+1");
                }
                catch
                {
                    // Ignore transient errors, we want to see if process crashes or hangs
                }
            });
        });

        readerTask.Wait(2000);
        running = false;
        writerTask.Wait(1000);
    }

    [Test]
    public void ConcurrentRecursiveEvaluation_ShouldNotDeadlock()
    {
        var engine = CreateEngine();
        var expr = "1 + 1";

        Parallel.For(0, 100, i =>
        {
            var child = engine.CreateChild();
            var grandChild = child.CreateChild();
            grandChild.Evaluate(expr);
        });
    }
}
