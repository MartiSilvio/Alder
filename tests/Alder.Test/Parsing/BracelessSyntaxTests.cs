using Alder.Test._Infrastructure;

namespace Alder.Test.Parsing;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class BracelessSyntaxTests(CompilationMode mode)
{

    [Test]
    public async Task Program_SingleExpression()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = "1 + 2";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(3));
        Assert.That(result, Is.EqualTo(csharpResult));
    }

    [Test]
    public async Task Program_VariableAndExpression()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = "var x = 5; x * 2";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(10));
        Assert.That(result, Is.EqualTo(csharpResult));
    }

    [Test]
    public async Task Program_MultipleStatements()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = "var x = 1; var y = 2; x + y";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(3));
        Assert.That(result, Is.EqualTo(csharpResult));
    }

    [Test]
    public async Task Program_ForLoop()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = @"
            var sum = 0;
            for (var i = 0; i < 5; i++) {
                sum += i;
            }
            sum";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(10));
        Assert.That(result, Is.EqualTo(csharpResult));
    }

    [Test]
    public async Task Program_WhileLoop()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = @"
            var i = 0;
            var sum = 0;
            while (i < 5) {
                sum += i;
                i++;
            }
            sum";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(10));
        Assert.That(result, Is.EqualTo(csharpResult));
    }

    [Test]
    public async Task Program_IfStatement()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = @"
            var x = 10;
            if (x > 5) {
                x = 100;
            }
            x";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(100));
        Assert.That(result, Is.EqualTo(csharpResult));
    }

    [Test]
    public async Task Program_IfElseStatement()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = @"
            var x = 3;
            var result = 0;
            if (x > 5) {
                result = 1;
            } else {
                result = 2;
            }
            result";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(2));
        Assert.That(result, Is.EqualTo(csharpResult));
    }



    [Test]
    public async Task If_SingleLine_NoBraces()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = "var x = 5; if (x > 3) x = 10; x";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(10));
        Assert.That(result, Is.EqualTo(csharpResult));
    }

    [Test]
    public async Task If_SingleLine_ConditionFalse()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = "var x = 5; if (x > 10) x = 100; x";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(5));
        Assert.That(result, Is.EqualTo(csharpResult));
    }

    [Test]
    public async Task IfElse_SingleLine_NoBraces()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = "var x = 3; if (x > 5) x = 10; else x = 20; x";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(20));
        Assert.That(result, Is.EqualTo(csharpResult));
    }

    [Test]
    public async Task If_Nested_SingleLine()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = "var x = 0; if (true) if (true) x = 42; x";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(42));
        Assert.That(result, Is.EqualTo(csharpResult));
    }

    [Test]
    public async Task IfElseIf_Chain_SingleLine()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = "var x = 5; var r = 0; if (x > 10) r = 1; else if (x > 3) r = 2; else r = 3; r";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(2));
        Assert.That(result, Is.EqualTo(csharpResult));
    }



    [Test]
    public async Task For_SingleLine_NoBraces()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = "var sum = 0; for (var i = 0; i < 5; i++) sum += i; sum";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(10));
        Assert.That(result, Is.EqualTo(csharpResult));
    }

    [Test]
    public async Task For_SingleLine_Multiplication()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = "var product = 1; for (var i = 1; i <= 5; i++) product *= i; product";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(120));
        Assert.That(result, Is.EqualTo(csharpResult));
    }



    [Test]
    public async Task While_SingleLine_NoBraces()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = "var i = 0; while (i < 5) i++; i";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(5));
        Assert.That(result, Is.EqualTo(csharpResult));
    }

    [Test]
    public async Task While_SingleLine_WithAccumulator()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = "var i = 0; var sum = 0; while (i < 5) sum += i++; sum";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(10));
        Assert.That(result, Is.EqualTo(csharpResult));
    }



    [Test]
    public async Task DoWhile_SingleLine_NoBraces()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = "var i = 0; do i++; while (i < 5); i";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(5));
        Assert.That(result, Is.EqualTo(csharpResult));
    }

    [Test]
    public async Task DoWhile_ExecutesAtLeastOnce()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = "var i = 10; do i++; while (i < 5); i";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(11));
        Assert.That(result, Is.EqualTo(csharpResult));
    }



    [Test]
    public async Task For_WithBracelessIf()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = @"
            var sum = 0;
            for (var i = 0; i < 10; i++)
                if (i % 2 == 0) sum += i;
            sum";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(20));
        Assert.That(result, Is.EqualTo(csharpResult));
    }

    [Test]
    public async Task While_WithBracelessIf()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = @"
            var i = 0;
            var sum = 0;
            while (i < 10)
                if (i++ % 2 == 0) sum += i - 1;
            sum";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(20));
        Assert.That(result, Is.EqualTo(csharpResult));
    }

    [Test]
    public async Task If_WithBracelessFor()
    {
        var engine = TestEngineFactory.Create(mode);

        const string expr = @"
            var sum = 0;
            var run = true;
            if (run)
                for (var i = 0; i < 5; i++) sum += i;
            sum";

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.EqualTo(10));
        Assert.That(result, Is.EqualTo(csharpResult));
    }

}
