using Alder.Test._Infrastructure;

namespace Alder.Test.Compliance;

/// <summary>
/// Parity tests for runtime semantics: member access (ECMA-334 S12.8.7),
/// type resolution (S7.8), assignment (S12.21), construction (S12.8.17),
/// and pattern matching (S11.2).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class RuntimeSemanticsParityTests(CompilationMode mode)
{
    #region Member Access (S12.8.7)

    [Test]
    public async Task MemberAccess_StringLength_Parity()
    {
        await AssertParityAsync("""
            "hello world".Length
            """);
    }

    [Test]
    public async Task MemberAccess_ChainedPropertyAccess_Parity()
    {
        await AssertParityAsync("""
            "hello".Length + "world".Length
            """);
    }

    [Test]
    public async Task MemberAccess_StaticField_DoubleNaN_Parity()
    {
        await AssertParityAsync("double.NaN != double.NaN");
    }

    [Test]
    public async Task MemberAccess_StaticProperty_IntMaxValue_Parity()
    {
        await AssertParityAsync("int.MaxValue");
    }

    [Test]
    public async Task MemberAccess_NullConditional_OnNull_Parity()
    {
        await AssertParityAsync("{ string? s = null; return s?.Length; }");
    }

    [Test]
    public async Task MemberAccess_NullConditional_OnNonNull_Parity()
    {
        await AssertParityAsync("""
            "hello"?.Length
            """);
    }

    [Test]
    public async Task MemberAccess_ArrayLength_Parity()
    {
        await AssertParityAsync("new int[] { 1, 2, 3 }.Length");
    }

    [Test]
    public async Task MemberAccess_MethodCall_Parity()
    {
        await AssertParityAsync("""
            "hello world".Substring(0, 5)
            """);
    }

    #endregion

    #region Type Resolution (S7.8)

    [Test]
    public async Task TypeResolution_BuiltInAlias_Int_Parity()
    {
        await AssertParityAsync("typeof(int) == typeof(System.Int32)");
    }

    [Test]
    public async Task TypeResolution_GenericType_ListOfInt_Parity()
    {
        await AssertParityAsync("new List<int> { 1, 2, 3 }.Count");
    }

    [Test]
    public async Task TypeResolution_ArrayType_Parity()
    {
        await AssertParityAsync("new int[3].Length");
    }

    [Test]
    public async Task TypeResolution_NullableValueType_Parity()
    {
        await AssertParityAsync("{ int? x = null; return x ?? -1; }");
    }

    [Test]
    public async Task TypeResolution_FullyQualified_Parity()
    {
        await AssertParityAsync("System.Math.PI > 3.14");
    }

    #endregion

    #region Assignment Semantics (S12.21)

    [Test]
    public async Task Assignment_Simple_Parity()
    {
        await AssertParityAsync("{ var x = 10; x = 20; return x; }");
    }

    [Test]
    public async Task Assignment_CompoundAdd_Parity()
    {
        await AssertParityAsync("{ var x = 10; x += 5; return x; }");
    }

    [Test]
    public async Task Assignment_CompoundSubtract_Parity()
    {
        await AssertParityAsync("{ var x = 10; x -= 3; return x; }");
    }

    [Test]
    public async Task Assignment_CompoundMultiply_Parity()
    {
        await AssertParityAsync("{ var x = 10; x *= 2; return x; }");
    }

    [Test]
    public async Task Assignment_CompoundDivide_Parity()
    {
        await AssertParityAsync("{ var x = 10; x /= 3; return x; }");
    }

    [Test]
    public async Task Assignment_CompoundModulo_Parity()
    {
        await AssertParityAsync("{ var x = 10; x %= 3; return x; }");
    }

    [Test]
    public async Task Assignment_CompoundBitwiseAnd_Parity()
    {
        await AssertParityAsync("{ var x = 0xFF; x &= 0x0F; return x; }");
    }

    [Test]
    public async Task Assignment_CompoundBitwiseOr_Parity()
    {
        await AssertParityAsync("{ var x = 0x0F; x |= 0xF0; return x; }");
    }

    [Test]
    public async Task Assignment_CompoundBitwiseXor_Parity()
    {
        await AssertParityAsync("{ var x = 0xFF; x ^= 0x0F; return x; }");
    }

    [Test]
    public async Task Assignment_CompoundLeftShift_Parity()
    {
        await AssertParityAsync("{ var x = 1; x <<= 4; return x; }");
    }

    [Test]
    public async Task Assignment_CompoundRightShift_Parity()
    {
        await AssertParityAsync("{ var x = 16; x >>= 2; return x; }");
    }

    [Test]
    public async Task Assignment_CompoundUnsignedRightShift_Parity()
    {
        await AssertParityAsync("{ var x = -1; x >>>= 28; return x; }");
    }

    [Test]
    public async Task Assignment_NullCoalescing_NullCase_Parity()
    {
        await AssertParityAsync("{ string? x = null; x ??= \"default\"; return x; }");
    }

    [Test]
    public async Task Assignment_NullCoalescing_NonNullCase_Parity()
    {
        await AssertParityAsync("""{ string? x = "original"; x ??= "default"; return x; }""");
    }

    [Test]
    public async Task Assignment_PrefixIncrement_Parity()
    {
        await AssertParityAsync("{ var x = 5; return ++x; }");
    }

    [Test]
    public async Task Assignment_PostfixIncrement_Parity()
    {
        await AssertParityAsync("{ var x = 5; return x++; }");
    }

    [Test]
    public async Task Assignment_PrefixDecrement_Parity()
    {
        await AssertParityAsync("{ var x = 5; return --x; }");
    }

    [Test]
    public async Task Assignment_PostfixDecrement_Parity()
    {
        await AssertParityAsync("{ var x = 5; return x--; }");
    }

    [Test]
    public async Task Assignment_IncrementAfterValue_Parity()
    {
        await AssertParityAsync("{ var x = 5; var y = x++; return x + y; }");
    }

    #endregion

    #region Construction (S12.8.17)

    [Test]
    public async Task Construction_NewObject_Parity()
    {
        await AssertParityAsync("new object() != null");
    }

    [Test]
    public async Task Construction_NewList_Parity()
    {
        await AssertParityAsync("new List<int> { 1, 2, 3 }.Count");
    }

    [Test]
    public async Task Construction_NewArray_SizedEmpty_Parity()
    {
        await AssertParityAsync("new int[5].Length");
    }

    [Test]
    public async Task Construction_NewArray_Initialized_Parity()
    {
        await AssertParityAsync("new int[] { 10, 20, 30 }[1]");
    }

    [Test]
    public async Task Construction_Tuple_Parity()
    {
        await AssertParityAsync("(1, 2, 3).Item2");
    }

    [Test]
    public async Task Construction_TupleDeconstruction_Parity()
    {
        await AssertParityAsync("{ var (a, b) = (10, 20); return a + b; }");
    }

    [Test]
    public async Task Construction_DictionaryInitializer_Parity()
    {
        await AssertParityAsync("""new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 }["b"]""");
    }

    #endregion

    #region Pattern Matching (S11.2)

    [Test]
    public async Task Pattern_ConstantPattern_Parity()
    {
        await AssertParityAsync("42 is 42");
    }

    [Test]
    public async Task Pattern_TypePattern_Parity()
    {
        await AssertParityAsync("{ object o = 42; return o is int i ? i * 2 : 0; }");
    }

    [Test]
    public async Task Pattern_VarPattern_Parity()
    {
        await AssertParityAsync("{ object o = 42; return o is var v ? (int)v + 1 : 0; }");
    }

    [Test]
    public async Task Pattern_SwitchDiscard_Parity()
    {
        await AssertParityAsync("""
            {
                object o = 42;
                return o switch
                {
                    int => "int",
                    _ => "other"
                };
            }
            """);
    }

    [Test]
    public async Task Pattern_RelationalPattern_Parity()
    {
        await AssertParityAsync("42 is > 10 and < 100");
    }

    [Test]
    public async Task Pattern_NotPattern_Parity()
    {
        await AssertParityAsync("{ object? o = 42; return o is not null; }");
    }

    [Test]
    public async Task Pattern_OrPattern_Parity()
    {
        await AssertParityAsync("42 is 41 or 42 or 43");
    }

    [Test]
    public async Task Pattern_PropertyPattern_Parity()
    {
        await AssertParityAsync("""
            "hello" is { Length: 5 }
            """);
    }

    [Test]
    public async Task Pattern_NestedCombinators_Parity()
    {
        await AssertParityAsync("42 is > 0 and not 100");
    }

    [Test]
    public async Task Pattern_TypePatternWithWhenGuard_Switch_Parity()
    {
        await AssertParityAsync("""
            {
                object val = 42;
                return val switch
                {
                    int n when n > 50 => "big",
                    int n when n > 0 => "small",
                    _ => "other"
                };
            }
            """);
    }

    #endregion

    #region Identifier Resolution

    [Test]
    public async Task Identifier_LocalVariable_Parity()
    {
        await AssertParityAsync("{ var x = 42; var y = x + 8; return y; }");
    }

    [Test]
    public async Task Identifier_TypeUsedAfterVariable_Parity()
    {
        await AssertParityAsync("System.Math.Max(1, 2)");
    }

    #endregion

    #region Execution Constraints

    [Test]
    public async Task Execution_ForLoop_Parity()
    {
        await AssertParityAsync("{ var sum = 0; for (var i = 0; i < 10; i++) { sum += i; } return sum; }");
    }

    [Test]
    public async Task Execution_WhileLoop_Parity()
    {
        await AssertParityAsync("{ var x = 10; while (x > 0) { x--; } return x; }");
    }

    [Test]
    public async Task Execution_Foreach_Parity()
    {
        await AssertParityAsync("{ var sum = 0; foreach (var n in new int[] { 1, 2, 3, 4, 5 }) { sum += n; } return sum; }");
    }

    #endregion

    private async Task AssertParityAsync(string expression)
    {
        var engine = TestEngineFactory.Create(mode, AlderOptions.Default);
        var alderResult = engine.Evaluate(expression);
        var roslynResult = await TestHelpers.EvaluateCSharpAsync(expression);

        Assert.That(alderResult, Is.EqualTo(roslynResult), $"Value mismatch for: {expression}");
        Assert.That(alderResult?.GetType(), Is.EqualTo(roslynResult?.GetType()), $"Type mismatch for: {expression}");
    }
}
