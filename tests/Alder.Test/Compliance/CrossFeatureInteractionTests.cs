using Alder.Test._Infrastructure;

namespace Alder.Test.Compliance;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class CrossFeatureInteractionTests(CompilationMode mode)
{
    private AlderEngine CreateEngine(LanguageMode lang = LanguageMode.Standard, HashSet<Type>? allowedTypes = null)
        => TestEngineFactory.Create(mode, o =>
        {
            o.LanguageMode = lang;
            if (allowedTypes != null)
                o.Sandbox = SandboxOptions.Trusted() with { TrustedTypes = allowedTypes };
        });

    [Test]
    public void IntLiteral_MinValueNegation()
    {
        // -2147483648 should be parsed as int.MinValue, not -(int.MaxValue+1)
        var result = CreateEngine().Evaluate("-2147483648");
        Assert.That(result, Is.EqualTo(int.MinValue));
        Assert.That(result, Is.TypeOf<int>());
    }
}
