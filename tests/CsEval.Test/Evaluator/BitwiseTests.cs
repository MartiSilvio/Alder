using NUnit.Framework;

namespace CsEval.Test.Evaluator;

[TestFixture]
public class BitwiseTests : EvaluatorTestBase
{
    #region Bitwise AND

    [Test]
    public void Eval_BitwiseAnd_Basic()
    {
        Assert.That(Eval("5 & 3"), Is.EqualTo(1L));     // 0101 & 0011 = 0001
        Assert.That(Eval("12 & 10"), Is.EqualTo(8L));  // 1100 & 1010 = 1000
    }

    [Test]
    public void Eval_BitwiseAnd_WithZero()
    {
        Assert.That(Eval("255 & 0"), Is.EqualTo(0L));
    }

    [Test]
    public void Eval_BitwiseAnd_AllOnes()
    {
        Assert.That(Eval("15 & 15"), Is.EqualTo(15L)); // 1111 & 1111 = 1111
    }

    #endregion

    #region Bitwise OR

    [Test]
    public void Eval_BitwiseOr_Basic()
    {
        Assert.That(Eval("5 | 3"), Is.EqualTo(7L));     // 0101 | 0011 = 0111
        Assert.That(Eval("12 | 10"), Is.EqualTo(14L)); // 1100 | 1010 = 1110
    }

    [Test]
    public void Eval_BitwiseOr_WithZero()
    {
        Assert.That(Eval("255 | 0"), Is.EqualTo(255L));
    }

    [Test]
    public void Eval_BitwiseOr_Combine()
    {
        Assert.That(Eval("1 | 2 | 4"), Is.EqualTo(7L)); // 001 | 010 | 100 = 111
    }

    #endregion

    #region Bitwise XOR

    [Test]
    public void Eval_BitwiseXor_Basic()
    {
        Assert.That(Eval("5 ^ 3"), Is.EqualTo(6L));     // 0101 ^ 0011 = 0110
        Assert.That(Eval("12 ^ 10"), Is.EqualTo(6L));  // 1100 ^ 1010 = 0110
    }

    [Test]
    public void Eval_BitwiseXor_SameValue()
    {
        Assert.That(Eval("42 ^ 42"), Is.EqualTo(0L));  // Same values XOR to 0
    }

    [Test]
    public void Eval_BitwiseXor_Toggle()
    {
        Assert.That(Eval("15 ^ 255"), Is.EqualTo(240L)); // Toggle bits
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
        Assert.That(Eval("~(-1)"), Is.EqualTo(0L));
    }

    [Test]
    public void Eval_BitwiseNot_DoubleNot()
    {
        Assert.That(Eval("~~42"), Is.EqualTo(42L));
    }

    #endregion

    #region Left Shift

    [Test]
    public void Eval_LeftShift_Basic()
    {
        Assert.That(Eval("1 << 1"), Is.EqualTo(2L));
        Assert.That(Eval("1 << 2"), Is.EqualTo(4L));
        Assert.That(Eval("1 << 3"), Is.EqualTo(8L));
    }

    [Test]
    public void Eval_LeftShift_MultiplyByPowerOfTwo()
    {
        Assert.That(Eval("5 << 2"), Is.EqualTo(20L));  // 5 * 4 = 20
        Assert.That(Eval("3 << 4"), Is.EqualTo(48L)); // 3 * 16 = 48
    }

    [Test]
    public void Eval_LeftShift_Zero()
    {
        Assert.That(Eval("42 << 0"), Is.EqualTo(42L));
        Assert.That(Eval("0 << 10"), Is.EqualTo(0L));
    }

    #endregion

    #region Right Shift

    [Test]
    public void Eval_RightShift_Basic()
    {
        Assert.That(Eval("8 >> 1"), Is.EqualTo(4L));
        Assert.That(Eval("8 >> 2"), Is.EqualTo(2L));
        Assert.That(Eval("8 >> 3"), Is.EqualTo(1L));
    }

    [Test]
    public void Eval_RightShift_DivideByPowerOfTwo()
    {
        Assert.That(Eval("20 >> 2"), Is.EqualTo(5L));  // 20 / 4 = 5
        Assert.That(Eval("48 >> 4"), Is.EqualTo(3L)); // 48 / 16 = 3
    }

    [Test]
    public void Eval_RightShift_Zero()
    {
        Assert.That(Eval("42 >> 0"), Is.EqualTo(42L));
    }

    #endregion

    #region Precedence

    [Test]
    public void Eval_Bitwise_Precedence_AndBeforeOr()
    {
        // & has higher precedence than |
        Assert.That(Eval("1 | 2 & 3"), Is.EqualTo(3L));  // 1 | (2 & 3) = 1 | 2 = 3
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
        Assert.That(Eval("7 ^ 3 & 5"), Is.EqualTo(6L));  // 7 ^ (3 & 5) = 7 ^ 1 = 6
    }

    [Test]
    public void Eval_Bitwise_Precedence_XorBeforeOr()
    {
        // ^ has higher precedence than |
        Assert.That(Eval("1 | 2 ^ 3"), Is.EqualTo(1L));  // 1 | (2 ^ 3) = 1 | 1 = 1
    }

    #endregion

    #region Combined with Other Operators

    [Test]
    public void Eval_Bitwise_WithArithmetic()
    {
        Assert.That(Eval("(2 + 3) & 7"), Is.EqualTo(5L));
        Assert.That(Eval("10 | (3 * 2)"), Is.EqualTo(14L)); // 1010 | 0110 = 1110
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
        Assert.That(Eval("true ? 5 & 3 : 0"), Is.EqualTo(1L));
    }

    #endregion
}
