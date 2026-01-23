using CsEval.Evaluation;
using NUnit.Framework;

namespace CsEval.Test.Evaluator;

[TestFixture]
public class VariableDeclarationTests : EvaluatorTestBase
{
    #region Var Declarations

    [Test]
    public void Var_InfersType()
    {
        var result = Eval("{ var x = 42; return x; }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Var_BlockScoped()
    {
        var result = Eval("{ var x = 10; var y = 20; return x + y; }");
        Assert.That(result, Is.EqualTo(30));
    }

    #endregion

    #region Typed Declarations

    [Test]
    public void TypedDeclaration_Int()
    {
        var result = Eval("{ int x = 42; return x; }");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void TypedDeclaration_Long()
    {
        var result = Eval("{ long x = 42; return x; }");
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void TypedDeclaration_Double()
    {
        var result = Eval("{ double x = 3.14; return x; }");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(3.14));
    }

    [Test]
    public void TypedDeclaration_Float()
    {
        var result = Eval("{ float x = 3.14f; return x; }");
        Assert.That(result, Is.TypeOf<float>());
        Assert.That((float)result!, Is.EqualTo(3.14f).Within(0.001f));
    }

    [Test]
    public void TypedDeclaration_Decimal()
    {
        var result = Eval("{ decimal x = 3.14m; return x; }");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(3.14m));
    }

    [Test]
    public void TypedDeclaration_String()
    {
        var result = Eval("{ string x = \"hello\"; return x; }");
        Assert.That(result, Is.TypeOf<string>());
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void TypedDeclaration_Bool()
    {
        var result = Eval("{ bool x = true; return x; }");
        Assert.That(result, Is.TypeOf<bool>());
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void TypedDeclaration_Object_AcceptsAny()
    {
        var result = Eval("{ object x = 42; return x; }");
        Assert.That(result, Is.EqualTo(42));
    }

    #endregion

    #region Type Coercion

    [Test]
    public void TypedDeclaration_Int_CoercesFromSmaller()
    {
        var context = new EvalContext();
        context.Define("b", (byte)100);
        var result = Eval("{ int x = b; return x; }", context);
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void TypedDeclaration_Long_CoercesFromInt()
    {
        var result = Eval("{ long x = 42; return x; }");
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void TypedDeclaration_Double_CoercesFromInt()
    {
        var result = Eval("{ double x = 42; return x; }");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(42.0));
    }

    #endregion

    #region Type Validation Errors

    [Test]
    public void TypedDeclaration_Int_ThrowsOnStringAssignment()
    {
        Assert.Throws<EvalException>(() => Eval("{ int x = \"hello\"; return x; }"));
    }

    [Test]
    public void TypedDeclaration_Int_ThrowsOnNullAssignment()
    {
        Assert.Throws<EvalException>(() => Eval("{ int x = null; return x; }"));
    }

    [Test]
    public void TypedDeclaration_String_ThrowsOnIntAssignment()
    {
        Assert.Throws<EvalException>(() => Eval("{ string x = 42; return x; }"));
    }

    [Test]
    public void TypedDeclaration_Bool_ThrowsOnIntAssignment()
    {
        Assert.Throws<EvalException>(() => Eval("{ bool x = 1; return x; }"));
    }

    #endregion

    #region Multiple Declarations

    [Test]
    public void TypedDeclaration_MultipleInBlock()
    {
        var result = Eval(@"{
            int x = 10;
            long y = 20L;
            double z = 1.5;
            return x + y + z;
        }");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(31.5));
    }

    #endregion
}
