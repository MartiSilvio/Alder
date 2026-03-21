using System.Reflection;
using System.Runtime.CompilerServices;
using Alder.Runtime;
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

    // ExtensionMethodResolver.FuncTypeDefinitions must cover arities 1-17 (inclusive)
    // so that lambda-based LINQ overloads can be matched without Type.GetType(string) calls.
    [Test]
    public void FuncTypeDefinitions_ContainsExactly17Types()
    {
        var field = typeof(ExtensionMethodResolver).GetField(
            "FuncTypeDefinitions",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(field, Is.Not.Null, "FuncTypeDefinitions field must exist");

        var value = field!.GetValue(null) as IEnumerable<Type>;
        Assert.That(value, Is.Not.Null, "FuncTypeDefinitions must be an IEnumerable<Type>");

        var types = value!.ToArray();
        Assert.That(types, Has.Length.EqualTo(17), "Must cover arities 1-17 inclusive");
    }

    // Each entry in FuncTypeDefinitions must be an open generic Func<> definition.
    [Test]
    public void FuncTypeDefinitions_AllAreOpenGenericFuncTypes()
    {
        var field = typeof(ExtensionMethodResolver).GetField(
            "FuncTypeDefinitions",
            BindingFlags.NonPublic | BindingFlags.Static);

        var value = (field!.GetValue(null) as IEnumerable<Type>)!.ToArray();

        foreach (var type in value)
        {
            Assert.That(type.IsGenericTypeDefinition, Is.True,
                $"{type.Name} must be an open generic type definition");
            Assert.That(type.Name, Does.StartWith("Func`"),
                $"{type.Name} must be a Func<> type");
        }
    }

    // FuncTypeDefinitions must cover every arity from 1 to 17 without gaps.
    [Test]
    public void FuncTypeDefinitions_CoversArities1Through17WithoutGaps()
    {
        var field = typeof(ExtensionMethodResolver).GetField(
            "FuncTypeDefinitions",
            BindingFlags.NonPublic | BindingFlags.Static);

        var types = (field!.GetValue(null) as IEnumerable<Type>)!.ToArray();
        var arities = types.Select(t => t.GetGenericArguments().Length).OrderBy(x => x).ToArray();

        Assert.That(arities, Is.EqualTo(Enumerable.Range(1, 17).ToArray()),
            "Arities must be exactly 1..17 with no gaps");
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

    // Registered type member access works correctly through the standard evaluation path.
    [Test]
    public void RegisteredType_MemberAccess_ReturnsExpectedValue()
    {
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);
        engine.RegisterFromType<AotTestDto>();
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
}
