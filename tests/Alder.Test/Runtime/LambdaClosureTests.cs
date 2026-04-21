using Alder.Test._Infrastructure;

namespace Alder.Test.Runtime;

[TestFixture]
public sealed class LambdaClosureTests
{
    [Test]
    public void InterpretedLambda_RebindsWhenCapturedZeroArgVariableTypeChanges()
    {
        using var engine = TestEngineFactory.Create(CompilationMode.Interpreted);
        engine.SetVariable<int>("captured", 3);

        var fn = engine.Evaluate<Func<object?>>("() => captured / 2");
        Assert.That(fn, Is.Not.Null);

        Assert.That(fn!(), Is.EqualTo(1));

        engine.SetVariable<double>("captured", 3.0);

        Assert.That(fn!(), Is.EqualTo(1.5));
    }

    [Test]
    public void InterpretedLambda_RebindsWhenCapturedVariableTypeChangesWithStableArgumentTypes()
    {
        using var engine = TestEngineFactory.Create(CompilationMode.Interpreted);
        engine.SetVariable<int>("captured", 2);

        var fn = engine.Evaluate<Func<int, object?>>("x => x / captured");
        Assert.That(fn, Is.Not.Null);

        Assert.That(fn!(9), Is.EqualTo(4));

        engine.SetVariable<double>("captured", 2.0);

        Assert.That(fn!(9), Is.EqualTo(4.5));
    }
}
