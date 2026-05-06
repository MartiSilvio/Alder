using Alder.Test._Infrastructure;

namespace Alder.Test.PatternMatching;

// Engine-only: PropertyPattern_NullFalse uses object-typed variable with member access
// that Roslyn cannot resolve from an object-typed variable.

/// <summary>
/// ECMA-334 §11.2 -- Pattern matching via is-expressions.
/// Tests constant patterns (section 11.2.3), type patterns with variable binding (section 11.2.2),
/// relational patterns (section 11.2.5), logical combinators (section 11.2.6),
/// property patterns (section 11.2.7), and switch-arm discard patterns.
/// </summary>
[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class PatternTests(CompilationMode mode)
{

    // Null test for property pattern with member access: engine-only (Roslyn cannot resolve
    // member on object-typed variable, and string? x = null doesn't support is { Length: > 0 }
    // without a warning/error in some Roslyn versions).
    [Test]
    public void PropertyPattern_NullFalse()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("{ object x = null; return x is { Length: > 0 }; }");
        Assert.That(result, Is.False);
    }

}
