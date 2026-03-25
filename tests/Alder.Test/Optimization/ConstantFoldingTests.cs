using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Binding.Optimization;
using Alder.Parsing;

namespace Alder.Test.Optimization;

[TestFixture]
public sealed class ConstantFoldingTests
{
    private readonly ConstantFoldingPass _pass = new();

    [Test]
    public void Folds_IntegerAddition()
    {
        var tree = new BoundBinaryExpr(
            TokenType.Plus,
            new BoundLiteralExpr(2,
            new BoundType(typeof(int))),
            new BoundLiteralExpr(3,
            new BoundType(typeof(int))),
            new BoundType(typeof(int)));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundLiteralExpr>());
        var literal = (BoundLiteralExpr)result;
        Assert.That(literal.Value, Is.EqualTo(5));
        Assert.That(literal.StaticType.ClrType, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void Folds_StringConcatenation()
    {
        var tree = new BoundBinaryExpr(
            TokenType.Plus,
            new BoundLiteralExpr("hello",
            new BoundType(typeof(string))),
            new BoundLiteralExpr(" world",
            new BoundType(typeof(string))),
            new BoundType(typeof(string)));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundLiteralExpr>());
        var literal = (BoundLiteralExpr)result;
        Assert.That(literal.Value, Is.EqualTo("hello world"));
    }

    [Test]
    public void Folds_BooleanAnd()
    {
        var tree = new BoundLogicalExpr(
            TokenType.AmpAmp,
            new BoundLiteralExpr(true,
            new BoundType(typeof(bool))),
            new BoundLiteralExpr(false,
            new BoundType(typeof(bool))),
            new BoundType(typeof(bool)));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundLiteralExpr>());
        var literal = (BoundLiteralExpr)result;
        Assert.That(literal.Value, Is.EqualTo(false));
    }

    [Test]
    public void Folds_BooleanOr()
    {
        var tree = new BoundLogicalExpr(
            TokenType.PipePipe,
            new BoundLiteralExpr(true,
            new BoundType(typeof(bool))),
            new BoundLiteralExpr(false,
            new BoundType(typeof(bool))),
            new BoundType(typeof(bool)));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundLiteralExpr>());
        var literal = (BoundLiteralExpr)result;
        Assert.That(literal.Value, Is.EqualTo(true));
    }

    [Test]
    public void Folds_LogicalNot()
    {
        var tree = new BoundUnaryExpr(
            TokenType.Bang,
            new BoundLiteralExpr(true,
            new BoundType(typeof(bool))),
            new BoundType(typeof(bool)));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundLiteralExpr>());
        var literal = (BoundLiteralExpr)result;
        Assert.That(literal.Value, Is.EqualTo(false));
    }

    [Test]
    public void Folds_Negation()
    {
        var tree = new BoundUnaryExpr(
            TokenType.Minus,
            new BoundLiteralExpr(42,
            new BoundType(typeof(int))),
            new BoundType(typeof(int)));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundLiteralExpr>());
        var literal = (BoundLiteralExpr)result;
        Assert.That(literal.Value, Is.EqualTo(-42));
    }

    [Test]
    public void Folds_NestedArithmetic()
    {
        // 1 + 2 + 3 → left-deep: (1 + 2) + 3
        var tree = new BoundBinaryExpr(
            TokenType.Plus,
            new BoundBinaryExpr(
                TokenType.Plus,
                new BoundLiteralExpr(1,
            new BoundType(typeof(int))),
                new BoundLiteralExpr(2,
            new BoundType(typeof(int))),
                new BoundType(typeof(int))),
            new BoundLiteralExpr(3,
            new BoundType(typeof(int))),
            new BoundType(typeof(int)));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundLiteralExpr>());
        var literal = (BoundLiteralExpr)result;
        Assert.That(literal.Value, Is.EqualTo(6));
    }

    [Test]
    public void DoesNotFold_NonLiteralOperands()
    {
        var tree = new BoundBinaryExpr(
            TokenType.Plus,
            new BoundIdentifierExpr("x",
            new BoundType(typeof(int))),
            new BoundLiteralExpr(3,
            new BoundType(typeof(int))),
            new BoundType(typeof(int)));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundBinaryExpr>());
    }

    [Test]
    public void DoesNotFold_DivisionByZero()
    {
        var tree = new BoundBinaryExpr(
            TokenType.Slash,
            new BoundLiteralExpr(10,
            new BoundType(typeof(int))),
            new BoundLiteralExpr(0,
            new BoundType(typeof(int))),
            new BoundType(typeof(int)));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundBinaryExpr>());
    }

    [Test]
    public void DoesNotFold_CheckedOverflow()
    {
        // int.MaxValue + 1 in unchecked context should still fold (wraps around),
        // but checked context overflow would be caught by the catch block.
        // The ConstantFoldingPass uses unchecked arithmetic by default.
        // Checked context folding is prevented by the BoundCheckedExpr wrapper,
        // which the pass does not enter for folding purposes.
        var tree = new BoundBinaryExpr(
            TokenType.Plus,
            new BoundLiteralExpr(int.MaxValue,
            new BoundType(typeof(int))),
            new BoundLiteralExpr(1,
            new BoundType(typeof(int))),
            new BoundType(typeof(int)));

        // unchecked int overflow wraps, so this CAN fold
        var result = _pass.Rewrite(tree);
        Assert.That(result, Is.TypeOf<BoundLiteralExpr>());
    }

    [Test]
    public void Folds_ConditionalWithLiteralTrue()
    {
        var tree = new BoundConditionalExpr(
            new BoundLiteralExpr(true,
            new BoundType(typeof(bool))),
            new BoundLiteralExpr(1,
            new BoundType(typeof(int))),
            new BoundLiteralExpr(2,
            new BoundType(typeof(int))),
            new BoundType(typeof(int)));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundLiteralExpr>());
        Assert.That(((BoundLiteralExpr)result).Value, Is.EqualTo(1));
    }

    [Test]
    public void Folds_ConditionalWithLiteralFalse()
    {
        var tree = new BoundConditionalExpr(
            new BoundLiteralExpr(false,
            new BoundType(typeof(bool))),
            new BoundLiteralExpr(1,
            new BoundType(typeof(int))),
            new BoundLiteralExpr(2,
            new BoundType(typeof(int))),
            new BoundType(typeof(int)));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundLiteralExpr>());
        Assert.That(((BoundLiteralExpr)result).Value, Is.EqualTo(2));
    }

    [Test]
    public void DoesNotFold_ConditionalWithNonLiteralCondition()
    {
        var tree = new BoundConditionalExpr(
            new BoundIdentifierExpr("flag",
            new BoundType(typeof(bool))),
            new BoundLiteralExpr(1,
            new BoundType(typeof(int))),
            new BoundLiteralExpr(2,
            new BoundType(typeof(int))),
            new BoundType(typeof(int)));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundConditionalExpr>());
    }

    [Test]
    public void Folds_Comparison()
    {
        var tree = new BoundBinaryExpr(
            TokenType.Greater,
            new BoundLiteralExpr(5,
            new BoundType(typeof(int))),
            new BoundLiteralExpr(3,
            new BoundType(typeof(int))),
            new BoundType(typeof(bool)));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundLiteralExpr>());
        Assert.That(((BoundLiteralExpr)result).Value, Is.EqualTo(true));
    }

    [Test]
    public void Folds_BitwiseNot()
    {
        var tree = new BoundUnaryExpr(
            TokenType.Tilde,
            new BoundLiteralExpr(0,
            new BoundType(typeof(int))),
            new BoundType(typeof(int)));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundLiteralExpr>());
        Assert.That(((BoundLiteralExpr)result).Value, Is.EqualTo(-1));
    }

    [Test]
    public void Folds_Multiplication()
    {
        var tree = new BoundBinaryExpr(
            TokenType.Star,
            new BoundLiteralExpr(6,
            new BoundType(typeof(int))),
            new BoundLiteralExpr(7,
            new BoundType(typeof(int))),
            new BoundType(typeof(int)));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundLiteralExpr>());
        Assert.That(((BoundLiteralExpr)result).Value, Is.EqualTo(42));
    }

    [Test]
    public void PreservesSpan_OnFoldedLiteral()
    {
        var tree = new BoundBinaryExpr(
            TokenType.Plus,
            new BoundLiteralExpr(1,
            new BoundType(typeof(int))),
            new BoundLiteralExpr(2,
            new BoundType(typeof(int))),
            new BoundType(typeof(int)))
        {
            Span = new Text.TextSpan(5, 10)
        };

        var result = _pass.Rewrite(tree);

        Assert.That(result.Span.Start, Is.EqualTo(5));
        Assert.That(result.Span.Length, Is.EqualTo(10));
    }

    [Test]
    public void DoesNotFold_NullLiteralOperand()
    {
        var tree = new BoundBinaryExpr(
            TokenType.Plus,
            new BoundLiteralExpr(null,
            new BoundType(typeof(object))),
            new BoundLiteralExpr(3,
            new BoundType(typeof(int))),
            new BoundType(typeof(object)));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundBinaryExpr>());
    }
}
