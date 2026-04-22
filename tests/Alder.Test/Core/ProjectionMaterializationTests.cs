using Alder;
using Alder.Diagnostics;
using Alder.Test.Compilation;
using Alder.Test._Infrastructure;

namespace Alder.Test.Core;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ProjectionMaterializationTests(CompilationMode mode)
{
    private sealed class OptionalParameterPreferenceDto
    {
        public string Name { get; }
        public int ConstructorArity { get; }

        public OptionalParameterPreferenceDto(string name)
        {
            Name = name;
            ConstructorArity = 1;
        }

        public OptionalParameterPreferenceDto(string name, int count = 0)
        {
            Name = name;
            ConstructorArity = 2;
        }
    }

    private sealed class ConvertibilityAwareConstructorDto
    {
        public string Name { get; }
        public int Count { get; }
        public string SelectedConstructor { get; }

        public ConvertibilityAwareConstructorDto(string name, int count)
        {
            Name = name;
            Count = count;
            SelectedConstructor = "int";
        }

        public ConvertibilityAwareConstructorDto(string name, Guid count)
        {
            Name = name;
            Count = -1;
            SelectedConstructor = "guid";
        }
    }

    private sealed class AmbiguousProjectionDto
    {
        public string Name { get; }

        public AmbiguousProjectionDto(string name, int count = 0)
        {
            Name = name;
        }

        public AmbiguousProjectionDto(string name, decimal price = 0m)
        {
            Name = name;
        }
    }

    private sealed class UnmatchedProjectionDto
    {
        public string Name { get; set; } = "default";
    }

    private sealed class NullableValueProjectionDto
    {
        public decimal? Price { get; set; }
    }

    [Test]
    public void Materialize_RecordTarget_FromStructuralProjection()
    {
        var engine = TestEngineFactory.Create(mode);
        var value = engine.Evaluate("""new { Name = "Widget", Price = 9.99m }""");

        var result = AlderProjectionMaterializer.Materialize<ProductSummaryRecord>(value);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.name, Is.EqualTo("Widget"));
        Assert.That(result.price, Is.EqualTo(9.99m));
    }

    [Test]
    public void Materialize_ClassTarget_FromStructuralProjection()
    {
        var engine = TestEngineFactory.Create(mode);
        var value = engine.Evaluate("""new { Name = "Widget", Price = 9.99m }""");

        var result = AlderProjectionMaterializer.Materialize<ProductSummaryDto>(value);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Widget"));
        Assert.That(result.Price, Is.EqualTo(9.99m));
    }

    [Test]
    public void Evaluate_RecordTarget_FromStructuralProjection()
    {
        var engine = TestEngineFactory.Create(mode);

        var result = engine.Evaluate<ProductSummaryRecord>("""new { Name = "Widget", Price = 9.99m }""");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.name, Is.EqualTo("Widget"));
        Assert.That(result.price, Is.EqualTo(9.99m));
    }

    [Test]
    public async Task Evaluate_RecordTarget_FromStructuralProjection_MatchesRoslynAnonymousProjectionStructurally()
    {
        var engine = TestEngineFactory.Create(mode);

        var result = engine.Evaluate<ProductSummaryRecord>("""new { Name = "Widget", Price = 9.99m }""");
        var roslyn = await TestHelpers.EvaluateCSharpAsync("""new { Name = "Widget", Price = 9.99m }""");

        Assert.That(result, Is.Not.Null);
        Assert.That(roslyn, Is.Not.Null);
        Assert.That(result!.name, Is.EqualTo(TestHelpers.ReadProjectedMember(roslyn, "Name")));
        Assert.That(result.price, Is.EqualTo(TestHelpers.ReadProjectedMember(roslyn, "Price")));
    }

    [Test]
    public void Evaluate_ClassTarget_FromStructuralProjection()
    {
        var engine = TestEngineFactory.Create(mode);

        var result = engine.Evaluate<ProductSummaryDto>("""new { Name = "Widget", Price = 9.99m }""");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Widget"));
        Assert.That(result.Price, Is.EqualTo(9.99m));
    }

    [Test]
    public void Evaluate_ClassTarget_FromStructuralProjection_BindsCaseInsensitively()
    {
        var engine = TestEngineFactory.Create(mode);

        var result = engine.Evaluate<ProductSummaryDto>("""new { name = "Widget", price = 9.99m }""");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Widget"));
        Assert.That(result.Price, Is.EqualTo(9.99m));
    }

    [Test]
    public async Task Evaluate_ClassTarget_FromStructuralProjection_MatchesRoslynAnonymousProjectionStructurally()
    {
        var engine = TestEngineFactory.Create(mode);

        var result = engine.Evaluate<ProductSummaryDto>("""new { Name = "Widget", Price = 9.99m }""");
        var roslyn = await TestHelpers.EvaluateCSharpAsync("""new { Name = "Widget", Price = 9.99m }""");

        Assert.That(result, Is.Not.Null);
        Assert.That(roslyn, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo(TestHelpers.ReadProjectedMember(roslyn, "Name")));
        Assert.That(result.Price, Is.EqualTo(TestHelpers.ReadProjectedMember(roslyn, "Price")));
    }

    [Test]
    public void TryEvaluate_ClassTarget_FromStructuralProjection_ReturnsTrue()
    {
        var engine = TestEngineFactory.Create(mode);

        var success = engine.TryEvaluate<ProductSummaryDto>(
            """new { Name = "Widget", Price = 9.99m }""",
            out var result);

        Assert.That(success, Is.True);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Widget"));
        Assert.That(result.Price, Is.EqualTo(9.99m));
    }

    [Test]
    public void Materialize_NestedClassTarget_FromStructuralProjection()
    {
        var engine = TestEngineFactory.Create(mode);
        var value = engine.Evaluate("""new { Product = new { Name = "Widget", Price = 9.99m } }""");

        var result = AlderProjectionMaterializer.Materialize<ProductEnvelopeDto>(value);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Product.Name, Is.EqualTo("Widget"));
        Assert.That(result.Product.Price, Is.EqualTo(9.99m));
    }

    [Test]
    public void Evaluate_NestedClassTarget_FromStructuralProjection()
    {
        var engine = TestEngineFactory.Create(mode);

        var result = engine.Evaluate<ProductEnvelopeDto>(
            """new { Product = new { Name = "Widget", Price = 9.99m } }""");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Product.Name, Is.EqualTo("Widget"));
        Assert.That(result.Product.Price, Is.EqualTo(9.99m));
    }

    [Test]
    public void Materialize_MissingRequiredMember_ThrowsProjectionMaterializationFailure()
    {
        var engine = TestEngineFactory.Create(mode);
        var value = engine.Evaluate("""new { Name = "Widget" }""");

        var ex = Assert.Throws<AlderException>(() =>
            AlderProjectionMaterializer.Materialize<ProductSummaryRecord>(value));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0406));
    }

    [Test]
    public void Evaluate_MissingRequiredMember_ThrowsProjectionMaterializationFailure()
    {
        var engine = TestEngineFactory.Create(mode);

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate<ProductSummaryRecord>("""new { Name = "Widget" }"""));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0406));
    }

    [Test]
    public void TryEvaluate_MissingRequiredMember_ReturnsFalse()
    {
        var engine = TestEngineFactory.Create(mode);

        var success = engine.TryEvaluate<ProductSummaryRecord>(
            """new { Name = "Widget" }""",
            out var result);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Evaluate_IncompatibleMemberType_ThrowsProjectionMaterializationFailure()
    {
        var engine = TestEngineFactory.Create(mode);

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate<ProductSummaryDto>("""new { Name = 123, Price = "oops" }"""));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0406));
    }

    [Test]
    public void Evaluate_NullToNonNullableValueMember_ThrowsProjectionMaterializationFailure()
    {
        var engine = TestEngineFactory.Create(mode);

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate<ProductSummaryDto>("""new { Name = "Widget", Price = (decimal?)null }"""));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0406));
    }

    [Test]
    public void Evaluate_NullToNullableValueMember_Succeeds()
    {
        var engine = TestEngineFactory.Create(mode);

        var result = engine.Evaluate<NullableValueProjectionDto>("""new { Price = (decimal?)null }""");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Price, Is.Null);
    }

    [Test]
    public void Evaluate_AmbiguousConstructor_ThrowsProjectionMaterializationFailure()
    {
        var engine = TestEngineFactory.Create(mode);

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate<AmbiguousProjectionDto>("""new { Name = "Widget" }"""));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0406));
    }

    [Test]
    public void Evaluate_PrefersConstructorThatDoesNotNeedOptionalDefaults()
    {
        var engine = TestEngineFactory.Create(mode);

        var result = engine.Evaluate<OptionalParameterPreferenceDto>("""new { Name = "Widget" }""");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Widget"));
        Assert.That(result.ConstructorArity, Is.EqualTo(1));
    }

    [Test]
    public void Evaluate_PrefersConstructorWithConvertibleParameterTypes()
    {
        var engine = TestEngineFactory.Create(mode);

        var result = engine.Evaluate<ConvertibilityAwareConstructorDto>(
            """new { Name = "Widget", Count = 5 }""");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Widget"));
        Assert.That(result.Count, Is.EqualTo(5));
        Assert.That(result.SelectedConstructor, Is.EqualTo("int"));
    }

    [Test]
    public void Evaluate_WhenProjectionBindsNoMembers_ThrowsProjectionMaterializationFailure()
    {
        var engine = TestEngineFactory.Create(mode);

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate<UnmatchedProjectionDto>("""new { Price = 9.99m }"""));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0406));
    }

    [Test]
    public void Evaluate_UnsupportedTarget_ThrowsProjectionMaterializationFailure()
    {
        var engine = TestEngineFactory.Create(mode);

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate<List<string>>("""new { Name = "Widget", Price = 9.99m }"""));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0406));
    }

    [Test]
    public void Evaluate_ScalarIntConversion_Unchanged()
    {
        var engine = TestEngineFactory.Create(mode);

        var result = engine.Evaluate<int>("1 + 2");

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Evaluate_DelegateConversion_Unchanged()
    {
        var engine = TestEngineFactory.Create(mode);

        var result = engine.Evaluate<Func<int, int>>("x => x + 1");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!(41), Is.EqualTo(42));
    }

    [Test]
    public void ExplicitMaterialize_ReusesSameMappingRules()
    {
        var value = TestEngineFactory.Create(mode).Evaluate("""new { Name = "Widget", Price = 9.99m }""");

        var result = AlderProjectionMaterializer.Materialize<ProductSummaryDto>(value);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Widget"));
        Assert.That(result.Price, Is.EqualTo(9.99m));
    }
}
