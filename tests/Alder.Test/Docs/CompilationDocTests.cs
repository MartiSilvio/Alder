using System.Linq.Expressions;
using Alder.Test._Infrastructure;

namespace Alder.Test.Docs;

[Parallelizable(ParallelScope.Children)]
public class CompilationDocTests
{
    [Test]
    public void UseCompiler()
    {
        var engine = TestEngineFactory.Create(CompilationMode.Compiled);
        Assert.That(engine.Evaluate<int>("1 + 2"), Is.EqualTo(3));
    }

    [Test]
    public void AutoCompile()
    {
        var engine = TestEngineFactory.Create(CompilationMode.Compiled);

        var result = engine.Evaluate<string>("""
            string.Join(", ", new[] { 3, 1, 4, 1, 5 }.Distinct().OrderBy(x => x))
            """);

        Assert.That(result, Is.EqualTo("1, 3, 4, 5"));
    }

    [Test]
    public void PreCompile()
    {
        var engine = TestEngineFactory.Create(CompilationMode.Compiled);
        engine.SetVariable<double>("x", 3.0);
        engine.SetVariable<double>("y", 4.0);

        var expr = engine.ParseAndCompile("Math.Sqrt(x * x + y * y)");
        Assert.That(engine.HasCompiledDelegate(expr), Is.True);

        var result = engine.Evaluate<double>(expr);
        Assert.That(result, Is.EqualTo(5.0));
    }

    [Test]
    public void CompiledExpression()
    {
        var engine = TestEngineFactory.Create(CompilationMode.Compiled);
        engine.SetVariable<int>("n", 100);

        var compiled = engine.Compile<int>("Enumerable.Range(1, n).Where(x => x % 3 == 0 || x % 5 == 0).Sum()");

        Assert.That(compiled.Invoke(), Is.EqualTo(2418));

        engine.SetVariable<int>("n", 10);
        Assert.That(compiled.Invoke(), Is.EqualTo(33));
    }

    [Test]
    public void CompileToFunc()
    {
        var engine = TestEngineFactory.Create(CompilationMode.Compiled);
        engine.SetVariable<double>("r", 5.0);

        var circleArea = engine.CompileToFunc<double>("Math.PI * r * r");
        Assert.That(circleArea(), Is.EqualTo(Math.PI * 25).Within(0.001));

        engine.SetVariable<double>("r", 10.0);
        Assert.That(circleArea(), Is.EqualTo(Math.PI * 100).Within(0.001));
    }

    [Test]
    public void ParseAsExpression()
    {
        var engine = TestEngineFactory.Create(CompilationMode.Compiled);

        Expression<Func<int, bool>> predicate =
            engine.ParseAsExpression<Func<int, bool>>("x => x > 18 && x < 65");

        var fn = predicate.Compile();
        Assert.That(fn(25), Is.True);
        Assert.That(fn(10), Is.False);
        Assert.That(fn(70), Is.False);
    }

    [Test]
    public void DynamicLinq_IEnumerable()
    {
        AlderEval.Reset();
        AlderEval.Configure(o => o.UseCompiler());

        var items = new List<int> { 1, 2, 3, 4, 5 };
        var result = items.WhereDynamic("x => x > 3").ToList();

        Assert.That(result, Is.EqualTo(new List<int> { 4, 5 }));
        AlderEval.Reset();
    }
}
