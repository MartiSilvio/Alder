using Alder.Diagnostics;

namespace Alder.Test.Diagnostics;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class MultiErrorTests
{
    [Test]
    public void TryValidate_TwoUnknownIdentifiers_ReturnsBothErrors()
    {
        var engine = new AlderEngine(AlderOptions.Default);

        var success = engine.TryValidate("unknownA + unknownB", out var diagnostics);

        Assert.That(success, Is.False);
        Assert.That(diagnostics.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(diagnostics.Any(d => d.Message.Contains("unknownA")), Is.True);
        Assert.That(diagnostics.Any(d => d.Message.Contains("unknownB")), Is.True);
    }

    [Test]
    public void TryValidate_ThreeErrors_ReportsAll()
    {
        var engine = new AlderEngine(AlderOptions.Default);

        var success = engine.TryValidate("a + b + c", out var diagnostics);

        Assert.That(success, Is.False);
        Assert.That(diagnostics.Count, Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void TryValidate_MixedValidAndInvalid_ReportsOnlyErrors()
    {
        var engine = new AlderEngine(AlderOptions.Default);
        engine.SetVariable("x", 1);

        var success = engine.TryValidate("x + unknownVar", out var diagnostics);

        Assert.That(success, Is.False);
        Assert.That(diagnostics.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(diagnostics.Any(d => d.Message.Contains("unknownVar")), Is.True);
    }

    [Test]
    public void TryValidate_ValidExpression_ReturnsNoDiagnostics()
    {
        var engine = new AlderEngine(AlderOptions.Default);
        engine.SetVariable("x", 1);
        engine.SetVariable("y", 2);

        var success = engine.TryValidate("x + y", out var diagnostics);

        Assert.That(success, Is.True);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void TryValidate_ErrorsHaveDiagnosticCodes()
    {
        var engine = new AlderEngine(AlderOptions.Default);

        var success = engine.TryValidate("unknownA + unknownB", out var diagnostics);

        Assert.That(success, Is.False);
        foreach (var d in diagnostics)
            Assert.That(d.Code, Is.Not.Null);
    }

    [Test]
    public void Evaluate_StillThrowsFirstError()
    {
        var engine = new AlderEngine(AlderOptions.Default);

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("unknownA + unknownB"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
    }
}
