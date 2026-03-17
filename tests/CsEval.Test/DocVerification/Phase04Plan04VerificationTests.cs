namespace CsEval.Test.DocVerification;

public static class Phase04Plan04Extensions
{
    public static string Shout(this string s) => s.ToUpper() + "!";
}

public class Plan04InitTarget
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class Phase04Plan04VerificationTests(CompilationMode mode)
{
    private CsEvalEngine Engine()
        => new(CsEvalOptions.Default with { CompilationMode = mode });

    // ═══════════════════════════════════════════════════════════════
    // ENG-09: Basic construction
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NewOperator_BasicConstruction_ListOfInt()
    {
        var result = Engine().Evaluate("new List<int>()");
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<List<int>>());
    }

    [Test]
    public void NewOperator_ConstructionWithArgs_DateTime()
    {
        var result = Engine().Evaluate<DateTime>("new DateTime(2024, 1, 1)");
        Assert.That(result.Year, Is.EqualTo(2024));
        Assert.That(result.Month, Is.EqualTo(1));
        Assert.That(result.Day, Is.EqualTo(1));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-09: Object initializer
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NewOperator_ObjectInitializer_SetsProperties()
    {
        var engine = Engine()
            .RegisterAssembly(typeof(Plan04InitTarget).Assembly)
            .RegisterNamespace("CsEval.Test.DocVerification");

        var result = engine.Evaluate("new Plan04InitTarget { Name = \"test\", Value = 42 }");
        Assert.That(result, Is.InstanceOf<Plan04InitTarget>());
        var obj = (Plan04InitTarget)result!;
        Assert.That(obj.Name, Is.EqualTo("test"));
        Assert.That(obj.Value, Is.EqualTo(42));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-09: Collection initializer
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NewOperator_CollectionInitializer_ListOfInt()
    {
        var result = Engine().Evaluate("new List<int> { 1, 2, 3 }");
        Assert.That(result, Is.InstanceOf<List<int>>());
        Assert.That(((List<int>)result!).Count, Is.EqualTo(3));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-09: Indexer initializer
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NewOperator_IndexerInitializer_Dictionary()
    {
        var result = Engine().Evaluate("new Dictionary<string, int> { [\"a\"] = 1, [\"b\"] = 2 }");
        Assert.That(result, Is.InstanceOf<Dictionary<string, int>>());
        var dict = (Dictionary<string, int>)result!;
        Assert.That(dict.Count, Is.EqualTo(2));
        Assert.That(dict["a"], Is.EqualTo(1));
        Assert.That(dict["b"], Is.EqualTo(2));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-09: Array creation with initializer
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NewOperator_ArrayWithInitializer()
    {
        var result = Engine().Evaluate("new int[] { 10, 20, 30 }");
        Assert.That(result, Is.InstanceOf<int[]>());
        var arr = (int[])result!;
        Assert.That(arr.Length, Is.EqualTo(3));
        Assert.That(arr[0], Is.EqualTo(10));
        Assert.That(arr[1], Is.EqualTo(20));
        Assert.That(arr[2], Is.EqualTo(30));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-09: Sized array
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NewOperator_SizedArray()
    {
        var result = Engine().Evaluate("new int[3]");
        Assert.That(result, Is.InstanceOf<int[]>());
        Assert.That(((int[])result!).Length, Is.EqualTo(3));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-09: Tuple creation
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NewOperator_TupleCreation()
    {
        var result = Engine().Evaluate("(1, \"hello\")");
        Assert.That(result, Is.Not.Null);
        var tuple = (System.Runtime.CompilerServices.ITuple)result!;
        Assert.That(tuple[0], Is.EqualTo(1));
        Assert.That(tuple[1], Is.EqualTo("hello"));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-09: AllowConstruction=false blocks construction
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NewOperator_SafeSandbox_BlocksConstruction()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });

        Assert.That(() => engine.Evaluate("new List<int>()"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-09: AllowedTypes restriction
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NewOperator_AllowedTypes_AllowedTypeWorks()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Trusted() with
            {
                AllowedTypes = new HashSet<Type> { typeof(List<int>), typeof(int) }
            }
        });

        var result = engine.Evaluate("new List<int>()");
        Assert.That(result, Is.InstanceOf<List<int>>());
    }

    [Test]
    public void NewOperator_AllowedTypes_NonAllowedTypeBlocked()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Trusted() with
            {
                AllowedTypes = new HashSet<Type> { typeof(List<int>), typeof(int) }
            }
        });

        Assert.That(() => engine.Evaluate("new Dictionary<string, int>()"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-10: Instance method
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void MethodInvocation_InstanceMethod_ToUpper()
    {
        var engine = Engine().SetVariable("name", "hello");
        Assert.That(engine.Evaluate<string>("name.ToUpper()"), Is.EqualTo("HELLO"));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-10: Static method
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void MethodInvocation_StaticMethod_MathMax()
    {
        Assert.That(Engine().Evaluate<int>("Math.Max(3, 7)"), Is.EqualTo(7));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-10: Overload resolution
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void MethodInvocation_OverloadResolution_DoubleVsString()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate<int>("Convert.ToInt32(42.9)"), Is.EqualTo(43));
        Assert.That(engine.Evaluate<int>("Convert.ToInt32(\"42\")"), Is.EqualTo(42));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-10: Generic method (explicit type args)
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void MethodInvocation_GenericMethod_ArrayEmpty()
    {
        var result = Engine().Evaluate("Array.Empty<int>()");
        Assert.That(result, Is.InstanceOf<int[]>());
        Assert.That(((int[])result!).Length, Is.EqualTo(0));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-10: Named arguments
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void MethodInvocation_NamedArguments()
    {
        var engine = Engine();
        var result = engine.Evaluate<string>("string.Format(format: \"{0} {1}\", arg0: \"hello\", arg1: \"world\")");
        Assert.That(result, Is.EqualTo("hello world"));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-10: out var
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void MethodInvocation_OutVar_TryParse()
    {
        var result = Engine().Evaluate<int>("{ int.TryParse(\"42\", out var x); return x; }");
        Assert.That(result, Is.EqualTo(42));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-10: params array
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void MethodInvocation_ParamsArray_StringFormat()
    {
        var result = Engine().Evaluate<string>("string.Format(\"{0} {1}\", \"a\", \"b\")");
        Assert.That(result, Is.EqualTo("a b"));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-10: Extension methods
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void MethodInvocation_ExtensionMethod()
    {
        var engine = Engine()
            .RegisterExtensionMethods(typeof(Phase04Plan04Extensions))
            .SetVariable("name", "hello");

        Assert.That(engine.Evaluate<string>("name.Shout()"), Is.EqualTo("HELLO!"));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-10: AllowMethodCalls=false blocks method calls
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void MethodInvocation_SafeSandbox_BlocksMethodCalls()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });
        engine.SetVariable("name", "hello");

        Assert.That(() => engine.Evaluate("name.ToUpper()"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-10: Static method (int.Parse)
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void MethodInvocation_StaticMethod_IntParse()
    {
        Assert.That(Engine().Evaluate<int>("int.Parse(\"42\")"), Is.EqualTo(42));
    }
}
