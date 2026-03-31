using Alder;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using NUnit.Framework;

namespace Alder.Test.Binding;

[TestFixture]
public sealed class ExtensionBindingVerificationTests
{
    [Test]
    public void Binder_ResolvesExtensionMethods_AsResolvedCallWithFlag()
    {
        var engine = new AlderEngine();
        engine.SetVariable("items", new[] { 1, 2, 3, 4, 5 });

        var parsed = engine.Parse("items.Where(x => x > 2).ToArray()");
        var ctx = new BindingContext(engine.GetContextForCompiled());
        var binder = new Binder();
        var bound = binder.Bind(parsed.Ast, ctx);

        // The outermost call should be a resolved extension call (ToArray)
        Assert.That(bound, Is.TypeOf<BoundResolvedCallExpr>(), 
            $"Expected BoundResolvedCallExpr but got {bound.GetType().Name}");
        var toArray = (BoundResolvedCallExpr)bound;
        Assert.That(toArray.IsExtensionCall, Is.True, "ToArray should be IsExtensionCall");
        Assert.That(toArray.IsStaticCall, Is.True, "Extension calls are static");
        Assert.That(toArray.SelectedMethod.Name, Is.EqualTo("ToArray"));

        // The callee's target should be the Where() call - also a resolved extension
        var toArrayCallee = (BoundMethodGroupExpr)toArray.Callee;
        // Arguments[0] is the receiver for extension calls
        // The receiver is the Where(...) call result
        // Find the Where call in the tree
        BoundResolvedCallExpr? whereCall = null;
        bound.EnumerateChildren(child =>
        {
            if (child is BoundResolvedCallExpr rc && rc.SelectedMethod.Name == "Where")
                whereCall = rc;
            child.EnumerateChildren(grandchild =>
            {
                if (grandchild is BoundResolvedCallExpr rc2 && rc2.SelectedMethod.Name == "Where")
                    whereCall = rc2;
            });
        });

        Assert.That(whereCall, Is.Not.Null, "Where() should be a BoundResolvedCallExpr");
        Assert.That(whereCall!.IsExtensionCall, Is.True, "Where should be IsExtensionCall");
        Assert.That(whereCall.StaticType.ClrType, Is.EqualTo(typeof(System.Collections.Generic.IEnumerable<int>)),
            "Where return type should flow as IEnumerable<int>");
    }
}
