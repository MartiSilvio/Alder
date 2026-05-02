using Alder.Test._Infrastructure;
using Billing;

namespace Alder.Test.Docs;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class TypeRegistrationDocTests(CompilationMode mode)
{
    [Test]
    public void TypeRegistration_AddAssembly_ResolvesQualifiedType()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Types.AddAssembly(typeof(Money).Assembly);
        });

        var value = engine.Evaluate<decimal>(
            "Billing.Money.FromDollars(125m).Amount");

        Assert.That(value, Is.EqualTo(125m));
    }

    [Test]
    public void TypeRegistration_AddNamespace_ResolvesUnqualifiedType()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Types.AddAssembly(typeof(Money).Assembly);
            options.Types.AddNamespace("Billing");
        });

        var value = engine.Evaluate<decimal>(
            "Money.FromDollars(125m).Amount");

        Assert.That(value, Is.EqualTo(125m));
    }

    [Test]
    public void TypeRegistration_AddExtensionMethods_ResolvesExtensionMethod()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Types.AddExtensionMethods(typeof(MoneyExtensions));
        });

        var accepted = engine.Evaluate<bool>(
            "money.IsHighValue(100m)",
            new { money = new Money(125m) });

        Assert.That(accepted, Is.True);
    }
}
