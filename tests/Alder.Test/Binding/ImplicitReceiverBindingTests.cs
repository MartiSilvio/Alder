using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Test.Binding;

[TestFixture]
public sealed class ImplicitReceiverBindingTests
{
    private sealed record ReceiverModel(decimal Price, int Tool, int Math);
    private sealed class ToolModule
    {
        public static int Value() => 42;
    }

    [Test]
    public void Identifier_BindsToReceiverMember_WhenImplicitReceiverEnabled()
    {
        var engine = new AlderEngine();
        var parsed = engine.Parse("Price");
        var runtime = engine.GetContextForCompiled().CreateChild();
        runtime.Define("it", null, typeof(ReceiverModel));

        var binder = new Binder();
        var context = new BindingContext(runtime, new BindingReceiver(typeof(ReceiverModel), "it"));

        var bound = binder.Bind(parsed.Ast, context);

        Assert.That(bound, Is.TypeOf<BoundPropertyAccessExpr>());
        var property = (BoundPropertyAccessExpr)bound;
        Assert.That(property.Target, Is.TypeOf<BoundIdentifierExpr>());
        Assert.That(((BoundIdentifierExpr)property.Target).Name, Is.EqualTo("it"));
    }

    [Test]
    public void Identifier_LocalWinsOverReceiverMember()
    {
        var engine = new AlderEngine();
        var parsed = engine.Parse("Price");
        var runtime = engine.GetContextForCompiled().CreateChild();
        runtime.Define("it", null, typeof(ReceiverModel));

        var binder = new Binder();
        var context = new BindingContext(runtime, new BindingReceiver(typeof(ReceiverModel), "it"));
        context.DeclareLocal("Price", new BoundType(typeof(decimal)));

        var bound = binder.Bind(parsed.Ast, context);

        Assert.That(bound, Is.TypeOf<BoundIdentifierExpr>());
        Assert.That(((BoundIdentifierExpr)bound).Name, Is.EqualTo("Price"));
    }

    [Test]
    public void Identifier_StillResolvesTypes_WhenImplicitReceiverEnabled()
    {
        var engine = new AlderEngine();
        var parsed = engine.Parse("DateTime");
        var runtime = engine.GetContextForCompiled().CreateChild();
        runtime.Define("it", null, typeof(ReceiverModel));

        var binder = new Binder();
        var context = new BindingContext(runtime, new BindingReceiver(typeof(ReceiverModel), "it"));

        var bound = binder.Bind(parsed.Ast, context);

        Assert.That(bound, Is.TypeOf<BoundTypeRefExpr>());
        Assert.That(((BoundTypeRefExpr)bound).TargetType, Is.EqualTo(typeof(DateTime)));
    }

    [Test]
    public void Identifier_ReceiverMemberWinsOverModuleName()
    {
        var engine = new AlderEngine(options => options.Modules.Register<ToolModule>("Tool", instance: null));
        var parsed = engine.Parse("Tool");
        var runtime = engine.GetContextForCompiled().CreateChild();
        runtime.Define("it", null, typeof(ReceiverModel));

        var binder = new Binder();
        var context = new BindingContext(runtime, new BindingReceiver(typeof(ReceiverModel), "it"));

        var bound = binder.Bind(parsed.Ast, context);

        Assert.That(bound, Is.TypeOf<BoundPropertyAccessExpr>());
        Assert.That(((BoundPropertyAccessExpr)bound).Property.Name, Is.EqualTo(nameof(ReceiverModel.Tool)));
    }

    [Test]
    public void Identifier_ReceiverMemberWinsOverTypeName()
    {
        var engine = new AlderEngine();
        var parsed = engine.Parse("Math");
        var runtime = engine.GetContextForCompiled().CreateChild();
        runtime.Define("it", null, typeof(ReceiverModel));

        var binder = new Binder();
        var context = new BindingContext(runtime, new BindingReceiver(typeof(ReceiverModel), "it"));

        var bound = binder.Bind(parsed.Ast, context);

        Assert.That(bound, Is.TypeOf<BoundPropertyAccessExpr>());
        Assert.That(((BoundPropertyAccessExpr)bound).Property.Name, Is.EqualTo(nameof(ReceiverModel.Math)));
    }
}
