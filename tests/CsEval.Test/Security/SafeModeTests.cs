using CsEval.Evaluation;
using NUnit.Framework;

namespace CsEval.Test.Security;

[TestFixture]
public class SafeModeTests
{
    #region SafeMode Disabled (Default)

    [Test]
    public void SafeModeOff_AllowsMethodCalls()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.ToUpper()");

        Assert.That(result, Is.EqualTo("HELLO"));
    }

    [Test]
    public void SafeModeOff_AllowsPropertyAccess()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.Length");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void SafeModeOff_BlocksGetType_ReflectionBlocked()
    {
        // Reflection types are always blocked, regardless of SafeMode
        var engine = new CsEvalEngine();
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<EvalException>(() => engine.Evaluate("text.GetType()"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
    }

    #endregion

    #region SafeMode Enabled - Method Blocking

    [Test]
    public void SafeModeOn_BlocksMethodCalls()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<EvalException>(() => engine.Evaluate("text.ToUpper()"));
        Assert.That(ex!.Message, Does.Contain("SafeMode"));
    }

    [Test]
    public void SafeModeOn_BlocksGetType()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });
        engine.SetVariable("obj", new { Name = "Test" });

        var ex = Assert.Throws<EvalException>(() => engine.Evaluate("obj.GetType()"));
        Assert.That(ex!.Message, Does.Contain("SafeMode"));
    }

    [Test]
    public void SafeModeOn_BlocksToString()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });
        engine.SetVariable("num", 42);

        var ex = Assert.Throws<EvalException>(() => engine.Evaluate("num.ToString()"));
        Assert.That(ex!.Message, Does.Contain("SafeMode"));
    }

    [Test]
    public void SafeModeOn_BlocksListMutatingMethods()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });
        engine.SetVariable("items", new List<int> { 1, 2, 3 });

        var ex = Assert.Throws<EvalException>(() => engine.Evaluate("items.Add(4)"));
        Assert.That(ex!.Message, Does.Contain("SafeMode"));
    }

    #endregion

    #region SafeMode Enabled - Property Access

    [Test]
    public void SafeModeOn_AllowsPropertyReadByDefault()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.Length");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void SafeModeOn_AllowsNestedPropertyRead()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });
        engine.SetVariable("person", new { Name = "John", Address = new { City = "NYC" } });

        var result = engine.Evaluate("person.Address.City");

        Assert.That(result, Is.EqualTo("NYC"));
    }

    [Test]
    public void SafeModeOn_AllowPropertyReadFalse_BlocksPropertyAccess()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions
            {
                SafeMode = true,
                AllowPropertyRead = false
            }
        });
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<EvalException>(() => engine.Evaluate("text.Length"));
        Assert.That(ex!.Message, Does.Contain("SafeMode"));
    }

    #endregion

    #region SafeMode - Modules Always Allowed

    [Test]
    public void SafeModeOn_AllowsModuleMethods()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });

        var result = engine.Evaluate("Math.Abs(-5)");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void SafeModeOn_AllowsModuleProperties()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });

        var result = engine.Evaluate("Math.PI");

        Assert.That(result, Is.EqualTo(Math.PI));
    }

    [Test]
    public void SafeModeOn_AllowsCustomModuleMethods()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });
        engine.RegisterModule<TestModule>("Test");

        var result = engine.Evaluate("Test.Double(5)");

        Assert.That(result, Is.EqualTo(10));
    }

    #endregion

    #region SafeMode - LINQ Always Allowed

    [Test]
    public void SafeModeOn_AllowsLinqWhere()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });
        engine.SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("items.Where(x => x > 2).ToList()") as List<object?>;

        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public void SafeModeOn_AllowsLinqSelect()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });
        engine.SetVariable("items", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("items.Select(x => x * 2).ToList()") as List<object?>;

        Assert.That(result, Is.EqualTo(new List<object?> { 2, 4, 6 }));
    }

    [Test]
    public void SafeModeOn_AllowsLinqAggregate()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });
        engine.SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("items.Sum()");

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void SafeModeOn_AllowsLinqChaining()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });
        engine.SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("items.Where(x => x > 2).Select(x => x * 10).Sum()");

        Assert.That(result, Is.EqualTo(120));
    }

    #endregion

    #region SafeMode - Index Access

    [Test]
    public void SafeModeOn_AllowsArrayIndex()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });
        engine.SetVariable("arr", new[] { 10, 20, 30 });

        var result = engine.Evaluate("arr[1]");

        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void SafeModeOn_AllowsDictionaryIndex()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });
        engine.SetVariable("dict", new Dictionary<string, object?> { ["key"] = "value" });

        var result = engine.Evaluate("dict[\"key\"]");

        Assert.That(result, Is.EqualTo("value"));
    }

    #endregion

    #region SafeMode - Registered Functions

    [Test]
    public void SafeModeOn_AllowsRegisteredFunctions()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });
        engine.RegisterFunction("triple", args => Convert.ToInt64(args[0]) * 3);

        var result = engine.Evaluate("triple(5)");

        Assert.That(result, Is.EqualTo(15L));
    }

    #endregion

    #region SafeMode - Real Security Scenarios

    [Test]
    public void SafeModeOn_PreventsReflectionAttack()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });
        engine.SetVariable("obj", new { Name = "Test" });

        // SafeMode blocks method calls before reflection guard is reached
        var ex = Assert.Throws<EvalException>(() => engine.Evaluate("obj.GetType()"));
        Assert.That(ex!.Message, Does.Contain("SafeMode"));
    }

    [Test]
    public void SafeModeOn_PreventsMutatingMethods()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });
        var list = new List<int> { 1, 2, 3 };
        engine.SetVariable("items", list);

        // Should block mutating methods
        var ex = Assert.Throws<EvalException>(() => engine.Evaluate("items.Clear()"));
        Assert.That(ex!.Message, Does.Contain("SafeMode"));
        Assert.That(list, Has.Count.EqualTo(3)); // List unchanged
    }

    #endregion

    #region SafeMode - AllowAssignment

    [Test]
    public void SafeModeOn_AllowsAssignmentByDefault()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });

        var result = engine.Evaluate("{ var x = 1; x = 5; return x; }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void SafeModeOn_AllowAssignmentFalse_BlocksSimpleAssignment()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions
            {
                SafeMode = true,
                AllowAssignment = false
            }
        });

        var ex = Assert.Throws<EvalException>(() => engine.Evaluate("{ var x = 1; x = 5; return x; }"));
        Assert.That(ex!.Message, Does.Contain("SafeMode"));
    }

    [Test]
    public void SafeModeOn_AllowAssignmentFalse_BlocksCompoundAssignment()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions
            {
                SafeMode = true,
                AllowAssignment = false
            }
        });

        var ex = Assert.Throws<EvalException>(() => engine.Evaluate("{ var x = 1; x += 5; return x; }"));
        Assert.That(ex!.Message, Does.Contain("SafeMode"));
    }

    [Test]
    public void SafeModeOn_AllowAssignmentFalse_BlocksNullCoalesceAssignment()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions
            {
                SafeMode = true,
                AllowAssignment = false
            }
        });

        var ex = Assert.Throws<EvalException>(() => engine.Evaluate("{ var x = null; x ??= 5; return x; }"));
        Assert.That(ex!.Message, Does.Contain("SafeMode"));
    }

    [Test]
    public void SafeModeOn_AllowAssignmentFalse_BlocksIncrement()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions
            {
                SafeMode = true,
                AllowAssignment = false
            }
        });

        var ex = Assert.Throws<EvalException>(() => engine.Evaluate("{ var x = 1; x++; return x; }"));
        Assert.That(ex!.Message, Does.Contain("SafeMode"));
    }

    [Test]
    public void SafeModeOn_AllowAssignmentFalse_BlocksDecrement()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions
            {
                SafeMode = true,
                AllowAssignment = false
            }
        });

        var ex = Assert.Throws<EvalException>(() => engine.Evaluate("{ var x = 5; --x; return x; }"));
        Assert.That(ex!.Message, Does.Contain("SafeMode"));
    }

    [Test]
    public void SafeModeOn_AllowAssignmentFalse_AllowsVariableDeclaration()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions
            {
                SafeMode = true,
                AllowAssignment = false
            }
        });

        var result = engine.Evaluate("{ var x = 5; return x; }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void SafeModeOn_AllowAssignmentFalse_NullCoalesceSkipsWhenNotNull()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions
            {
                SafeMode = true,
                AllowAssignment = false
            }
        });

        // When value is not null, ??= doesn't assign, so it should succeed
        var result = engine.Evaluate("{ var x = 5; x ??= 10; return x; }");

        Assert.That(result, Is.EqualTo(5));
    }

    #endregion

    #region SafeMode - AllowPropertySet

    [Test]
    public void SafeModeOn_AllowsPropertySetByDefault()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });

        var result = engine.Evaluate(@"
        {
            var obj = new { Value = 1 };
            obj.Value = 42;
            return obj.Value;
        }");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void SafeModeOn_AllowPropertySetFalse_BlocksPropertyAssignment()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions
            {
                SafeMode = true,
                AllowPropertySet = false
            }
        });

        var ex = Assert.Throws<EvalException>(() => engine.Evaluate(@"
        {
            var obj = new { Value = 1 };
            obj.Value = 42;
            return obj.Value;
        }"));
        Assert.That(ex!.Message, Does.Contain("SafeMode"));
    }

    [Test]
    public void SafeModeOn_AllowPropertySetFalse_AllowsPropertyRead()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions
            {
                SafeMode = true,
                AllowPropertySet = false
            }
        });

        var result = engine.Evaluate(@"
        {
            var obj = new { Value = 42 };
            return obj.Value;
        }");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void SafeModeOn_AllowPropertySetFalse_BlocksNestedPropertySet()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions
            {
                SafeMode = true,
                AllowPropertySet = false
            }
        });

        var ex = Assert.Throws<EvalException>(() => engine.Evaluate(@"
        {
            var obj = new { Inner = new { Value = 1 } };
            obj.Inner.Value = 42;
            return obj.Inner.Value;
        }"));
        Assert.That(ex!.Message, Does.Contain("SafeMode"));
    }

    #endregion

    #region SafeMode - AllowIndexSet

    [Test]
    public void SafeModeOn_AllowsIndexSetByDefault()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
        });

        var result = engine.Evaluate(@"
        {
            var arr = [1, 2, 3];
            arr[1] = 99;
            return arr[1];
        }");

        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public void SafeModeOn_AllowIndexSetFalse_BlocksArrayIndexAssignment()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions
            {
                SafeMode = true,
                AllowIndexSet = false
            }
        });

        var ex = Assert.Throws<EvalException>(() => engine.Evaluate(@"
        {
            var arr = [1, 2, 3];
            arr[1] = 99;
            return arr[1];
        }"));
        Assert.That(ex!.Message, Does.Contain("SafeMode"));
    }

    [Test]
    public void SafeModeOn_AllowIndexSetFalse_AllowsIndexRead()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions
            {
                SafeMode = true,
                AllowIndexSet = false
            }
        });

        var result = engine.Evaluate(@"
        {
            var arr = [1, 2, 3];
            return arr[1];
        }");

        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void SafeModeOn_AllowIndexSetFalse_BlocksDictionaryIndexAssignment()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions
            {
                SafeMode = true,
                AllowIndexSet = false
            }
        });

        var ex = Assert.Throws<EvalException>(() => engine.Evaluate(@"
        {
            var dict = new { key = ""value"" };
            dict[""key""] = ""new"";
            return dict[""key""];
        }"));
        Assert.That(ex!.Message, Does.Contain("SafeMode"));
    }

    [Test]
    public void SafeModeOn_AllowIndexSetFalse_AllowsDictionaryRead()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions
            {
                SafeMode = true,
                AllowIndexSet = false
            }
        });

        var result = engine.Evaluate(@"
        {
            var dict = new { key = ""value"" };
            return dict[""key""];
        }");

        Assert.That(result, Is.EqualTo("value"));
    }

    #endregion

    #region SafeMode - Combined Security Settings

    [Test]
    public void SafeModeOn_AllSecurityDisabled_AllowsOnlyVariableDeclarationAndRead()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions
            {
                SafeMode = true,
                AllowAssignment = false,
                AllowPropertySet = false,
                AllowIndexSet = false
            }
        });

        // Variable declaration should still work
        var result = engine.Evaluate(@"
        {
            var x = 42;
            return x;
        }");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void SafeModeOn_PropertyAndIndexSetDisabled_AssignmentStillWorks()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            Security = new CsEvalOptions.SecurityOptions
            {
                SafeMode = true,
                AllowAssignment = true,
                AllowPropertySet = false,
                AllowIndexSet = false
            }
        });

        // Simple variable assignment should work
        var result = engine.Evaluate(@"
        {
            var x = 1;
            x = 99;
            return x;
        }");

        Assert.That(result, Is.EqualTo(99));
    }

    #endregion

    #region Helper Classes

    public class TestModule
    {
        public static int Double(int x) => x * 2;
    }

    #endregion
}
