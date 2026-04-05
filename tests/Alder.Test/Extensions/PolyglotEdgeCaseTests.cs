using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Extensions;

/// <summary>
/// Edge cases, error handling, standard mode rejection, and side-effect tests
/// for polyglot extended features. Happy-path behavior is covered by .csx parity
/// tests in TestData/ValidExpressions/SyntaxSugar/.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class PolyglotEdgeCaseTests(CompilationMode mode)
{
    [Test]
    public void BareMath_UserVariableShadowsConstant()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        engine.SetVariable("pi", 3);
        Assert.That(engine.Evaluate("pi"), Is.EqualTo(3));
    }

    [Test]
    public void BareMath_UserFunctionShadowsBuiltIn()
    {
        var engine = TestEngineFactory.Create(mode, o => {
            o.LanguageMode = LanguageMode.Extended;
            o.Functions.Register("sin", args => 42);
        });
        Assert.That(engine.Evaluate("sin(5.0)"), Is.EqualTo(42));
    }

    [Test]
    public void BareMath_ModuleNameDoesNotShadowBuiltInFunction()
    {
        var engine = TestEngineFactory.Create(mode, o => {
            o.LanguageMode = LanguageMode.Extended;
            o.Modules.Register("sin", typeof(Math));
        });
        Assert.That(engine.Evaluate("sin(0.0)"), Is.EqualTo(0.0).Within(1e-12));
    }

    [Test]
    public void BareMath_ShadowDoesNotLeakToNewEngine()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        engine.SetVariable("pi", 3);
        Assert.That(engine.Evaluate("pi"), Is.EqualTo(3));

        var engine2 = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine2.Evaluate("pi"), Is.EqualTo(Math.PI));
    }

    [Test]
    public void IdentifierCall_FunctionShadowsVariable()
    {
        var engine = TestEngineFactory.Create(mode, o => {
            o.LanguageMode = LanguageMode.Extended;
            o.Functions.Register("inc", args => Convert.ToInt32(args[0]) + 1);
        });
        engine.SetVariable("inc", (Func<int, int>)(x => x + 100));
        Assert.That(engine.Evaluate("inc(5)"), Is.EqualTo(6));
    }

    [Test]
    public void IdentifierCall_ModuleShadowsVariable()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        engine.SetVariable("Math", (Func<int, int>)(x => x + 1));
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("Math(5)"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1955));
        Assert.That(ex.Message, Does.Contain("ModuleInfo"));
    }



    [Test]
    public void Range_ReversedRange_ReturnsEmpty()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("(10..=1).Count()"), Is.EqualTo(0));
    }

    [Test]
    public void Range_SpreadStillWorks()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        var result = engine.Evaluate("{ var a = new[] {1, 2}; var b = new[] {3, 4}; return [..a, ..b]; }");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }



    [Test]
    public void Pipeline_NonCallableRight_Throws()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(() => engine.Evaluate("5 |> 10"),
            Throws.InstanceOf<AlderException>().With.Message.Contains("|>"));
    }

    [Test]
    public void Pipeline_NullRight_Throws()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(() => engine.Evaluate("5 |> null"),
            Throws.InstanceOf<AlderException>().With.Message.Contains("|>"));
    }

    [Test]
    public void Pipeline_NullLeft_PassesNullToFunction()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("null |> (x => x == null)"), Is.True);
    }

    [Test]
    public void Pipeline_NonCallableIdentifier_Throws()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        engine.SetVariable("f", 10);
        Assert.That(() => engine.Evaluate("5 |> f"),
            Throws.InstanceOf<AlderException>());
    }

    [Test]
    public void Pipeline_IdentifierCall_MatchesDirectCall()
    {
        var engine = TestEngineFactory.Create(mode, o => {
            o.LanguageMode = LanguageMode.Extended;
            o.Functions.Register("inc", args => Convert.ToInt32(args[0]) + 1);
        });
        Assert.That(engine.Evaluate("5 |> inc"), Is.EqualTo(engine.Evaluate("inc(5)")));
    }

    [Test]
    public void Pipeline_Precedence_ArithmeticBindsTighter()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("2 + 3 |> (x => x * 2)"), Is.EqualTo(10));
    }



    [Test]
    public void SliceStep_ZeroStep_Throws()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(() => engine.Evaluate("{ var arr = new[] {1, 2, 3}; return arr[::0]; }"),
            Throws.InstanceOf<AlderException>());
    }

    [Test]
    public void SliceStep_TwoArgSlice_Unchanged()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        var result = engine.Evaluate("{ var arr = new[] {10, 20, 30, 40, 50}; return arr[1:4]; }");
        Assert.That(result, Is.EqualTo(new[] { 20, 30, 40 }));
    }



    [Test]
    public void LikeOperator_UnderscoreMatchesSingleCharacter()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate(""" "abc" like "a_c" """), Is.True);
        Assert.That(engine.Evaluate(""" "ac" like "a_c" """), Is.False);
    }

    [Test]
    public void LikeOperator_MultiPercentPattern_Matches()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate(""" "abXXcdYYef" like "ab%cd%ef" """), Is.True);
    }

    [Test]
    public void LikeOperator_RegexMetacharactersStayLiteral()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate(""" "a.c" like "a.c" """), Is.True);
        Assert.That(engine.Evaluate(""" "abc" like "a.c" """), Is.False);
    }

    [Test]
    public void LikeOperator_NullLeftOperand_ThrowsConsistentException()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        engine.SetVariable<string>("text", null!);

        Assert.That(() => engine.Evaluate("""text like "a%" """),
            Throws.InstanceOf<AlderException>().With.Message.Contains("like"));
    }



    [Test]
    public void InOperator_NullCollectionVariable_ThrowsConsistentException()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        engine.SetVariable<List<int>>("numbers", null!);

        Assert.That(() => engine.Evaluate("1 in numbers"),
            Throws.InstanceOf<AlderException>().With.Message.Contains("in"));
    }

    [Test]
    public void NotInAlias_NullCollectionVariable_ThrowsConsistentException()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        engine.SetVariable<List<int>>("numbers", null!);

        Assert.That(() => engine.Evaluate("1 not in numbers"),
            Throws.InstanceOf<AlderException>().With.Message.Contains("in"));
    }



    [Test]
    public void Regex_LeftNull_Throws()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(() => engine.Evaluate("""null =~ "pattern" """),
            Throws.InstanceOf<AlderException>());
    }

    [Test]
    public void Regex_RightNull_Throws()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(() => engine.Evaluate(""" "text" =~ null"""),
            Throws.InstanceOf<AlderException>());
    }



    [Test]
    public void Spaceship_BothNull_ReturnsZero()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("null <=> null"), Is.EqualTo(0));
    }

    [Test]
    public void Spaceship_LeftNull_ReturnsNegativeOne()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("null <=> 5"), Is.EqualTo(-1));
    }

    [Test]
    public void Spaceship_RightNull_ReturnsOne()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("5 <=> null"), Is.EqualTo(1));
    }



    [Test]
    public void ChainedComparison_MiddleOperandEvaluatedOnce()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
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
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
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
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        engine.SetVariable("a", 5);
        engine.SetVariable("b", 5);
        Assert.That(engine.Evaluate("a == b == 5"), Is.True);
    }

    [Test]
    public void ChainedComparison_WithNaN_ReturnsFalse()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("0.0 < double.NaN < 10.0"), Is.False);
    }



    [Test]
    public void Deconstruction_NoDeconstructMethod_Throws()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(() => engine.Evaluate("{ var (a, b) = 42; return a; }"),
            Throws.InstanceOf<AlderException>());
    }



    [Test]
    public void ExtendedMode_And_WorksAsGeneralBooleanOperator()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("true and true"), Is.True);
        Assert.That(engine.Evaluate("true and false"), Is.False);
    }

    [Test]
    public void ExtendedMode_Or_WorksAsGeneralBooleanOperator()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("false or true"), Is.True);
        Assert.That(engine.Evaluate("false or false"), Is.False);
    }

    [Test]
    public void ExtendedMode_Not_WorksAsGeneralBooleanOperator()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("not true"), Is.False);
        Assert.That(engine.Evaluate("not false"), Is.True);
    }

    [Test]
    public void StandardMode_And_InExpression_Throws()
        => Assert.That(() => TestEngineFactory.Create(mode).Evaluate("true and false"),
            Throws.InstanceOf<AlderException>());

    [Test]
    public void StandardMode_Or_InExpression_Throws()
        => Assert.That(() => TestEngineFactory.Create(mode).Evaluate("false or true"),
            Throws.InstanceOf<AlderException>());

    [Test]
    public void StandardMode_Not_InExpression_Throws()
        => Assert.That(() => TestEngineFactory.Create(mode).Evaluate("not true"),
            Throws.InstanceOf<AlderException>());

    [Test]
    public void StandardMode_And_InPattern_StillWorks()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", 5);
        Assert.That(engine.Evaluate("x is > 0 and < 10"), Is.True);
    }

    [Test]
    public void StandardMode_Or_InPattern_StillWorks()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", 5);
        Assert.That(engine.Evaluate("x is 5 or 10"), Is.True);
        Assert.That(engine.Evaluate("x is 3 or 5"), Is.True);
        Assert.That(engine.Evaluate("x is 3 or 4"), Is.False);
    }

    [Test]
    public void StandardMode_Not_InPattern_StillWorks()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", 5);
        Assert.That(engine.Evaluate("x is not null"), Is.True);
    }

    [Test]
    public void StandardMode_SymbolicAndOr_StillWork()
    {
        var engine = TestEngineFactory.Create(mode);
        Assert.That(engine.Evaluate("true && true"), Is.True);
        Assert.That(engine.Evaluate("false || true"), Is.True);
        Assert.That(engine.Evaluate("!false"), Is.True);
    }



    [Test]
    public void StrictEquality_SameTypeSameValue_ReturnsTrue()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("1 === 1"), Is.True);
    }

    [Test]
    public void StrictEquality_DifferentNumericTypes_ReturnsFalse()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("1 === 1L"), Is.False);       // int vs long
        Assert.That(engine.Evaluate("1 === 1.0"), Is.False);      // int vs double
        Assert.That(engine.Evaluate("1.0f === 1.0"), Is.False);   // float vs double
    }

    [Test]
    public void StrictEquality_Strings_ReturnsTrue()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate(""" "abc" === "abc" """), Is.True);
    }

    [Test]
    public void StrictEquality_NullBothSides_ReturnsTrue()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("null === null"), Is.True);
    }

    [Test]
    public void StrictEquality_NullVsValue_ReturnsFalse()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("null === 0"), Is.False);
        Assert.That(engine.Evaluate("0 === null"), Is.False);
    }

    [Test]
    public void StrictEquality_BoolVsInt_ReturnsFalse()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("true === 1"), Is.False);
    }

    [Test]
    public void StrictInequality_DifferentNumericTypes_ReturnsTrue()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("1 !== 1L"), Is.True);
    }

    [Test]
    public void StrictInequality_SameTypeSameValue_ReturnsFalse()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("1 !== 1"), Is.False);
    }

    [Test]
    public void StrictEquality_NaN_ReturnsFalse()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("double.NaN === double.NaN"), Is.False);
    }

    [Test]
    public void RegularEquality_StillWidens()
    {
        var engine = TestEngineFactory.Create(mode, LanguageMode.Extended);
        Assert.That(engine.Evaluate("1 == 1L"), Is.True);
    }



    [Test]
    public void StandardMode_BareMathSin_Throws()
        => Assert.That(() => TestEngineFactory.Create(mode).Evaluate("sin(1.0)"),
            Throws.InstanceOf<AlderException>());

    [Test]
    public void StandardMode_BareMathPi_Throws()
        => Assert.That(() => TestEngineFactory.Create(mode).Evaluate("pi"),
            Throws.InstanceOf<AlderException>());

    [Test]
    public void StandardMode_IdentifierCall_FunctionShadowsVariable()
    {
        var engine = TestEngineFactory.Create(mode, o => {
            o.LanguageMode = LanguageMode.Standard;
            o.Functions.Register("inc", args => Convert.ToInt32(args[0]) + 1);
        });
        engine.SetVariable("inc", (Func<int, int>)(x => x + 100));
        Assert.That(engine.Evaluate("inc(5)"), Is.EqualTo(6));
    }

    [Test]
    public void StandardMode_IdentifierCall_ModuleShadowsVariable()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("Math", (Func<int, int>)(x => x + 1));
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("Math(5)"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1955));
        Assert.That(ex.Message, Does.Contain("ModuleInfo"));
    }


    [Test]
    public void StandardMode_Range_ProducesSystemRange()
    {
        var result = TestEngineFactory.Create(mode).Evaluate("1..10");
        Assert.That(result, Is.TypeOf<Range>());
    }

    [Test]
    public void StandardMode_Pipeline_Throws()
        => Assert.That(() => TestEngineFactory.Create(mode).Evaluate("5 |> (x => x * 2)"),
            Throws.InstanceOf<AlderException>());

    [Test]
    public void StandardMode_SliceStep_Throws()
        => Assert.That(() => TestEngineFactory.Create(mode).Evaluate("{ var a = new[] {1,2,3}; return a[::2]; }"),
            Throws.InstanceOf<AlderException>());

    [Test]
    public void StandardMode_RegexMatch_Throws()
        => Assert.That(() => TestEngineFactory.Create(mode).Evaluate(""" "hi" =~ "h" """),
            Throws.InstanceOf<AlderException>());

    [Test]
    public void StandardMode_RegexNotMatch_Throws()
        => Assert.That(() => TestEngineFactory.Create(mode).Evaluate(""" "hi" !~ "h" """),
            Throws.InstanceOf<AlderException>());

    [Test]
    public void StandardMode_Spaceship_Throws()
        => Assert.That(() => TestEngineFactory.Create(mode).Evaluate("1 <=> 2"),
            Throws.InstanceOf<AlderException>());

[Test]
    public void StandardMode_Unless_Throws()
        => Assert.That(() => TestEngineFactory.Create(mode).Evaluate("{ var x = 0; unless (false) { x = 1; } return x; }"),
            Throws.InstanceOf<AlderException>());

    [Test]
    public void StandardMode_Until_Throws()
        => Assert.That(() => TestEngineFactory.Create(mode).Evaluate("{ var x = 0; until (true) { x = 1; } return x; }"),
            Throws.InstanceOf<AlderException>());

}
