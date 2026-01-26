using CsEval.Parsing;

namespace CsEval.Test.Parsing;

/// <summary>
/// Tests for JavaScript-friendly syntax features that work alongside C# syntax.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class JsFriendlySyntaxTests(CompilationMode mode) : TestBase
{
    #region Let as Var

    [Test]
    public void Let_TreatedAsVar()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ let x = 42; return x; }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Const_IsReservedKeyword()
    {
        var engine = CreateEngine(mode);
        // const is a reserved keyword but cannot be used for variable declarations
        Assert.Throws<ParserException>(() => engine.Evaluate("{ const x = 'hello'; return x; }"));
    }

    [Test]
    public void Super_IsReservedKeyword()
    {
        var engine = CreateEngine(mode);
        // super is reserved (JS equivalent of C# base)
        Assert.Throws<ParserException>(() => engine.Evaluate("{ var super = 1; return super; }"));
    }

    #endregion

    #region Undefined

    [Test]
    public void Undefined_IsNull()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("undefined");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Undefined_Comparison()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("undefined == null"), Is.EqualTo(true));
        Assert.That(engine.Evaluate("undefined === null"), Is.EqualTo(true));
    }

    #endregion

    #region Strict Equality (=== and !==)

    [Test]
    public void StrictEquality_Works()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("5 === 5"), Is.EqualTo(true));
        Assert.That(engine.Evaluate("5 === 10"), Is.EqualTo(false));
        Assert.That(engine.Evaluate("'hello' === 'hello'"), Is.EqualTo(true));
    }

    [Test]
    public void StrictInequality_Works()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("5 !== 10"), Is.EqualTo(true));
        Assert.That(engine.Evaluate("5 !== 5"), Is.EqualTo(false));
        Assert.That(engine.Evaluate("'a' !== 'b'"), Is.EqualTo(true));
    }

    [Test]
    public void StrictEquality_InExpression()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 5);
        Assert.That(engine.Evaluate("x === 5 ? 'yes' : 'no'"), Is.EqualTo("yes"));
        Assert.That(engine.Evaluate("x !== 5 ? 'yes' : 'no'"), Is.EqualTo("no"));
    }

    #endregion

    #region Anonymous Objects (C# Style)

    [Test]
    public void AnonymousObject_Works()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("new { Name = 'John' }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
    }

    [Test]
    public void AnonymousObject_MultipleProperties()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("new { Name = 'John', Age = 30 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result!["Age"], Is.EqualTo(30));
    }

    #endregion
}
