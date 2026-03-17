using System.Collections;

namespace CsEval.Test.DocVerification;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class Phase05Plan01VerificationTests(CompilationMode mode)
{
    private CsEvalEngine Engine()
        => TestEngineFactory.Create(mode);

    private CsEvalEngine Engine(CsEvalOptions options)
        => TestEngineFactory.Create(mode, options);

    // ═══════════════════════════════════════════════════════════════
    // SEC-01: Permission Flags — each flag blocks/allows correctly
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Flag_AllowMethodCalls_False_BlocksMethodCall()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted() with { AllowMethodCalls = false }
        });
        engine.SetVariable("str", "hello");
        Assert.That(() => engine.Evaluate("str.ToUpper()"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    [Test]
    public void Flag_AllowMethodCalls_True_AllowsMethodCall()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted()
        });
        engine.SetVariable("str", "hello");
        Assert.That(engine.Evaluate("str.ToUpper()"), Is.EqualTo("HELLO"));
    }

    [Test]
    public void Flag_AllowPropertyRead_False_BlocksPropertyRead()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted() with { AllowPropertyRead = false }
        });
        engine.SetVariable("str", "hello");
        Assert.That(() => engine.Evaluate("str.Length"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    [Test]
    public void Flag_AllowPropertyRead_True_AllowsPropertyRead()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Strict()
        });
        engine.SetVariable("str", "hello");
        Assert.That(engine.Evaluate("str.Length"), Is.EqualTo(5));
    }

    [Test]
    public void Flag_AllowStaticPropertyRead_False_BlocksStaticProp()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted() with { AllowStaticPropertyRead = false }
        });
        Assert.That(() => engine.Evaluate("DateTime.Now"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    [Test]
    public void Flag_AllowStaticPropertyRead_True_AllowsStaticProp()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted()
        });
        Assert.That(engine.Evaluate("DateTime.Now"), Is.InstanceOf<DateTime>());
    }

    [Test]
    public void Flag_AllowStaticFieldRead_False_BlocksStaticField()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted() with { AllowStaticFieldRead = false }
        });
        Assert.That(() => engine.Evaluate("string.Empty"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    [Test]
    public void Flag_AllowStaticFieldRead_True_AllowsStaticField()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted()
        });
        Assert.That(engine.Evaluate("string.Empty"), Is.EqualTo(""));
    }

    [Test]
    public void Flag_AllowAssignment_False_BlocksReassignment()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted() with { AllowAssignment = false }
        });
        Assert.That(() => engine.Evaluate("{ var x = 1; x = 5; return x; }"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    [Test]
    public void Flag_AllowAssignment_True_AllowsReassignment()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted()
        });
        Assert.That(engine.Evaluate("{ var x = 1; x = 5; return x; }"), Is.EqualTo(5));
    }

    [Test]
    public void Flag_AllowPropertySet_False_BlocksPropertyWrite()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted() with { AllowPropertySet = false }
        });
        Assert.That(() => engine.Evaluate("{ var obj = new { Value = 1 }; obj.Value = 42; return obj.Value; }"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    [Test]
    public void Flag_AllowPropertySet_True_AllowsPropertyWrite()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted()
        });
        Assert.That(engine.Evaluate("{ var obj = new { Value = 1 }; obj.Value = 42; return obj.Value; }"),
            Is.EqualTo(42));
    }

    [Test]
    public void Flag_AllowIndexSet_False_BlocksIndexWrite()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted() with { AllowIndexSet = false }
        });
        engine.SetVariable("arr", new[] { 1, 2, 3 });
        Assert.That(() => engine.Evaluate("{ arr[0] = 99; return arr[0]; }"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    [Test]
    public void Flag_AllowIndexSet_True_AllowsIndexWrite()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted()
        });
        engine.SetVariable("arr", new[] { 1, 2, 3 });
        Assert.That(engine.Evaluate("{ arr[0] = 99; return arr[0]; }"), Is.EqualTo(99));
    }

    [Test]
    public void Flag_AllowConstruction_False_BlocksNew()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted() with { AllowConstruction = false }
        });
        Assert.That(() => engine.Evaluate("new List<int>()"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    [Test]
    public void Flag_AllowConstruction_True_AllowsNew()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted()
        });
        Assert.That(engine.Evaluate("new List<int>()"), Is.InstanceOf<List<int>>());
    }

    // ═══════════════════════════════════════════════════════════════
    // SEC-01: Factory Presets
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Preset_Trusted_AllowsMethodCall()
    {
        var engine = Engine(new CsEvalOptions { Sandbox = SandboxOptions.Trusted() });
        engine.SetVariable("name", "alice");
        Assert.That(engine.Evaluate("name.ToUpper()"), Is.EqualTo("ALICE"));
    }

    [Test]
    public void Preset_Safe_BlocksMethodCall()
    {
        var engine = Engine(new CsEvalOptions { Sandbox = SandboxOptions.Safe() });
        engine.SetVariable("name", "alice");
        Assert.That(() => engine.Evaluate("name.ToUpper()"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    [Test]
    public void Preset_Strict_BlocksAssignment()
    {
        var engine = Engine(new CsEvalOptions { Sandbox = SandboxOptions.Strict() });
        Assert.That(() => engine.Evaluate("{ var x = 1; x = 5; return x; }"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    // ═══════════════════════════════════════════════════════════════
    // SEC-01: What's Always Allowed
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void AlwaysAllowed_VarDeclaration_InStrict()
    {
        var engine = Engine(new CsEvalOptions { Sandbox = SandboxOptions.Strict() });
        Assert.That(engine.Evaluate("{ var x = 42; return x; }"), Is.EqualTo(42));
    }

    [Test]
    public void AlwaysAllowed_RegisteredFunction_InStrict()
    {
        var engine = Engine(new CsEvalOptions { Sandbox = SandboxOptions.Strict() });
        engine.RegisterFunction("add", args => (int)args[0]! + (int)args[1]!);
        Assert.That(engine.Evaluate("add(3, 4)"), Is.EqualTo(7));
    }

    [Test]
    public void AlwaysAllowed_Typeof_InStrict()
    {
        var engine = Engine(new CsEvalOptions { Sandbox = SandboxOptions.Strict() });
        Assert.That(engine.Evaluate("typeof(int)"), Is.EqualTo(typeof(int)));
    }

    [Test]
    public void AlwaysAllowed_Lambda_InSafe()
    {
        var engine = Engine(new CsEvalOptions { Sandbox = SandboxOptions.Safe() });
        var result = engine.Evaluate("{ var fn = (int x) => x * 2; return fn(5); }");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void AlwaysAllowed_Linq_InStrict()
    {
        var engine = Engine(new CsEvalOptions { Sandbox = SandboxOptions.Strict() });
        engine.SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });
        Assert.That(engine.Evaluate("items.Where(x => x > 2).Sum()"), Is.EqualTo(12));
    }

    [Test]
    public void AlwaysAllowed_Module_InStrict()
    {
        var engine = Engine(new CsEvalOptions { Sandbox = SandboxOptions.Strict() });
        Assert.That(engine.Evaluate("Math.Abs(-5)"), Is.EqualTo(5));
    }

    // ═══════════════════════════════════════════════════════════════
    // SEC-01: AllowedTypes
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void AllowedTypes_ExactType_Succeeds()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted() with
            {
                AllowedTypes = new HashSet<Type> { typeof(List<int>), typeof(int) }
            }
        });
        var result = engine.Evaluate("new List<int> { 1, 2, 3 }");
        Assert.That(result, Is.InstanceOf<List<int>>());
    }

    [Test]
    public void AllowedTypes_WrongConstructedType_Blocked()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted() with
            {
                AllowedTypes = new HashSet<Type> { typeof(List<int>), typeof(int) }
            }
        });
        Assert.That(() => engine.Evaluate("new List<string> { \"a\", \"b\" }"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    // ═══════════════════════════════════════════════════════════════
    // SEC-01: Reflection Leak Guard
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void ReflectionGuard_Typeof_Succeeds_InAnyMode()
    {
        var engine = Engine(new CsEvalOptions { Sandbox = SandboxOptions.Strict() });
        Assert.That(engine.Evaluate("typeof(int)"), Is.EqualTo(typeof(int)));
    }

    [Test]
    public void ReflectionGuard_GetType_Throws_InTrusted()
    {
        var engine = Engine(new CsEvalOptions { Sandbox = SandboxOptions.Trusted() });
        engine.SetVariable("text", "hello");
        Assert.That(() => engine.Evaluate("text.GetType()"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    // ═══════════════════════════════════════════════════════════════
    // SEC-01: Custom Preset with 'with' syntax
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void CustomPreset_StrictWithMethodCalls_AllowsMethodButBlocksConstruction()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Strict() with { AllowMethodCalls = true }
        });
        engine.SetVariable("name", "alice");
        Assert.That(engine.Evaluate("name.ToUpper()"), Is.EqualTo("ALICE"));
        Assert.That(() => engine.Evaluate("new List<int>()"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    // ═══════════════════════════════════════════════════════════════
    // SEC-02: MaxStatements
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void MaxStatements_ExceedsLimit_Throws()
    {
        var engine = Engine(new CsEvalOptions
        {
            Constraints = new ExecutionConstraints { MaxStatements = 100 }
        });
        var ex = Assert.Catch<CsEvalExecutionLimitException>(
            () => engine.Evaluate("{ while (true) {} }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.Statements));
        Assert.That(ex.StatementsExecuted, Is.GreaterThanOrEqualTo(100));
    }

    [Test]
    public void MaxStatements_UnderLimit_Succeeds()
    {
        var engine = Engine(new CsEvalOptions
        {
            Constraints = new ExecutionConstraints { MaxStatements = 1000 }
        });
        var result = engine.Evaluate<int>("{ var sum = 0; for (int i = 0; i < 10; i++) sum += i; return sum; }");
        Assert.That(result, Is.EqualTo(45));
    }

    // ═══════════════════════════════════════════════════════════════
    // SEC-02: MaxTimeout
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void MaxTimeout_ExceedsLimit_Throws()
    {
        var engine = Engine(new CsEvalOptions
        {
            Constraints = new ExecutionConstraints { MaxTimeout = TimeSpan.FromMilliseconds(100) }
        });
        Assert.That(() => engine.Evaluate("{ while (true) {} }"),
            Throws.InstanceOf<CsEvalExecutionLimitException>());
    }

    // ═══════════════════════════════════════════════════════════════
    // SEC-02: MaxExpressionDepth
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void MaxExpressionDepth_ExceedsLimit_Throws()
    {
        var engine = Engine(new CsEvalOptions { MaxExpressionDepth = 10 });
        // Deeply nested ternary: a ? a ? a ? ... : 0 : 0 : 0
        var nested = string.Join("", Enumerable.Repeat("true ? (", 15)) +
                     "1" +
                     string.Join("", Enumerable.Repeat(") : 0", 15));
        Assert.That(() => engine.Evaluate(nested),
            Throws.InstanceOf<CsEvalDepthException>());
    }

    // ═══════════════════════════════════════════════════════════════
    // SEC-02: Mutability — constraints can change between evaluations
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Constraints_Mutable_BetweenEvaluations()
    {
        var constraints = new ExecutionConstraints { MaxStatements = 5 };
        var engine = Engine(new CsEvalOptions { Constraints = constraints });

        // First: tight limit -- infinite loop throws
        Assert.That(() => engine.Evaluate("{ while (true) {} }"),
            Throws.InstanceOf<CsEvalExecutionLimitException>());

        // Relax limit
        constraints.MaxStatements = 50_000;

        // Second: relaxed limit -- larger computation succeeds
        var result = engine.Evaluate<int>("{ var sum = 0; for (int i = 0; i < 1000; i++) sum += i; return sum; }");
        Assert.That(result, Is.EqualTo(499500));
    }

    // ═══════════════════════════════════════════════════════════════
    // SEC-02: MaxExpressionDepth immutability
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void MaxExpressionDepth_IsImmutable_SetAtCreation()
    {
        var options = new CsEvalOptions { MaxExpressionDepth = 100 };
        Assert.That(options.MaxExpressionDepth, Is.EqualTo(100));
        // CsEvalOptions is a sealed record with init-only -- cannot change after creation
        // (verified at compile time -- no runtime test needed)
    }

    // ═══════════════════════════════════════════════════════════════
    // SEC-03: Common Mistakes — wrong examples throw
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Mistake_SafeAllowsAssignment()
    {
        var engine = Engine(new CsEvalOptions { Sandbox = SandboxOptions.Safe() });
        engine.SetVariable("x", 0);
        var result = engine.Evaluate("{ x = 999; return x; }");
        Assert.That(result, Is.EqualTo(999));
    }

    [Test]
    public void Mistake_StrictBlocksAssignment()
    {
        var engine = Engine(new CsEvalOptions { Sandbox = SandboxOptions.Strict() });
        engine.SetVariable("x", 0);
        Assert.That(() => engine.Evaluate("{ x = 999; return x; }"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    [Test]
    public void Mistake_AllowedTypes_OpenGeneric_Fails()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted() with
            {
                AllowedTypes = new HashSet<Type> { typeof(List<>) }
            }
        });
        Assert.That(() => engine.Evaluate("new List<int>()"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    [Test]
    public void Mistake_AllowedTypes_ExactConstructed_Succeeds()
    {
        var engine = Engine(new CsEvalOptions
        {
            Sandbox = SandboxOptions.Trusted() with
            {
                AllowedTypes = new HashSet<Type> { typeof(List<int>), typeof(int) }
            }
        });
        Assert.That(engine.Evaluate("new List<int>()"), Is.InstanceOf<List<int>>());
    }

    [Test]
    public void Mistake_PostFreeze_RegisterFunction_Throws()
    {
        var engine = new CsEvalEngine();
        engine.Evaluate("1 + 1");
        Assert.That(() => engine.RegisterFunction("test", args => args[0]),
            Throws.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public void Mistake_PostFreeze_SetVariable_Succeeds()
    {
        var engine = new CsEvalEngine();
        engine.Evaluate("1 + 1");
        engine.SetVariable("x", 42);
        Assert.That(engine.Evaluate("x"), Is.EqualTo(42));
    }

    [Test]
    public void Mistake_ReflectionLeak_GetType_Throws_InTrusted()
    {
        var engine = Engine(new CsEvalOptions { Sandbox = SandboxOptions.Trusted() });
        engine.SetVariable("text", "hello");
        Assert.That(() => engine.Evaluate("text.GetType()"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    [Test]
    public void Mistake_Typeof_Succeeds()
    {
        var engine = Engine(new CsEvalOptions { Sandbox = SandboxOptions.Trusted() });
        Assert.That(engine.Evaluate("typeof(int)"), Is.EqualTo(typeof(int)));
        Assert.That(engine.Evaluate("typeof(string)"), Is.EqualTo(typeof(string)));
    }
}
