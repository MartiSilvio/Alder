using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Binding.Optimization;
using Alder.Parsing;

namespace Alder.Test.Optimization;

[TestFixture]
public sealed class ConversionInsertionTests
{
    private readonly ConversionInsertionPass _pass = new();

    [Test]
    public void Inserts_IntToLong_Conversion()
    {
        var tree = new BoundBinaryExpr(
            TokenType.Plus,
            new BoundLiteralExpr(1, typeof(int)),
            new BoundLiteralExpr(2L, typeof(long)),
            typeof(long));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundBinaryExpr>());
        var binary = (BoundBinaryExpr)result;
        Assert.That(binary.Left, Is.TypeOf<BoundCastExpr>());
        var cast = (BoundCastExpr)binary.Left;
        Assert.That(cast.TargetType, Is.EqualTo(typeof(long)));
        Assert.That(cast.SourceStaticType, Is.EqualTo(typeof(int)));
        Assert.That(binary.Right, Is.TypeOf<BoundLiteralExpr>());
    }

    [Test]
    public void Inserts_FloatToDouble_Conversion()
    {
        var tree = new BoundBinaryExpr(
            TokenType.Plus,
            new BoundLiteralExpr(1.0f, typeof(float)),
            new BoundLiteralExpr(2.0, typeof(double)),
            typeof(double));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundBinaryExpr>());
        var binary = (BoundBinaryExpr)result;
        Assert.That(binary.Left, Is.TypeOf<BoundCastExpr>());
        var cast = (BoundCastExpr)binary.Left;
        Assert.That(cast.TargetType, Is.EqualTo(typeof(double)));
        Assert.That(cast.SourceStaticType, Is.EqualTo(typeof(float)));
    }

    [Test]
    public void NoConversion_SameTypes()
    {
        var tree = new BoundBinaryExpr(
            TokenType.Plus,
            new BoundLiteralExpr(1, typeof(int)),
            new BoundLiteralExpr(2, typeof(int)),
            typeof(int));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundBinaryExpr>());
        var binary = (BoundBinaryExpr)result;
        Assert.That(binary.Left, Is.TypeOf<BoundLiteralExpr>());
        Assert.That(binary.Right, Is.TypeOf<BoundLiteralExpr>());
    }

    [Test]
    public void NoConversion_ObjectOperand()
    {
        var tree = new BoundBinaryExpr(
            TokenType.Plus,
            new BoundIdentifierExpr("x", typeof(object)),
            new BoundLiteralExpr(2, typeof(int)),
            typeof(object));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundBinaryExpr>());
        var binary = (BoundBinaryExpr)result;
        Assert.That(binary.Left, Is.TypeOf<BoundIdentifierExpr>());
        Assert.That(binary.Right, Is.TypeOf<BoundLiteralExpr>());
    }

    [Test]
    public void NoConversion_NonArithmeticTypes()
    {
        var tree = new BoundBinaryExpr(
            TokenType.Plus,
            new BoundLiteralExpr("hello", typeof(string)),
            new BoundLiteralExpr(" world", typeof(string)),
            typeof(string));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundBinaryExpr>());
        var binary = (BoundBinaryExpr)result;
        Assert.That(binary.Left, Is.TypeOf<BoundLiteralExpr>());
        Assert.That(binary.Right, Is.TypeOf<BoundLiteralExpr>());
    }

    [Test]
    public void Inserts_IntToDouble_Conversion()
    {
        var tree = new BoundBinaryExpr(
            TokenType.Star,
            new BoundLiteralExpr(5, typeof(int)),
            new BoundLiteralExpr(2.5, typeof(double)),
            typeof(double));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundBinaryExpr>());
        var binary = (BoundBinaryExpr)result;
        Assert.That(binary.Left, Is.TypeOf<BoundCastExpr>());
        var cast = (BoundCastExpr)binary.Left;
        Assert.That(cast.TargetType, Is.EqualTo(typeof(double)));
    }

    [Test]
    public void PreservesSpan_OnInsertedCast()
    {
        var left = new BoundLiteralExpr(1, typeof(int))
        {
            Span = new Text.TextSpan(0, 1)
        };
        var tree = new BoundBinaryExpr(
            TokenType.Plus,
            left,
            new BoundLiteralExpr(2L, typeof(long)),
            typeof(long));

        var result = _pass.Rewrite(tree);

        var binary = (BoundBinaryExpr)result;
        var cast = (BoundCastExpr)binary.Left;
        Assert.That(cast.Span.Start, Is.EqualTo(0));
        Assert.That(cast.Span.Length, Is.EqualTo(1));
    }
}
