using Alder.Attributes;
using Alder.Diagnostics;

namespace Alder.Test.Core;

[TestFixture]
public class ExplicitModuleTests
{
    [Test]
    public void ExplicitOnly_OnlyExposesAttributedMethods()
    {
        var engine = new AlderEngine(o => o.Modules.Register<ExplicitModule>("Mod", explicitOnly: true));

        // Attributed method should work
        var result = engine.Evaluate("Mod.Allowed()");
        Assert.That(result, Is.EqualTo("allowed"));

        // Non-attributed method should throw
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("Mod.NotAllowed()"));
        Assert.That(ex!.Message, Does.Contain("NotAllowed"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0117));
    }

    [Test]
    public void ExplicitOnly_UsesCustomName()
    {
        var engine = new AlderEngine(o => o.Modules.Register<ExplicitModule>("Mod", explicitOnly: true));

        // Should be accessible by custom name
        var result = engine.Evaluate("Mod.CustomName()");
        Assert.That(result, Is.EqualTo("custom"));
    }

    [Test]
    public void ExplicitOnly_FalseExposesAllMethods()
    {
        var engine = new AlderEngine(o => o.Modules.Register<ExplicitModule>("Mod", explicitOnly: false));

        Assert.That(engine.Evaluate("Mod.Allowed()"), Is.EqualTo("allowed"));
        Assert.That(engine.Evaluate("Mod.NotAllowed()"), Is.EqualTo("not allowed"));
    }

    [Test]
    public void ModuleAttribute_ExplicitOnlyProperty()
    {
        var engine = new AlderEngine(o => o.Modules.Register<AttributedExplicitModule>("Mod"));

        // Attributed method should work
        var result = engine.Evaluate("Mod.Exposed()");
        Assert.That(result, Is.EqualTo("exposed"));

        // Non-attributed method should throw (ExplicitOnly=true on class)
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("Mod.Hidden()"));
        Assert.That(ex!.Message, Does.Contain("Hidden"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0117));
    }

    [Test]
    public void AttributeWithoutName_UsesMethodName()
    {
        var engine = new AlderEngine(o => o.Modules.Register<ExplicitModule>("Mod", explicitOnly: true));

        // Method marked with [AlderFunction] (no name) uses method name
        var result = engine.Evaluate("Mod.Allowed()");
        Assert.That(result, Is.EqualTo("allowed"));
    }

    [Test]
    public void ExplicitOnly_PropertiesNotExposed()
    {
        var engine = new AlderEngine(o => o.Modules.Register<ModuleWithProperty>("Mod", explicitOnly: true));

        // Properties are not exposed in explicit mode
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("Mod.Value"));
        Assert.That(ex!.Message, Does.Contain("Value"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0117));
    }

    [Test]
    public void NonExplicit_PropertiesExposed()
    {
        var engine = new AlderEngine(o => o.Modules.Register<ModuleWithProperty>("Mod", explicitOnly: false));

        var result = engine.Evaluate("Mod.Value");
        Assert.That(result, Is.EqualTo(42));
    }

    private class ExplicitModule
    {
        [AlderFunction]
        public string Allowed() => "allowed";

        [AlderFunction("CustomName")]
        public string MethodWithCustomName() => "custom";

        public string NotAllowed() => "not allowed";
    }

    [AlderModule(ExplicitOnly = true)]
    private class AttributedExplicitModule
    {
        [AlderFunction]
        public string Exposed() => "exposed";

        public string Hidden() => "hidden";
    }

    private class ModuleWithProperty
    {
        [AlderFunction]
        public int GetValue() => 42;

        public int Value => 42;
    }
}
