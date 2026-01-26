namespace CsEval.Test.Evaluator;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class VariableDeclarationTests(CompilationMode mode) : TestBase
{
    #region Var Declarations

    [Test]
    public void Var_InfersType()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ var x = 42; return x; }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Var_BlockScoped()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ var x = 10; var y = 20; return x + y; }");
        Assert.That(result, Is.EqualTo(30));
    }

    #endregion

    #region Typed Declarations

    [Test]
    public void TypedDeclaration_Int()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ int x = 42; return x; }");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void TypedDeclaration_Long()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ long x = 42; return x; }");
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void TypedDeclaration_Double()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ double x = 3.14; return x; }");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(3.14));
    }

    [Test]
    public void TypedDeclaration_Float()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ float x = 3.14f; return x; }");
        Assert.That(result, Is.TypeOf<float>());
        Assert.That((float)result!, Is.EqualTo(3.14f).Within(0.001f));
    }

    [Test]
    public void TypedDeclaration_Decimal()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ decimal x = 3.14m; return x; }");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(3.14m));
    }

    [Test]
    public void TypedDeclaration_String()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ string x = \"hello\"; return x; }");
        Assert.That(result, Is.TypeOf<string>());
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void TypedDeclaration_Bool()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ bool x = true; return x; }");
        Assert.That(result, Is.TypeOf<bool>());
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void TypedDeclaration_Object_AcceptsAny()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ object x = 42; return x; }");
        Assert.That(result, Is.EqualTo(42));
    }

    #endregion

    #region Type Coercion

    [Test]
    public void TypedDeclaration_Int_CoercesFromSmaller()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("b", (byte)100);
        var result = engine.Evaluate("{ int x = b; return x; }");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void TypedDeclaration_Long_CoercesFromInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ long x = 42; return x; }");
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void TypedDeclaration_Double_CoercesFromInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ double x = 42; return x; }");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(42.0));
    }

    #endregion

    #region Type Validation Errors

    [Test]
    public void TypedDeclaration_Int_ThrowsOnStringAssignment()
    {
        var engine = CreateEngine(mode);
        Assert.Throws<CsEvalException>(() => engine.Evaluate("{ int x = \"hello\"; return x; }"));
    }

    [Test]
    public void TypedDeclaration_Int_ThrowsOnNullAssignment()
    {
        var engine = CreateEngine(mode);
        Assert.Throws<CsEvalException>(() => engine.Evaluate("{ int x = null; return x; }"));
    }

    [Test]
    public void TypedDeclaration_String_ThrowsOnIntAssignment()
    {
        var engine = CreateEngine(mode);
        Assert.Throws<CsEvalException>(() => engine.Evaluate("{ string x = 42; return x; }"));
    }

    [Test]
    public void TypedDeclaration_Bool_ThrowsOnIntAssignment()
    {
        var engine = CreateEngine(mode);
        Assert.Throws<CsEvalException>(() => engine.Evaluate("{ bool x = 1; return x; }"));
    }

    #endregion

    #region Multiple Declarations

    [Test]
    public void TypedDeclaration_MultipleInBlock()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"{
            int x = 10;
            long y = 20L;
            double z = 1.5;
            return x + y + z;
        }");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(31.5));
    }

    #endregion

    #region Nullable Types

    [Test]
    public void NullableInt_AcceptsNull()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ int? x = null; return x; }");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void NullableInt_AcceptsValue()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ int? x = 42; return x; }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void NullableLong_AcceptsNull()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ long? x = null; return x; }");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void NullableLong_CoercesFromInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ long? x = 42; return x; }");
        Assert.That(result, Is.EqualTo(42L));
    }

    [Test]
    public void NullableDouble_AcceptsNull()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ double? x = null; return x; }");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void NullableBool_AcceptsNull()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ bool? x = null; return x; }");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void NullableBool_AcceptsValue()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ bool? x = true; return x; }");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void NullableInt_ThrowsOnStringAssignment()
    {
        var engine = CreateEngine(mode);
        Assert.Throws<CsEvalException>(() => engine.Evaluate("{ int? x = \"hello\"; return x; }"));
    }

    #endregion
}
