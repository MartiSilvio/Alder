namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class NullHandlingTests(CompilationMode mode)
{
    [TestCase("""
              {
                  int? x = null;
                  x ??= 42;
                  return x;
              }
              """,
        42,
        TestName = "NullCoalesceAssign_AssignsWhenNull")]
    [TestCase("""
              {
                  int? x = null;
                  return x ??= 42;
              }
              """,
        42,
        TestName = "NullCoalesceAssign_ReturnsAssignedValue")]
    [TestCase("""
              {
                  var x = "hello";
                  return x ??= "world";
              }
              """,
        "hello",
        TestName = "NullCoalesceAssign_ReturnsExistingValue")]
    [TestCase("""
              {
                  int? x = null;
                  x ??= 5 + 5;
                  return x;
              }
              """,
        10,
        TestName = "NullCoalesceAssign_WithExpression")]
    [TestCase("""
              {
                  int? x = null;
                  if (true) {
                      x ??= 100;
                  }
                  return x;
              }
              """,
        100,
        TestName = "NullCoalesceAssign_InIfStatement")]
    public async Task Eval_NullCoalesceAssign(string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), $"Type mismatch for: {expr}");
    }

    [Test]
    public void Eval_NullCoalesceAssign_ThrowsOnNonNullableType()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("""
                                                                      {
                                                                          var x = 10;
                                                                          x ??= 42;
                                                                          return x;
                                                                      }
                                                                      """));
        Assert.That(ex!.Message, Does.Contain("??=").And.Contain("Int32"));
    }

    // Tests with External Variables

    [Test]
    public void Eval_NullCoalesce()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", null);
        engine.SetVariable("y", "default");

        Assert.That(engine.Evaluate("x ?? y"), Is.EqualTo("default"));
        Assert.That(engine.Evaluate("y ?? \"other\""), Is.EqualTo("default"));
    }

    [Test]
    public void Eval_NullSafeAccess()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("obj", null);

        Assert.That(engine.Evaluate("obj?.Name"), Is.Null);
    }
}