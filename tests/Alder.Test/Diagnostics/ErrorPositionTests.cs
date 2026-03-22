using Alder.Diagnostics;

namespace Alder.Test.Diagnostics;

/// <summary>
/// Verifies that errors carry correct source positions (line, column, span).
/// Interpreted mode enriches positions at the evaluator frame boundary.
/// Compiled mode enriches positions for binding errors; runtime errors in compiled
/// delegates do not yet carry positions (the compiled IL has no frame boundary).
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ErrorPositionTests
{
    private static AlderEngine Interpreted() => new(new AlderOptions());

    [Test]
    public void UndefinedVariable_ReportsCorrectPosition()
    {
        var ex = Assert.Catch<AlderException>(() => Interpreted().Evaluate("1 + xyz"));
        Assert.That(ex!.Line, Is.EqualTo(1));
        Assert.That(ex.Column, Is.EqualTo(5));
        Assert.That(ex.Span.Start, Is.EqualTo(4));
        Assert.That(ex.Span.Length, Is.EqualTo(3));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
    }

    [Test]
    public void UndefinedVariable_AtStart_ReportsColumn1()
    {
        var ex = Assert.Catch<AlderException>(() => Interpreted().Evaluate("xyz + 1"));
        Assert.That(ex!.Line, Is.EqualTo(1));
        Assert.That(ex.Column, Is.EqualTo(1));
        Assert.That(ex.Span.Start, Is.EqualTo(0));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
    }

    [Test]
    public void InvalidOperator_ReportsExpressionPosition()
    {
        var ex = Assert.Catch<AlderException>(() => Interpreted().Evaluate("1 + true"));
        Assert.That(ex!.Line, Is.EqualTo(1));
        Assert.That(ex.Column, Is.EqualTo(1));
        Assert.That(ex.Span.Start, Is.EqualTo(0));
        Assert.That(ex.Span.Length, Is.EqualTo(8));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
    }

    [Test]
    public void Assignment_ToUndefined_ReportsNamePosition()
    {
        var ex = Assert.Catch<AlderException>(() => Interpreted().Evaluate("xyz = 5"));
        Assert.That(ex!.Line, Is.EqualTo(1));
        Assert.That(ex.Column, Is.EqualTo(1));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
    }

    [Test]
    public void MultiLine_ReportsCorrectLine()
    {
        var expr = "{ var x = 1;\n var y = unknownVar; }";
        var ex = Assert.Catch<AlderException>(() => Interpreted().Evaluate(expr));
        Assert.That(ex!.Line, Is.EqualTo(2));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
    }

    [Test]
    public void NestedExpression_ReportsInnerPosition()
    {
        var ex = Assert.Catch<AlderException>(() => Interpreted().Evaluate("1 + (2 * xyz)"));
        Assert.That(ex!.Line, Is.EqualTo(1));
        Assert.That(ex.Column, Is.EqualTo(10));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
    }

    [Test]
    public void MemberAccess_OnUndefined_ReportsPosition()
    {
        var ex = Assert.Catch<AlderException>(() => Interpreted().Evaluate("xyz.Length"));
        Assert.That(ex!.Line, Is.EqualTo(1));
        Assert.That(ex.Column, Is.EqualTo(1));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
    }

    [Test]
    public void BindingError_TypeResolution_ReportsPosition_Interpreted()
    {
        var ex = Assert.Catch<AlderException>(() => Interpreted().Evaluate("(NonExistentType)42"));
        Assert.That(ex!.Line, Is.EqualTo(1));
        Assert.That(ex.Column, Is.Not.Null);
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0246));
    }

    [Test]
    public void BindingError_TypeResolution_ReportsPosition_Compiled()
    {
        var engine = new AlderEngine(new AlderOptions().UseCompiler());
        var ex = Assert.Catch<AlderException>(() => engine.Evaluate("(NonExistentType)42"));
        Assert.That(ex!.Line, Is.EqualTo(1));
        Assert.That(ex.Column, Is.Not.Null);
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0246));
    }
}
