namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ControlFlowTests(CompilationMode mode)
{
    [TestCase("true ? 1 : 2", 1, TestName = "Ternary_TrueBranch")]
    [TestCase("false ? 1 : 2", 2, TestName = "Ternary_FalseBranch")]
    [TestCase("{ var x = 5; return x * 2; }", 10, TestName = "Block_WithReturn")]
    [TestCase("{ object x = null; if (x == null) return 42; return 0; }", 42, TestName = "If_EarlyReturnWhenTrue")]
    [TestCase("{ var x = 10; if (x == null) return 42; return 0; }", 0, TestName = "If_EarlyReturnWhenFalse")]
    [TestCase("{ var x = true; if (x) return 1; else return 2; }", 1, TestName = "IfElse_TakesThenBranch")]
    [TestCase("{ var x = false; if (x) return 1; else return 2; }", 2, TestName = "IfElse_TakesElseBranch")]
    [TestCase("""
              {
                  var x = 5;
                  if (x > 3) {
                      var y = x * 2;
                      return y;
                  }
                  return 0;
              }
              """,
        10, TestName = "If_WithBlock")]
    [TestCase("""
              {
                  var x = 1;
                  if (x > 3) {
                      return 100;
                  } else {
                      return 200;
                  }
              }
              """,
        200, TestName = "If_WithElseBlock")]
    [TestCase("""
              {
                  var x = 10;
                  var y = 20;
                  if (x > 5) {
                      if (y > 15) return 1;
                      else return 2;
                  }
                  return 3;
              }
              """,
        1, TestName = "If_Nested")]
    [TestCase("""
              {
                  var x = 2;
                  if (x == 1) return "one";
                  else if (x == 2) return "two";
                  else return "other";
              }
              """,
        "two", TestName = "If_ElseIf")]
    [TestCase("""
              {
                  var x = 5;
                  if (x < 3) {
                      return 0;
                  }
                  return x * 2;
              }
              """,
        10, TestName = "If_NoReturnContinuesExecution")]
    // ECMA-334 §12.18 - Conditional Operator Type Inference
    [TestCase("true ? 1 : 2L", 1L, TestName = "Ternary_IntLong_PromotesToLong")]
    [TestCase("false ? 1 : 2L", 2L, TestName = "Ternary_IntLong_FalseBranch")]
    [TestCase("true ? 1.0f : 2.0", 1.0, TestName = "Ternary_FloatDouble_PromotesToDouble")]
    [TestCase("true ? null : \"hello\"", null, TestName = "Ternary_NullString_ReturnsNull")]
    [TestCase("false ? null : \"hello\"", "hello", TestName = "Ternary_NullString_ReturnsString")]
    [TestCase("true ? 1 : 2", 1, TestName = "Ternary_SameType_NoPromotion")]
    // ECMA-334 §12.18 - Nested Ternary
    [TestCase("true ? true ? 1 : 2 : 3", 1, TestName = "Ternary_Nested_InnerTrue")]
    [TestCase("true ? false ? 1 : 2 : 3", 2, TestName = "Ternary_Nested_InnerFalse")]
    [TestCase("false ? 1 : true ? 2 : 3", 2, TestName = "Ternary_Nested_ElseBranch")]
    public async Task Eval_ControlFlow(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #region Tests with External Variables

    [Test]
    public void Eval_Ternary_WithExpression()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10L);
        Assert.That(engine.Evaluate("x > 5 ? \"big\" : \"small\""), Is.EqualTo("big"));
    }

    [Test]
    public void Eval_Ternary_WithArrays()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("true ? [] : [1, 2, 3]");
        Assert.That(result, Is.InstanceOf<IList>());
        Assert.That(result, Has.Count.EqualTo(0));
    }

    [Test]
    public void Eval_IfStatement_NullCheck_Pattern()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("person", new TestPerson { Name = "John", Age = 30 });

        var result = engine.Evaluate(@"{
            var p = person;
            if (p == null) return null;
            return p + new { Extra = ""test"" };
        }") as IDictionary<string, object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Extra"], Is.EqualTo("test"));
    }

    #endregion
}