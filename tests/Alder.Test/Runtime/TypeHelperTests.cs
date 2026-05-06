// All tests engine-only: test TypeHelpers.CanImplicitlyConvert internal API directly.

using Alder.Diagnostics;

namespace Alder.Test.Runtime;

/// <summary>
/// Unit tests for TypeHelpers utility methods.
/// Tests the internal TypeHelpers.CanImplicitlyConvert API directly -- no expression evaluation.
/// </summary>
[TestFixture]
public class TypeHelperTests
{
    [Test]
    public void CanImplicitlyConvert_ByteToChar_ReturnsFalse()
    {
        // ECMA-334 §10.2.3: No implicit conversion from byte to char
        Assert.That(
            Alder.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(byte), typeof(char)),
            Is.False,
            "byte -> char must NOT be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_UShortToChar_ReturnsFalse()
    {
        // ECMA-334 §10.2.3: No implicit conversion from ushort to char
        Assert.That(
            Alder.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(ushort), typeof(char)),
            Is.False,
            "ushort -> char must NOT be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_CharToUShort_ReturnsTrue()
    {
        // ECMA-334 §10.2.3: char -> ushort is an implicit conversion
        Assert.That(
            Alder.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(char), typeof(ushort)),
            Is.True,
            "char -> ushort must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_CharToInt_ReturnsTrue()
    {
        // ECMA-334 §10.2.3: char -> int is an implicit conversion
        Assert.That(
            Alder.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(char), typeof(int)),
            Is.True,
            "char -> int must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_IntToLong_ReturnsTrue()
    {
        // ECMA-334 §10.2.3: int -> long is an implicit conversion
        Assert.That(
            Alder.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(int), typeof(long)),
            Is.True,
            "int -> long must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_IntToNullableInt_ReturnsTrue()
    {
        // ECMA-334 §10.6.1: T -> T? is an implicit nullable conversion
        Assert.That(
            Alder.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(int), typeof(int?)),
            Is.True,
            "int -> int? must be an implicit conversion per ECMA-334 §10.6.1");
    }

    [Test]
    public void CanImplicitlyConvert_IntToNullableLong_ReturnsTrue()
    {
        // ECMA-334 §10.6.1: S -> T? where S -> T is implicit numeric
        Assert.That(
            Alder.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(int), typeof(long?)),
            Is.True,
            "int -> long? must be an implicit conversion (lifted from int -> long) per ECMA-334 §10.6.1");
    }

    [Test]
    public void CanImplicitlyConvert_FloatToDouble_ReturnsTrue()
    {
        Assert.That(
            Alder.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(float), typeof(double)),
            Is.True,
            "float -> double must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_DoubleToFloat_ReturnsFalse()
    {
        Assert.That(
            Alder.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(double), typeof(float)),
            Is.False,
            "double -> float is NOT an implicit conversion (requires explicit cast)");
    }

    [Test]
    public void CanImplicitlyConvert_LongToInt_ReturnsFalse()
    {
        Assert.That(
            Alder.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(long), typeof(int)),
            Is.False,
            "long -> int is NOT an implicit conversion (requires explicit cast)");
    }

    [Test]
    public void RuntimeCast_UncheckedIntToByte_Wraps()
    {
        var result = Alder.Runtime.TypeHelpers.RuntimeCast(256, typeof(int), typeof(byte), isChecked: false);
        Assert.That(result, Is.EqualTo((byte)0));
    }

    [Test]
    public void RuntimeCast_CheckedIntToByte_ThrowsOverflow()
    {
        Assert.Throws<OverflowException>(
            () => Alder.Runtime.TypeHelpers.RuntimeCast(256, typeof(int), typeof(byte), isChecked: true));
    }

    [Test]
    public void RuntimeCast_EnumToInt_ReturnsUnderlyingValue()
    {
        var result = Alder.Runtime.TypeHelpers.RuntimeCast(CastProbe.B, typeof(CastProbe), typeof(int));
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void RuntimeCast_IntToEnum_ReturnsEnumValue()
    {
        var result = Alder.Runtime.TypeHelpers.RuntimeCast(2, typeof(int), typeof(CastProbe));
        Assert.That(result, Is.EqualTo(CastProbe.B));
    }

    [Test]
    public void RuntimeCast_BoolToInt_ThrowsAlderException()
    {
        var ex = Assert.Throws<AlderException>(
            () => Alder.Runtime.TypeHelpers.RuntimeCast(true, typeof(bool), typeof(int)));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0030));
    }

    private enum CastProbe
    {
        A = 1,
        B = 2
    }
}
