using System.Security.Cryptography;
using System.Text;
using Alder.Test._Infrastructure;
using Billing;

namespace Alder.Test.Docs;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
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

    [Test]
    public void TypeRegistration_FunctionAlternative_ExposesNarrowOperation()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Functions.Register("isHighValue", args =>
            {
                var money = (Money)args[0]!;
                var threshold = Convert.ToDecimal(args[1]);
                return money.Amount >= threshold;
            });
        });

        var accepted = engine.Evaluate<bool>(
            "isHighValue(money, 100m)",
            new { money = new Money(125m) });

        Assert.That(accepted, Is.True);
    }

    [Test]
    public void TypeRegistration_ModuleAlternative_ExposesGroupedOperation()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Modules.Register<DocPricingRules>("pricing");
        });

        var accepted = engine.Evaluate<bool>(
            "pricing.IsHighValue(money, 100m)",
            new { money = new Money(125m) });

        Assert.That(accepted, Is.True);
    }

    [Test]
    public async Task RootReadme_HostIntegration_ConfiguresModulesFunctionsTypesAndExtensions()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Modules.Register<DocTaxModule>("tax");
            options.Functions.Register("hash", args => Sha256((string)args[0]!));
            options.Types.AddAssembly(typeof(Money).Assembly);
            options.Types.AddNamespace("Billing");
            options.Types.AddExtensionMethods(typeof(MoneyExtensions));
        });
        var cart = new Cart { Subtotal = 120m, Discount = 20m, PostalCode = "94107" };

        var total = await engine.EvaluateAsync<decimal>(
            """
            var discounted = cart.Subtotal - cart.Discount;
            var tax = await tax.CalculateAsync(cart.PostalCode, discounted);
            return discounted + tax;
            """,
            new { cart });

        Assert.That(total, Is.EqualTo(108m));
        Assert.That(engine.Evaluate<string>("""hash("cart")"""), Has.Length.EqualTo(64));
        Assert.That(engine.Evaluate<decimal>("Money.FromDollars(125m).Amount"), Is.EqualTo(125m));
        Assert.That(
            engine.Evaluate<bool>("money.IsHighValue(100m)", new { money = new Money(125m) }),
            Is.True);
    }

    private static string Sha256(string value)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    public sealed class DocPricingRules
    {
        public bool IsHighValue(Money money, decimal threshold) =>
            money.Amount >= threshold;
    }
}
