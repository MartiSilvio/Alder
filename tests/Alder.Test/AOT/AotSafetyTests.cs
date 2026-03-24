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
}
