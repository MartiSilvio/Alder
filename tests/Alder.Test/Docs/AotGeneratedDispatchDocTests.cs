using Alder.Aot;
using Alder.Test.AOT;
using Alder.Test._Infrastructure;

namespace Alder.Test.Docs;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class AotGeneratedDispatchDocTests(CompilationMode mode)
{
    [Test]
    public void GeneratedContext_ProvidesReflectionFreeMemberAndMethodDispatch()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Aot.UseGeneratedContext(TestGeneratedContext.Default);
        });

        var model = new TestModel { Name = "Ada", Value = 10 };
        engine.SetVariable("model", model);

        Assert.That(engine.Evaluate<string>("model.Name"), Is.EqualTo("Ada"));
        Assert.That(engine.Evaluate<int>("model.Add(3, 4)"), Is.EqualTo(7));

        engine.Evaluate("model.Value = 42");
        Assert.That(model.Value, Is.EqualTo(42));
    }

    [Test]
    public void GeneratedContext_RequiresConcreteRuntimeTypesReachedByExpressions()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Aot.UseGeneratedContext(TestGeneratedContext.Default);
        });

        var indexed = new TestIndexedModel
        {
            ["size"] = 12
        };
        engine.SetVariable("indexed", indexed);

        Assert.That(engine.Evaluate<int>("""indexed["size"]"""), Is.EqualTo(12));
    }

    [Test]
    public void TypeResolutionAndGeneratedDispatch_AreSeparateConcerns()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Aot.UseGeneratedContext(TestGeneratedContext.Default);
            options.Types.AddAssembly(typeof(TestModel).Assembly);
            options.Types.AddNamespace("Alder.Test.AOT");
        });

        var result = engine.Evaluate("""new TestModel("x", 42).Value""");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void NativeAotChecklist_RegisterGeneratedContext()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Aot.UseGeneratedContext(TestGeneratedContext.Default);
        });

        engine.SetVariable("model", new TestModel { Name = "Ada", Value = 10 });

        var accepted = engine.Evaluate<bool>(
            """model.Greet() == "Hello, Ada" && model.Value == 10""");

        Assert.That(accepted, Is.True);
    }

    [Test]
    public void NativeAotChecklist_TypeResolutionAndGeneratedDispatch()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Aot.UseGeneratedContext(TestGeneratedContext.Default);
            options.Types.AddAssembly(typeof(TestModel).Assembly);
            options.Types.AddNamespace("Alder.Test.AOT");
        });

        var value = engine.Evaluate<int>(
            """new TestModel("sample", 42).Value""");

        Assert.That(value, Is.EqualTo(42));
    }
}
