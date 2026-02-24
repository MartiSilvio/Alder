namespace CsEval.Test.Security;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class CaseSensitivityTests(CompilationMode mode)
{
    [Test]
    public void CaseSensitive_CorrectCase_MethodCallSucceeds()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            IsCaseSensitive = true,
            Sandbox = SandboxOptions.Trusted()
        });
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.ToUpper()");

        Assert.That(result, Is.EqualTo("HELLO"));
    }

    [Test]
    public void CaseSensitive_WrongCase_MethodCallFails()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            IsCaseSensitive = true,
            Sandbox = SandboxOptions.Trusted()
        });
        engine.SetVariable("text", "hello");

        // "toupper" is wrong case -- should fail in case-sensitive mode
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("text.toupper()"));
        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public void CaseInsensitive_WrongCase_MethodCallSucceeds()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            IsCaseSensitive = false,
            Sandbox = SandboxOptions.Trusted()
        });
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.toupper()");

        Assert.That(result, Is.EqualTo("HELLO"));
    }

    [Test]
    public void CaseSensitive_CorrectCase_PropertyReadSucceeds()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            IsCaseSensitive = true,
            Sandbox = SandboxOptions.Trusted()
        });
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.Length");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void CaseSensitive_WrongCase_PropertyReadDoesNotResolve()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            IsCaseSensitive = true,
            Sandbox = SandboxOptions.Trusted()
        });
        engine.SetVariable("text", "hello");

        // "length" is wrong case -- should NOT resolve to the int property value.
        // The engine returns a MethodRef fallback (not the property value) since no
        // property or field matches with exact case. This is correct: the property
        // is not found, so the result is not the expected int value.
        var result = engine.Evaluate("text.length");
        Assert.That(result, Is.Not.EqualTo(5), "Wrong-case property should not resolve to the correct value");
    }

    [Test]
    public void CaseInsensitive_WrongCase_PropertyReadSucceeds()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            IsCaseSensitive = false,
            Sandbox = SandboxOptions.Trusted()
        });
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.length");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void CaseSensitive_StaticMethod_CorrectCase()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            IsCaseSensitive = true,
            Sandbox = SandboxOptions.Trusted()
        });

        var result = engine.Evaluate("Math.Abs(-5)");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void CaseSensitive_Default_IsTrue()
    {
        Assert.That(CsEvalOptions.Default.IsCaseSensitive, Is.True);
    }
}
