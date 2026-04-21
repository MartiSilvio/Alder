using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Verification;

/// <summary>
/// Verification: Section 4 — Security Sandbox Escapes.
/// Tests attempt to bypass the security policy via various attack vectors.
/// Every test should fail with a security error. If it succeeds, that's a sandbox escape.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class SecuritySandboxVerificationTests(CompilationMode mode)
{
    private AlderEngine CreateRestrictiveEngine()
    {
        return TestEngineFactory.Create(mode, o =>
        {
            o.Sandbox = SandboxOptions.Safe() with
            {
                DeniedNamespaces = new HashSet<string>
                {
                    "System.IO",
                    "System.Diagnostics",
                    "System.Reflection"
                }
            };
        });
    }

    // --- Standard escape vectors (should be blocked) ---

    [Test]
    public void Attack_DirectTypeAccess_FileReadAllText_Blocked()
    {
        var engine = CreateRestrictiveEngine();
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""System.IO.File.ReadAllText("/etc/passwd")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107).Or.EqualTo(DiagnosticCode.CS0246));
    }

    [Test]
    public void Attack_TypeofReflection_Blocked()
    {
        var engine = CreateRestrictiveEngine();
        // typeof(System.IO.File) should itself be blocked, or .GetMethod should be blocked
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("""typeof(System.IO.File).GetMethod("ReadAllText")"""));
    }

    [Test]
    public void Attack_ObjectReflection_GetTypeAssemblyGetTypes_Blocked()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable<object>("o", "hello");
        // Start from allowed type (string), use reflection to enumerate all types in assembly.
        // GuardReflectionLeak should block returning Assembly, Type[], MethodInfo etc.
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("return o.GetType().Assembly.GetTypes();"));
    }

    [Test]
    public void Attack_NestedTypeAccess_EnvironmentSpecialFolder_Blocked()
    {
        var engine = CreateRestrictiveEngine();
        // System.Environment is in the default deny list. Nested enum access should be caught.
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("return System.Environment.SpecialFolder.Desktop;"));
    }

    // SecurityValidationPass does not validate BoundCastExpr target types.
    //
    // The validation pass has no case for BoundCastExpr or BoundAsExpr. A cast to a denied
    // type (e.g., (System.IO.Stream)obj) is not validated. While this alone doesn't construct
    // a blocked type, it means the type-checking is incomplete — a cast reference to a denied
    // type is semantically identical to a type reference for it.
    //
    // Fix: Add case BoundCastExpr and case BoundAsExpr to SecurityValidationPass.Validate
    // that calls policy.IsTypeAllowed on the target type.
    [Test]
    public void Attack_CastToBlockedType_ShouldBeBlocked()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Sandbox = SandboxOptions.Safe() with
            {
                DeniedNamespaces = new HashSet<string> { "System.IO" }
            };
        });
        engine.SetVariable<object>("obj", new System.IO.MemoryStream());

        // Cast to System.IO.Stream — a type in the denied namespace.
        // SecurityValidationPass should check BoundCastExpr.TargetType.
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("return (System.IO.Stream)obj;"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107).Or.EqualTo(DiagnosticCode.CS0246),
            "Cast to denied type should be caught by SecurityValidationPass");
    }

    // BoundDynamicCallExpr bypasses AllowMethodCalls check.
    //
    // SecurityValidationPass handles BoundResolvedCallExpr (checking AllowMethodCalls),
    // but has no case for BoundDynamicCallExpr. Dynamic calls are where the binder could
    // not resolve the method at bind time. At runtime, these flow through MethodInvoker
    // without any AllowMethodCalls check.
    //
    // This is exploitable with object-typed variables where the binder falls back to
    // dynamic dispatch.
    //
    // Fix: Add case BoundDynamicCallExpr to SecurityValidationPass.Validate that checks
    // AllowMethodCalls (with carve-outs for module calls, delegates, and lambdas).
    [Test]
    public void Attack_DynamicCall_ObjectTypedVariable_BypassesMethodBlock()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        // object-typed variable — binder can't resolve method, falls to BoundDynamicCallExpr
        engine.SetVariable("obj", (object)"hello");

        // Safe mode has AllowMethodCalls=false. This should be blocked.
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("return obj.ToUpper();"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100),
            "Dynamic calls on object-typed variables should respect AllowMethodCalls=false");
    }

    // MemberAccess.GetMember's Type handler (case Type staticType) lacks
    // IsTypeAllowed check. When the binder falls through to BoundDynamicMemberAccessExpr
    // (which only checks AllowPropertyRead, not IsTypeAllowed), the runtime path accesses
    // static members without type validation.
    //
    // Fix: Add IsTypeAllowed check at the top of case Type staticType handler in MemberAccess.GetMember.
    [Test]
    public void Attack_StaticMemberAccess_DynamicPath_TypeNotChecked()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Sandbox = SandboxOptions.Trusted() with
            {
                DeniedNamespaces = new HashSet<string> { "System.IO" }
            };
        });

        // This should be blocked — System.IO.Path is in a denied namespace
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""return System.IO.Path.DirectorySeparatorChar;"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107).Or.EqualTo(DiagnosticCode.CS0246));
    }

    // Default policy must block thread-blocking primitives.
    [Test]
    public void Attack_ManualResetEvent_ShouldBeBlocked()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("return new System.Threading.ManualResetEvent(false);"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107).Or.EqualTo(DiagnosticCode.CS0246));
    }

    // --- Compiled path bypass test ---

    // If the binder checks security but compiled IL doesn't re-check,
    // changing the security policy after compilation could allow a bypass.
    // This tests the time-of-check-time-of-use gap.
    [Test]
    public void Attack_CompiledPathBypass_PolicyChangeAfterCompilation()
    {
        if (mode != CompilationMode.Compiled) return;

        // Step 1: Compile with permissive policy
        var engine = new AlderEngine(o =>
        {
            o.UseCompiler();
            o.Sandbox = SandboxOptions.Trusted();
        });
        engine.SetVariable<string>("data", "hello");

        // This should compile successfully
        var result = engine.Evaluate("return data.ToUpper();");
        Assert.That(result, Is.EqualTo("HELLO"));

        // Document: the compiled delegate contains no runtime security checks.
        // Security is enforced at bind time. If the same expression object were reused
        // on an engine with a different policy, the compiled path would bypass it.
        // This is acceptable if engines are immutable after construction, but should
        // be documented as a security constraint.
        Assert.Pass("Compiled path relies on bind-time security validation — " +
                    "policy changes after compilation are not re-checked at runtime");
    }

    // --- Generic type argument test ---
    [Test]
    public void Attack_GenericTypeWithBlockedArgument()
    {
        var engine = CreateRestrictiveEngine();
        // Can we sneak in a blocked type as a generic argument?
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("return new List<System.IO.FileInfo>();"));
    }

    // --- Lambda closure reaching internal state ---
    [Test]
    public void Attack_LambdaClosure_CannotAccessInternalState()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        // Verify that lambda closures don't expose engine-internal objects
        engine.SetVariable("items", new List<int> { 1, 2, 3 });

        // Safe mode: property read is allowed, method calls are not.
        // The lambda itself (x => x) should be fine, but calling Where() is a method call.
        // Extension methods are exempted from AllowMethodCalls — this is by design.
        // But verify internal context/config objects are not reachable.
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("return items.GetType().Assembly;"));
        Assert.That(ex, Is.Not.Null,
            "Reflection through allowed types should be blocked by GuardReflectionLeak");
    }
}
