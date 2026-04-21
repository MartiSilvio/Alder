using Alder.Test._Infrastructure;
using Alder.Runtime.Introspection;

namespace Alder.Test.AOT;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Category("AOT")]
public class GeneratedContextIntegrationTests(CompilationMode mode)
{
    private AlderEngine CreateEngine(bool useGeneratedContext = true)
    {
        return TestEngineFactory.Create(mode, o =>
        {
            if (useGeneratedContext)
                o.Aot.UseGeneratedContext(TestGeneratedContext.Default);
        });
    }

    private AlderEngine CreateEngineWithTypeResolution(bool useGeneratedContext = true)
    {
        return TestEngineFactory.Create(mode, o =>
        {
            if (useGeneratedContext)
                o.Aot.UseGeneratedContext(TestGeneratedContext.Default);
            o.Types.AddAssembly(typeof(TestModel).Assembly);
            o.Types.AddNamespace("Alder.Test.AOT");
        });
    }

    [Test]
    public void GetProperty_UsesGeneratedDispatch()
    {
        var engine = CreateEngine();
        engine.SetVariable("m", new TestModel { Name = "hello" });

        var result = engine.Evaluate("m.Name");

        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void GetNullableProperty_UsesGeneratedDispatch()
    {
        var engine = CreateEngine();
        engine.SetVariable("m", new TestModel { Name = null });

        var result = engine.Evaluate("m.Name");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void SetProperty_UsesGeneratedDispatch()
    {
        var engine = CreateEngine();
        var model = new TestModel();
        engine.SetVariable("m", model);

        engine.Evaluate("""m.Name = "updated" """);

        Assert.That(model.Name, Is.EqualTo("updated"));
    }

    [Test]
    public void GetField_UsesGeneratedDispatch()
    {
        var engine = CreateEngine();
        engine.SetVariable("m", new TestModel());

        var result = engine.Evaluate("m.Id");

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void CreateInstance_DefaultConstructor_UsesGeneratedDispatch()
    {
        var engine = CreateEngineWithTypeResolution();

        var result = engine.Evaluate("new TestModel()");

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<TestModel>());
    }

    [Test]
    public void CreateInstance_ParameterizedConstructor_UsesGeneratedDispatch()
    {
        var engine = CreateEngineWithTypeResolution();

        var result = engine.Evaluate("""new TestModel("x", 42) """);

        Assert.That(result, Is.TypeOf<TestModel>());
        var model = (TestModel)result!;
        Assert.That(model.Name, Is.EqualTo("x"));
        Assert.That(model.Value, Is.EqualTo(42));
    }

    [Test]
    public void GetStaticProperty_UsesGeneratedDispatch()
    {
        var engine = CreateEngineWithTypeResolution();
        TestModel.Label = "test";

        var result = engine.Evaluate("TestModel.Label");

        Assert.That(result, Is.EqualTo("test"));
    }

    [Test]
    public void GetIndex_UsesGeneratedDispatch()
    {
        var engine = CreateEngine();
        var indexed = new TestIndexedModel
        {
            ["key"] = 99
        };
        engine.SetVariable("d", indexed);

        var result = engine.Evaluate("""d["key"] """);

        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public void SetIndex_UsesGeneratedDispatch()
    {
        var engine = CreateEngine();
        var indexed = new TestIndexedModel();
        engine.SetVariable("d", indexed);

        engine.Evaluate("""d["key"] = 42 """);

        Assert.That(indexed["key"], Is.EqualTo(42));
    }

    [Test]
    public void InvokeMethod_InstanceWithArgs_UsesGeneratedDispatch()
    {
        var engine = CreateEngine();
        engine.SetVariable("obj", new TestModel("World", 0));

        var result = engine.Evaluate("obj.Add(3, 4)");

        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void InvokeMethod_InstanceNoArgs_UsesGeneratedDispatch()
    {
        var engine = CreateEngine();
        engine.SetVariable("obj", new TestModel { Name = "World" });

        var result = engine.Evaluate("obj.Greet()");

        Assert.That(result, Is.EqualTo("Hello, World"));
    }

    [Test]
    public void InvokeMethod_VoidMethod_UsesGeneratedDispatch()
    {
        var engine = CreateEngine();
        var model = new TestModel { Value = 5 };
        engine.SetVariable("obj", model);

        engine.Evaluate("obj.IncrementValue()");

        Assert.That(model.Value, Is.EqualTo(6));
    }

    [Test]
    public void InvokeStaticMethod_UsesGeneratedDispatch()
    {
        var engine = CreateEngineWithTypeResolution();

        var result = engine.Evaluate("""TestModel.Parse("42") """);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void NoGeneratedContext_ReflectionFallback()
    {
        var engine = CreateEngine(useGeneratedContext: false);
        engine.SetVariable("m", new TestModel { Name = "reflection" });

        var result = engine.Evaluate("m.Name");

        Assert.That(result, Is.EqualTo("reflection"));
    }

    [Test]
    public void GeneratedTypesAutoRegistered()
    {
        var engine = CreateEngineWithTypeResolution();
        engine.SetVariable("m", new TestModel { Name = "registered", Value = 7 });

        var name = engine.Evaluate("m.Name");
        var value = engine.Evaluate("m.Value");

        Assert.That(name, Is.EqualTo("registered"));
        Assert.That(value, Is.EqualTo(7));
    }

    [Test]
    public void AotFactories_AreEngineScoped()
    {
        var firstEngine = TestEngineFactory.Create(mode, o =>
        {
            o.Aot.UseGeneratedContext(new ConstantDelegateFactoryContext(111));
        });

        var secondEngine = TestEngineFactory.Create(mode, o =>
        {
            o.Aot.UseGeneratedContext(new ConstantDelegateFactoryContext(222));
        });

        var first = firstEngine.Evaluate<Func<int, int>>("x => x + 1");
        var second = secondEngine.Evaluate<Func<int, int>>("x => x + 1");

        Assert.That(first!(5), Is.EqualTo(111));
        Assert.That(second!(5), Is.EqualTo(222));
    }

    [Test]
    public void CaseInsensitiveGeneratedContext_GetMember_UsesReflectionUnderJit()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.IsCaseSensitive = false;
            o.Aot.UseGeneratedContext(new CaseSensitivitySentinelContext());
        });
        engine.SetVariable("m", new CaseSensitivitySentinelType { Name = "reflection-name" });

        var result = engine.Evaluate("m.name");

        Assert.That(result, Is.EqualTo("reflection-name"));
    }

    [Test]
    public void GeneratedContext_ExactCase_GetMember_UsesReflectionUnderJit()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Aot.UseGeneratedContext(new CaseSensitivitySentinelContext());
        });
        engine.SetVariable("m", new CaseSensitivitySentinelType { Name = "reflection-name" });

        var result = engine.Evaluate("m.Name");

        Assert.That(result, Is.EqualTo("reflection-name"));
    }

    [Test]
    public void CaseInsensitiveGeneratedContext_SetMember_UsesReflectionUnderJit()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.IsCaseSensitive = false;
            o.Aot.UseGeneratedContext(new CaseSensitivitySentinelContext());
        });
        var model = new CaseSensitivitySentinelType { Name = "before" };
        engine.SetVariable("m", model);

        engine.Evaluate("""m.name = "after" """);

        Assert.That(model.Name, Is.EqualTo("after"));
    }

    [Test]
    public void CaseInsensitiveGeneratedContext_InvokeMethod_UsesReflectionUnderJit()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.IsCaseSensitive = false;
            o.Aot.UseGeneratedContext(new CaseSensitivitySentinelContext());
        });
        engine.SetVariable("m", new CaseSensitivitySentinelType());

        var result = engine.Evaluate("""m.echo("ping")""");

        Assert.That(result, Is.EqualTo("reflection:ping"));
    }

    [Test]
    public void SimulatedAot_CaseInsensitiveGeneratedContext_GetMember_UsesGeneratedDispatch()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.IsCaseSensitive = false;
            o.Aot.UseGeneratedContext(new CaseSensitivitySentinelContext());
        });
        engine.SetVariable("m", new CaseSensitivitySentinelType { Name = "reflection-name" });

        var result = engine.Evaluate("m.name");

        Assert.That(result, Is.EqualTo("aot-name"));
    }

    [Test]
    public void SimulatedAot_GeneratedContext_ExactCase_GetMember_UsesGeneratedDispatch()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Aot.UseGeneratedContext(new CaseSensitivitySentinelContext());
        });
        engine.SetVariable("m", new CaseSensitivitySentinelType { Name = "reflection-name" });

        var result = engine.Evaluate("m.Name");

        Assert.That(result, Is.EqualTo("aot-name"));
    }

    [Test]
    public void SimulatedAot_CaseInsensitiveGeneratedContext_SetMember_UsesGeneratedDispatch()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.IsCaseSensitive = false;
            o.Aot.UseGeneratedContext(new CaseSensitivitySentinelContext());
        });
        var model = new CaseSensitivitySentinelType { Name = "before" };
        engine.SetVariable("m", model);

        engine.Evaluate("""m.name = "after" """);

        Assert.That(model.Name, Is.EqualTo("aot:after"));
    }

    [Test]
    public void SimulatedAot_CaseInsensitiveGeneratedContext_InvokeMethod_UsesGeneratedDispatch()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.IsCaseSensitive = false;
            o.Aot.UseGeneratedContext(new CaseSensitivitySentinelContext());
        });
        engine.SetVariable("m", new CaseSensitivitySentinelType());

        var result = engine.Evaluate("""m.echo("ping")""");

        Assert.That(result, Is.EqualTo("aot:ping"));
    }

    [TearDown]
    public void ResetStaticState()
    {
        TestModel.Label = "default";
        TestModel.Counter = 0;
    }

    private sealed class ConstantDelegateFactoryContext(int constant) : Alder.Aot.AlderTypeContext
    {
        public override IReadOnlyList<Alder.Aot.TypedDispatch> GetTypeMetadata() => [];

        public override IReadOnlyDictionary<Type, Func<object, Delegate>>? GetDelegateFactories()
        {
            return new Dictionary<Type, Func<object, Delegate>>
            {
                [typeof(Func<int, int>)] = _ => (Func<int, int>)(_ => constant)
            };
        }
    }

    private sealed class CaseSensitivitySentinelContext : Alder.Aot.AlderTypeContext
    {
        private static readonly IReadOnlyList<Alder.Aot.TypedDispatch> Metadata =
        [
            new CaseSensitivitySentinelDispatch()
        ];

        public override IReadOnlyList<Alder.Aot.TypedDispatch> GetTypeMetadata() => Metadata;
    }

    private sealed class CaseSensitivitySentinelDispatch : Alder.Aot.TypedDispatch
    {
        public override Type Type => typeof(CaseSensitivitySentinelType);

        public override bool TryGet(string name, object instance, out object? value)
        {
            if (name == nameof(CaseSensitivitySentinelType.Name))
            {
                value = "aot-name";
                return true;
            }

            value = null;
            return false;
        }

        public override bool TrySet(string name, object instance, object? value)
        {
            if (name == nameof(CaseSensitivitySentinelType.Name))
            {
                ((CaseSensitivitySentinelType)instance).Name = $"aot:{value}";
                return true;
            }

            return false;
        }

        public override bool TryInvoke(string name, object instance, object?[] args, out object? result)
        {
            if (name == nameof(CaseSensitivitySentinelType.Echo))
            {
                result = $"aot:{args[0]}";
                return true;
            }

            result = null;
            return false;
        }
    }

    private sealed class CaseSensitivitySentinelType
    {
        public string Name { get; set; } = string.Empty;

        public string Echo(string value) => $"reflection:{value}";
    }
}
