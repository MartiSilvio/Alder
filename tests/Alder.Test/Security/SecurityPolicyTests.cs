using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Security;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class SecurityPolicyTests(CompilationMode mode)
{
    [Test]
    public void ExplicitPolicy_AllowPropertyReadFalse_BlocksPropertyAccess()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Security = new SecurityOptions
            {
                AllowStaticPropertyRead = true,
                AllowStaticFieldRead = true,
                AllowAssignment = true,
                AllowPropertySet = true,
                AllowIndexSet = true
            });
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("text.Length"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0103));
    }

    [Test]
    public void ExplicitPolicy_AllowAssignmentFalse_BlocksSimpleAssignment()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Security = new SecurityOptions
            {
                AllowPropertyRead = true,
                AllowStaticPropertyRead = true,
                AllowStaticFieldRead = true,
                AllowPropertySet = true,
                AllowIndexSet = true
            });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("""
            var x = 1;
            x = 5;
            return x;
            """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
    }

    [Test]
    public void ExplicitPolicy_AllowAssignmentFalse_AllowsVariableDeclaration()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Security = new SecurityOptions
            {
                AllowPropertyRead = true,
                AllowStaticPropertyRead = true,
                AllowStaticFieldRead = true,
                AllowPropertySet = true,
                AllowIndexSet = true
            });

        var result = engine.Evaluate("""
            var x = 5;
            return x;
            """);

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void ExplicitPolicy_AllowPropertySetFalse_BlocksPropertyAssignment()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Security = new SecurityOptions
            {
                AllowPropertyRead = true,
                AllowStaticPropertyRead = true,
                AllowStaticFieldRead = true,
                AllowAssignment = true,
                AllowIndexSet = true
            });
        engine.SetVariable("obj", new TestMutableObject { Value = 10 });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("obj.Value = 99"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0105));
    }

    [Test]
    public void ExplicitPolicy_AllowPropertySetFalse_AllowsPropertyRead()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Security = new SecurityOptions
            {
                AllowPropertyRead = true,
                AllowStaticPropertyRead = true,
                AllowStaticFieldRead = true,
                AllowAssignment = true,
                AllowIndexSet = true
            });
        engine.SetVariable("obj", new TestMutableObject { Value = 10 });

        var result = engine.Evaluate("obj.Value");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void ExplicitPolicy_AllowIndexSetFalse_BlocksArrayIndexAssignment()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Security = new SecurityOptions
            {
                AllowPropertyRead = true,
                AllowStaticPropertyRead = true,
                AllowStaticFieldRead = true,
                AllowAssignment = true,
                AllowPropertySet = true
            });
        engine.SetVariable("items", new[] { 1, 2, 3 });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("items[0] = 99"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0102));
    }

    [Test]
    public void ExplicitPolicy_AllowIndexSetFalse_AllowsDictionaryRead()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Security = new SecurityOptions
            {
                AllowPropertyRead = true,
                AllowStaticPropertyRead = true,
                AllowStaticFieldRead = true,
                AllowAssignment = true,
                AllowPropertySet = true
            });
        engine.SetVariable("dict", new Dictionary<string, object?> { ["key"] = "value" });

        var result = engine.Evaluate("""dict["key"]""");
        Assert.That(result, Is.EqualTo("value"));
    }

    [Test]
    public void ReadOnlyPolicy_AllowsOnlyVariableDeclarationAndRead()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Security = new SecurityOptions
        {
            AllowPropertyRead = true,
            AllowStaticPropertyRead = true,
            AllowStaticFieldRead = true
        });

        var result = engine.Evaluate("""
            var x = 5;
            return x;
            """);

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void ReadOnlyPolicy_WithAllowAssignmentTrue_AllowsAssignment()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Security = new SecurityOptions
            {
                AllowPropertyRead = true,
                AllowStaticPropertyRead = true,
                AllowStaticFieldRead = true,
                AllowAssignment = true
            });

        var result = engine.Evaluate("""
            var x = 1;
            x = 5;
            return x;
            """);

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void ReadOnlyPolicy_WithAllowMethodCallsTrue_AllowsMethodCalls()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Security = new SecurityOptions
            {
                AllowPropertyRead = true,
                AllowStaticPropertyRead = true,
                AllowStaticFieldRead = true,
                AllowMethodCalls = true
            });
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.ToUpper()");
        Assert.That(result, Is.EqualTo("HELLO"));
    }

    [Test]
    public void DenyAll_DefaultSecurityOptions_BlocksMethodCalls()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Security = new SecurityOptions());
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("text.ToUpper()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void DenyAll_DefaultSecurityOptions_BlocksPropertyRead()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Security = new SecurityOptions());
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("text.Length"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0103));
    }

    [Test]
    public void DenyAll_DefaultSecurityOptions_AllowsPureExpressions()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Security = new SecurityOptions());

        var result = engine.Evaluate("1 + 2 * 3");
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void DenyAll_AllowsDelegateInvocation()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Security = new SecurityOptions());
        engine.SetVariable("fn", new Func<int, int>(x => x + 1));

        var result = engine.Evaluate("fn(5)");
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void DenyAll_AllowsRegisteredFunctions()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Security = new SecurityOptions();
            o.Functions.Register("triple", args => Convert.ToInt64(args[0]) * 3);
        });

        var result = engine.Evaluate("triple(5)");
        Assert.That(result, Is.EqualTo(15L));
    }

    [Test]
    public void MemberAssign_AllowAssignmentFalse_AllowPropertySetTrue_Succeeds()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Security = new SecurityOptions
            {
                AllowPropertyRead = true,
                AllowStaticPropertyRead = true,
                AllowStaticFieldRead = true,
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
    public void SecurityPolicy_AllowConstruction_Blocks_New()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Security = new SecurityOptions
            {
                AllowPropertyRead = true,
                AllowStaticPropertyRead = true,
                AllowStaticFieldRead = true,
                AllowAssignment = true,
                AllowPropertySet = true,
                AllowIndexSet = true
            });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("new List<int>()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0106));
    }

    [Test]
    public void SecurityPolicy_AllowConstruction_Permits_When_Enabled()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Security = new SecurityOptions { AllowConstruction = true });

        var result = engine.Evaluate("new List<int>()");
        Assert.That(result, Is.InstanceOf<List<int>>());
    }

    [Test]
    public void SecurityPolicy_TrustedTypes_OverridesDenyList_ForConstruction()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Security = new SecurityOptions
            {
                AllowPropertyRead = true,
                AllowStaticPropertyRead = true,
                AllowStaticFieldRead = true,
                AllowAssignment = true,
                AllowPropertySet = true,
                AllowIndexSet = true,
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
