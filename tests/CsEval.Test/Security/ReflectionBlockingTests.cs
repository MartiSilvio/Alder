using CsEval.Diagnostics;

namespace CsEval.Test.Security;

/// <summary>
/// Tests that verify reflection types are blocked in all modes.
/// User code must never obtain a value whose runtime type is System.Type
/// or any reflection metadata type (MemberInfo, Assembly, Module, etc.).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ReflectionBlockingTests(CompilationMode mode)
{
    #region GetType() Blocking

    [Test]
    public void BlocksGetType_OnString()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("text.GetType()"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.Message, Does.Contain("RuntimeType"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CSEV0030));
    }

    [Test]
    public void BlocksGetType_OnInt()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("num", 42);

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("num.GetType()"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CSEV0030));
    }

    [Test]
    public void BlocksGetType_OnList()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<int> { 1, 2, 3 });

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("items.GetType()"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CSEV0030));
    }

    [Test]
    public void BlocksGetType_OnAnonymousObject()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("obj", new { Name = "Test", Value = 42 });

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("obj.GetType()"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CSEV0030));
    }

    #endregion

    #region Type as Property Value

    [Test]
    public void BlocksTypePropertyAccess()
    {
        var engine = TestEngineFactory.Create(mode);
        var holder = new TypeHolder { TypeValue = typeof(string) };
        engine.SetVariable("holder", holder);

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("holder.TypeValue"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CSEV0030));
    }

    [Test]
    public void BlocksTypeFromDictionary()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("dict", new Dictionary<string, object?> { ["type"] = typeof(int) });

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("""dict["type"] """));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CSEV0030));
    }

    [Test]
    public void BlocksTypeFromArray()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("arr", new object[] { typeof(string), typeof(int) });

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("arr[0]"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CSEV0030));
    }

    #endregion

    #region MemberInfo Blocking

    [Test]
    public void BlocksMethodInfo_FromModule()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.RegisterModule<ReflectionTestModule>("Test");

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("Test.GetMethodInfo()"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CSEV0030));
    }

    [Test]
    public void BlocksPropertyInfo()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.RegisterModule<ReflectionTestModule>("Test");

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("Test.GetPropertyInfo()"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CSEV0030));
    }

    [Test]
    public void BlocksFieldInfo()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.RegisterModule<ReflectionTestModule>("Test");

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("Test.GetFieldInfo()"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CSEV0030));
    }

    #endregion

    #region Assembly and Module Blocking

    [Test]
    public void BlocksAssembly()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.RegisterModule<ReflectionTestModule>("Test");

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("Test.GetAssembly()"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CSEV0030));
    }

    [Test]
    public void BlocksModule()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.RegisterModule<ReflectionTestModule>("Test");

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("Test.GetModule()"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CSEV0030));
    }

    #endregion

    #region LINQ with Reflection

    [Test]
    public void BlocksSelectReturningType()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<object> { "hello", 42, 3.14 });

        // Even through LINQ, reflection types are blocked
        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("items.Select(x => x.GetType()).ToList()"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CSEV0030));
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
        public static System.Reflection.MethodInfo? GetMethodInfo()
            => typeof(string).GetMethod("ToUpper", Type.EmptyTypes);

        public static System.Reflection.PropertyInfo? GetPropertyInfo()
            => typeof(string).GetProperty("Length");

        public static System.Reflection.FieldInfo? GetFieldInfo()
            => typeof(string).GetField("Empty");

        public static System.Reflection.Assembly GetAssembly()
            => typeof(string).Assembly;

        public static System.Reflection.Module GetModule()
            => typeof(string).Module;
    }

    #endregion
}
