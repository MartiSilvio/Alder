using Alder.Test._Infrastructure;

namespace Alder.Test.Verification;

/// <summary>
/// Verification: Section 2 — Compilation Path Parity.
/// Each test evaluates an expression in both interpreted and compiled mode
/// and asserts identical results (same value, same type, same exception if thrown).
/// </summary>
[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
[Parallelizable(ParallelScope.Children)]
public class CompilationParityVerificationTests(CompilationMode mode)
{
    private AlderEngine CreateEngine(Action<AlderOptions>? configure = null)
        => TestEngineFactory.Create(mode, configure);

    // bool<->arithmetic casts must be rejected in both pipelines.
    //
    // The CastEmitter fast path (CastEmitter.cs:16) includes bool as a valid source/target for
    // LinqExpression.Convert. CLR Convert(bool, int) succeeds (true->1), but the interpreter calls
    // TypeHelpers.ExplicitCast -> RuntimeCast, which excludes bool from IsNumericOrCharType
    // (TypeCode.Boolean=3 < TypeCode.SByte=5), and throws NoExplicitConversion.
    //
    // The interpreter is correct per ECMA-334 §10.3.2: C# defines no explicit conversions
    // between bool and numeric types. (int)true is CS0030 in Roslyn.
    //
    // Fix: Remove `|| sourceType == typeof(bool)` and `|| targetType == typeof(bool)` from
    // CastEmitter.cs fast-path guard. Let bool casts fall through to ExplicitCastMethod slow path.
    [Test]
    public void Cast_BoolToInt_ShouldThrow()
    {
        var engine = CreateEngine();
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("return (int)true;"));
        Assert.That(ex, Is.Not.Null, "Both paths should throw for (int)true per ECMA-334 §10.3.2");
    }

    [Test]
    public void Cast_IntToBool_ShouldThrow()
    {
        var engine = CreateEngine();
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("return (bool)1;"));
        Assert.That(ex, Is.Not.Null, "Both paths should throw for (bool)1 per ECMA-334 §10.3.2");
    }

    // NaN arithmetic must return NaN, not bool, in interpreted path.
    //
    // The interpreter's promoted-type fast path (BinaryEvaluator.cs:54-55) has a NaN early-exit:
    //   if (IsNaN(left) || IsNaN(right))
    //       return binary.Operator == TokenType.BangEqual ? BoxedConstants.True : BoxedConstants.False;
    //
    // This returns false (bool) for ALL operators including +, -, *, /. The NaN check is correct
    // for comparison operators (NaN is unordered), but completely wrong for arithmetic:
    // float.NaN + float.NaN should be float.NaN, not false.
    //
    // This is hit when the promoted type is not int or double (the two with specialized inline paths
    // at lines 48-51). The most common trigger is float operations with NaN.
    //
    // The compiled path correctly converts both operands and emits the CLR operation, which produces NaN.
    //
    // Fix: Restrict the NaN early-exit to comparison/equality operators only:
    //   if ((IsNaN(left) || IsNaN(right)) &&
    //       binary.Operator is TokenType.EqualEqual or TokenType.BangEqual or
    //       TokenType.Less or TokenType.LessEqual or TokenType.Greater or TokenType.GreaterEqual)
    // Apply same fix to EvaluateBinarySingleAsync (same code at lines 208-211).
    [Test]
    public void NaN_FloatAddition_ShouldBeNaN()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("return float.NaN + float.NaN;");
        Assert.That(result, Is.InstanceOf<float>(), "Result type should be float, not bool");
        Assert.That(float.IsNaN((float)result!), Is.True, "float.NaN + float.NaN should be float.NaN");
    }

    [Test]
    public void NaN_FloatMultiplication_ShouldBeNaN()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("return float.NaN * 1.0f;");
        Assert.That(result, Is.InstanceOf<float>(), "Result type should be float, not bool");
        Assert.That(float.IsNaN((float)result!), Is.True, "float.NaN * 1.0f should be float.NaN");
    }

    [Test]
    public void NaN_MixedFloatDouble_Addition_ShouldBeNaN()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("return float.NaN + 1.0;");
        Assert.That(result, Is.InstanceOf<double>(), "Result type should be double (float promoted to double)");
        Assert.That(double.IsNaN((double)result!), Is.True);
    }

    [Test]
    public void NaN_FloatComparison_ShouldBeFalse()
    {
        var engine = CreateEngine();
        // NaN comparisons should still return false (bool) — this is correct behavior
        var result = engine.Evaluate("return float.NaN == float.NaN;");
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void NaN_FloatNotEqual_ShouldBeTrue()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("return float.NaN != float.NaN;");
        Assert.That(result, Is.EqualTo(true));
    }

    // --- Standard parity tests (these should pass in both modes) ---

    [Test]
    public void Parity_NullableArithmetic_NullPlusInt()
    {
        var engine = CreateEngine();
        engine.SetVariable<int?>("x", null);
        var result = engine.Evaluate("return x + 1;");
        Assert.That(result, Is.Null, "null + 1 should be null per lifted operators §12.4.8");
    }

    [Test]
    public void Parity_NullableArithmetic_NullMultiply()
    {
        var engine = CreateEngine();
        engine.SetVariable<int?>("x", 5);
        engine.SetVariable<int?>("y", null);
        var result = engine.Evaluate("return x * y;");
        Assert.That(result, Is.Null, "5 * null should be null per lifted operators §12.4.8");
    }

    [Test]
    public void Parity_MixedNumericWidening()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("return 1 + 2L + 3.0f + 4.0;");
        Assert.That(result, Is.InstanceOf<double>(), "Promotion chain int->long->float->double");
        Assert.That(result, Is.EqualTo(10.0));
    }

    [Test]
    public void Parity_NullConditionalChain()
    {
        var engine = CreateEngine();
        engine.SetVariable<string?>("s", null);
        var result = engine.Evaluate("return s?.Trim()?.ToUpper()?.Length;");
        Assert.That(result, Is.Null, "Null-conditional chain on null should short-circuit to null");
    }

    [Test]
    public void Parity_StringInterpolation_FormatSpecifier()
    {
        var engine = CreateEngine();
        engine.SetVariable("pi", 3.14159);
        var result = engine.Evaluate("""return $"{pi:F2}";""");
        Assert.That(result, Is.EqualTo("3.14"));
    }

    [Test]
    public void Parity_AsCast_ReturnsNull()
    {
        var engine = CreateEngine();
        engine.SetVariable<object>("x", "hello");
        var result = engine.Evaluate("return x as int?;");
        Assert.That(result, Is.Null, "string as int? should be null");
    }

    [Test]
    public void Parity_IsCheck_ReturnsFalse()
    {
        var engine = CreateEngine();
        engine.SetVariable<object>("x", 42);
        var result = engine.Evaluate("return x is string;");
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void Parity_Ternary_DifferentArmTypes()
    {
        var engine = CreateEngine();
        engine.SetVariable("b", true);
        var resultTrue = engine.Evaluate("return b ? 1 : 2.0;");
        Assert.That(resultTrue, Is.InstanceOf<double>(), "int arm should be widened to double");
        Assert.That(resultTrue, Is.EqualTo(1.0));

        engine.SetVariable("b", false);
        var resultFalse = engine.Evaluate("return b ? 1 : 2.0;");
        Assert.That(resultFalse, Is.EqualTo(2.0));
    }

    [Test]
    public void Parity_LambdaCapture_LoopVariable()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("""
            var funcs = new List<Func<int>>();
            for (int i = 0; i < 3; i++)
            {
                int captured = i;
                funcs.Add(() => captured);
            }
            return funcs[1]();
        """);
        Assert.That(result, Is.EqualTo(1), "Classic closure-over-loop-variable: captured copy should be 1");
    }

    [Test]
    public void Parity_NestedNullConditional_ArrayNull()
    {
        var engine = CreateEngine();
        engine.SetVariable<string[]?>("arr", null);
        var result = engine.Evaluate("return arr?.FirstOrDefault()?.ToUpper();");
        Assert.That(result, Is.Null, "null array should short-circuit without throwing");
    }
}
