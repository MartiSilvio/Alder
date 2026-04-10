using System.Reflection;
using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Security;

/// <summary>
/// Tests that verify reflection metadata types (MemberInfo, Assembly, Module, etc.)
/// are blocked in all modes. Type objects are allowed — they are inert metadata.
/// The guard blocks the next step: MethodInfo, FieldInfo, etc. which enable invocation.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ReflectionBlockingTests(CompilationMode mode)
{
    #region Type Objects Are Allowed

    [Test]
    public void AllowsGetType_OnString()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.GetType()");

        Assert.That(result, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void AllowsGetType_OnInt()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("num", 42);

        var result = engine.Evaluate("num.GetType()");

        Assert.That(result, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void AllowsGetType_OnList()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("items.GetType()");

        Assert.That(result, Is.EqualTo(typeof(List<int>)));
    }

    [Test]
    public void AllowsGetType_OnAnonymousObject()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("obj", new { Name = "Test", Value = 42 });

        var result = engine.Evaluate("obj.GetType()");

        Assert.That(result, Is.InstanceOf<Type>());
    }

    [Test]
    public void AllowsTypePropertyAccess()
    {
        var engine = TestEngineFactory.Create(mode);
        var holder = new TypeHolder { TypeValue = typeof(string) };
        engine.SetVariable("holder", holder);

        var result = engine.Evaluate("holder.TypeValue");

        Assert.That(result, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void AllowsTypeFromDictionary()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("dict", new Dictionary<string, object?> { ["type"] = typeof(int) });

        var result = engine.Evaluate("""dict["type"] """);

        Assert.That(result, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void AllowsTypeFromArray()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("arr", new object[] { typeof(string), typeof(int) });

        var result = engine.Evaluate("arr[0]");

        Assert.That(result, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void AllowsSelectReturningType()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<object> { "hello", 42, 3.14 });

        var result = engine.Evaluate<List<Type>>("items.Select(x => x.GetType()).ToList()");

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result![0], Is.EqualTo(typeof(string)));
    }

    [Test]
    public void AllowsTypeNameAccess()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.GetType().Name");

        Assert.That(result, Is.EqualTo("String"));
    }

    [Test]
    public void AllowsTypeComparison()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate<bool>("text.GetType() == typeof(string)");

        Assert.That(result, Is.True);
    }

    #endregion

    #region MemberInfo Blocking

    [Test]
    public void BlocksMethodInfo_FromModule()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Modules.Register<ReflectionTestModule>("Test"));

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("Test.GetMethodInfo()"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
    }

    [Test]
    public void BlocksPropertyInfo()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Modules.Register<ReflectionTestModule>("Test"));

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("Test.GetPropertyInfo()"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
    }

    [Test]
    public void BlocksFieldInfo()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Modules.Register<ReflectionTestModule>("Test"));

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("Test.GetFieldInfo()"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
    }

    #endregion

    #region Assembly and Module Blocking

    [Test]
    public void BlocksAssembly()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Modules.Register<ReflectionTestModule>("Test"));

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("Test.GetAssembly()"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
    }

    [Test]
    public void BlocksModule()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Modules.Register<ReflectionTestModule>("Test"));

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("Test.GetModule()"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
    }

    #endregion

    #region Chaining from Type to MemberInfo is Blocked

    [Test]
    public void BlocksGetMethodOnType()
    {
        var engine = TestEngineFactory.Create(mode);

        // Use GetType() which is unambiguous on object, returns MethodInfo → guard blocks it
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""typeof(object).GetMethod("GetType")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
    }

    [Test]
    public void BlocksAssemblyOnType()
    {
        var engine = TestEngineFactory.Create(mode);

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("typeof(string).Assembly"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
    }

    #endregion

    #region Safe Operations Still Work

    [Test]
    public void AllowsNormalMethodCalls()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.ToUpper()");

        Assert.That(result, Is.EqualTo("HELLO"));
    }

    [Test]
    public void AllowsNormalPropertyAccess()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.Length");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void AllowsNormalIndexAccess()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("arr", new[] { 10, 20, 30 });

        var result = engine.Evaluate("arr[1]");

        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void AllowsModuleMethods()
    {
        var engine = TestEngineFactory.Create(mode);

        var result = engine.Evaluate("Math.Abs(-5)");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void AllowsLinqOperations()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("items.Where(x => x > 1).Sum()");

        Assert.That(result, Is.EqualTo(5));
    }

    #endregion

    #region Helper Classes

    public class TypeHolder
    {
        public Type? TypeValue { get; set; }
    }

    public class ReflectionTestModule
    {
        public static MethodInfo? GetMethodInfo()
            => typeof(string).GetMethod("ToUpper", Type.EmptyTypes);

        public static PropertyInfo? GetPropertyInfo()
            => typeof(string).GetProperty("Length");

        public static FieldInfo? GetFieldInfo()
            => typeof(string).GetField("Empty");

        public static Assembly GetAssembly()
            => typeof(string).Assembly;

        public static Module GetModule()
            => typeof(string).Module;
    }

    #endregion
}
