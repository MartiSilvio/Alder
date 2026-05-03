using System.Reflection;
using System.Runtime.CompilerServices;
using Alder.Aot;
using Alder.Runtime;
using Alder.Runtime.Introspection;
using Alder.Test._Infrastructure;

namespace Alder.Test.AOT;

/// <summary>
/// Verifies AOT-specific code paths without requiring a NativeAOT toolchain.
/// These tests run under JIT where IsDynamicCodeSupported is true; the smoke script
/// (scripts/aot-smoke.sh) covers the actual NativeAOT binary execution.
/// </summary>
[TestFixture]
[Category("AOT")]
public class AotSafetyTests
{
    private record AotTestDto(int Value, string Label);
    private sealed class ConstantDelegateFactoryContext(int constant) : AlderTypeContext
    {
        public override IReadOnlyList<TypedDispatch> GetTypeMetadata() => [];

        public override IReadOnlyDictionary<RootedType, Func<object, Delegate>>? GetDelegateFactories()
        {
            return new Dictionary<RootedType, Func<object, Delegate>>
            {
                [new RootedType(typeof(Func<int, int>))] = _ => (Func<int, int>)(_ => constant)
            };
        }
    }

    // TryInvokeFast works under JIT for a known static method: Math.Abs(int).
    [Test]
    public void TryInvokeFast_KnownStaticMethod_ReturnsCorrectResult()
    {
        Assert.That(RuntimeFeature.IsDynamicCodeSupported, Is.True,
            "This test requires JIT; the smoke script covers NativeAOT");

        var absMethod = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)])!;
        var success = MethodDispatchCache.TryInvokeFast(absMethod, null, [-5], out var result);

        Assert.That(success, Is.True, "TryInvokeFast must succeed under JIT");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void TryInvokeFast_NonVisibleIteratorMethod_ReturnsFalse()
    {
        var source = new List<int> { 1, 2, 3 }.Where(x => x > 1);
        var selectMethod = source.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(m => m.Name == "Select" && m.IsGenericMethodDefinition)
            .MakeGenericMethod(typeof(int));

        var success = MethodDispatchCache.TryInvokeFast(selectMethod, source, [(Func<int, int>)(x => x)], out var result);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
    }

    // Registered type member access works correctly through the standard evaluation path.
    [Test]
    public void RegisteredType_MemberAccess_ReturnsExpectedValue()
    {
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted, o => o.Modules.RegisterFromType<AotTestDto>());
        engine.SetVariable("dto", new AotTestDto(42, "hello"));

        var result = engine.Evaluate("dto.Value");

        Assert.That(result, Is.EqualTo(42));
    }

    // Unregistered type member access succeeds under JIT because the AOT gate only fires
    // when IsDynamicCodeSupported is false (i.e., inside a NativeAOT binary).
    [Test]
    public void UnregisteredType_MemberAccess_SucceedsUnderJit()
    {
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);
        engine.SetVariable("dto", new AotTestDto(7, "world"));

        var result = engine.Evaluate("dto.Value");

        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void SimulatedAot_UnregisteredType_MemberAccess_FailsClearly()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);
        engine.SetVariable("model", new TestModel { Name = "unregistered" });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("model.Name"));

        Assert.That(ex!.Message, Does.Contain("authoritative generated mode"));
    }

    [Test]
    public void SimulatedAot_UnregisteredType_MethodInvoke_FailsClearly()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);
        engine.SetVariable("model", new TestModel("x", 1));

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("""model.Greet()"""));

        Assert.That(ex!.Message, Does.Contain("authoritative generated mode"));
    }

    [Test]
    public void SimulatedAot_CuratedType_ObjectBaseMembers_RemainAvailable()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);
        engine.SetVariable("value", 5);

        var typeResult = engine.Evaluate("return value.GetType() == typeof(int);");
        var stringResult = engine.Evaluate("return value.ToString();");

        Assert.That(typeResult, Is.EqualTo(true));
        Assert.That(stringResult, Is.EqualTo("5"));
    }

    [Test]
    public async Task SimulatedAot_TaskFromResult_Await_RemainsAvailable()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);

        var result = await engine.EvaluateAsync("return await Task.FromResult(42);");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task SimulatedAot_TaskFromResult_CuratedPrimitiveAndReferenceRoots_RemainAvailable()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);

        Assert.That(await engine.EvaluateAsync("""return await Task.FromResult(100L);"""), Is.EqualTo(100L));
        Assert.That(await engine.EvaluateAsync("""return await Task.FromResult(3.14);"""), Is.EqualTo(3.14));
        Assert.That(await engine.EvaluateAsync("""return await Task.FromResult(1.5m);"""), Is.EqualTo(1.5m));
        Assert.That(await engine.EvaluateAsync("""return await Task.FromResult('x');"""), Is.EqualTo('x'));
        Assert.That(await engine.EvaluateAsync("""return await Task.FromResult("hello");"""), Is.EqualTo("hello"));
        Assert.That(await engine.EvaluateAsync("""return (await Task.FromResult<object>(null)) == null;"""), Is.EqualTo(true));
        Assert.That(await engine.EvaluateAsync("""return (await Task.FromResult<string>(null)) == null;"""), Is.EqualTo(true));
    }

    [Test]
    public async Task SimulatedAot_ValueTask_CuratedRoots_RemainAvailable()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);

        Assert.That(await engine.EvaluateAsync("""return await new ValueTask<int>(42);"""), Is.EqualTo(42));
        Assert.That(await engine.EvaluateAsync("""return await new ValueTask<string>("alder");"""), Is.EqualTo("alder"));
        Assert.That(await engine.EvaluateAsync("""return await new ValueTask<double>(2.5);"""), Is.EqualTo(2.5));
    }

    [Test]
    public async Task SimulatedAot_ValueTask_CompletedTask_RemainsAvailable()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);

        var result = await engine.EvaluateAsync("""
            await ValueTask.CompletedTask;
            return 1;
            """);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void SimulatedAot_CuratedStaticHelper_DateTimeTryParse_RemainsAvailable()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);

        var result = engine.Evaluate("""return DateTime.TryParse("2024-06-15", out var dt) ? dt.Year : -1;""");

        Assert.That(result, Is.EqualTo(2024));
    }

    [Test]
    public void SimulatedAot_CuratedStaticHelper_GuidTryParse_RemainsAvailable()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);

        var result = engine.Evaluate(
            """return Guid.TryParse("550e8400-e29b-41d4-a716-446655440000", out var g) ? g.ToString() : "bad";""");

        Assert.That(result, Is.EqualTo("550e8400-e29b-41d4-a716-446655440000"));
    }

    [Test]
    public void SimulatedAot_NullableInt_HasValue_WhenValue_RemainsAvailable()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);

        var result = engine.Evaluate("""
            int? x = 42;
            return x.HasValue && x.Value == 42;
            """);

        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void SimulatedAot_NullableInt_HasValue_WhenNull_RemainsAvailable()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);

        var result = engine.Evaluate("""
            int? x = null;
            return x.HasValue;
            """);

        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void SimulatedAot_NullableInt_GetValueOrDefault_WhenValue_RemainsAvailable()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);

        var result = engine.Evaluate("""
            int? x = 7;
            return x.GetValueOrDefault();
            """);

        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void SimulatedAot_NullableInt_GetValueOrDefault_WhenNull_RemainsAvailable()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);

        var result = engine.Evaluate("""
            int? x = null;
            return x.GetValueOrDefault();
            """);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void SimulatedAot_NullableIntArray_ExplicitCreation_RemainsAvailable()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);

        var result = (int?[])engine.Evaluate("""
            return new int?[] { 1, null, 3 };
            """)!;

        Assert.That(result, Is.EqualTo(new int?[] { 1, null, 3 }));
    }

    [Test]
    public void SimulatedAot_NullableIntArray_ExtendedLiteral_RemainsAvailable()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted, o => o.LanguageMode = LanguageMode.Extended);

        var result = (int?[])engine.Evaluate("""
            return [1, null, 3];
            """)!;

        Assert.That(result, Is.EqualTo(new int?[] { 1, null, 3 }));
    }

    [Test]
    public void SimulatedAot_UnregisteredType_ConstructorInvoke_FailsClearly()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted, o =>
        {
            o.Types.AddAssembly(typeof(TestModel).Assembly);
            o.Types.AddNamespace("Alder.Test.AOT");
        });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("new TestModel()"));

        Assert.That(ex!.Message, Does.Contain("authoritative generated mode"));
    }

    [Test]
    public void RuntimeGenericClosure_KnownNullableType_ClosesUnderSimulatedAot()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);

        var success = RuntimeGenericClosure.TryCloseType(typeof(Nullable<>), [typeof(int)], out var closedType);

        Assert.That(success, Is.True);
        Assert.That(closedType, Is.EqualTo(typeof(int?)));
    }

    [Test]
    public void RuntimeGenericClosure_RootedBuiltInTuple_ClosesUnderSimulatedAot()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);

        var success = RuntimeGenericClosure.TryCloseType(typeof(ValueTuple<,>), [typeof(int), typeof(int)], out var closedType);

        Assert.That(success, Is.True);
        Assert.That(closedType, Is.EqualTo(typeof((int, int))));
    }

    [Test]
    public void RuntimeGenericClosure_MixedBuiltInTuple_ClosesUnderSimulatedAot()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);

        var success = RuntimeGenericClosure.TryCloseType(
            typeof(ValueTuple<,,>),
            [typeof(int), typeof(string), typeof(double)],
            out var closedType);

        Assert.That(success, Is.True);
        Assert.That(closedType, Is.EqualTo(typeof((int, string, double))));
    }

    [Test]
    public void RuntimeGenericClosure_LargeBuiltInTuple_ClosesUnderSimulatedAot()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);

        var success = RuntimeGenericClosure.TryCloseType(
            typeof(ValueTuple<,,,,,,,>),
            [
                typeof(int), typeof(int), typeof(int), typeof(int),
                typeof(int), typeof(int), typeof(int), typeof((int, int))
            ],
            out var closedType);

        Assert.That(success, Is.True);
        Assert.That(
            closedType,
            Is.EqualTo(typeof(ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>)));
    }

    [Test]
    public void RuntimeGenericClosure_ListOfTuple_ClosesUnderSimulatedAot()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);

        var success = RuntimeGenericClosure.TryCloseType(
            typeof(List<>),
            [typeof((int, int))],
            out var closedType);

        Assert.That(success, Is.True);
        Assert.That(closedType, Is.EqualTo(typeof(List<(int, int)>)));
    }

    [Test]
    public void RuntimeGenericClosure_KeyValuePair_ClosesUnderSimulatedAot()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);

        var success = RuntimeGenericClosure.TryCloseType(
            typeof(KeyValuePair<,>),
            [typeof(string), typeof(int)],
            out var closedType);

        Assert.That(success, Is.True);
        Assert.That(closedType, Is.EqualTo(typeof(KeyValuePair<string, int>)));
    }

    [Test]
    public void RuntimeGenericClosure_FuncDelegateForLinqSelector_IsOutsideBoundedAotSurface()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);

        var success = RuntimeGenericClosure.TryCloseType(
            typeof(Func<,>),
            [typeof(int), typeof(int)],
            out var closedType);

        Assert.That(success, Is.False);
        Assert.That(closedType, Is.Null);
    }

    [Test]
    public void SimulatedAot_LiftedNullableArithmetic_EvaluatesWithoutGenericClosureFailure()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);

        var result = engine.Evaluate("""
            double? a = 5.0;
            double? b = 3.0;
            return a * b;
            """);

        Assert.That(result, Is.EqualTo(15.0d));
    }

    [Test]
    public void SimulatedAot_RuntimeArrayFactory_InfersNullableArrayWithoutGenericClosureFailure()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);

        var array = (Array)RuntimeArrayFactory.InferAndCreateArray([1, null]);

        Assert.That(array.GetType(), Is.EqualTo(typeof(int?[])));
        Assert.That(array.GetValue(0), Is.EqualTo(1));
        Assert.That(array.GetValue(1), Is.Null);
    }

    [Test]
    public void SimulatedAot_TwoElementTuple_EvaluatesWithoutGenericClosureFailure()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);

        var result = engine.Evaluate("""
            (int, int) t = (1, 2);
            return t.Item1 + t.Item2;
            """);

        Assert.That(result, Is.EqualTo(3));
    }

    [TestCase(CompilationMode.Interpreted)]
    [TestCase(CompilationMode.Compiled)]
    public void SimulatedAot_ResolvedBuiltInProperty_RemainsAvailable(CompilationMode mode)
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("s", "hello");

        var result = engine.Evaluate("return s.Length;");

        Assert.That(result, Is.EqualTo(5));
    }

    [TestCase(CompilationMode.Interpreted)]
    [TestCase(CompilationMode.Compiled)]
    public void SimulatedAot_ResolvedBuiltInMethod_RemainsAvailable(CompilationMode mode)
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("s", "hello");

        var result = engine.Evaluate("""return s.Contains("ell");""");

        Assert.That(result, Is.EqualTo(true));
    }

    [TestCase(CompilationMode.Interpreted)]
    [TestCase(CompilationMode.Compiled)]
    public void SimulatedAot_BuiltInResolvedProperty_RemainsAvailable_WithAdditionalGeneratedContext(CompilationMode mode)
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(mode, o => o.Aot.UseGeneratedContext(TestGeneratedContext.Default));
        engine.SetVariable("s", "hello");

        var result = engine.Evaluate("return s.Length;");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void SimulatedAot_MixedTuple_EvaluatesWithoutGenericClosureFailure()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);

        var result = engine.Evaluate("""
            (int, string, double) t = (7, "hello", 2.5);
            return t.Item1 + t.Item2.Length + (int)t.Item3;
            """);

        Assert.That(result, Is.EqualTo(14));
    }

    [Test]
    public void SimulatedAot_TupleInForeach_EvaluatesWithoutGenericClosureFailure()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);

        var result = engine.Evaluate("""
            var pairs = new List<(int, int)> { (1, 2), (3, 4), (5, 6) };
            int total = 0;
            foreach (var p in pairs)
                total += p.Item1 + p.Item2;
            return total;
            """);

        Assert.That(result, Is.EqualTo(21));
    }

    [Test]
    public void SimulatedAot_KeyValuePairDeconstruction_EvaluatesWithoutGenericClosureFailure()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);

        var result = engine.Evaluate("""
            var kvp = new KeyValuePair<string, int>("hello", 42);
            var (key, value) = kvp;
            return key.Length + value;
            """);

        Assert.That(result, Is.EqualTo(47));
    }

    [Test]
    public void SimulatedAot_LinqExtensionMethods_AreOutsideBoundedAotSurface()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("""
            var items = new List<int> { 1, 2, 3, 4, 5 };
            return items.Where(x => x > 3).Count();
            """));

        Assert.That(ex!.Message, Does.Contain("authoritative generated mode"));
    }

    [Test]
    public void SimulatedAot_CustomContextDelegateFactoriesRootDelegateTypes()
    {
        using var _ = RuntimeGenericClosure.OverrideDynamicCodeSupportForTesting(false);
        var engine = TestEngineFactory.Create(
            CompilationMode.Interpreted,
            o => o.Aot.UseGeneratedContext(new ConstantDelegateFactoryContext(321)));

        var result = engine.Evaluate<Func<int, int>>("x => x + 1");

        Assert.That(result!(5), Is.EqualTo(321));
    }
}
