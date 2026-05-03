using System.Collections.Concurrent;
using Alder.Test._Infrastructure;

namespace Alder.Test.Stress;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ConcurrencyHammerTests(CompilationMode mode)
{
    [Test]
    public void ParallelChildren_ShouldBeSafe_IfParentIsReadOnly()
    {
        var engine = TestEngineFactory.Create(mode);
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
        var engine = TestEngineFactory.Create(mode);
        const string expr = "1 + 1";

        Parallel.For(0, 10000, i =>
        {
            engine.Evaluate(expr);
        });
    }

    [Test]
    public void ParallelParsing_DifferentExpressions_ShouldStressCacheWraps()
    {
        var engine = TestEngineFactory.Create(mode);

        Parallel.For(0, 5000, i =>
        {
            engine.Evaluate($"{i} + {i}");
        });
    }

    [Test]
    public void ConcurrentRecursiveEvaluation_ShouldNotDeadlock()
    {
        var engine = TestEngineFactory.Create(mode);
        var expr = "1 + 1";

        Parallel.For(0, 100, i =>
        {
            var child = engine.CreateChild();
            var grandChild = child.CreateChild();
            grandChild.Evaluate(expr);
        });
    }
}
