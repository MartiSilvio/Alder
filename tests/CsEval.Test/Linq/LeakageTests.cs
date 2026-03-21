namespace CsEval.Test.Linq;

// Engine-only: Tests lambda variable isolation internals - verifies CsEval's scoping implementation
// ensures no state leakage between lambda invocations across LINQ operations.

[TestFixture]
public class LeakageTests
{
    [Test]
    public void Lambda_VariablesDoNotLeakBetweenInvocations()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("numbers", new[] { 1, 2, 3 });

        var result = engine.Evaluate<List<int>>("numbers.Select(x => { var temp = x * 10; return temp + x; }).ToList()");

        Assert.That(result, Is.EqualTo(new[] { 11, 22, 33 }));
    }

    [Test]
    public void Lambda_LoopStateDoesNotLeakBetweenInvocations()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("data", new[] { 1, 2, 3 });

        var result = engine.Evaluate<List<int>>("data.Select(x => { var sum = 0; for (var i = 0; i < x; i++) sum += i; return sum; }).ToList()");

        Assert.That(result, Is.EqualTo(new[] { 0, 1, 3 }));
    }

    [Test]
    public void NestedLambdas_DoNotLeakState()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("matrix", new[] { new[] { 1, 2 }, new[] { 3, 4 } });

        // Use explicit cast in the outer lambda to avoid nested extension method type inference
        // (the binder can't resolve inner extension method return types statically)
        var result = engine.Evaluate<List<int>>("matrix.SelectMany(row => (IEnumerable<int>)row.Select(x => x * 2)).ToList()");

        Assert.That(result, Is.EqualTo(new[] { 2, 4, 6, 8 }));
    }

    [Test]
    public void Aggregate_AccumulatorIsolation()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("vals", new[] { 1, 2, 3, 4 });

        var result = engine.Evaluate<int>("vals.Aggregate(0, (acc, x) => acc + x)");

        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void ChainedLambdas_AreIndependent()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("items", new[] { 1, 2, 3, 4, 5, 6 });

        var result = engine.Evaluate<List<int>>("items.Where(x => x % 2 == 0).Select(x => x * x).ToList()");

        Assert.That(result, Is.EqualTo(new[] { 4, 16, 36 }));
    }

    [Test]
    public void LargeIteration_NoIterationCountLeakage()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("bigList", Enumerable.Range(1, 1000).ToArray());

        var result = engine.Evaluate<int>("bigList.Select(x => x * 2).Count()");

        Assert.That(result, Is.EqualTo(1000));
    }

    [Test]
    public void SequentialEvaluations_NoAccumulatedState()
    {
        var engine = new CsEvalEngine();

        for (int run = 0; run < 5; run++)
        {
            engine.SetVariable("seq", new[] { 1, 2, 3 });
            var result = engine.Evaluate<int>("seq.Select(x => x + 1).Sum()");
            Assert.That(result, Is.EqualTo(9), $"Failed on run {run + 1}");
        }
    }

    [Test]
    public void Lambda_WithBlockAndMultipleStatements_NoLeakage()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("nums", new[] { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate<List<int>>(@"
            nums.Select(n => {
                var doubled = n * 2;
                var squared = doubled * doubled;
                var result = squared + n;
                return result;
            }).ToList()
        ");

        Assert.That(result, Is.EqualTo(new[] { 5, 18, 39, 68, 105 }));
    }

    [Test]
    public void GroupBy_LambdaIsolation()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("items", new[] { 1, 2, 3, 4, 5, 6 });

        var result = engine.Evaluate<int>("items.GroupBy(x => x % 2).Count()");

        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void OrderBy_LambdaIsolation()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("items", new[] { 3, 1, 4, 1, 5, 9, 2, 6 });

        var result = engine.Evaluate<List<int>>("items.OrderBy(x => x).ToList()");

        Assert.That(result, Is.EqualTo(new[] { 1, 1, 2, 3, 4, 5, 6, 9 }));
    }

    [Test]
    public void Zip_TwoParameterLambdaIsolation()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("a", new[] { 1, 2, 3 });
        engine.SetVariable("b", new[] { 10, 20, 30 });

        var result = engine.Evaluate<List<int>>("a.Zip(b, (x, y) => x + y).ToList()");

        Assert.That(result, Is.EqualTo(new[] { 11, 22, 33 }));
    }

    [Test]
    public void All_PredicateIsolation()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("positives", new[] { 1, 2, 3, 4, 5 });
        engine.SetVariable("mixed", new[] { 1, -2, 3 });

        var allPositive1 = engine.Evaluate<bool>("positives.All(x => x > 0)");
        var allPositive2 = engine.Evaluate<bool>("mixed.All(x => x > 0)");

        Assert.That(allPositive1, Is.True);
        Assert.That(allPositive2, Is.False);
    }

    [Test]
    public void MultipleEngines_NoSharedState()
    {
        var engine1 = new CsEvalEngine();
        var engine2 = new CsEvalEngine();

        engine1.SetVariable("nums", new[] { 1, 2, 3 });
        engine2.SetVariable("nums", new[] { 10, 20, 30 });

        var result1 = engine1.Evaluate<int>("nums.Select(x => x * 2).Sum()");
        var result2 = engine2.Evaluate<int>("nums.Select(x => x * 2).Sum()");

        Assert.That(result1, Is.EqualTo(12));
        Assert.That(result2, Is.EqualTo(120));
    }
}
