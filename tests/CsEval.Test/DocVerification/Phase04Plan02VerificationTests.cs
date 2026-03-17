using System.Reflection;
using CsEval.Attributes;

namespace CsEval.Test.DocVerification;

public static class Phase04Plan02Extensions
{
    public static string Shout(this string s) => s.ToUpper() + "!";
}

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class Phase04Plan02VerificationTests(CompilationMode mode)
{
    private CsEvalEngine Engine()
        => TestEngineFactory.Create(mode);

    // ═══════════════════════════════════════════════════════════════
    // Helper types for registration tests
    // ═══════════════════════════════════════════════════════════════

    public class StringUtils
    {
        public string Reverse(string s) => new(s.Reverse().ToArray());
        public string Upper(string s) => s.ToUpper();
    }

    public class Helpers
    {
        [CsEvalFunction]
        public int Double(int n) => n * 2;

        [CsEvalFunction]
        public int Triple(int n) => n * 3;
    }

    public class Selective
    {
        [CsEvalFunction]
        public int Allowed() => 1;

        public int Hidden() => 2;
    }

    [CsEvalModule("tools", ExplicitOnly = true)]
    public class ToolsModule
    {
        [CsEvalFunction("hi")]
        public string Greet(string name) => $"Hello, {name}!";

        public string Secret() => "not visible";
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-04: RegisterFunction
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void RegisterFunction_Lambda_CallInExpression()
    {
        var engine = Engine()
            .RegisterFunction("add", args => (int)args[0]! + (int)args[1]!);

        Assert.That(engine.Evaluate<int>("add(3, 4)"), Is.EqualTo(7));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-04: RegisterModule<T>
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void RegisterModule_Generic_CallModuleMethod()
    {
        var engine = Engine()
            .RegisterModule<StringUtils>("Str");

        Assert.That(engine.Evaluate<string>(@"Str.Upper(""hello"")"), Is.EqualTo("HELLO"));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-04: RegisterModule with explicitOnly
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void RegisterModule_ExplicitOnly_OnlyAttributedMethodsExposed()
    {
        var engine = Engine()
            .RegisterModule<Selective>("sel", explicitOnly: true);

        Assert.That(engine.Evaluate<int>("sel.Allowed()"), Is.EqualTo(1));

        Assert.That(() => engine.Evaluate("sel.Hidden()"), Throws.InstanceOf<CsEvalException>());
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-04: RegisterFromType<T>
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void RegisterFromType_MethodsAsTopLevelFunctions()
    {
        var engine = Engine()
            .RegisterFromType<Helpers>();

        Assert.That(engine.Evaluate<int>("Double(5)"), Is.EqualTo(10));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-04: RegisterFromAssembly
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void RegisterFromAssembly_ScansAttributedClasses()
    {
        var engine = Engine()
            .RegisterFromAssembly(typeof(ToolsModule).Assembly);

        Assert.That(engine.Evaluate<string>(@"tools.hi(""World"")"), Is.EqualTo("Hello, World!"));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-04: [CsEvalFunction("customName")]
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void CsEvalFunction_CustomName_UsedInExpression()
    {
        var engine = Engine()
            .RegisterModule<ToolsModule>("tools", explicitOnly: true);

        Assert.That(engine.Evaluate<string>(@"tools.hi(""test"")"), Is.EqualTo("Hello, test!"));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-04: Built-in Math module
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void BuiltIn_Math_Abs()
    {
        Assert.That(Engine().Evaluate<int>("Math.Abs(-5)"), Is.EqualTo(5));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-04: Built-in Convert module
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void BuiltIn_Convert_ToInt32()
    {
        Assert.That(Engine().Evaluate<int>(@"Convert.ToInt32(""42"")"), Is.EqualTo(42));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-04: GetRegisteredModules
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void GetRegisteredModules_ContainsBuiltInsAndCustom()
    {
        var engine = Engine()
            .RegisterModule<StringUtils>("Str");

        var modules = engine.GetRegisteredModules();

        Assert.That(modules.ContainsKey("Math"), Is.True);
        Assert.That(modules.ContainsKey("Convert"), Is.True);
        Assert.That(modules.ContainsKey("Str"), Is.True);
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-04: Fluent chaining
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void FluentChaining_CompilesToSingleStatement()
    {
        var engine = Engine()
            .RegisterFunction("add", args => (int)args[0]! + (int)args[1]!)
            .RegisterModule<StringUtils>("Str")
            .SetVariable("x", 10);

        Assert.That(engine.Evaluate<int>("add(x, 5)"), Is.EqualTo(15));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-04: Registration after freeze throws
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void RegisterFunction_AfterFreeze_ThrowsInvalidOperationException()
    {
        var engine = Engine();
        engine.Evaluate("1 + 1");

        Assert.That(() => engine.RegisterFunction("late", args => 0),
            Throws.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public void RegisterModule_AfterFreeze_ThrowsInvalidOperationException()
    {
        var engine = Engine();
        engine.Evaluate("1 + 1");

        Assert.That(() => engine.RegisterModule<StringUtils>("Str"),
            Throws.InstanceOf<InvalidOperationException>());
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-05: RegisterAssembly + use type in expression
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void RegisterAssembly_UseTypeInExpression()
    {
        var engine = Engine()
            .RegisterAssembly(typeof(System.Text.StringBuilder).Assembly)
            .RegisterNamespace("System.Text");

        var result = engine.Evaluate("new StringBuilder().Append(\"hi\").ToString()");
        Assert.That(result, Is.EqualTo("hi"));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-05: RegisterNamespace
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void RegisterNamespace_EnablesShortTypeName()
    {
        var engine = Engine()
            .RegisterAssembly(typeof(System.Text.StringBuilder).Assembly)
            .RegisterNamespace("System.Text");

        var result = engine.Evaluate("new StringBuilder()");
        Assert.That(result, Is.InstanceOf<System.Text.StringBuilder>());
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-05: RegisterExtensionMethods
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void RegisterExtensionMethods_CallExtensionInExpression()
    {
        var engine = Engine()
            .RegisterExtensionMethods(typeof(Phase04Plan02Extensions))
            .SetVariable("name", "hello");

        Assert.That(engine.Evaluate<string>("name.Shout()"), Is.EqualTo("HELLO!"));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-05: Type resolution precedence — built-in keyword
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void TypeResolution_BuiltInKeyword_NoRegistration()
    {
        Assert.That(Engine().Evaluate<int>("int.MaxValue"), Is.EqualTo(int.MaxValue));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-05: Type resolution — implicit BCL imports
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void TypeResolution_ImplicitBcl_ListAvailable()
    {
        var result = Engine().Evaluate("new List<int>()");
        Assert.That(result, Is.InstanceOf<List<int>>());
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-05: AllowedTypes
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void AllowedTypes_AllowedTypeWorks()
    {
        var engine = TestEngineFactory.Create(mode, new CsEvalOptions {
                        Sandbox = SandboxOptions.Trusted() with
            {
                AllowedTypes = new HashSet<Type> { typeof(List<int>), typeof(int) }
            }
        });

        var result = engine.Evaluate("new List<int>()");
        Assert.That(result, Is.InstanceOf<List<int>>());
    }

    [Test]
    public void AllowedTypes_NonAllowedTypeThrows()
    {
        var engine = TestEngineFactory.Create(mode, new CsEvalOptions {
                        Sandbox = SandboxOptions.Trusted() with
            {
                AllowedTypes = new HashSet<Type> { typeof(int) }
            }
        });

        Assert.That(() => engine.Evaluate("new List<int>()"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-05: Registration after freeze throws
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void RegisterAssembly_AfterFreeze_ThrowsInvalidOperationException()
    {
        var engine = Engine();
        engine.Evaluate("1 + 1");

        Assert.That(() => engine.RegisterAssembly(typeof(string).Assembly),
            Throws.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public void RegisterNamespace_AfterFreeze_ThrowsInvalidOperationException()
    {
        var engine = Engine();
        engine.Evaluate("1 + 1");

        Assert.That(() => engine.RegisterNamespace("System.Net"),
            Throws.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public void RegisterExtensionMethods_AfterFreeze_ThrowsInvalidOperationException()
    {
        var engine = Engine();
        engine.Evaluate("1 + 1");

        Assert.That(() => engine.RegisterExtensionMethods(typeof(Phase04Plan02Extensions)),
            Throws.InstanceOf<InvalidOperationException>());
    }
}
