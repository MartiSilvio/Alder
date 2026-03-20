using CsEval.Test._Infrastructure;

namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public sealed class InvocationPipelineTests(CompilationMode mode)
{
    [Test]
    public void ParamsArrayExpansion_UsesAllPositionalArguments()
    {
        var engine = CreateEngine();
        var target = new InvocationTarget();
        engine.SetVariable("target", target);
        engine.SetVariable("x", 1);
        engine.SetVariable("y", 2);
        engine.SetVariable("z", 3);
        engine.SetVariable("value", 4);

        var result = engine.Evaluate("target.Sum(x, y, z, value)");

        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void OptionalArgument_UsesDefault_WhenNotSupplied()
    {
        var engine = CreateEngine();
        var target = new InvocationTarget();
        engine.SetVariable("target", target);
        engine.SetVariable("x", 7);

        var result = engine.Evaluate("target.WithOptional(x)");

        Assert.That(result, Is.EqualTo(17));
    }

    [Test]
    public void OptionalArgument_UsesExplicitValue_WhenSupplied()
    {
        var engine = CreateEngine();
        var target = new InvocationTarget();
        engine.SetVariable("target", target);
        engine.SetVariable("x", 7);
        engine.SetVariable("y", 2);

        var result = engine.Evaluate("target.WithOptional(x, y)");

        Assert.That(result, Is.EqualTo(9));
    }

    [Test]
    public void NestedInvocationArguments_DoNotOverwriteSiblingArguments()
    {
        var engine = CreateEngine();
        var target = new InvocationTarget();
        engine.SetVariable("target", target);

        var result = engine.Evaluate("target.Combine(target.One(), target.Two())");

        Assert.That(result, Is.EqualTo(12));
    }

    private CsEvalEngine CreateEngine()
    {
        return TestEngineFactory.Create(mode);
    }

    private sealed class InvocationTarget
    {
        public int Sum(params int[] values)
        {
            var total = 0;
            for (var i = 0; i < values.Length; i++)
                total += values[i];
            return total;
        }

        public int WithOptional(int value, int extra = 10) => value + extra;

        public int One() => 1;

        public int Two() => 2;

        public int Combine(int left, int right) => left * 10 + right;
    }
}
