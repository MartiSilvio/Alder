namespace CsEval.Test.AOT;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Category("AOT")]
public class GeneratedContextIntegrationTests(CompilationMode mode)
{
    private CsEvalEngine CreateEngine(bool useGeneratedContext = true)
    {
        var engine = TestEngineFactory.Create(mode);
        if (useGeneratedContext)
            engine.UseGeneratedContext(TestGeneratedContext.Default);
        return engine;
    }

    private CsEvalEngine CreateEngineWithTypeResolution(bool useGeneratedContext = true)
    {
        var engine = CreateEngine(useGeneratedContext);
        engine.RegisterAssembly(typeof(TestModel).Assembly);
        engine.RegisterNamespace("CsEval.Test.AOT");
        return engine;
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

        engine.Evaluate("m.Name = \"updated\"");

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

        var result = engine.Evaluate("new TestModel(\"x\", 42)");

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
        var indexed = new TestIndexedModel();
        indexed["key"] = 99;
        engine.SetVariable("d", indexed);

        var result = engine.Evaluate("d[\"key\"]");

        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public void SetIndex_UsesGeneratedDispatch()
    {
        var engine = CreateEngine();
        var indexed = new TestIndexedModel();
        engine.SetVariable("d", indexed);

        engine.Evaluate("d[\"key\"] = 42");

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

        var result = engine.Evaluate("TestModel.Parse(\"42\")");

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

    [TearDown]
    public void ResetStaticState()
    {
        TestModel.Label = "default";
        TestModel.Counter = 0;
    }
}
