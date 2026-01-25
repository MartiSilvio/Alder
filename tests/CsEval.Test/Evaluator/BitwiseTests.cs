namespace CsEval.Test.Evaluator;

[TestFixture]
public class BitwiseTests : EvaluatorTestBase
{
    #region Bitwise AND

    [Test]
    public void Eval_BitwiseAnd_Basic()
    {
        Assert.That(Eval("5 & 3"), Is.EqualTo(1));     // 0101 & 0011 = 0001
        Assert.That(Eval("12 & 10"), Is.EqualTo(8));  // 1100 & 1010 = 1000
    }

    [Test]
    public void Eval_BitwiseAnd_WithZero()
    {
        Assert.That(Eval("255 & 0"), Is.EqualTo(0));
    }

    [Test]
    public void Eval_BitwiseAnd_AllOnes()
    {
        Assert.That(Eval("15 & 15"), Is.EqualTo(15)); // 1111 & 1111 = 1111
    }

    #endregion

    #region Bitwise OR

    [Test]
    public void Eval_BitwiseOr_Basic()
    {
        Assert.That(Eval("5 | 3"), Is.EqualTo(7));     // 0101 | 0011 = 0111
        Assert.That(Eval("12 | 10"), Is.EqualTo(14)); // 1100 | 1010 = 1110
    }

    [Test]
    public void Eval_BitwiseOr_WithZero()
    {
        Assert.That(Eval("255 | 0"), Is.EqualTo(255));
    }

    [Test]
    public void Eval_BitwiseOr_Combine()
    {
        Assert.That(Eval("1 | 2 | 4"), Is.EqualTo(7)); // 001 | 010 | 100 = 111
    }

    #endregion

    #region Bitwise XOR

    [Test]
    public void Eval_BitwiseXor_Basic()
    {
        Assert.That(Eval("5 ^ 3"), Is.EqualTo(6));     // 0101 ^ 0011 = 0110
        Assert.That(Eval("12 ^ 10"), Is.EqualTo(6));  // 1100 ^ 1010 = 0110
    }

    [Test]
    public void Eval_BitwiseXor_SameValue()
    {
        Assert.That(Eval("42 ^ 42"), Is.EqualTo(0));  // Same values XOR to 0
    }

    [Test]
    public void Eval_BitwiseXor_Toggle()
    {
        Assert.That(Eval("15 ^ 255"), Is.EqualTo(240)); // Toggle bits
    }

    #endregion

    #region Bitwise NOT

    [Test]
    public void Eval_BitwiseNot_Basic()
    {
        Assert.That(Eval("~0"), Is.EqualTo(-1L));
        Assert.That(Eval("~1"), Is.EqualTo(-2L));
    }

    [Test]
    public void Eval_BitwiseNot_NegativeOne()
    {
        Assert.That(Eval("~(-1)"), Is.EqualTo(0));
    }

    [Test]
    public void Eval_BitwiseNot_DoubleNot()
    {
        Assert.That(Eval("~~42"), Is.EqualTo(42));
    }

    #endregion

    #region Left Shift

    [Test]
    public void Eval_LeftShift_Basic()
    {
        Assert.That(Eval("1 << 1"), Is.EqualTo(2));
        Assert.That(Eval("1 << 2"), Is.EqualTo(4));
        Assert.That(Eval("1 << 3"), Is.EqualTo(8));
    }

    [Test]
    public void Eval_LeftShift_MultiplyByPowerOfTwo()
    {
        Assert.That(Eval("5 << 2"), Is.EqualTo(20));  // 5 * 4 = 20
        Assert.That(Eval("3 << 4"), Is.EqualTo(48)); // 3 * 16 = 48
    }

    [Test]
    public void Eval_LeftShift_Zero()
    {
        Assert.That(Eval("42 << 0"), Is.EqualTo(42));
        Assert.That(Eval("0 << 10"), Is.EqualTo(0));
    }

    #endregion

    #region Right Shift

    [Test]
    public void Eval_RightShift_Basic()
    {
        Assert.That(Eval("8 >> 1"), Is.EqualTo(4));
        Assert.That(Eval("8 >> 2"), Is.EqualTo(2));
        Assert.That(Eval("8 >> 3"), Is.EqualTo(1));
    }

    [Test]
    public void Eval_RightShift_DivideByPowerOfTwo()
    {
        Assert.That(Eval("20 >> 2"), Is.EqualTo(5));  // 20 / 4 = 5
        Assert.That(Eval("48 >> 4"), Is.EqualTo(3)); // 48 / 16 = 3
    }

    [Test]
    public void Eval_RightShift_Zero()
    {
        Assert.That(Eval("42 >> 0"), Is.EqualTo(42));
    }

    #endregion

    #region Precedence

    [Test]
    public void Eval_Bitwise_Precedence_AndBeforeOr()
    {
        // & has higher precedence than |
        Assert.That(Eval("1 | 2 & 3"), Is.EqualTo(3));  // 1 | (2 & 3) = 1 | 2 = 3
    }

    [Test]
    public void Eval_Bitwise_Precedence_ShiftBeforeComparison()
    {
        // << has higher precedence than <
        Assert.That(Eval("1 << 2 < 5"), Is.EqualTo(true));  // (1 << 2) < 5 = 4 < 5
    }

    [Test]
    public void Eval_Bitwise_Precedence_AndBeforeXor()
    {
        // & has higher precedence than ^
        Assert.That(Eval("7 ^ 3 & 5"), Is.EqualTo(6));  // 7 ^ (3 & 5) = 7 ^ 1 = 6
    }

    [Test]
    public void Eval_Bitwise_Precedence_XorBeforeOr()
    {
        // ^ has higher precedence than |
        Assert.That(Eval("1 | 2 ^ 3"), Is.EqualTo(1));  // 1 | (2 ^ 3) = 1 | 1 = 1
    }

    #endregion

    #region Combined with Other Operators

    [Test]
    public void Eval_Bitwise_WithArithmetic()
    {
        Assert.That(Eval("(2 + 3) & 7"), Is.EqualTo(5));
        Assert.That(Eval("10 | (3 * 2)"), Is.EqualTo(14)); // 1010 | 0110 = 1110
    }

    [Test]
    public void Eval_Bitwise_WithLogical()
    {
        // Bitwise should have higher precedence than logical
        Assert.That(Eval("(5 & 1) == 1 && true"), Is.EqualTo(true));
    }

    [Test]
    public void Eval_Bitwise_InTernary()
    {
        Assert.That(Eval("true ? 5 & 3 : 0"), Is.EqualTo(1));
    }

    #endregion

    #region Boolean AND (&) - Non-Short-Circuit

    [Test]
    public void Eval_BitwiseAnd_TrueAndTrue_ReturnsTrue()
    {
        Assert.That(Eval("true & true"), Is.EqualTo(true));
    }

    [Test]
    public void Eval_BitwiseAnd_TrueAndFalse_ReturnsFalse()
    {
        Assert.That(Eval("true & false"), Is.EqualTo(false));
    }

    [Test]
    public void Eval_BitwiseAnd_FalseAndFalse_ReturnsFalse()
    {
        Assert.That(Eval("false & false"), Is.EqualTo(false));
    }

    #endregion

    #region Boolean OR (|) - Non-Short-Circuit

    [Test]
    public void Eval_BitwiseOr_TrueOrFalse_ReturnsTrue()
    {
        Assert.That(Eval("true | false"), Is.EqualTo(true));
    }

    [Test]
    public void Eval_BitwiseOr_FalseOrTrue_ReturnsTrue()
    {
        Assert.That(Eval("false | true"), Is.EqualTo(true));
    }

    [Test]
    public void Eval_BitwiseOr_FalseOrFalse_ReturnsFalse()
    {
        Assert.That(Eval("false | false"), Is.EqualTo(false));
    }

    #endregion

    #region Boolean XOR (^)

    [Test]
    public void Eval_BitwiseXor_TrueXorTrue_ReturnsFalse()
    {
        Assert.That(Eval("true ^ true"), Is.EqualTo(false));
    }

    [Test]
    public void Eval_BitwiseXor_TrueXorFalse_ReturnsTrue()
    {
        Assert.That(Eval("true ^ false"), Is.EqualTo(true));
    }

    [Test]
    public void Eval_BitwiseXor_FalseXorFalse_ReturnsFalse()
    {
        Assert.That(Eval("false ^ false"), Is.EqualTo(false));
    }

    #endregion

    #region IL Compiled Boolean Bitwise

    [Test]
    public void ILCompiled_BitwiseAnd_Booleans()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("true & false");
        
        Assert.That(expr.TryCompile(), Is.True, "Boolean & should be IL-compilable");
        Assert.That(engine.Evaluate(expr), Is.EqualTo(false));
    }

    [Test]
    public void ILCompiled_BitwiseOr_Booleans()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("false | true");
        
        Assert.That(expr.TryCompile(), Is.True);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(true));
    }

    [Test]
    public void ILCompiled_BitwiseXor_Booleans()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("true ^ true");
        
        Assert.That(expr.TryCompile(), Is.True);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(false));
    }

    #endregion
}
