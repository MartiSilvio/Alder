namespace CsEval.Test.DocVerification;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class Phase02Plan04VerificationTests(CompilationMode mode)
{
    private CsEvalEngine Engine()
        => new(CsEvalOptions.Default with { CompilationMode = mode });

    private object? Eval(string expr) => Engine().Evaluate(expr);

    // ═══════════════════════════════════════════════════════════════
    // Lambda — exploratory: figure out invocation patterns
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Lambda_SingleParam_VarAssign_Invoke()
        => Assert.That(Eval("{ var f = (int x) => x + 1; return f(5); }"), Is.EqualTo(6));

    [Test]
    public void Lambda_MultipleParams()
        => Assert.That(Eval("{ var f = (int x, int y) => x + y; return f(3, 4); }"), Is.EqualTo(7));

    [Test]
    public void Lambda_NoParams()
        => Assert.That(Eval("{ var f = () => 42; return f(); }"), Is.EqualTo(42));

    [Test]
    public void Lambda_BlockBody()
        => Assert.That(Eval("{ var f = (int x) => { var y = x * 2; return y + 1; }; return f(3); }"), Is.EqualTo(7));

    [Test]
    public void Lambda_Closure_CapturesVariable()
        => Assert.That(Eval("{ var n = 10; var f = (int x) => x + n; return f(5); }"), Is.EqualTo(15));

    [Test]
    public void Lambda_UntypedSingleParam()
        => Assert.That(Eval("{ var f = x => x + 1; return f(5); }"), Is.EqualTo(6));

    // ═══════════════════════════════════════════════════════════════
    // Pattern matching — constant patterns
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Pattern_Constant_IntMatch()
        => Assert.That(Eval("42 is 42"), Is.EqualTo(true));

    [Test]
    public void Pattern_Constant_IntNoMatch()
        => Assert.That(Eval("42 is 99"), Is.EqualTo(false));

    [Test]
    public void Pattern_Constant_StringMatch()
        => Assert.That(Eval(@"""hello"" is ""hello"""), Is.EqualTo(true));

    [Test]
    public void Pattern_Constant_NullMatch()
        => Assert.That(Eval("(object)null is null"), Is.EqualTo(true));

    [Test]
    public void Pattern_Constant_NullNoMatch()
        => Assert.That(Eval(@"""hello"" is null"), Is.EqualTo(false));

    // ═══════════════════════════════════════════════════════════════
    // Pattern matching — type patterns
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Pattern_Type_IntMatch()
        => Assert.That(Eval("(object)42 is int"), Is.EqualTo(true));

    [Test]
    public void Pattern_Type_StringNoMatch()
        => Assert.That(Eval(@"(object)""hello"" is int"), Is.EqualTo(false));

    [Test]
    public void Pattern_Type_WithBinding()
        => Assert.That(Eval(@"{ object x = ""hello""; return x is string s ? s.Length : -1; }"), Is.EqualTo(5));

    [Test]
    public void Pattern_Type_WithBinding_NoMatch()
        => Assert.That(Eval("{ object x = 42; return x is string s ? s.Length : -1; }"), Is.EqualTo(-1));

    // ═══════════════════════════════════════════════════════════════
    // Pattern matching — var pattern
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Pattern_Var_AlwaysMatches()
        => Assert.That(Eval("42 is var v"), Is.EqualTo(true));

    [Test]
    public void Pattern_Var_BindsValue()
        => Assert.That(Eval("{ return 42 is var v ? v : -1; }"), Is.EqualTo(42));

    // ═══════════════════════════════════════════════════════════════
    // Pattern matching — relational patterns
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Pattern_Relational_GreaterThan()
        => Assert.That(Eval("42 is > 0"), Is.EqualTo(true));

    [Test]
    public void Pattern_Relational_LessThan_False()
        => Assert.That(Eval("42 is < 0"), Is.EqualTo(false));

    [Test]
    public void Pattern_Relational_Range()
        => Assert.That(Eval("42 is >= 10 and <= 100"), Is.EqualTo(true));

    // ═══════════════════════════════════════════════════════════════
    // Pattern matching — logical patterns
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Pattern_Logical_NotNull()
        => Assert.That(Eval(@"(object)""hello"" is not null"), Is.EqualTo(true));

    [Test]
    public void Pattern_Logical_NotNull_Null()
        => Assert.That(Eval("(object)null is not null"), Is.EqualTo(false));

    [Test]
    public void Pattern_Logical_And()
        => Assert.That(Eval("42 is > 0 and < 100"), Is.EqualTo(true));

    [Test]
    public void Pattern_Logical_Or()
        => Assert.That(Eval("(object)42 is int or string"), Is.EqualTo(true));

    [Test]
    public void Pattern_Logical_Or_SecondMatch()
        => Assert.That(Eval(@"(object)""hi"" is int or string"), Is.EqualTo(true));

    [Test]
    public void Pattern_Logical_Or_NoMatch()
        => Assert.That(Eval("(object)3.14 is int or string"), Is.EqualTo(false));

    // ═══════════════════════════════════════════════════════════════
    // Pattern matching — property patterns
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Pattern_Property_LengthGreaterThan()
        => Assert.That(Eval(@"""hello world"" is { Length: > 5 }"), Is.EqualTo(true));

    [Test]
    public void Pattern_Property_LengthNoMatch()
        => Assert.That(Eval(@"""hi"" is { Length: > 5 }"), Is.EqualTo(false));

    [Test]
    public void Pattern_Property_NullFalse()
        => Assert.That(Eval("{ object x = null; return x is { Length: > 0 }; }"), Is.EqualTo(false));

    // ═══════════════════════════════════════════════════════════════
    // Pattern matching — switch expression
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Switch_ConstantArms()
        => Assert.That(Eval(@"1 switch { 1 => ""one"", 2 => ""two"", _ => ""other"" }"), Is.EqualTo("one"));

    [Test]
    public void Switch_DiscardArm()
        => Assert.That(Eval(@"99 switch { 1 => ""one"", _ => ""other"" }"), Is.EqualTo("other"));

    [Test]
    public void Switch_TypeArm()
        => Assert.That(Eval(@"(object)42 switch { int n => n * 2, string s => s.Length, _ => -1 }"), Is.EqualTo(84));

    [Test]
    public void Switch_RelationalArms()
        => Assert.That(Eval(@"75 switch { < 60 => ""F"", < 70 => ""D"", < 80 => ""C"", < 90 => ""B"", _ => ""A"" }"), Is.EqualTo("C"));

    [Test]
    public void Switch_NoMatch_Throws()
        => Assert.Throws<System.Runtime.CompilerServices.SwitchExpressionException>(
            () => Eval(@"99 switch { 1 => ""one"" }"));

    // ═══════════════════════════════════════════════════════════════
    // Pattern matching — positional (tuple) patterns
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Pattern_Positional_TupleMatch()
        => Assert.That(Eval(@"(1, ""hello"") switch { (1, ""hello"") => true, _ => false }"), Is.EqualTo(true));

    [Test]
    public void Pattern_Positional_WithDiscard()
        => Assert.That(Eval(@"(1, ""hello"") switch { (1, _) => true, _ => false }"), Is.EqualTo(true));

    // ═══════════════════════════════════════════════════════════════
    // Lambda — closure mutation
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Lambda_Closure_MutatesOuter()
        => Assert.That(Eval("{ var n = 0; var inc = () => { n = n + 1; return n; }; inc(); inc(); return inc(); }"), Is.EqualTo(3));

    // ═══════════════════════════════════════════════════════════════
    // Lambda — additional doc examples
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Lambda_TypedParam_Multiply()
        => Assert.That(Eval("{ var f = (int x) => x * 2; return f(5); }"), Is.EqualTo(10));

    [Test]
    public void Lambda_BlockBody_Classifier()
        => Assert.That(Eval(@"{ var classify = (int x) => { if (x > 0) return ""positive""; if (x < 0) return ""negative""; return ""zero""; }; return classify(-5); }"), Is.EqualTo("negative"));

    [Test]
    public void Lambda_Greet_StringInterpolation()
        => Assert.That(Eval(@"{ var greet = (string name) => $""Hello {name}!""; return greet(""World""); }"), Is.EqualTo("Hello World!"));

    [Test]
    public void Lambda_FuncTyped()
        => Assert.That(Eval("{ Func<int, int> doubler = x => x * 2; return doubler(5); }"), Is.EqualTo(10));

    [Test]
    public void Lambda_Linq_WhereCount()
        => Assert.That(Eval("{ var numbers = new int[] { 1, 2, 3, 4, 5 }; return numbers.Where(x => x > 3).Count(); }"), Is.EqualTo(2));

    // ═══════════════════════════════════════════════════════════════
    // Pattern matching — additional doc examples
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Pattern_Var_WithMultiply()
        => Assert.That(Eval("{ return 42 is var v ? v * 2 : 0; }"), Is.EqualTo(84));

    [Test]
    public void Pattern_Logical_And_OutOfRange()
        => Assert.That(Eval("150 is > 0 and < 100"), Is.EqualTo(false));

    [Test]
    public void Pattern_Combined_TypeAndRelational()
        => Assert.That(Eval("{ object x = 42; return x is int n and > 0 ? n : -1; }"), Is.EqualTo(42));

    [Test]
    public void Switch_ClassifyLambda()
        => Assert.That(Eval(@"{ var classify = (int score) => score switch { < 0 or > 100 => ""invalid"", >= 90 => ""A"", >= 80 => ""B"", >= 70 => ""C"", >= 60 => ""D"", _ => ""F"" }; return classify(85); }"), Is.EqualTo("B"));
}
