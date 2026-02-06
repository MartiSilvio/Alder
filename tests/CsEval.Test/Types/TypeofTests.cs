namespace CsEval.Test.Types;

/// <summary>
/// Tests for typeof(T) expression (ECMA-334 §12.8.17 - The typeof operator).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class TypeofTests(CompilationMode mode)
{
    #region ECMA-334 §12.8.17 - The typeof operator

    // typeof returns System.Type objects -- these are singletons so reference equality works.
    // For parity tests with expected values, we use the 2-arg RunCSharpParityTestAsync
    // (no explicit expected) since Type objects can't be passed as TestCase attributes.

    [Test]
    public async Task Typeof_Int()
        => await TestHelpers.RunCSharpParityTestAsync("typeof(int)", mode);

    [Test]
    public async Task Typeof_String()
        => await TestHelpers.RunCSharpParityTestAsync("typeof(string)", mode);

    [Test]
    public async Task Typeof_Bool()
        => await TestHelpers.RunCSharpParityTestAsync("typeof(bool)", mode);

    [Test]
    public async Task Typeof_Double()
        => await TestHelpers.RunCSharpParityTestAsync("typeof(double)", mode);

    [Test]
    public async Task Typeof_Void()
        => await TestHelpers.RunCSharpParityTestAsync("typeof(void)", mode);

    [Test]
    public async Task Typeof_Char()
        => await TestHelpers.RunCSharpParityTestAsync("typeof(char)", mode);

    [Test]
    public async Task Typeof_Decimal()
        => await TestHelpers.RunCSharpParityTestAsync("typeof(decimal)", mode);

    [Test]
    public async Task Typeof_Object()
        => await TestHelpers.RunCSharpParityTestAsync("typeof(object)", mode);

    [Test]
    public async Task Typeof_Float()
        => await TestHelpers.RunCSharpParityTestAsync("typeof(float)", mode);

    [Test]
    public async Task Typeof_Long()
        => await TestHelpers.RunCSharpParityTestAsync("typeof(long)", mode);

    [Test]
    public async Task Typeof_Byte()
        => await TestHelpers.RunCSharpParityTestAsync("typeof(byte)", mode);

    // Member access on Type object returned by typeof
    [TestCase("typeof(int).Name", "Int32", TestName = "Typeof_Int_Name")]
    [TestCase("typeof(string).Name", "String", TestName = "Typeof_String_Name")]
    [TestCase("typeof(bool).Name", "Boolean", TestName = "Typeof_Bool_Name")]
    [TestCase("typeof(double).Name", "Double", TestName = "Typeof_Double_Name")]
    [TestCase("typeof(void).Name", "Void", TestName = "Typeof_Void_Name")]
    public async Task Typeof_MemberAccess(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Equality comparison of Type objects (Type singletons)
    [TestCase("typeof(int) == typeof(int)", true, TestName = "Typeof_EqualsSameType")]
    [TestCase("typeof(int) != typeof(string)", true, TestName = "Typeof_NotEqualsOtherType")]
    public async Task Typeof_Equality(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion
}
