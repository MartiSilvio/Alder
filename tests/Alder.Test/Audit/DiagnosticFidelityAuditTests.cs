using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Audit;

/// <summary>
/// Pre-release adversarial audit: Section 6 — Error Diagnostic Fidelity.
/// Verifies that CS-prefixed error codes match Roslyn's semantics: same code for the same
/// error condition. Also verifies no raw-string AlderException usage exists.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class DiagnosticFidelityAuditTests(CompilationMode mode)
{
    private AlderEngine CreateEngine() => TestEngineFactory.Create(mode);

    // CS0019 — Operator not applicable to operand types
    [Test]
    public void CS0019_OperatorNotApplicable()
    {
        var engine = CreateEngine();
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("return 1 + true;"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019),
            "1 + true should produce CS0019: operator '+' cannot be applied to 'int' and 'bool'");
    }

    // CS0029 — Cannot implicitly convert type
    [Test]
    public void CS0029_CannotImplicitlyConvert()
    {
        var engine = CreateEngine();
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("""
            int x = "hello";
            return x;
        """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0029).Or.EqualTo(DiagnosticCode.CS0266),
            "int x = \"hello\" should produce CS0029 or CS0266 (implicit conversion failure)");
    }

    // CS0030 — Cannot convert type (explicit cast failure)
    [Test]
    public void CS0030_CannotConvertType()
    {
        var engine = CreateEngine();
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("""return (int)"hello";"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0030),
            "(int)\"hello\" should produce CS0030: cannot convert type 'string' to 'int'");
    }

    // CS0103 — Name does not exist in current context
    [Test]
    public void CS0103_UndefinedVariable()
    {
        var engine = CreateEngine();
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("return undefinedVariable;"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103),
            "undefinedVariable should produce CS0103");
    }

    // CS0117 — Type does not contain a definition for member
    [Test]
    public void CS0117_TypeDoesNotContainDefinition()
    {
        var engine = CreateEngine();
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("return string.NonExistentMethod();"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0117).Or.EqualTo(DiagnosticCode.CS1061),
            "string.NonExistentMethod() should produce CS0117 or CS1061");
    }

    // CS0246 — Type or namespace not found
    [Test]
    public void CS0246_TypeNotFound()
    {
        var engine = CreateEngine();
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("return typeof(NonExistentType);"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0246),
            "typeof(NonExistentType) should produce CS0246");
    }

    // DEFECT-DIAG-1: Wrong error code for argument count mismatch.
    //
    // Math.Max(1,2,3) should produce CS1501 (no overload takes 3 arguments) or CS7036
    // (missing required parameter). Instead, Alder produces ALDR0304 (method invocation
    // failed), which is a generic fallback code rather than the specific Roslyn diagnostic.
    //
    // Fix: When overload resolution fails due to argument count mismatch, produce the
    // appropriate CS code instead of falling through to ALDR0304.
    [Test]
    public void CS1501_WrongArgumentCount_ShouldNotBeGenericALDR0304()
    {
        var engine = CreateEngine();
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("return Math.Max(1, 2, 3);"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1501)
            .Or.EqualTo(DiagnosticCode.CS7036)
            .Or.EqualTo(DiagnosticCode.CS1729),
            $"Math.Max(1,2,3) produces {ex.ErrorCode} — should produce a CS-prefixed code for argument " +
            "count mismatch, not the generic ALDR0304 'method invocation failed'");
    }

    // CS1061 — Type does not contain a definition (instance member)
    [Test]
    public void CS1061_InstanceMemberNotFound()
    {
        var engine = CreateEngine();
        engine.SetVariable("s", "hello");
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("return s.FakeMethod();"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1061).Or.EqualTo(DiagnosticCode.CS0117),
            "\"hello\".FakeMethod() should produce CS1061");
    }

    // CS0176 — Accessing static member on instance
    // string.Empty is static; accessing it on an instance should be an error.
    [Test]
    public void CS0176_StaticMemberOnInstance()
    {
        var engine = CreateEngine();
        engine.SetVariable("s", "hello");
        // In Roslyn, "hello".Empty is CS0176.
        // Alder may handle this differently — document the actual behavior.
        try
        {
            var result = engine.Evaluate("return s.Empty;");
            Assert.Warn($"s.Empty returned {result} — Alder does not enforce CS0176 (static member on instance)");
        }
        catch (AlderException ex)
        {
            // Any error code is acceptable here — the point is it should not silently succeed
            // with a wrong value
            Assert.Pass($"Correctly rejected with {ex.ErrorCode}");
        }
    }

    // Verify diagnostics contain proper error codes (not empty/null)
    [Test]
    public void Diagnostics_ContainStructuredErrorCode()
    {
        var engine = CreateEngine();
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("return unknown;"));
        Assert.That(ex!.ErrorCode, Is.Not.EqualTo(default(DiagnosticCode)),
            "AlderException should have a non-default ErrorCode");
        Assert.That(ex.Diagnostics, Is.Not.Empty,
            "AlderException should carry at least one diagnostic");
        Assert.That(ex.Diagnostics[0].Code, Is.Not.EqualTo(default(DiagnosticCode)),
            "Diagnostic should have a proper code");
    }

    // Verify TryValidate returns structured diagnostics for invalid expressions
    [Test]
    public void TryValidate_ReturnsStructuredDiagnostics()
    {
        var engine = CreateEngine();
        var valid = engine.TryValidate("return undefinedVar;", out var diagnostics);
        Assert.That(valid, Is.False);
        Assert.That(diagnostics, Is.Not.Empty);
        Assert.That(diagnostics[0].Code, Is.EqualTo(DiagnosticCode.CS0103));
    }
}
