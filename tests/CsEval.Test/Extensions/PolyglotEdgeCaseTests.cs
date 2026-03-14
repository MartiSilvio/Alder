namespace CsEval.Test.Extensions;

/// <summary>
/// Edge cases, error handling, standard mode rejection, and side-effect tests
/// for polyglot extended features. Happy-path behavior is covered by .csx parity
/// tests in TestData/ValidExpressions/SyntaxSugar/.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class PolyglotEdgeCaseTests(CompilationMode mode)
{
    private CsEvalEngine CreateEngine() =>
        new(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

    private CsEvalEngine CreateStandardEngine() =>
        new(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Standard });

    #region Bare math: shadowing

    [Test]
    public void BareMath_UserVariableShadowsConstant()
    {
        var engine = CreateEngine();
        engine.SetVariable("pi", 3);
        Assert.That(engine.Evaluate("pi"), Is.EqualTo(3));
    }

    [Test]
    public void BareMath_UserFunctionShadowsBuiltIn()
    {
        var engine = CreateEngine();
        engine.RegisterFunction("sin", args => 42);
        Assert.That(engine.Evaluate("sin(5.0)"), Is.EqualTo(42));
    }

    [Test]
    public void BareMath_ModuleNameDoesNotShadowBuiltInFunction()
    {
        var engine = CreateEngine();
        engine.RegisterModule("sin", typeof(Math));
        Assert.That(engine.Evaluate("sin(0.0)"), Is.EqualTo(0.0).Within(1e-12));
    }

    [Test]
    public void BareMath_ShadowDoesNotLeakToNewEngine()
    {
        var engine = CreateEngine();
        engine.SetVariable("pi", 3);
        Assert.That(engine.Evaluate("pi"), Is.EqualTo(3));

        var engine2 = CreateEngine();
        Assert.That(engine2.Evaluate("pi"), Is.EqualTo(Math.PI));
    }

    [Test]
    public void ScopeFunctions_LetAlsoRunWith_Work()
    {
        var engine = CreateEngine();

        Assert.That(engine.Evaluate("7.let(x => x * x)"), Is.EqualTo(49));
        Assert.That(engine.Evaluate("7.also(x => x + 1)"), Is.EqualTo(7));
        Assert.That(engine.Evaluate("7.run(x => x + 1)"), Is.EqualTo(8));
        Assert.That(engine.Evaluate("7.with(x => x * x)"), Is.EqualTo(49));
    }

    [Test]
    public void ScopeFunctions_ApplyDoesNotExist()
    {
        var engine = CreateEngine();

        // apply is removed — it was identical to also, which is misleading vs Kotlin semantics
        Assert.Throws<CsEvalException>(() => engine.Evaluate("7.apply(x => x + 1)"));
    }

    [Test]
    public void IdentifierCall_FunctionShadowsVariable()
    {
        var engine = CreateEngine();
        engine.SetVariable("inc", (Func<int, int>)(x => x + 100));
        engine.RegisterFunction("inc", args => Convert.ToInt32(args[0]) + 1);
        Assert.That(engine.Evaluate("inc(5)"), Is.EqualTo(6));
    }

    [Test]
    public void IdentifierCall_ModuleShadowsVariable()
    {
        var engine = CreateEngine();
        engine.SetVariable("Math", (Func<int, int>)(x => x + 1));
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("Math(5)"));
        Assert.That(ex!.Message, Does.Contain("ModuleInfo"));
    }

    #endregion

    #region Range: edge cases

    [Test]
    public void Range_ReversedRange_ReturnsEmpty()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("(10..1).Count()"), Is.EqualTo(0));
    }

    [Test]
    public void Range_SpreadStillWorks()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("{ var a = new[] {1, 2}; var b = new[] {3, 4}; return [..a, ..b]; }");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    #endregion

    #region Implicit multiply: edge cases

    [Test]
    public void ImplicitMultiply_WholeIdentifier_2xy()
    {
        var engine = CreateEngine();
        engine.SetVariable("xy", 5);
        Assert.That(engine.Evaluate("2xy"), Is.EqualTo(10));
    }

    [Test]
    public void ImplicitMultiply_ScientificNotation_NotTriggered()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("2e10"), Is.EqualTo(2e10));
    }

    [Test]
    public void ImplicitMultiply_LambdaCall_NotTriggered()
    {
        var engine = CreateEngine();
        engine.SetVariable("f", (Func<int, int>)(x => x * 2));
        Assert.That(engine.Evaluate("f(5)"), Is.EqualTo(10));
    }

    #endregion

    #region Pipeline: error handling

    [Test]
    public void Pipeline_NonCallableRight_Throws()
    {
        var engine = CreateEngine();
        Assert.That(() => engine.Evaluate("5 |> 10"),
            Throws.InstanceOf<CsEvalException>().With.Message.Contains("|>"));
    }

    [Test]
    public void Pipeline_NullRight_Throws()
    {
        var engine = CreateEngine();
        Assert.That(() => engine.Evaluate("5 |> null"),
            Throws.InstanceOf<CsEvalException>().With.Message.Contains("|>"));
    }

    [Test]
    public void Pipeline_NullLeft_PassesNullToFunction()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("null |> (x => x == null)"), Is.EqualTo(true));
    }

    [Test]
    public void Pipeline_NonCallableIdentifier_Throws()
    {
        var engine = CreateEngine();
        engine.SetVariable("f", 10);
        Assert.That(() => engine.Evaluate("5 |> f"),
            Throws.InstanceOf<CsEvalException>());
    }

    [Test]
    public void Pipeline_IdentifierCall_MatchesDirectCall()
    {
        var engine = CreateEngine();
        engine.RegisterFunction("inc", args => Convert.ToInt32(args[0]) + 1);
        Assert.That(engine.Evaluate("5 |> inc"), Is.EqualTo(engine.Evaluate("inc(5)")));
    }

    [Test]
    public void Pipeline_Precedence_ArithmeticBindsTighter()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("2 + 3 |> (x => x * 2)"), Is.EqualTo(10));
    }

    #endregion

    #region Slice step: error handling

    [Test]
    public void SliceStep_ZeroStep_Throws()
    {
        var engine = CreateEngine();
        Assert.That(() => engine.Evaluate("{ var arr = new[] {1, 2, 3}; return arr[::0]; }"),
            Throws.InstanceOf<CsEvalException>());
    }

    [Test]
    public void SliceStep_TwoArgSlice_Unchanged()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("{ var arr = new[] {10, 20, 30, 40, 50}; return arr[1:4]; }");
        Assert.That(result, Is.EqualTo(new[] { 20, 30, 40 }));
    }

    #endregion

    #region Like pattern: wildcard semantics

    [Test]
    public void LikeOperator_UnderscoreMatchesSingleCharacter()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("\"abc\" like \"a_c\""), Is.EqualTo(true));
        Assert.That(engine.Evaluate("\"ac\" like \"a_c\""), Is.EqualTo(false));
    }

    [Test]
    public void LikeOperator_MultiPercentPattern_Matches()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("\"abXXcdYYef\" like \"ab%cd%ef\""), Is.EqualTo(true));
    }

    [Test]
    public void LikeOperator_RegexMetacharactersStayLiteral()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("\"a.c\" like \"a.c\""), Is.EqualTo(true));
        Assert.That(engine.Evaluate("\"abc\" like \"a.c\""), Is.EqualTo(false));
    }

    [Test]
    public void LikeOperator_NullLeftOperand_ThrowsConsistentException()
    {
        var engine = CreateEngine();
        engine.SetVariable<string>("text", null!);

        Assert.That(() => engine.Evaluate("text like \"a%\""),
            Throws.InstanceOf<CsEvalException>().With.Message.Contains("like"));
    }

    #endregion

    #region In operator: null handling

    [Test]
    public void InOperator_NullCollectionVariable_ThrowsConsistentException()
    {
        var engine = CreateEngine();
        engine.SetVariable<List<int>>("numbers", null!);

        Assert.That(() => engine.Evaluate("1 in numbers"),
            Throws.InstanceOf<CsEvalException>().With.Message.Contains("in"));
    }

    [Test]
    public void NotInAlias_NullCollectionVariable_ThrowsConsistentException()
    {
        var engine = CreateEngine();
        engine.SetVariable<List<int>>("numbers", null!);

        Assert.That(() => engine.Evaluate("1 not in numbers"),
            Throws.InstanceOf<CsEvalException>().With.Message.Contains("in"));
    }

    #endregion

    #region Regex: error handling

    [Test]
    public void Regex_LeftNull_Throws()
    {
        var engine = CreateEngine();
        Assert.That(() => engine.Evaluate("null =~ \"pattern\""),
            Throws.InstanceOf<CsEvalException>());
    }

    [Test]
    public void Regex_RightNull_Throws()
    {
        var engine = CreateEngine();
        Assert.That(() => engine.Evaluate("\"text\" =~ null"),
            Throws.InstanceOf<CsEvalException>());
    }

    #endregion

    #region Spaceship: null handling

    [Test]
    public void Spaceship_BothNull_ReturnsZero()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("null <=> null"), Is.EqualTo(0));
    }

    [Test]
    public void Spaceship_LeftNull_ReturnsNegativeOne()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("null <=> 5"), Is.EqualTo(-1));
    }

    [Test]
    public void Spaceship_RightNull_ReturnsOne()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("5 <=> null"), Is.EqualTo(1));
    }

    #endregion

    #region Chained comparison: side effects and short-circuit

    [Test]
    public void ChainedComparison_MiddleOperandEvaluatedOnce()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("""
            var counter = 0;
            var f = () => { counter = counter + 1; return counter; };
            var result = 0 < f() < 10;
            result.ToString() + "," + counter.ToString()
            """);
        Assert.That(result, Is.EqualTo("True,1"));
    }

    [Test]
    public void ChainedComparison_ShortCircuitsOnFalse()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("""
            var counter = 0;
            var g = () => { counter = counter + 1; return 100; };
            var result = 5 < 3 < g();
            result.ToString() + "," + counter.ToString()
            """);
        Assert.That(result, Is.EqualTo("False,0"));
    }

    [Test]
    public void ChainedComparison_EqualityChain()
    {
        var engine = CreateEngine();
        engine.SetVariable("a", 5);
        engine.SetVariable("b", 5);
        Assert.That(engine.Evaluate("a == b == 5"), Is.EqualTo(true));
    }

    [Test]
    public void ChainedComparison_WithNaN_ReturnsFalse()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("0.0 < double.NaN < 10.0"), Is.EqualTo(false));
    }

    #endregion

    #region Deconstruction: error handling

    [Test]
    public void Deconstruction_NoDeconstructMethod_Throws()
    {
        var engine = CreateEngine();
        Assert.That(() => engine.Evaluate("{ var (a, b) = 42; return a; }"),
            Throws.InstanceOf<CsEvalException>());
    }

    #endregion

    #region and/or/not: general-purpose boolean operators

    [Test]
    public void ExtendedMode_And_WorksAsGeneralBooleanOperator()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("true and true"), Is.True);
        Assert.That(engine.Evaluate("true and false"), Is.False);
    }

    [Test]
    public void ExtendedMode_Or_WorksAsGeneralBooleanOperator()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("false or true"), Is.True);
        Assert.That(engine.Evaluate("false or false"), Is.False);
    }

    [Test]
    public void ExtendedMode_Not_WorksAsGeneralBooleanOperator()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("not true"), Is.False);
        Assert.That(engine.Evaluate("not false"), Is.True);
    }

    [Test]
    public void StandardMode_And_InExpression_Throws()
        => Assert.That(() => CreateStandardEngine().Evaluate("true and false"),
            Throws.InstanceOf<CsEvalLanguageModeException>());

    [Test]
    public void StandardMode_Or_InExpression_Throws()
        => Assert.That(() => CreateStandardEngine().Evaluate("false or true"),
            Throws.InstanceOf<CsEvalLanguageModeException>());

    [Test]
    public void StandardMode_Not_InExpression_Throws()
        => Assert.That(() => CreateStandardEngine().Evaluate("not true"),
            Throws.InstanceOf<CsEvalLanguageModeException>());

    [Test]
    public void StandardMode_And_InPattern_StillWorks()
    {
        var engine = CreateStandardEngine();
        engine.SetVariable("x", 5);
        Assert.That(engine.Evaluate("x is > 0 and < 10"), Is.True);
    }

    [Test]
    public void StandardMode_Or_InPattern_StillWorks()
    {
        var engine = CreateStandardEngine();
        engine.SetVariable("x", 5);
        Assert.That(engine.Evaluate("x is 5 or 10"), Is.True);
        Assert.That(engine.Evaluate("x is 3 or 5"), Is.True);
        Assert.That(engine.Evaluate("x is 3 or 4"), Is.False);
    }

    [Test]
    public void StandardMode_Not_InPattern_StillWorks()
    {
        var engine = CreateStandardEngine();
        engine.SetVariable("x", 5);
        Assert.That(engine.Evaluate("x is not null"), Is.True);
    }

    [Test]
    public void StandardMode_SymbolicAndOr_StillWork()
    {
        var engine = CreateStandardEngine();
        Assert.That(engine.Evaluate("true && true"), Is.True);
        Assert.That(engine.Evaluate("false || true"), Is.True);
        Assert.That(engine.Evaluate("!false"), Is.True);
    }

    #endregion

    #region Strict equality: type-exact semantics

    [Test]
    public void StrictEquality_SameTypeSameValue_ReturnsTrue()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("1 === 1"), Is.True);
    }

    [Test]
    public void StrictEquality_DifferentNumericTypes_ReturnsFalse()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("1 === 1L"), Is.False);       // int vs long
        Assert.That(engine.Evaluate("1 === 1.0"), Is.False);      // int vs double
        Assert.That(engine.Evaluate("1.0f === 1.0"), Is.False);   // float vs double
    }

    [Test]
    public void StrictEquality_Strings_ReturnsTrue()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("\"abc\" === \"abc\""), Is.True);
    }

    [Test]
    public void StrictEquality_NullBothSides_ReturnsTrue()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("null === null"), Is.True);
    }

    [Test]
    public void StrictEquality_NullVsValue_ReturnsFalse()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("null === 0"), Is.False);
        Assert.That(engine.Evaluate("0 === null"), Is.False);
    }

    [Test]
    public void StrictEquality_BoolVsInt_ReturnsFalse()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("true === 1"), Is.False);
    }

    [Test]
    public void StrictInequality_DifferentNumericTypes_ReturnsTrue()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("1 !== 1L"), Is.True);
    }

    [Test]
    public void StrictInequality_SameTypeSameValue_ReturnsFalse()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("1 !== 1"), Is.False);
    }

    [Test]
    public void StrictEquality_NaN_ReturnsFalse()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("double.NaN === double.NaN"), Is.False);
    }

    [Test]
    public void RegularEquality_StillWidens()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("1 == 1L"), Is.True);
    }

    #endregion

    #region Standard mode rejection (all features)

    [Test]
    public void StandardMode_BareMathSin_Throws()
        => Assert.That(() => CreateStandardEngine().Evaluate("sin(1.0)"),
            Throws.InstanceOf<CsEvalException>());

    [Test]
    public void StandardMode_BareMathPi_Throws()
        => Assert.That(() => CreateStandardEngine().Evaluate("pi"),
            Throws.InstanceOf<CsEvalException>());

    [Test]
    public void StandardMode_IdentifierCall_FunctionShadowsVariable()
    {
        var engine = CreateStandardEngine();
        engine.SetVariable("inc", (Func<int, int>)(x => x + 100));
        engine.RegisterFunction("inc", args => Convert.ToInt32(args[0]) + 1);
        Assert.That(engine.Evaluate("inc(5)"), Is.EqualTo(6));
    }

    [Test]
    public void StandardMode_IdentifierCall_ModuleShadowsVariable()
    {
        var engine = CreateStandardEngine();
        engine.SetVariable("Math", (Func<int, int>)(x => x + 1));
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("Math(5)"));
        Assert.That(ex!.Message, Does.Contain("ModuleInfo"));
    }

    [Test]
    public void StandardMode_Range_ProducesSystemRange()
    {
        var result = CreateStandardEngine().Evaluate("1..10");
        Assert.That(result, Is.TypeOf<Range>());
    }

    [Test]
    public void StandardMode_Pipeline_Throws()
        => Assert.That(() => CreateStandardEngine().Evaluate("5 |> (x => x * 2)"),
            Throws.InstanceOf<CsEvalException>());

    [Test]
    public void StandardMode_SliceStep_Throws()
        => Assert.That(() => CreateStandardEngine().Evaluate("{ var a = new[] {1,2,3}; return a[::2]; }"),
            Throws.InstanceOf<CsEvalException>());

    [Test]
    public void StandardMode_RegexMatch_Throws()
        => Assert.That(() => CreateStandardEngine().Evaluate("\"hi\" =~ \"h\""),
            Throws.InstanceOf<CsEvalException>());

    [Test]
    public void StandardMode_RegexNotMatch_Throws()
        => Assert.That(() => CreateStandardEngine().Evaluate("\"hi\" !~ \"h\""),
            Throws.InstanceOf<CsEvalException>());

    [Test]
    public void StandardMode_Spaceship_Throws()
        => Assert.That(() => CreateStandardEngine().Evaluate("1 <=> 2"),
            Throws.InstanceOf<CsEvalException>());

[Test]
    public void StandardMode_Unless_Throws()
        => Assert.That(() => CreateStandardEngine().Evaluate("{ var x = 0; unless (false) { x = 1; } return x; }"),
            Throws.InstanceOf<CsEvalException>());

    [Test]
    public void StandardMode_Until_Throws()
        => Assert.That(() => CreateStandardEngine().Evaluate("{ var x = 0; until (true) { x = 1; } return x; }"),
            Throws.InstanceOf<CsEvalException>());

    #endregion
}
