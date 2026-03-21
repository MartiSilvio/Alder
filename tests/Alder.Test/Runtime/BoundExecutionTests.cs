using Alder.Parsing;

namespace Alder.Test.Runtime;

[TestFixture]
public sealed class BoundExecutionTests
{
    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_WhenBindingIsSupported()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });
        engine.SetVariable("x", -4);
        engine.SetVariable("y", 2);
        engine.SetVariable("z", 3);

        var expression = engine.Parse("Math.Abs(x - y) + Math.Max(y, z)");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(9));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForNotInAlias()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
            LanguageMode = LanguageMode.Extended
        });
        engine.SetVariable("x", 42);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 5, 8, 13 });

        var expression = engine.Parse("x not in numbers");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.True);
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForPipelineFunction()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
            LanguageMode = LanguageMode.Extended
        });
        engine.RegisterFunction("inc", args => Convert.ToInt32(args[0]) + 1);
        engine.SetVariable("x", 41);

        var expression = engine.Parse("x |> inc");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForLambdaPredicateCall()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });
        engine.SetVariable("numbers", new List<int> { 1, 3, 5, 9, 10 });
        engine.SetVariable("threshold", 4);

        var expression = engine.Parse("numbers.Where((n) => n > threshold).Count()");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(3));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForCastExpression()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("(int)5.9");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(5));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForAsExpression()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });
        engine.SetVariable("x", "abc");

        var expression = engine.Parse("x as string");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo("abc"));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForNullCoalesceExpression()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });
        engine.SetVariable<string?>("x", null);

        var expression = engine.Parse("x ?? \"fallback\"");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo("fallback"));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForConditionalExpression()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("true ? 1 : 2L");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(1L));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForBlockVariableAssignReturn()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse(@"
            var x = 1;
            x = x + 1;
            return x;
        ");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(2));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForIfElseStatement()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("""
                                      {
                                          var x = 2;
                                          if (x > 3)
                                          {
                                              x = 10;
                                          }
                                          else
                                          {
                                              x = x + 5;
                                          }
                                          return x;
                                      }
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(7));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForWhileWithBreak()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("""
                                      {
                                          var x = 0;
                                          while (x < 10)
                                          {
                                              x = x + 1;
                                              if (x == 4)
                                                  break;
                                          }
                                          return x;
                                      }
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(4));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForWhileWithContinue()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("""
                                      {
                                          var x = 0;
                                          var sum = 0;
                                          while (x < 5)
                                          {
                                              x = x + 1;
                                              if (x % 2 == 0)
                                                  continue;
                                              sum = sum + x;
                                          }
                                          return sum;
                                      }
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(9));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForForLoop_WithControlFlow()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("""
                                      {
                                          var sum = 0;
                                          for (var i = 0; i < 6; i = i + 1)
                                          {
                                              if (i % 2 == 0)
                                                  continue;
                                              sum = sum + i;
                                              if (sum > 6)
                                                  break;
                                          }
                                          return sum;
                                      }
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(9));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForDoWhileLoop_WithContinue()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("""
                                      {
                                          var i = 0;
                                          var sum = 0;
                                          do
                                          {
                                              i = i + 1;
                                              if (i == 2)
                                                  continue;
                                              sum = sum + i;
                                          }
                                          while (i < 4);
                                          return sum;
                                      }
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(8));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForForEachLoop_WithControlFlow()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });
        engine.SetVariable("items", new List<int> { 1, 2, 3, 5, 8 });

        var expression = engine.Parse("""
                                      {
                                          var sum = 0;
                                          foreach (var item in items)
                                          {
                                              if (item == 2)
                                                  continue;
                                              if (item == 5)
                                                  break;
                                              sum = sum + item;
                                          }
                                          return sum;
                                      }
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(4));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForForLoop_WithIncrementAndCompoundAssign()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("""
                                      {
                                          var sum = 0;
                                          for (var i = 0; i < 5; i++)
                                          {
                                              sum += i;
                                          }
                                          return sum;
                                      }
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(10));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForIncrementDecrementExpression()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("""
                                          var i = 1;
                                          var a = i++;
                                          var b = ++i;
                                          return a * 10 + b;
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(13));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForNullCoalesceAssignment()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("""
                                          string s = null;
                                          s ??= "fallback";
                                          return s;
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo("fallback"));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForMemberAssignmentOperators()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });
        engine.SetVariable("box", new MutableBox());

        var expression = engine.Parse("""
                                          box.Value = 2;
                                          box.Value += 3;
                                          box.Value++;
                                          ++box.Value;
                                          return box.Value;
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(7));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForIndexAssignmentOperators()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });
        engine.SetVariable("items", new List<int> { 1, 2, 3 });

        var expression = engine.Parse("""
                                          items[1] = 10;
                                          items[1] += 5;
                                          items[1]++;
                                          return items[1];
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(16));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForMemberAndIndexNullCoalesceAssignment()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });
        engine.SetVariable("box", new MutableBox());
        engine.SetVariable("dict", new Dictionary<string, string?> { ["name"] = null });

        var expression = engine.Parse("""
                                          box.Text ??= "member";
                                          dict["name"] ??= "index";
                                          return box.Text + ":" + dict["name"];
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo("member:index"));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForUsingStatement()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });
        var probe = new DisposableProbe();
        engine.SetVariable("res", probe);

        var expression = engine.Parse("""
                                      {
                                          var x = 0;
                                          using (res)
                                          {
                                              x = 42;
                                          }
                                          return x;
                                      }
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(probe.DisposeCount, Is.EqualTo(1));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForLockStatement()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });
        engine.SetVariable("lockObj", new object());

        var expression = engine.Parse("""
                                      {
                                          var x = 0;
                                          lock (lockObj)
                                          {
                                              x = 10;
                                          }
                                          return x;
                                      }
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(10));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForNameofTypeofDefaultAndSizeof()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("""
                                          var a = nameof(System.String);
                                          var b = typeof(int);
                                          var c = default(int);
                                          var d = sizeof(int);
                                          return a + ":" + b.Name + ":" + c + ":" + d;
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo("String:Int32:0:4"));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForIsPattern()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("""
                                          object x = 7;
                                          return x is int n && n > 5;
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(true));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForSwitchWithWhenGuard()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("""
                                      {
                                          object x = 3;
                                          switch (x)
                                          {
                                              case int n when n < 0:
                                                  return -1;
                                              case int n when n > 0:
                                                  return n * 2;
                                              default:
                                                  return 0;
                                          }
                                      }
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(6));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForTryCatchFinallyWithThrowExpression()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });
        engine.SetVariable("missingEx", new Exception("missing"));

        var expression = engine.Parse("""
                                      {
                                          var marker = 0;
                                          try
                                          {
                                              string s = null;
                                              var value = s ?? throw missingEx;
                                          }
                                          catch (Exception ex) when (ex.Message == "missing")
                                          {
                                              marker = 1;
                                          }
                                          finally
                                          {
                                              marker = marker + 1;
                                          }
                                          return marker;
                                      }
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(2));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForThrowStatementRethrow()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });
        engine.SetVariable("boomEx", new Exception("boom"));

        var expression = engine.Parse("""
                                      {
                                          try
                                          {
                                              try
                                              {
                                                  throw boomEx;
                                              }
                                              catch (Exception ex)
                                              {
                                                  throw;
                                              }
                                          }
                                          catch (Exception ex2)
                                          {
                                              return ex2.Message;
                                          }
                                      }
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo("boom"));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForObjectCreation()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("""
                                          var ex = new System.InvalidOperationException("bad");
                                          return ex.Message;
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo("bad"));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForTypedArrayCreation()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("""
                                          var arr = new int[3];
                                          arr[0] = 1;
                                          arr[1] = 2;
                                          arr[2] = 3;
                                          return arr[1];
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(2));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForInterpolatedString()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });
        engine.SetVariable("name", "alpha");
        engine.SetVariable("n", 7);

        var expression = engine.Parse("$\"{name}-{n:000}\"");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo("alpha-007"));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForCheckedExpression()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("checked(10 + 20)");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(30));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForChainedComparison()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
            LanguageMode = LanguageMode.Extended
        });

        var expression = engine.Parse("1 < 2 < 3");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(true));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForRangeExpression()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
            LanguageMode = LanguageMode.Extended
        });

        var expression = engine.Parse("1..=3");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.TypeOf<Alder.Runtime.InclusiveRange>());
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForSwitchExpression()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("5 switch { < 0 => \"neg\", > 0 => \"pos\", _ => \"zero\" }");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo("pos"));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForArrayLiteralWithSpread()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
            LanguageMode = LanguageMode.Extended
        });

        var expression = engine.Parse("""
                                          var arr = [1, ..[2, 3], 4];
                                          return arr[2];
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(3));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForObjectLiteralWithSpread()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
            LanguageMode = LanguageMode.Extended
        });
        engine.SetVariable("obj", new Dictionary<string, object?> { ["A"] = 1 });

        var expression = engine.Parse("new { ..obj, B = 2 }");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.AssignableTo<IDictionary<string, object?>>());
        var dict = (IDictionary<string, object?>)result!;
        Assert.That(dict["A"], Is.EqualTo(1));
        Assert.That(dict["B"], Is.EqualTo(2));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForTupleExpression()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("(1, 2).Item2");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(2));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForSliceExpression()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
            LanguageMode = LanguageMode.Extended
        });

        var expression = engine.Parse("\"hello\"[1:4]");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo("ell"));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForNamedArguments()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("\"hello\".Substring(startIndex: 1, length: 2)");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo("el"));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForOutArguments()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("""
                                          int.TryParse("42", out var n);
                                          return n;
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForMultiDimensionalArrayAccess()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("""
                                          var matrix = new int[2, 2];
                                          matrix[1, 0] = 42;
                                          return matrix[1, 0];
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_ForDeconstruction()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = engine.Parse("""
                                          var (a, b) = (1, 2);
                                          return a + b;
                                      """);
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(3));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Interpreted_ShouldRecordBoundFallback_WhenBindingIsUnsupported()
    {
        var engine = new AlderEngine(AlderOptions.Default with
        {
        });

        var expression = new AlderExpression("unsupported", new UnsupportedExpr());
        Assert.That(() => engine.Evaluate(expression), Throws.InstanceOf<Exception>());
        Assert.That(expression.BoundExecutionCount, Is.EqualTo(0));
        Assert.That(expression.BoundFallbackCount, Is.GreaterThan(0));
        Assert.That(expression.LastBoundFallbackReason, Is.Not.Null.And.Not.Empty);
    }

    private sealed class MutableBox
    {
        public int Value { get; set; }
        public string? Text { get; set; }
    }

    private sealed class DisposableProbe : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
        }
    }

    private sealed record UnsupportedExpr : Expr
    {
        public override T Accept<T>(IExprVisitor<T> visitor) => throw new NotSupportedException("Synthetic unsupported expression");
    }
}
