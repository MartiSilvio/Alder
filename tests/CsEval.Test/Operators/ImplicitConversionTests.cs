using CsEval.Test._Infrastructure;

namespace CsEval.Test.Operators;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ImplicitConversionTests(CompilationMode mode)
{
    [Test]
    public void UserDefinedImplicitConversion_InMethodBinding_Works()
    {
        var engine = TestEngineFactory.Create(mode);
        var consumer = new ImplicitConsumer();
        engine.SetVariable("consumer", consumer);
        engine.SetVariable("from", new ImplicitFrom(7));

        var result = engine.Evaluate("consumer.Accept(from)");
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void UserDefinedImplicitConversion_MultipleArgs_Works()
    {
        var engine = TestEngineFactory.Create(mode);
        var consumer = new ImplicitConsumer();
        engine.SetVariable("consumer", consumer);
        engine.SetVariable("a", new ImplicitFrom(3));
        engine.SetVariable("b", new ImplicitFrom(5));

        var result = engine.Evaluate("consumer.AcceptSum(a, b)");
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void UserDefinedImplicitConversion_PreferExactMatchOverConversion()
    {
        var engine = TestEngineFactory.Create(mode);
        var consumer = new ImplicitConsumer();
        engine.SetVariable("consumer", consumer);
        engine.SetVariable("to", new ImplicitTo(99));

        var result = engine.Evaluate("consumer.Accept(to)");
        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public void ExtensionMethod_SignedPreferredOverUnsigned()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.RegisterExtensionMethods(typeof(ImplicitConversionExtensionProbe));

        var result = engine.Evaluate("1.ExtAmb((byte)1)");
        Assert.That(result, Is.EqualTo("short"));
    }

    public sealed class ImplicitFrom
    {
        public ImplicitFrom(int value) => Value = value;
        public int Value { get; }
        public static implicit operator ImplicitTo(ImplicitFrom input) => new(input.Value);
    }

    public sealed class ImplicitTo
    {
        public ImplicitTo(int value) => Value = value;
        public int Value { get; }
    }

    public sealed class ImplicitConsumer
    {
        public int Accept(ImplicitTo value) => value.Value;
        public int AcceptSum(ImplicitTo a, ImplicitTo b) => a.Value + b.Value;
    }
}

internal static class ImplicitConversionExtensionProbe
{
    public static string ExtAmb(this int value, short x) => "short";
    public static string ExtAmb(this int value, ushort x) => "ushort";
}
