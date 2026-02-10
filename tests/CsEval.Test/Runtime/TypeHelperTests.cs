// All tests engine-only: test TypeHelpers.CanImplicitlyConvert internal API directly.

namespace CsEval.Test.Runtime;

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
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(byte), typeof(char)),
            Is.False,
            "byte -> char must NOT be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_UShortToChar_ReturnsFalse()
    {
        // ECMA-334 §10.2.3: No implicit conversion from ushort to char
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(ushort), typeof(char)),
            Is.False,
            "ushort -> char must NOT be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_CharToUShort_ReturnsTrue()
    {
        // ECMA-334 §10.2.3: char -> ushort is an implicit conversion
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(char), typeof(ushort)),
            Is.True,
            "char -> ushort must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_CharToInt_ReturnsTrue()
    {
        // ECMA-334 §10.2.3: char -> int is an implicit conversion
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(char), typeof(int)),
            Is.True,
            "char -> int must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_IntToLong_ReturnsTrue()
    {
        // ECMA-334 §10.2.3: int -> long is an implicit conversion
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(int), typeof(long)),
            Is.True,
            "int -> long must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_IntToNullableInt_ReturnsTrue()
    {
        // ECMA-334 §10.6.1: T -> T? is an implicit nullable conversion
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(int), typeof(int?)),
            Is.True,
            "int -> int? must be an implicit conversion per ECMA-334 §10.6.1");
    }

    [Test]
    public void CanImplicitlyConvert_IntToNullableLong_ReturnsTrue()
    {
        // ECMA-334 §10.6.1: S -> T? where S -> T is implicit numeric
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(int), typeof(long?)),
            Is.True,
            "int -> long? must be an implicit conversion (lifted from int -> long) per ECMA-334 §10.6.1");
    }

    [Test]
    public void CanImplicitlyConvert_FloatToDouble_ReturnsTrue()
    {
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(float), typeof(double)),
            Is.True,
            "float -> double must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_DoubleToFloat_ReturnsFalse()
    {
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(double), typeof(float)),
            Is.False,
            "double -> float is NOT an implicit conversion (requires explicit cast)");
    }

    [Test]
    public void CanImplicitlyConvert_LongToInt_ReturnsFalse()
    {
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(long), typeof(int)),
            Is.False,
            "long -> int is NOT an implicit conversion (requires explicit cast)");
    }
}
