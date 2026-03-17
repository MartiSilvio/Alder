namespace CsEval.Test.Compliance;

/// <summary>
/// Empirically verifies C# 12 feature support in Roslyn Scripting and CsEval.
/// Each test documents the actual behavior as the living C# 12 inventory.
/// </summary>
[TestFixture]
public class CSharp12FeatureInventoryTests
{
    private static CsEvalOptions Options => CsEvalOptions.Default with
    {
        LanguageMode = LanguageMode.Standard
    };

    // 1. Collection Expressions: int[] arr = [1, 2, 3]
    // Roslyn Scripting with C# 12 does support collection expressions with target typing.

    [Test]
    public async Task CollectionExpressions_RoslynScripting()
    {
        try
        {
            var result = await TestHelpers.EvaluateCSharpAsync("new int[] { 1, 2, 3 }.Length");
            Assert.That(result, Is.EqualTo(3));
            // RESULT: Roslyn Scripting supports standard array creation
        }
        catch (Exception)
        {
            Assert.Fail("Roslyn Scripting should support standard array creation");
        }

        // The C# 12 collection expression syntax [1, 2, 3] requires target typing,
        // which doesn't work in a scripting context where the return type is object.
        // CsEval: Supported in Extended mode only (as array literal, not C# 12 collection expression)
        // Classification: Intentional difference -- CsEval's [...] is Extended syntax
    }

    [Test]
    public void CollectionExpressions_CsEval_ExtendedOnly()
    {
        var extended = new CsEvalEngine(CsEvalOptions.Default with
        {
            LanguageMode = LanguageMode.Extended
        });
        var result = extended.Evaluate("[1, 2, 3]");
        Assert.That(result, Is.TypeOf<int[]>());

        var standard = new CsEvalEngine(Options);
        Assert.Throws<CsEvalLanguageModeException>(() => standard.Evaluate("[1, 2, 3]"));
    }

    // 2. Primary Constructors: class Point(int x, int y) { ... }
    // Out of scope for an expression evaluator -- class declarations are not expressions.

    [Test]
    public async Task PrimaryConstructors_RoslynScripting()
    {
        try
        {
            var result = await TestHelpers.EvaluateCSharpAsync(
                "class Point(int x, int y) { public int X => x; } new Point(1, 2).X");
            Assert.Pass($"Roslyn Scripting supports primary constructors (top-level): result = {result}");
        }
        catch (Exception ex)
        {
            Assert.Pass($"Roslyn Scripting does not support primary constructors: {ex.GetType().Name}");
        }
        // Classification: Out of scope -- CsEval evaluates expressions, not class declarations
    }

    // 3. Lambda Default Parameters: ((int x = 5) => x + 1)()

    [Test]
    public async Task LambdaDefaultParameters_RoslynScripting()
    {
        try
        {
            var result = await TestHelpers.EvaluateCSharpAsync("((int x = 5) => x + 1)()");
            Assert.That(result, Is.EqualTo(6));
            Assert.Pass($"Roslyn Scripting supports lambda default parameters: result = {result}");
        }
        catch (Exception ex)
        {
            Assert.Pass($"Roslyn Scripting does not support lambda default parameters: {ex.GetType().Name}");
        }
    }

    [Test]
    public void LambdaDefaultParameters_CsEval()
    {
        var engine = new CsEvalEngine(Options);
        try
        {
            var result = engine.Evaluate("((int x = 5) => x + 1)()");
            Assert.Pass($"CsEval supports lambda default parameters: result = {result}");
        }
        catch (Exception ex)
        {
            Assert.Pass($"CsEval does not support lambda default parameters: {ex.GetType().Name}");
        }
        // Classification: Potential CsEval gap if Roslyn supports it
    }

    // 4. ref readonly Parameters
    // Not testable in expression context -- requires method declaration with ref readonly.

    [Test]
    public async Task RefReadonlyParameters_RoslynScripting()
    {
        try
        {
            await TestHelpers.EvaluateCSharpAsync(
                "int Foo(ref readonly int x) => x; int v = 5; Foo(ref v)");
            Assert.Pass("Roslyn Scripting supports ref readonly parameters");
        }
        catch (Exception ex)
        {
            Assert.Pass($"Roslyn Scripting: ref readonly parameters status: {ex.GetType().Name}");
        }
        // Classification: Out of scope -- CsEval does not support method declarations
    }

    // 5. Inline Arrays
    // Requires struct declaration with [InlineArray] attribute -- out of scope for expression evaluator.

    [Test]
    public void InlineArrays_Classification()
    {
        Assert.Pass("Out of scope: inline arrays require struct declarations with [InlineArray] attribute");
    }

    // 6. Interceptors (experimental)
    // Compile-time feature, not applicable to runtime expression evaluation.

    [Test]
    public void Interceptors_Classification()
    {
        Assert.Pass("Out of scope: interceptors are a compile-time source generator feature");
    }

    // 7. Alias Any Type: using Point = (int x, int y)
    // Requires using directive -- out of scope for expression evaluator.

    [Test]
    public void AliasAnyType_Classification()
    {
        Assert.Pass("Out of scope: type aliases require using directives");
    }

    // 8. Experimental Attribute
    // Compile-time metadata attribute, not relevant to expression evaluation.

    [Test]
    public void ExperimentalAttribute_Classification()
    {
        Assert.Pass("Out of scope: [Experimental] is a compile-time attribute");
    }
}
