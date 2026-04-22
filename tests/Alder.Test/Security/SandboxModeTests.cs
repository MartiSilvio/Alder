using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Security;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class SandboxModeTests(CompilationMode mode)
{
    [Test]
    public void Safe_AllowPropertyReadFalse_BlocksPropertyAccess()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Sandbox = SandboxOptions.Safe() with { AllowPropertyRead = false });
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("text.Length"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0103));
    }

    [Test]
    public void Safe_AllowAssignmentFalse_BlocksSimpleAssignment()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Sandbox = SandboxOptions.Safe() with { AllowAssignment = false });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("""
            var x = 1;
            x = 5;
            return x;
            """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
    }

    [Test]
    public void Safe_AllowAssignmentFalse_AllowsVariableDeclaration()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Sandbox = SandboxOptions.Safe() with { AllowAssignment = false });

        var result = engine.Evaluate("""
            var x = 5;
            return x;
            """);

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Safe_AllowPropertySetFalse_BlocksPropertyAssignment()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Sandbox = SandboxOptions.Safe() with { AllowPropertySet = false });
        engine.SetVariable("obj", new TestMutableObject { Value = 10 });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("obj.Value = 99"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0105));
    }

    [Test]
    public void Safe_AllowPropertySetFalse_AllowsPropertyRead()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Sandbox = SandboxOptions.Safe() with { AllowPropertySet = false });
        engine.SetVariable("obj", new TestMutableObject { Value = 10 });

        var result = engine.Evaluate("obj.Value");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Safe_AllowIndexSetFalse_BlocksArrayIndexAssignment()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Sandbox = SandboxOptions.Safe() with { AllowIndexSet = false });
        engine.SetVariable("items", new[] { 1, 2, 3 });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("items[0] = 99"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0102));
    }

    [Test]
    public void Safe_AllowIndexSetFalse_AllowsDictionaryRead()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Sandbox = SandboxOptions.Safe() with { AllowIndexSet = false });
        engine.SetVariable("dict", new Dictionary<string, object?> { ["key"] = "value" });

        var result = engine.Evaluate("""dict["key"]""");
        Assert.That(result, Is.EqualTo("value"));
    }

    [Test]
    public void Strict_AllowsOnlyVariableDeclarationAndRead()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict());

        var result = engine.Evaluate("""
            var x = 5;
            return x;
            """);

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Strict_WithAllowAssignmentTrue_AllowsAssignment()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Sandbox = SandboxOptions.Strict() with { AllowAssignment = true });

        var result = engine.Evaluate("""
            var x = 1;
            x = 5;
            return x;
            """);

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Strict_WithAllowMethodCallsTrue_AllowsMethodCalls()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Sandbox = SandboxOptions.Strict() with { AllowMethodCalls = true });
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.ToUpper()");
        Assert.That(result, Is.EqualTo("HELLO"));
    }

    [Test]
    public void DenyAll_DefaultSandbox_BlocksMethodCalls()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = new SandboxOptions());
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("text.ToUpper()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void DenyAll_DefaultSandbox_BlocksPropertyRead()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = new SandboxOptions());
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("text.Length"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0103));
    }

    [Test]
    public void DenyAll_DefaultSandbox_AllowsPureExpressions()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = new SandboxOptions());

        var result = engine.Evaluate("1 + 2 * 3");
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void DenyAll_AllowsDelegateInvocation()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = new SandboxOptions());
        engine.SetVariable("fn", new Func<int, int>(x => x + 1));

        var result = engine.Evaluate("fn(5)");
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void DenyAll_AllowsRegisteredFunctions()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Sandbox = new SandboxOptions();
            o.Functions.Register("triple", args => Convert.ToInt64(args[0]) * 3);
        });

        var result = engine.Evaluate("triple(5)");
        Assert.That(result, Is.EqualTo(15L));
    }

    [Test]
    public void MemberAssign_AllowAssignmentFalse_AllowPropertySetTrue_Succeeds()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Sandbox = SandboxOptions.Safe() with
            {
                AllowAssignment = false,
                AllowPropertySet = true
            });
        engine.SetVariable("obj", new TestMutableObject { Value = 10 });

        var result = engine.Evaluate("""
            obj.Value = 25;
            return obj.Value;
            """);

        Assert.That(result, Is.EqualTo(25));
    }

    [Test]
    public void Sandbox_AllowConstruction_Blocks_New()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Sandbox = SandboxOptions.Safe() with { AllowConstruction = false });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("new List<int>()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0106));
    }

    [Test]
    public void Sandbox_AllowConstruction_Permits_When_Enabled()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Sandbox = new SandboxOptions { AllowConstruction = true });

        var result = engine.Evaluate("new List<int>()");
        Assert.That(result, Is.InstanceOf<List<int>>());
    }

    [Test]
    public void Sandbox_TrustedTypes_OverridesDenyList_ForConstruction()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Sandbox = SandboxOptions.Safe() with
            {
                TrustedTypes = [typeof(System.Text.StringBuilder)],
                AllowConstruction = true
            };
        });

        var result = engine.Evaluate("new System.Text.StringBuilder()");
        Assert.That(result, Is.InstanceOf<System.Text.StringBuilder>());
    }

    private sealed class TestMutableObject
    {
        public int Value { get; set; }
    }
}
