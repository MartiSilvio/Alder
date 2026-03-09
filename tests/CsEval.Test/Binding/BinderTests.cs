using CsEval.Binding;
using CsEval.Binding.BoundNodes;
using CsEval.Parsing;

namespace CsEval.Test;

[TestFixture]
public sealed class BinderTests
{
    [Test]
    public void Binder_ShouldProduceBoundBinary_ForSimpleArithmetic()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("x", 5);

        var parsed = engine.Parse("x + 2");
        var bindingContext = new BindingContext(engine.GetContextForCompiled());
        var binder = new Binder();

        var bound = binder.Bind(parsed.Ast, bindingContext);

        Assert.That(bound, Is.TypeOf<BoundBinaryExpr>());
        var binary = (BoundBinaryExpr)bound;
        Assert.That(binary.Operator, Is.EqualTo(TokenType.Plus));
        Assert.That(binary.Left, Is.TypeOf<BoundIdentifierExpr>());
        Assert.That(binary.Right, Is.TypeOf<BoundLiteralExpr>());
    }
}
