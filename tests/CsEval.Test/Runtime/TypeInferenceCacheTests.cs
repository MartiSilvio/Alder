namespace CsEval.Test.Runtime;

[TestFixture]
public class TypeInferenceCacheTests
{
    [Test]
    public void Interpreted_TypeInferenceCache_Invalidates_WhenVariableStaticTypeChanges()
    {
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);
        var expression = engine.Parse("(long)x");

        engine.SetVariable<int>("x", 42);
        var first = engine.Evaluate(expression);
        Assert.That(first, Is.EqualTo(42L));

        // Static type becomes object, so explicit cast must follow object unboxing rules.
        engine.SetVariable<object>("x", 42);
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate(expression));
        Assert.That(ex!.ErrorCode, Is.EqualTo(CsEval.Diagnostics.DiagnosticCode.CS0030));
    }
}
