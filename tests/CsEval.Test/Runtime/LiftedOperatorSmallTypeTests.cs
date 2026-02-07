// ECMA-334 §12.4.8 - Lifted Operator Compliance Tests for Small and Unsigned Types
// Validates lifted operator semantics for nullable types that undergo numeric promotion.
// byte?, sbyte?, short?, ushort? promote to int for arithmetic (ECMA-334 §12.4.7).
// uint?, ulong? have their own predefined operators.
// All tests validated against Roslyn for parity.

using CsEval.TestData.Data;

namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LiftedOperatorSmallTypeTests(CompilationMode mode)
{
    [TestCaseSource(typeof(LiftedOperatorSmallTypeData), nameof(LiftedOperatorSmallTypeData.ValueCases))]
    public async Task LiftedOperatorSmallType_Value(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(LiftedOperatorSmallTypeData), nameof(LiftedOperatorSmallTypeData.ParityCases))]
    public async Task LiftedOperatorSmallType_Parity(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);
}
