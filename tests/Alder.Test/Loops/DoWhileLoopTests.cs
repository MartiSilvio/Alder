using Alder.Test._Infrastructure;

namespace Alder.Test.Loops;

// Engine-only: this file keeps only do-while-specific behavior that still adds signal
// beyond the shared limit/cancellation/parsing API suites.

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class DoWhileLoopTests(CompilationMode mode)
{
    // Engine-only: SetVariable
    [Test]
    public void DoWhileLoop_WithExternalVariable_ModifiesCorrectly()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("limit", 10);

        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 1;
            do {
                sum = sum + i;
                i = i + 1;
            } while (i <= limit);
            return sum;
        }");

        Assert.That(result, Is.EqualTo(55));
    }

    // Engine-only: SetVariable
    [Test]
    public void DoWhileLoop_WithTernaryCondition_WorksCorrectly()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("useShort", true);

        var result = engine.Evaluate(@"
        {
            var count = 0;
            var i = 0;
            do {
                count = count + 1;
                i = i + 1;
            } while (i < (useShort ? 3 : 10));
            return count;
        }");

        Assert.That(result, Is.EqualTo(3));
    }

    // Engine-only: SetVariable
    [Test]
    public void DoWhileLoop_WithConditionalReturn_ReturnsCorrectValue()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("target", 7L);

        var result = engine.Evaluate(@"
        {
            var i = 0;
            do {
                if (i == target) {
                    return $""Found at {i}"";
                }
                i = i + 1;
            } while (i < 20);
            return ""Not found"";
        }");

        Assert.That(result, Is.EqualTo("Found at 7"));
    }



    // Engine-only: SetVariable with List<int>
    [Test]
    public void DoWhileLoop_WithListCount_WorksCorrectly()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<int> { 10, 20, 30, 40 });

        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 0;
            do {
                sum = sum + items[i];
                i = i + 1;
            } while (i < items.Count());
            return sum;
        }");

        Assert.That(result, Is.EqualTo(100));
    }

    // Engine-only: structural object projections
    [Test]
    public void DoWhileLoop_BuildingObjects_WorksCorrectly()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate(@"
        {
            var i = 0;
            object lastObj = null;
            do {
                lastObj = new { Index = i, Squared = i * i };
                i = i + 1;
            } while (i < 3);
            return lastObj;
        }");

        Assert.That(result, Is.Not.Null);
        Assert.That(TestHelpers.ReadProjectedMember(result, "Index"), Is.EqualTo(2));
        Assert.That(TestHelpers.ReadProjectedMember(result, "Squared"), Is.EqualTo(4));
    }
}
