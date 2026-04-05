using Alder.Test._Infrastructure;

namespace Alder.Test.Compliance;

public enum LongEnum : long { A = 1, B = 2, C = 3 }

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class OperatorConformanceTests(CompilationMode mode)
{
    private AlderEngine CreateEngine(LanguageMode lang = LanguageMode.Standard, HashSet<Type>? allowedTypes = null)
        => TestEngineFactory.Create(mode, o =>
        {
            o.LanguageMode = lang;
            if (allowedTypes != null)
                o.Sandbox = SandboxOptions.Trusted() with { TrustedTypes = allowedTypes };
        });

    #region §12.18 Conditional operator — type rules
    [Test]
    public void ConditionalOperator_IntAndLong_ProducesLong()
    {
        // If one arm is int and other is long, implicit conversion → long
        var result = CreateEngine().Evaluate(@"
            var x = true ? 1 : 2L;
            return x;
        ");
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void ConditionalOperator_IntAndDouble_ProducesDouble()
    {
        var result = CreateEngine().Evaluate(@"
            var x = true ? 1 : 2.0;
            return x;
        ");
        Assert.That(result, Is.TypeOf<double>());
    }

    #endregion

    #region §12.9.3 Unary minus — edge cases
    [Test]
    public void UnaryMinus_IntMinValue_CheckedThrows()
    {
        // checked(-int.MinValue) overflows
        Assert.Throws<OverflowException>(() => CreateEngine().Evaluate("checked(-int.MinValue)"));
    }

    #endregion

    #region §12.10.3 Division operator — edge cases
    [Test]
    public void IntegerDivision_ByZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(() => CreateEngine().Evaluate(@"
            var x = 1;
            var y = 0;
            return x / y;
        "));
    }

    [Test]
    public void IntegerDivision_MinValueByMinusOne_CheckedThrows()
    {
        // §12.10.3: int.MinValue / -1 overflows in checked
        Assert.Throws<OverflowException>(() => CreateEngine().Evaluate(@"
            return checked(int.MinValue / -1);
        "));
    }

    [Test]
    public void IntegerRemainder_ByZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(() => CreateEngine().Evaluate(@"
            var x = 1;
            var y = 0;
            return x % y;
        "));
    }

    #endregion

    #region §12.10.5/6 Enum arithmetic — more edge cases
    [Test]
    public void EnumSubtraction_IntUnderlying_ReturnsInt()
    {
        // §12.10.6: E - E → U where U is the underlying type
        // DayOfWeek has int underlying
        var result = CreateEngine().Evaluate("System.DayOfWeek.Friday - System.DayOfWeek.Monday");
        Assert.That(result, Is.EqualTo(4));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void EnumSubtraction_LongUnderlying_ReturnsLong()
    {
        // §12.10.6: E - E → U where U is the underlying type
        var engine = CreateEngine();
        engine.SetVariable("a", LongEnum.A);
        engine.SetVariable("b", LongEnum.B);
        var result = engine.Evaluate("b - a");
        Assert.That(result, Is.EqualTo(1L));
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void EnumArithmetic_BitwiseXor()
    {
        var engine = CreateEngine(allowedTypes: [typeof(FileAccess)]);
        var result = engine.Evaluate(@"
            var a = System.IO.FileAccess.ReadWrite;
            var b = System.IO.FileAccess.Write;
            return a ^ b;
        ");
        Assert.That(result, Is.EqualTo(FileAccess.Read));
    }

    [Test]
    public void EnumArithmetic_BitwiseNot()
    {
        var engine = CreateEngine(allowedTypes: [typeof(FileAccess)]);
        var result = engine.Evaluate(@"
            var a = System.IO.FileAccess.Read;
            return ~a;
        ");
        Assert.That(result, Is.EqualTo(~FileAccess.Read));
    }

    [Test]
    public void EnumArithmetic_HasFlag_Pattern()
    {
        var engine = CreateEngine(allowedTypes: [typeof(FileAccess)]);
        var result = engine.Evaluate(@"
            var flags = System.IO.FileAccess.Read | System.IO.FileAccess.Write;
            return (flags & System.IO.FileAccess.Read) != 0;
        ");
        Assert.That(result, Is.True);
    }

    #endregion

    #region §12.21.4 Compound assignment — type narrowing
    [Test]
    public void CompoundAssign_BytePlusEquals_ByteValue()
    {
        // §12.21.4: byte += byte → implicit narrowing back to byte
        var result = CreateEngine().Evaluate(@"
            byte b = 10;
            b += 5;
            return b;
        ");
        Assert.That(result, Is.EqualTo((byte)15));
        Assert.That(result, Is.TypeOf<byte>());
    }

    [Test]
    public void CompoundAssign_ShortTimesEquals()
    {
        var result = CreateEngine().Evaluate(@"
            short s = 10;
            s *= 3;
            return s;
        ");
        Assert.That(result, Is.EqualTo((short)30));
        Assert.That(result, Is.TypeOf<short>());
    }

    #endregion

    #region §12.10.5 Addition — checked overflow
    [Test]
    public void CheckedAdd_IntOverflow_Throws()
    {
        Assert.Throws<OverflowException>(() => CreateEngine().Evaluate("checked(int.MaxValue + 1)"));
    }

    [Test]
    public void CheckedMultiply_IntOverflow_Throws()
    {
        Assert.Throws<OverflowException>(() => CreateEngine().Evaluate("checked(int.MaxValue * 2)"));
    }

    #endregion

    #region §12.8.17 typeof operator
    [Test]
    public void Typeof_GenericList()
    {
        var result = CreateEngine().Evaluate("typeof(List<int>).Name");
        Assert.That(result, Does.Contain("List"));
    }

    [Test]
    public void Typeof_Nullable()
    {
        var result = CreateEngine().Evaluate("typeof(int?).Name");
        Assert.That(result, Does.Contain("Nullable"));
    }
    #endregion
}
