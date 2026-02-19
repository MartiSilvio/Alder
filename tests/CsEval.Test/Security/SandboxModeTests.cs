namespace CsEval.Test.Security;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class SandboxModeTests(CompilationMode mode)
{
    #region Trusted Mode (Default)

    [Test]
    public void Trusted_AllowsMethodCalls()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.ToUpper()");

        Assert.That(result, Is.EqualTo("HELLO"));
    }

    [Test]
    public void Trusted_AllowsPropertyAccess()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.Length");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Trusted_BlocksGetType_ReflectionBlocked()
    {
        // Reflection types are always blocked, regardless of sandbox mode
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("text.GetType()"));
        Assert.That(ex!.Message, Does.Contain("reflection"));
    }

    #endregion

    #region Safe Mode - Method Blocking

    [Test]
    public void Safe_BlocksMethodCalls()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("text.ToUpper()"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    [Test]
    public void Safe_BlocksGetType()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });
        engine.SetVariable("obj", new { Name = "Test" });

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("obj.GetType()"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    [Test]
    public void Safe_BlocksToString()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });
        engine.SetVariable("num", 42);

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("num.ToString()"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    [Test]
    public void Safe_BlocksListMutatingMethods()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });
        engine.SetVariable("items", new List<int> { 1, 2, 3 });

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("items.Add(4)"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    #endregion

    #region Safe Mode - Property Access

    [Test]
    public void Safe_AllowsPropertyReadByDefault()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.Length");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Safe_AllowsNestedPropertyRead()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });
        engine.SetVariable("person", new { Name = "John", Address = new { City = "NYC" } });

        var result = engine.Evaluate("person.Address.City");

        Assert.That(result, Is.EqualTo("NYC"));
    }

    [Test]
    public void Safe_AllowPropertyReadFalse_BlocksPropertyAccess()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe() with { AllowPropertyRead = false }
        });
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("text.Length"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    #endregion

    #region Safe Mode - Modules Always Allowed

    [Test]
    public void Safe_AllowsModuleMethods()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });

        var result = engine.Evaluate("Math.Abs(-5)");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Safe_AllowsModuleProperties()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });

        var result = engine.Evaluate("Math.PI");

        Assert.That(result, Is.EqualTo(Math.PI));
    }

    [Test]
    public void Safe_AllowsCustomModuleMethods()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });
        engine.RegisterModule<TestModule>("Test");

        var result = engine.Evaluate("Test.Double(5)");

        Assert.That(result, Is.EqualTo(10));
    }

    #endregion

    #region Safe Mode - LINQ Always Allowed

    [Test]
    public void Safe_AllowsLinqWhere()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });
        engine.SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("items.Where(x => x > 2).ToList()") as IList;

        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public void Safe_AllowsLinqSelect()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });
        engine.SetVariable("items", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("items.Select(x => x * 2).ToList()");

        Assert.That(result, Is.InstanceOf<IList>());
        Assert.That(result, Is.EqualTo(new int[] { 2, 4, 6 }));
    }

    [Test]
    public void Safe_AllowsLinqAggregate()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });
        engine.SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("items.Sum()");

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void Safe_AllowsLinqChaining()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });
        engine.SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("items.Where(x => x > 2).Select(x => x * 10).Sum()");

        Assert.That(result, Is.EqualTo(120));
    }

    #endregion

    #region Safe Mode - Index Access

    [Test]
    public void Safe_AllowsArrayIndex()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });
        engine.SetVariable("arr", new[] { 10, 20, 30 });

        var result = engine.Evaluate("arr[1]");

        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void Safe_AllowsDictionaryIndex()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });
        engine.SetVariable("dict", new Dictionary<string, object?> { ["key"] = "value" });

        var result = engine.Evaluate("dict[\"key\"]");

        Assert.That(result, Is.EqualTo("value"));
    }

    #endregion

    #region Safe Mode - Registered Functions

    [Test]
    public void Safe_AllowsRegisteredFunctions()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });
        engine.RegisterFunction("triple", args => Convert.ToInt64(args[0]) * 3);

        var result = engine.Evaluate("triple(5)");

        Assert.That(result, Is.EqualTo(15L));
    }

    #endregion

    #region Safe Mode - Real Security Scenarios

    [Test]
    public void Safe_PreventsReflectionAttack()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });
        engine.SetVariable("obj", new { Name = "Test" });

        // Safe mode blocks method calls before reflection guard is reached
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("obj.GetType()"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    [Test]
    public void Safe_PreventsMutatingMethods()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });
        var list = new List<int> { 1, 2, 3 };
        engine.SetVariable("items", list);

        // Should block mutating methods
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("items.Clear()"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(list, Has.Count.EqualTo(3)); // List unchanged
    }

    #endregion

    #region Safe Mode - AllowAssignment

    [Test]
    public void Safe_AllowsAssignmentByDefault()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });

        var result = engine.Evaluate("{ var x = 1; x = 5; return x; }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Safe_AllowAssignmentFalse_BlocksSimpleAssignment()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe() with { AllowAssignment = false }
        });

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("{ var x = 1; x = 5; return x; }"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    [Test]
    public void Safe_AllowAssignmentFalse_BlocksCompoundAssignment()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe() with { AllowAssignment = false }
        });

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("{ var x = 1; x += 5; return x; }"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    [Test]
    public void Safe_AllowAssignmentFalse_BlocksNullCoalesceAssignment()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe() with { AllowAssignment = false }
        });

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("{ int? x = null; x ??= 5; return x; }"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    [Test]
    public void Safe_AllowAssignmentFalse_BlocksIncrement()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe() with { AllowAssignment = false }
        });

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("{ var x = 1; x++; return x; }"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    [Test]
    public void Safe_AllowAssignmentFalse_BlocksDecrement()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe() with { AllowAssignment = false }
        });

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("{ var x = 5; --x; return x; }"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    [Test]
    public void Safe_AllowAssignmentFalse_AllowsVariableDeclaration()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe() with { AllowAssignment = false }
        });

        var result = engine.Evaluate("{ var x = 5; return x; }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Safe_AllowAssignmentFalse_NullCoalesceSkipsWhenNotNull()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe() with { AllowAssignment = false }
        });

        // When value is not null, ??= doesn't assign, so it should succeed
        // Using string (reference type) since ??= only works on nullable types
        var result = engine.Evaluate(@"{ var x = ""hello""; x ??= ""world""; return x; }");

        Assert.That(result, Is.EqualTo("hello"));
    }

    #endregion

    #region Safe Mode - AllowPropertySet

    [Test]
    public void Safe_AllowsPropertySetByDefault()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
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
    public void Safe_AllowPropertySetFalse_BlocksPropertyAssignment()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe() with { AllowPropertySet = false }
        });

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate(@"
        {
            var obj = new { Value = 1 };
            obj.Value = 42;
            return obj.Value;
        }"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    [Test]
    public void Safe_AllowPropertySetFalse_AllowsPropertyRead()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe() with { AllowPropertySet = false }
        });

        var result = engine.Evaluate(@"
        {
            var obj = new { Value = 42 };
            return obj.Value;
        }");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Safe_AllowPropertySetFalse_BlocksNestedPropertySet()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe() with { AllowPropertySet = false }
        });

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate(@"
        {
            var obj = new { Inner = new { Value = 1 } };
            obj.Inner.Value = 42;
            return obj.Inner.Value;
        }"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    #endregion

    #region Safe Mode - AllowIndexSet

    [Test]
    public void Safe_AllowsIndexSetByDefault()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Extended,
            Sandbox = SandboxOptions.Safe()
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
    public void Safe_AllowIndexSetFalse_BlocksArrayIndexAssignment()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Extended,
            Sandbox = SandboxOptions.Safe() with { AllowIndexSet = false }
        });

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate(@"
        {
            var arr = [1, 2, 3];
            arr[1] = 99;
            return arr[1];
        }"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    [Test]
    public void Safe_AllowIndexSetFalse_AllowsIndexRead()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Extended,
            Sandbox = SandboxOptions.Safe() with { AllowIndexSet = false }
        });

        var result = engine.Evaluate(@"
        {
            var arr = [1, 2, 3];
            return arr[1];
        }");

        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void Safe_AllowIndexSetFalse_BlocksDictionaryIndexAssignment()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe() with { AllowIndexSet = false }
        });

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate(@"
        {
            var dict = new { key = ""value"" };
            dict[""key""] = ""new"";
            return dict[""key""];
        }"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    [Test]
    public void Safe_AllowIndexSetFalse_AllowsDictionaryRead()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe() with { AllowIndexSet = false }
        });

        var result = engine.Evaluate(@"
        {
            var dict = new { key = ""value"" };
            return dict[""key""];
        }");

        Assert.That(result, Is.EqualTo("value"));
    }

    #endregion

    #region Strict Mode

    [Test]
    public void Strict_AllowsOnlyVariableDeclarationAndRead()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Strict()
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
    public void Strict_BlocksAssignment()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Strict()
        });

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("{ var x = 1; x = 5; return x; }"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    [Test]
    public void Strict_BlocksMethodCalls()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Strict()
        });
        engine.SetVariable("text", "hello");

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("text.ToUpper()"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    [Test]
    public void Strict_AllowsPropertyRead()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Strict()
        });
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.Length");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Strict_BlocksPropertySet()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Strict()
        });

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate(@"
        {
            var obj = new { Value = 1 };
            obj.Value = 42;
            return obj.Value;
        }"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    [Test]
    public void Strict_BlocksIndexSet()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Extended,
            Sandbox = SandboxOptions.Strict()
        });

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate(@"
        {
            var arr = [1, 2, 3];
            arr[1] = 99;
            return arr[1];
        }"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    [Test]
    public void Strict_AllowsModuleMethods()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Strict()
        });

        var result = engine.Evaluate("Math.Abs(-5)");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Strict_AllowsLinq()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Strict()
        });
        engine.SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("items.Where(x => x > 2).Sum()");

        Assert.That(result, Is.EqualTo(12));
    }

    #endregion

    #region Combined Settings - Preset with Override

    [Test]
    public void Safe_WithPropertyAndIndexSetDisabled_AssignmentStillWorks()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe() with { AllowPropertySet = false, AllowIndexSet = false }
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

    [Test]
    public void Strict_WithAllowAssignmentTrue_AllowsAssignment()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Strict() with { AllowAssignment = true }
        });

        var result = engine.Evaluate("{ var x = 1; x = 5; return x; }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Strict_WithAllowMethodCallsTrue_AllowsMethodCalls()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Strict() with { AllowMethodCalls = true }
        });
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.ToUpper()");

        Assert.That(result, Is.EqualTo("HELLO"));
    }

    #endregion

    #region Helper Classes

    public class TestModule
    {
        public static int Double(int x) => x * 2;
    }

    #endregion
}
