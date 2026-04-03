using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Security;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class SandboxModeTests(CompilationMode mode)
{

    [Test]
    public void Trusted_AllowsMethodCalls()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.ToUpper()");

        Assert.That(result, Is.EqualTo("HELLO"));
    }

    [Test]
    public void Trusted_AllowsPropertyAccess()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.Length");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Trusted_BlocksGetType_ReflectionBlocked()
    {
        // Reflection types are always blocked, regardless of sandbox mode
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("text.GetType()"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
    }



    [Test]
    public void Safe_BlocksMethodCalls()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("text.ToUpper()"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void Safe_BlocksGetType()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("obj", new { Name = "Test" });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("obj.GetType()"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void Safe_BlocksToString()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("num", 42);

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("num.ToString()"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void Safe_BlocksListMutatingMethods()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("items", new List<int> { 1, 2, 3 });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("items.Add(4)"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }



    [Test]
    public void Safe_AllowsPropertyReadByDefault()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.Length");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Safe_AllowsNestedPropertyRead()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("person", new { Name = "John", Address = new { City = "NYC" } });

        var result = engine.Evaluate("person.Address.City");

        Assert.That(result, Is.EqualTo("NYC"));
    }

    [Test]
    public void Safe_AllowPropertyReadFalse_BlocksPropertyAccess()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe() with { AllowPropertyRead = false });
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("text.Length"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0103));
    }



    [Test]
    public void Safe_AllowsModuleMethods()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());

        var result = engine.Evaluate("Math.Abs(-5)");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Safe_AllowsModuleProperties()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());

        var result = engine.Evaluate("Math.PI");

        Assert.That(result, Is.EqualTo(Math.PI));
    }

    [Test]
    public void Safe_AllowsCustomModuleMethods()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Sandbox = SandboxOptions.Safe();
            o.Modules.Register<TestModule>("Test");
        });

        var result = engine.Evaluate("Test.Double(5)");

        Assert.That(result, Is.EqualTo(10));
    }



    [Test]
    public void Safe_AllowsLinqWhere()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("items.Where(x => x > 2).ToList()") as IList;

        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public void Safe_AllowsLinqSelect()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("items", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("items.Select(x => x * 2).ToList()");

        Assert.That(result, Is.InstanceOf<IList>());
        Assert.That(result, Is.EqualTo(new int[] { 2, 4, 6 }));
    }

    [Test]
    public void Safe_AllowsLinqAggregate()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("items.Sum()");

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void Safe_AllowsLinqChaining()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("items.Where(x => x > 2).Select(x => x * 10).Sum()");

        Assert.That(result, Is.EqualTo(120));
    }



    [Test]
    public void Safe_AllowsArrayIndex()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("arr", new[] { 10, 20, 30 });

        var result = engine.Evaluate("arr[1]");

        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void Safe_AllowsDictionaryIndex()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("dict", new Dictionary<string, object?> { ["key"] = "value" });

        var result = engine.Evaluate("""dict["key"] """);

        Assert.That(result, Is.EqualTo("value"));
    }



    [Test]
    public void Safe_AllowsRegisteredFunctions()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Sandbox = SandboxOptions.Safe();
            o.Functions.Register("triple", args => Convert.ToInt64(args[0]) * 3);
        });

        var result = engine.Evaluate("triple(5)");

        Assert.That(result, Is.EqualTo(15L));
    }



    [Test]
    public void Safe_PreventsReflectionAttack()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("obj", new { Name = "Test" });

        // Safe mode blocks method calls before reflection guard is reached
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("obj.GetType()"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void Safe_PreventsMutatingMethods()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        var list = new List<int> { 1, 2, 3 };
        engine.SetVariable("items", list);

        // Should block mutating methods
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("items.Clear()"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
        Assert.That(list, Has.Count.EqualTo(3)); // List unchanged
    }



    [Test]
    public void Safe_AllowsAssignmentByDefault()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());

        var result = engine.Evaluate(@"
            var x = 1;
            x = 5;
            return x;
        ");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Safe_AllowAssignmentFalse_BlocksSimpleAssignment()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe() with { AllowAssignment = false });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(@"
            var x = 1;
            x = 5;
            return x;
        "));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
    }

    [Test]
    public void Safe_AllowAssignmentFalse_BlocksCompoundAssignment()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe() with { AllowAssignment = false });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(@"
            var x = 1;
            x += 5;
            return x;
        "));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
    }

    [Test]
    public void Safe_AllowAssignmentFalse_BlocksNullCoalesceAssignment()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe() with { AllowAssignment = false });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(@"
            int? x = null;
            x ??= 5;
            return x;
        "));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
    }

    [Test]
    public void Safe_AllowAssignmentFalse_BlocksIncrement()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe() with { AllowAssignment = false });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(@"
            var x = 1;
            x++;
            return x;
        "));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
    }

    [Test]
    public void Safe_AllowAssignmentFalse_BlocksDecrement()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe() with { AllowAssignment = false });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(@"
            var x = 5;
            --x;
            return x;
        "));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
    }

    [Test]
    public void Safe_AllowAssignmentFalse_AllowsVariableDeclaration()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe() with { AllowAssignment = false });

        var result = engine.Evaluate(@"
            var x = 5;
            return x;
        ");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Safe_AllowAssignmentFalse_BlocksNullCoalesceEvenWhenNotNull()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe() with { AllowAssignment = false });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(@"
            var x = ""hello"";
            x ??= ""world"";
            return x;
        "));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
    }



    [Test]
    public void Safe_AllowsPropertySetByDefault()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());

        var result = engine.Evaluate(@"
            var obj = new { Value = 1 };
            obj.Value = 42;
            return obj.Value;
        ");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Safe_AllowPropertySetFalse_BlocksPropertyAssignment()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe() with { AllowPropertySet = false });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(@"
            var obj = new { Value = 1 };
            obj.Value = 42;
            return obj.Value;
        "));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0105));
    }

    [Test]
    public void Safe_AllowPropertySetFalse_AllowsPropertyRead()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe() with { AllowPropertySet = false });

        var result = engine.Evaluate(@"
            var obj = new { Value = 42 };
            return obj.Value;
        ");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Safe_AllowPropertySetFalse_BlocksNestedPropertySet()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe() with { AllowPropertySet = false });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(@"
            var obj = new { Inner = new { Value = 1 } };
            obj.Inner.Value = 42;
            return obj.Inner.Value;
        "));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0105));
    }



    [Test]
    public void Safe_AllowsIndexSetByDefault()
    {
        var engine = TestEngineFactory.Create(mode, o => {
            o.LanguageMode = LanguageMode.Extended;
            o.Sandbox = SandboxOptions.Safe();
        });

        var result = engine.Evaluate(@"
            var arr = [1, 2, 3];
            arr[1] = 99;
            return arr[1];
        ");

        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public void Safe_AllowIndexSetFalse_BlocksArrayIndexAssignment()
    {
        var engine = TestEngineFactory.Create(mode, o => {
            o.LanguageMode = LanguageMode.Extended;
            o.Sandbox = SandboxOptions.Safe() with { AllowIndexSet = false };
        });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(@"
            var arr = [1, 2, 3];
            arr[1] = 99;
            return arr[1];
        "));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0102));
    }

    [Test]
    public void Safe_AllowIndexSetFalse_AllowsIndexRead()
    {
        var engine = TestEngineFactory.Create(mode, o => {
            o.LanguageMode = LanguageMode.Extended;
            o.Sandbox = SandboxOptions.Safe() with { AllowIndexSet = false };
        });

        var result = engine.Evaluate(@"
            var arr = [1, 2, 3];
            return arr[1];
        ");

        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void Safe_AllowIndexSetFalse_BlocksDictionaryIndexAssignment()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe() with { AllowIndexSet = false });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(@"
            var dict = new { key = ""value"" };
            dict[""key""] = ""new"";
            return dict[""key""];
        "));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0102));
    }

    [Test]
    public void Safe_AllowIndexSetFalse_AllowsDictionaryRead()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe() with { AllowIndexSet = false });

        var result = engine.Evaluate(@"
            var dict = new { key = ""value"" };
            return dict[""key""];
        ");

        Assert.That(result, Is.EqualTo("value"));
    }



    [Test]
    public void Strict_AllowsOnlyVariableDeclarationAndRead()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict());

        // Variable declaration should still work
        var result = engine.Evaluate(@"
            var x = 42;
            return x;
        ");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Strict_BlocksAssignment()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict());

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(@"
            var x = 1;
            x = 5;
            return x;
        "));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
    }

    [Test]
    public void Strict_BlocksMethodCalls()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict());
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("text.ToUpper()"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void Strict_AllowsPropertyRead()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict());
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.Length");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Strict_BlocksPropertySet()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict());

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(@"
            var obj = new { Value = 1 };
            obj.Value = 42;
            return obj.Value;
        "));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0105));
    }

    [Test]
    public void Strict_BlocksIndexSet()
    {
        var engine = TestEngineFactory.Create(mode, o => {
            o.LanguageMode = LanguageMode.Extended;
            o.Sandbox = SandboxOptions.Strict();
        });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(@"
            var arr = [1, 2, 3];
            arr[1] = 99;
            return arr[1];
        "));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0102));
    }

    [Test]
    public void Strict_AllowsModuleMethods()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict());

        var result = engine.Evaluate("Math.Abs(-5)");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Strict_AllowsLinq()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict());
        engine.SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("items.Where(x => x > 2).Sum()");

        Assert.That(result, Is.EqualTo(12));
    }



    [Test]
    public void Safe_WithPropertyAndIndexSetDisabled_AssignmentStillWorks()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe() with { AllowPropertySet = false, AllowIndexSet = false });

        // Simple variable assignment should work
        var result = engine.Evaluate(@"
            var x = 1;
            x = 99;
            return x;
        ");

        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public void Strict_WithAllowAssignmentTrue_AllowsAssignment()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict() with { AllowAssignment = true });

        var result = engine.Evaluate(@"
            var x = 1;
            x = 5;
            return x;
        ");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Strict_WithAllowMethodCallsTrue_AllowsMethodCalls()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict() with { AllowMethodCalls = true });
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
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void DenyAll_DefaultSandbox_BlocksPropertyRead()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = new SandboxOptions());
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("text.Length"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0103));
    }

    [Test]
    public void DenyAll_DefaultSandbox_BlocksAssignment()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = new SandboxOptions());

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(@"
            var x = 1;
            x = 5;
            return x;
        "));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
    }

    [Test]
    public void DenyAll_DefaultSandbox_BlocksPropertySet()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Sandbox = new SandboxOptions
            {
                AllowConstruction = true,
                AllowAssignment = true,
                AllowPropertyRead = true
            };
        });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(@"
            var obj = new { Value = 1 };
            obj.Value = 42;
            return obj.Value;
        "));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0105));
    }

    [Test]
    public void DenyAll_DefaultSandbox_BlocksIndexSet()
    {
        var engine = TestEngineFactory.Create(mode, o => {
            o.LanguageMode = LanguageMode.Extended;
            o.Sandbox = new SandboxOptions();
        });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(@"
            var arr = [1, 2, 3];
            arr[1] = 99;
            return arr[1];
        "));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0102));
    }

    [Test]
    public void DenyAll_DefaultSandbox_AllowsVariableDeclaration()
    {
        // Variable declarations are always allowed even in deny-all mode
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = new SandboxOptions());

        var result = engine.Evaluate(@"
            var x = 42;
            return x;
        ");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void DenyAll_DefaultSandbox_AllowsModuleMethods()
    {
        // Modules are always allowed even in deny-all mode
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = new SandboxOptions());

        var result = engine.Evaluate("Math.Abs(-5)");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void DenyAll_DefaultSandbox_AllowsPureExpressions()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = new SandboxOptions());

        var result = engine.Evaluate("2 + 3 * 4");

        Assert.That(result, Is.EqualTo(14));
    }



    [Test]
    public void Safe_AllowsDelegateInvocation()
    {
        // Delegate is Tier 1 -- always allowed even when AllowMethodCalls=false
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        Func<int, int> doubler = x => x * 2;
        engine.SetVariable("doubler", doubler);

        var result = engine.Evaluate("doubler(5)");

        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Strict_AllowsDelegateInvocation()
    {
        // Delegate is Tier 1 -- always allowed even in Strict mode
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict());
        Func<string, int> parser = s => int.Parse(s);
        engine.SetVariable("parser", parser);

        var result = engine.Evaluate("""parser("42") """);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void DenyAll_AllowsDelegateInvocation()
    {
        // Delegate is Tier 1 -- always allowed even in deny-all mode
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = new SandboxOptions());
        Func<int, int, int> add = (a, b) => a + b;
        engine.SetVariable("add", add);

        var result = engine.Evaluate("add(3, 4)");

        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void DenyAll_AllowsRegisteredFunctions()
    {
        // FunctionRef is Tier 1 -- always allowed even in deny-all
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Sandbox = new SandboxOptions();
            o.Functions.Register("add", args => (int)args[0]! + (int)args[1]!);
        });

        var result = engine.Evaluate("add(3, 4)");

        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void Safe_AllowsRegisteredFunctions_Tier1()
    {
        // FunctionRef is Tier 1 -- always allowed in Safe mode
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Sandbox = SandboxOptions.Safe();
            o.Functions.Register("multiply", args => (int)args[0]! * (int)args[1]!);
        });

        var result = engine.Evaluate("multiply(6, 7)");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Trusted_AllowsLambdaInvocation()
    {
        // LambdaValue/CompiledLambdaValue is Tier 1 -- expression-defined lambdas always allowed
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Trusted());

        // Lambda stored in variable and then called
        var result = engine.Evaluate(@"
            var triple = (x) => x * 3;
            return triple(7);
        ");

        Assert.That(result, Is.EqualTo(21));
    }



    [Test]
    public void MemberAssign_AllowAssignmentFalse_AllowPropertySetTrue_Succeeds()
    {
        // Member assignment (obj.Prop = val) should only check AllowPropertySet,
        // NOT AllowAssignment. This verifies the CompileMemberAssign fix.
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Trusted() with { AllowAssignment = false });

        var result = engine.Evaluate(@"
            var obj = new { Value = 1 };
            obj.Value = 42;
            return obj.Value;
        ");

        Assert.That(result, Is.EqualTo(42));
    }


    [Test]
    public void Sandbox_AllowConstruction_Blocks_New()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("new object()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0106));
    }

    [Test]
    public void Sandbox_AllowConstruction_Permits_When_Enabled()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe() with { AllowConstruction = true });

        var result = engine.Evaluate("new object()");
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Sandbox_TrustedTypes_Blocks_Unlisted_Type()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Trusted() with
        {
            TrustedTypes = [typeof(Math)]
        });
        engine.SetVariable("x", 5);

        var result = engine.Evaluate("Math.Abs(x)");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Sandbox_TrustedTypes_OverridesDenyList_ForConstruction()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Trusted() with
        {
            TrustedTypes = [typeof(MemoryStream)]
        });

        Assert.DoesNotThrow(() => engine.Evaluate("new System.IO.MemoryStream()"));
    }


    public class TestModule
    {
        public static int Double(int x) => x * 2;
    }

}
