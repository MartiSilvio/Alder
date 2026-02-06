namespace CsEval.Test.Types;

/// <summary>
/// Tests for tuple expressions (ECMA-334 §12.8.6 - Tuple expressions).
/// Tuples create System.ValueTuple instances with element access via Item1/Item2/etc.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class TupleTests(CompilationMode mode)
{
    #region ECMA-334 §12.8.6 - Basic tuple creation

    [Test]
    public void Tuple_TwoInts_CreatesValueTuple()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("(1, 2)");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GetType().Name, Does.Contain("ValueTuple"));
    }

    [Test]
    public void Tuple_MixedTypes_CreatesValueTuple()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("(1, \"hello\")");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GetType().Name, Does.Contain("ValueTuple"));
    }

    [Test]
    public void Tuple_ThreeElements_CreatesValueTuple()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("(true, 3.14, \"test\")");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GetType().Name, Does.Contain("ValueTuple"));
    }

    [Test]
    public void Tuple_SevenElements_CreatesValueTuple()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("(1, 2, 3, 4, 5, 6, 7)");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GetType().Name, Does.Contain("ValueTuple"));
    }

    #endregion

    #region ECMA-334 §12.8.6 - Element access via Item1/Item2

    [Test]
    public async Task Tuple_Item1_ReturnsFirstElement()
        => await TestHelpers.RunCSharpParityTestAsync("(1, 2).Item1", 1, mode);

    [Test]
    public async Task Tuple_Item2_ReturnsSecondElement()
        => await TestHelpers.RunCSharpParityTestAsync("(1, 2).Item2", 2, mode);

    [Test]
    public async Task Tuple_StringIntItem1_ReturnsString()
        => await TestHelpers.RunCSharpParityTestAsync("(\"hello\", 42).Item1", "hello", mode);

    [Test]
    public async Task Tuple_StringIntItem2_ReturnsInt()
        => await TestHelpers.RunCSharpParityTestAsync("(\"hello\", 42).Item2", 42, mode);

    #endregion

    #region ECMA-334 §12.8.6 - Tuple in variable

    [Test]
    public void Tuple_VarItem1_ReturnsFirstElement()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("var t = (1, 2); t.Item1");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Tuple_VarItem3_ReturnsThirdElement()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("var t = (1, \"hello\", true); t.Item3");
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion

    #region ECMA-334 §12.8.6 - Named tuple elements

    [Test]
    public async Task Tuple_NamedElements_AccessViaItem1()
        => await TestHelpers.RunCSharpParityTestAsync("(x: 1, y: 2).Item1", 1, mode);

    [Test]
    public async Task Tuple_NamedElements_AccessViaItem2()
        => await TestHelpers.RunCSharpParityTestAsync("(x: 1, y: 2).Item2", 2, mode);

    #endregion

    #region ECMA-334 §12.8.6 - Nested tuples

    [Test]
    public void Tuple_Nested_Item1_ReturnsInnerTuple()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("((1, 2), 3).Item1");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GetType().Name, Does.Contain("ValueTuple"));
    }

    [Test]
    public async Task Tuple_Nested_Item2_ReturnsOuterElement()
        => await TestHelpers.RunCSharpParityTestAsync("((1, 2), 3).Item2", 3, mode);

    #endregion

    #region ECMA-334 §12.8.6 - Expressions as elements

    [Test]
    public async Task Tuple_ExpressionElements_Item1()
        => await TestHelpers.RunCSharpParityTestAsync("(1 + 2, 3 * 4).Item1", 3, mode);

    [Test]
    public async Task Tuple_ExpressionElements_Item2()
        => await TestHelpers.RunCSharpParityTestAsync("(1 + 2, 3 * 4).Item2", 12, mode);

    #endregion

    #region ECMA-334 §12.8.6 - Tuple with null element

    [Test]
    public void Tuple_NullElement_CreatesValueTuple()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", (object?)null);
        var result = engine.Evaluate("(x, 42)");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GetType().Name, Does.Contain("ValueTuple"));
    }

    #endregion

    #region Parser disambiguation - lambda vs tuple

    // Ensure that lambdas with parenthesized parameters still work
    [Test]
    public void Parser_LambdaSingleParam_StillWorks()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("nums", new List<int> { 1, 2, 3, 4, 5 });
        var result = engine.Evaluate("nums.Where((x) => x > 3).Count()");
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void Parser_LambdaMultiParam_StillWorks()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("nums", new List<int> { 10, 20, 30 });
        var result = engine.Evaluate("nums.Select((x, i) => x + i).First()");
        Assert.That(result, Is.EqualTo(10));
    }

    // Ensure grouping still works
    [Test]
    public async Task Parser_Grouping_StillWorks()
        => await TestHelpers.RunCSharpParityTestAsync("(1 + 2) * 3", 9, mode);

    #endregion

    #region ECMA-334 §12.12.11 - Tuple equality operators

    // Same-type equality
    [Test]
    public async Task TupleEquality_SameInts_Equal()
        => await TestHelpers.RunCSharpParityTestAsync("(1, 2) == (1, 2)", true, mode);

    [Test]
    public async Task TupleEquality_DifferentInts_NotEqual()
        => await TestHelpers.RunCSharpParityTestAsync("(1, 2) == (1, 3)", false, mode);

    [Test]
    public async Task TupleInequality_DifferentInts_True()
        => await TestHelpers.RunCSharpParityTestAsync("(1, 2) != (1, 3)", true, mode);

    [Test]
    public async Task TupleInequality_SameInts_False()
        => await TestHelpers.RunCSharpParityTestAsync("(1, 2) != (1, 2)", false, mode);

    [Test]
    public async Task TupleEquality_Strings_Equal()
        => await TestHelpers.RunCSharpParityTestAsync("(\"hello\", \"world\") == (\"hello\", \"world\")", true, mode);

    // Cross-type equality with numeric promotion
    [Test]
    public async Task TupleEquality_IntLong_Promotion()
        => await TestHelpers.RunCSharpParityTestAsync("(1, 2L) == (1L, 2)", true, mode);

    [Test]
    public async Task TupleEquality_IntDouble_Promotion()
        => await TestHelpers.RunCSharpParityTestAsync("(1, 2.0) == (1.0, 2)", true, mode);

    // Multi-element equality
    [Test]
    public async Task TupleEquality_ThreeElements_Equal()
        => await TestHelpers.RunCSharpParityTestAsync("(1, 2, 3) == (1, 2, 3)", true, mode);

    [Test]
    public async Task TupleInequality_ThreeElements_DifferentLast()
        => await TestHelpers.RunCSharpParityTestAsync("(1, 2, 3) != (1, 2, 4)", true, mode);

    // Mixed type equality
    [Test]
    public async Task TupleEquality_MixedTypes_Equal()
        => await TestHelpers.RunCSharpParityTestAsync("(1, \"hello\") == (1, \"hello\")", true, mode);

    #endregion

    #region ECMA-334 §12.8.6 - Four and five element tuples

    [Test]
    public void Tuple_FourElements_Item4()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("(1, 2, 3, 4).Item4");
        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void Tuple_FiveElements_Item5()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("(1, 2, 3, 4, 5).Item5");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Tuple_SixElements_Item6()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("(1, 2, 3, 4, 5, 6).Item6");
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void Tuple_SevenElements_Item7()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("(1, 2, 3, 4, 5, 6, 7).Item7");
        Assert.That(result, Is.EqualTo(7));
    }

    #endregion

    #region ECMA-334 sections 10.2.13, 10.3.6 - Tuple conversions

    [Test]
    public void TupleConversion_IsTupleType_DetectsTupleTypes()
    {
        // Verify IsTupleType correctly identifies ValueTuple types
        Assert.That(CsEval.Runtime.TypeHelpers.IsTupleType(typeof(ValueTuple<int, int>)), Is.True);
        Assert.That(CsEval.Runtime.TypeHelpers.IsTupleType(typeof(ValueTuple<int, string>)), Is.True);
        Assert.That(CsEval.Runtime.TypeHelpers.IsTupleType(typeof(ValueTuple<int, long, double>)), Is.True);
        Assert.That(CsEval.Runtime.TypeHelpers.IsTupleType(typeof(int)), Is.False);
        Assert.That(CsEval.Runtime.TypeHelpers.IsTupleType(typeof(string)), Is.False);
    }

    [Test]
    public void TupleConversion_CanImplicitlyConvert_SameType()
    {
        // Same tuple type should be implicitly convertible
        Assert.That(CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(
            typeof(ValueTuple<int, int>), typeof(ValueTuple<int, int>)), Is.True);
    }

    [Test]
    public void TupleConversion_CanImplicitlyConvert_IntToLong()
    {
        // (int, int) -> (long, long) should be implicitly convertible
        Assert.That(CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(
            typeof(ValueTuple<int, int>), typeof(ValueTuple<long, long>)), Is.True);
    }

    [Test]
    public void TupleConversion_CanImplicitlyConvert_IntToDouble()
    {
        // (int, int) -> (double, double) should be implicitly convertible
        Assert.That(CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(
            typeof(ValueTuple<int, int>), typeof(ValueTuple<double, double>)), Is.True);
    }

    [Test]
    public void TupleConversion_CanImplicitlyConvert_MixedWidening()
    {
        // (int, string) -> (long, object) should be implicitly convertible
        Assert.That(CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(
            typeof(ValueTuple<int, string>), typeof(ValueTuple<long, object>)), Is.True);
    }

    [Test]
    public void TupleConversion_CannotImplicitlyConvert_LongToInt()
    {
        // (long, int) -> (int, int) should NOT be implicitly convertible (long -> int is narrowing)
        Assert.That(CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(
            typeof(ValueTuple<long, int>), typeof(ValueTuple<int, int>)), Is.False);
    }

    [Test]
    public void TupleConversion_CannotImplicitlyConvert_DifferentArity()
    {
        // (int, int) -> (int, int, int) should NOT be implicitly convertible
        Assert.That(CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(
            typeof(ValueTuple<int, int>), typeof(ValueTuple<int, int, int>)), Is.False);
    }

    [Test]
    public void Tuple_RuntimeType_IsValueTuple()
    {
        // Verify the runtime type is a ValueTuple via direct result inspection
        // (GetType().Name is blocked by sandbox reflection guard)
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("(1, 2)");
        Assert.That(result, Is.Not.Null);
        Assert.That(CsEval.Runtime.TypeHelpers.IsTupleType(result!.GetType()), Is.True);
    }

    [Test]
    public void Tuple_VarAssignment_WorksWithImplicitConversion()
    {
        // Tuple assigned to var should work without issues
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("var t = (1, \"hello\"); t.Item1");
        Assert.That(result, Is.EqualTo(1));
    }

    #endregion
}
