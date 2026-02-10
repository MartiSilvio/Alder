namespace CsEval.Test.Types;

/// <summary>
/// Tests for tuple expressions (ECMA-334 §12.8.6 - Tuple expressions).
/// Engine-only tests for structural/type assertions and API tests.
/// Parity tests migrated to TestData/Types/Tuple/*.csx and Parity/TupleTests.cs.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class TupleTests(CompilationMode mode)
{
    #region Engine-only: Type name assertions (GetType().Name checks, not value comparison)

    [Test]
    public void Tuple_TwoInts_CreatesValueTuple()
    {
        // Engine-only: asserts type name, not value
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("(1, 2)");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GetType().Name, Does.Contain("ValueTuple"));
    }

    [Test]
    public void Tuple_MixedTypes_CreatesValueTuple()
    {
        // Engine-only: asserts type name, not value
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("(1, \"hello\")");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GetType().Name, Does.Contain("ValueTuple"));
    }

    [Test]
    public void Tuple_ThreeElements_CreatesValueTuple()
    {
        // Engine-only: asserts type name, not value
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("(true, 3.14, \"test\")");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GetType().Name, Does.Contain("ValueTuple"));
    }

    [Test]
    public void Tuple_SevenElements_CreatesValueTuple()
    {
        // Engine-only: asserts type name, not value
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("(1, 2, 3, 4, 5, 6, 7)");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GetType().Name, Does.Contain("ValueTuple"));
    }

    #endregion

    #region Engine-only: Nested tuple returning inner tuple (asserts type name, not value)

    [Test]
    public void Tuple_Nested_Item1_ReturnsInnerTuple()
    {
        // Engine-only: asserts inner tuple is ValueTuple type, not value comparison
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("((1, 2), 3).Item1");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GetType().Name, Does.Contain("ValueTuple"));
    }

    #endregion

    #region Engine-only: Null element tests (SetVariable with null, non-serializable for Roslyn)

    [Test]
    public void Tuple_NullElement_CreatesValueTuple()
    {
        // Engine-only: SetVariable with null object (non-serializable for Roslyn)
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", (object?)null);
        var result = engine.Evaluate("(x, 42)");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GetType().Name, Does.Contain("ValueTuple"));
    }

    #endregion

    #region Engine-only: Lambda disambiguation (SetVariable with List<int>, non-serializable)

    [Test]
    public void Parser_LambdaSingleParam_StillWorks()
    {
        // Engine-only: SetVariable with List<int> (non-serializable for Roslyn)
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("nums", new List<int> { 1, 2, 3, 4, 5 });
        var result = engine.Evaluate("nums.Where((x) => x > 3).Count()");
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void Parser_LambdaMultiParam_StillWorks()
    {
        // Engine-only: SetVariable with List<int> (non-serializable for Roslyn)
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("nums", new List<int> { 10, 20, 30 });
        var result = engine.Evaluate("nums.Select((x, i) => x + i).First()");
        Assert.That(result, Is.EqualTo(10));
    }

    #endregion

    #region Engine-only: TypeHelpers API tests (test internal API directly)

    [Test]
    public void TupleConversion_IsTupleType_DetectsTupleTypes()
    {
        // Engine-only: tests TypeHelpers internal API directly
        Assert.That(CsEval.Runtime.TypeHelpers.IsTupleType(typeof(ValueTuple<int, int>)), Is.True);
        Assert.That(CsEval.Runtime.TypeHelpers.IsTupleType(typeof(ValueTuple<int, string>)), Is.True);
        Assert.That(CsEval.Runtime.TypeHelpers.IsTupleType(typeof(ValueTuple<int, long, double>)), Is.True);
        Assert.That(CsEval.Runtime.TypeHelpers.IsTupleType(typeof(int)), Is.False);
        Assert.That(CsEval.Runtime.TypeHelpers.IsTupleType(typeof(string)), Is.False);
    }

    [Test]
    public void TupleConversion_CanImplicitlyConvert_SameType()
    {
        // Engine-only: tests TypeHelpers internal API directly
        Assert.That(CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(
            typeof(ValueTuple<int, int>), typeof(ValueTuple<int, int>)), Is.True);
    }

    [Test]
    public void TupleConversion_CanImplicitlyConvert_IntToLong()
    {
        // Engine-only: tests TypeHelpers internal API directly
        Assert.That(CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(
            typeof(ValueTuple<int, int>), typeof(ValueTuple<long, long>)), Is.True);
    }

    [Test]
    public void TupleConversion_CanImplicitlyConvert_IntToDouble()
    {
        // Engine-only: tests TypeHelpers internal API directly
        Assert.That(CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(
            typeof(ValueTuple<int, int>), typeof(ValueTuple<double, double>)), Is.True);
    }

    [Test]
    public void TupleConversion_CanImplicitlyConvert_MixedWidening()
    {
        // Engine-only: tests TypeHelpers internal API directly
        Assert.That(CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(
            typeof(ValueTuple<int, string>), typeof(ValueTuple<long, object>)), Is.True);
    }

    [Test]
    public void TupleConversion_CannotImplicitlyConvert_LongToInt()
    {
        // Engine-only: tests TypeHelpers internal API directly
        Assert.That(CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(
            typeof(ValueTuple<long, int>), typeof(ValueTuple<int, int>)), Is.False);
    }

    [Test]
    public void TupleConversion_CannotImplicitlyConvert_DifferentArity()
    {
        // Engine-only: tests TypeHelpers internal API directly
        Assert.That(CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(
            typeof(ValueTuple<int, int>), typeof(ValueTuple<int, int, int>)), Is.False);
    }

    [Test]
    public void Tuple_RuntimeType_IsValueTuple()
    {
        // Engine-only: tests TypeHelpers.IsTupleType API directly
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("(1, 2)");
        Assert.That(result, Is.Not.Null);
        Assert.That(CsEval.Runtime.TypeHelpers.IsTupleType(result!.GetType()), Is.True);
    }

    #endregion
}
