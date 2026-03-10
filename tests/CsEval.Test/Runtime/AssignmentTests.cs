// Engine-only: CsEval-specific syntax ([1,2,3] collection expressions, mutable anonymous objects),
// SetVariable API, RegisterExtensionMethods, pre-parsed engine reuse, error assertions.
// Migratable parity tests extracted to TestData/Runtime/Assignment/*.csx.

namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class AssignmentTests(CompilationMode mode)
{
    #region CsEval-Specific Syntax (Engine-Only)

    // Engine-only: mutable anonymous objects as IDictionary
    [Test]
    public void Assignment_AnonymousObject_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var obj = new { Name = ""John"" };
            obj = new { Name = ""Jane"", Age = 30 };
            return obj;
        }") as IDictionary<string, object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("Jane"));
        Assert.That(result["Age"], Is.EqualTo(30));
    }

    #endregion

    #region SetVariable API (Engine-Only)

    // Engine-only: SetVariable with long type + int literal reassignment (CsEval returns int, Roslyn returns long)
    [Test]
    public void Assignment_ToExternalVariable_UpdatesValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10L);

        var result = engine.Evaluate(@"
        {
            x = 50;
            return x;
        }");

        Assert.That(result, Is.EqualTo(50));
    }

    // Engine-only: SetVariable with non-serializable List<int> type
    [Test]
    public void Assignment_WithLinqResult_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate(@"
        {
            var filtered = numbers.Where(x => x > 2).ToList();
            return filtered;
        }");

        Assert.That(result, Is.InstanceOf<IList>());
        Assert.That(result, Is.EqualTo(new List<int> { 3, 4, 5 }));
    }

    #endregion

    #region Error Cases (Engine-Only)

    // Engine-only: CsEvalException assertion
    [Test]
    public void Assignment_ToUndefinedVariable_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                undefinedVar = 10;
                return undefinedVar;
            }"));
    }

    #endregion

    #region Pre-Parsed Assignment (Engine-Only)

    // Engine-only: engine.Parse() + SetVariable reuse pattern
    [Test]
    public void Assignment_PreParsed_CanBeReused()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var expr = engine.Parse(@"
        {
            var x = startVal;
            x = x * 2;
            return x;
        }");

        engine.SetVariable("startVal", 5L);
        var result1 = engine.Evaluate(expr);
        Assert.That(result1, Is.EqualTo(10));

        engine.SetVariable("startVal", 100L);
        var result2 = engine.Evaluate(expr);
        Assert.That(result2, Is.EqualTo(200));
    }

    #endregion
}
