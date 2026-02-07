// ECMA-334 §12.4.8 - Lifted Operator Compliance Tests
// Validates that CsEval handles all four categories of lifted operator semantics correctly:
//   1. Arithmetic/bitwise: null operand -> null result
//   2. Equality (==, !=): null-aware comparison
//   3. Relational (<, >, <=, >=): null operand -> false result
//   4. Three-value bool? logic (&, |, ^): ECMA-334 §12.13.5
// All tests validated against Roslyn for parity.

using CsEval.TestData.Data;

namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LiftedOperatorTests(CompilationMode mode)
{
    [TestCaseSource(typeof(LiftedOperatorData), nameof(LiftedOperatorData.ValueCases))]
    public async Task LiftedOperator_Value(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(LiftedOperatorData), nameof(LiftedOperatorData.ParityCases))]
    public async Task LiftedOperator_Parity(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);
}
