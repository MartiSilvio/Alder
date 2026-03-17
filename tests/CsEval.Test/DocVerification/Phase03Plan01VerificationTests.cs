namespace CsEval.Test.DocVerification;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class Phase03Plan01VerificationTests(CompilationMode mode)
{
    private CsEvalEngine Engine()
        => new(CsEvalOptions.Default with { CompilationMode = mode });

    private object? Eval(string expr) => Engine().Evaluate(expr);

    // ═══════════════════════════════════════════════════════════════
    // Declaration Statements — var
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Var_Int() => Assert.That(Eval("{ var x = 42; return x; }"), Is.EqualTo(42));

    [Test]
    public void Var_String() => Assert.That(Eval(@"{ var name = ""hello""; return name; }"), Is.EqualTo("hello"));

    [Test]
    public void Var_Bool() => Assert.That(Eval("{ var flag = true; return flag; }"), Is.EqualTo(true));

    [Test]
    public void Var_Null_ParserError()
        => Assert.That(() => Eval("{ var x = null; return x; }"), Throws.InstanceOf<CsEvalException>());

    // ═══════════════════════════════════════════════════════════════
    // Declaration Statements — typed
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Typed_Int() => Assert.That(Eval("{ int x = 42; return x; }"), Is.EqualTo(42));

    [Test]
    public void Typed_String() => Assert.That(Eval(@"{ string s = ""world""; return s; }"), Is.EqualTo("world"));

    [Test]
    public void Typed_Double() => Assert.That(Eval("{ double pi = 3.14; return pi; }"), Is.EqualTo(3.14));

    // ═══════════════════════════════════════════════════════════════
    // Declaration Statements — generic and FQN types
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Generic_ListInt()
        => Assert.That(Eval("{ List<int> nums = new List<int>(); nums.Add(1); nums.Add(2); return nums.Count; }"), Is.EqualTo(2));

    [Test]
    public void FQN_StringBuilder()
        => Assert.That(Eval(@"{ System.Text.StringBuilder sb = new System.Text.StringBuilder(""hello""); return sb.ToString(); }"), Is.EqualTo("hello"));

    // ═══════════════════════════════════════════════════════════════
    // Declaration Statements — const
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Const_Int() => Assert.That(Eval("{ const int MAX = 100; return MAX; }"), Is.EqualTo(100));

    [Test]
    public void Const_String() => Assert.That(Eval(@"{ const string GREETING = ""hello""; return GREETING; }"), Is.EqualTo("hello"));

    // ═══════════════════════════════════════════════════════════════
    // Declaration Statements — multi-var
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void MultiVar_Two() => Assert.That(Eval("{ int x = 1, y = 2; return x + y; }"), Is.EqualTo(3));

    [Test]
    public void MultiVar_Three() => Assert.That(Eval("{ int a = 10, b = 20, c = 30; return a + b + c; }"), Is.EqualTo(60));

    // ═══════════════════════════════════════════════════════════════
    // Declaration Statements — deconstruction
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Deconstruct_Pair() => Assert.That(Eval("{ var (a, b) = (1, 2); return a + b; }"), Is.EqualTo(3));

    [Test]
    public void Deconstruct_Triple() => Assert.That(Eval("{ var (x, y, z) = (10, 20, 30); return x + y + z; }"), Is.EqualTo(60));

    // ═══════════════════════════════════════════════════════════════
    // Declaration Statements — local functions
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void LocalFunc_Square()
        => Assert.That(Eval("{ int Square(int n) { return n * n; } return Square(5); }"), Is.EqualTo(25));

    [Test]
    public void LocalFunc_Add()
        => Assert.That(Eval("{ int Add(int a, int b) { return a + b; } return Add(3, 4); }"), Is.EqualTo(7));

    [Test]
    public void LocalFunc_Closure()
        => Assert.That(Eval("{ var factor = 10; int Scale(int n) { return n * factor; } return Scale(3); }"), Is.EqualTo(30));

    [Test]
    public void LocalFunc_NonKeywordParam_Fails()
        => Assert.That(() => Eval(@"{ string GetMsg(Exception ex) { return ex.Message; } return GetMsg(new System.Exception(""hi"")); }"),
            Throws.InstanceOf<CsEvalException>());

    // ═══════════════════════════════════════════════════════════════
    // Declaration Statements — block scoping
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Block_InnerScope()
    {
        var result = Eval(@"
        {
            var x = 1;
            { var y = 2; x = x + y; }
            return x;
        }");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void If_InnerScope()
    {
        var result = Eval(@"
        {
            var x = 1;
            if (true) { var y = 2; x = x + y; }
            return x;
        }");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void For_LoopVarScope()
        => Assert.That(Eval("{ var sum = 0; for (var i = 0; i < 3; i++) { sum += i; } return sum; }"), Is.EqualTo(3));

    // ═══════════════════════════════════════════════════════════════
    // Declaration Statements — using statement
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Using_Parenthesized()
    {
        var result = Eval(@"
        {
            var result = 0;
            using (var ms = new System.IO.MemoryStream())
            {
                result = 1;
            }
            return result;
        }");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Using_WithoutParens_Errors()
        => Assert.That(() => Eval("{ using var x = new System.IO.MemoryStream(); return 1; }"),
            Throws.InstanceOf<CsEvalException>());

    // ═══════════════════════════════════════════════════════════════
    // Declaration Statements — lock statement
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Lock_Statement()
    {
        var result = Eval(@"
        {
            var obj = new object();
            var x = 0;
            lock (obj) { x = 42; }
            return x;
        }");
        Assert.That(result, Is.EqualTo(42));
    }

    // ═══════════════════════════════════════════════════════════════
    // Selection Statements — if/else
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void If_True()
        => Assert.That(Eval(@"{ var x = 5; if (x > 3) { return ""big""; } else { return ""small""; } }"), Is.EqualTo("big"));

    [Test]
    public void IfElseIf_Chain()
    {
        var result = Eval(@"
        {
            var x = 5;
            if (x > 10) { return ""large""; }
            else if (x > 3) { return ""medium""; }
            else { return ""small""; }
        }");
        Assert.That(result, Is.EqualTo("medium"));
    }

    [Test]
    public void If_SingleStatement()
        => Assert.That(Eval(@"{ var x = 10; if (x > 5) return ""yes""; return ""no""; }"), Is.EqualTo("yes"));

    // ═══════════════════════════════════════════════════════════════
    // Selection Statements — switch constant patterns
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Switch_ConstantPattern()
    {
        var result = Eval(@"
        {
            var x = 2;
            var result = """";
            switch (x)
            {
                case 1: result = ""one""; break;
                case 2: result = ""two""; break;
                case 3: result = ""three""; break;
                default: result = ""other""; break;
            }
            return result;
        }");
        Assert.That(result, Is.EqualTo("two"));
    }

    // ═══════════════════════════════════════════════════════════════
    // Selection Statements — switch type patterns
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Switch_TypePattern()
    {
        var result = Eval(@"
        {
            object x = 42;
            var result = """";
            switch (x)
            {
                case int n: result = ""int:"" + n; break;
                case string s: result = ""string:"" + s; break;
                default: result = ""other""; break;
            }
            return result;
        }");
        Assert.That(result, Is.EqualTo("int:42"));
    }

    // ═══════════════════════════════════════════════════════════════
    // Selection Statements — switch when guards
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Switch_WhenGuard()
    {
        var result = Eval(@"
        {
            object x = 42;
            var result = """";
            switch (x)
            {
                case int n when n > 10: result = ""big int""; break;
                case int n: result = ""small int""; break;
                default: result = ""other""; break;
            }
            return result;
        }");
        Assert.That(result, Is.EqualTo("big int"));
    }

    // ═══════════════════════════════════════════════════════════════
    // Selection Statements — goto case / goto default
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Switch_GotoCase()
    {
        var result = Eval(@"
        {
            var x = 1;
            var result = """";
            switch (x)
            {
                case 1: result = ""start""; goto case 2;
                case 2: result = result + ""-end""; break;
                default: result = ""default""; break;
            }
            return result;
        }");
        Assert.That(result, Is.EqualTo("start-end"));
    }

    [Test]
    public void Switch_GotoDefault()
    {
        var result = Eval(@"
        {
            var x = 1;
            var result = """";
            switch (x)
            {
                case 1: result = ""start""; goto default;
                case 2: result = ""two""; break;
                default: result = result + ""-default""; break;
            }
            return result;
        }");
        Assert.That(result, Is.EqualTo("start-default"));
    }

    // ═══════════════════════════════════════════════════════════════
    // Selection Statements — fall-through
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Switch_NonEmptyFallThrough_Error()
    {
        Assert.That(() => Eval(@"
        {
            var x = 1;
            var result = """";
            switch (x)
            {
                case 1: result = ""one"";
                case 2: result = ""two""; break;
            }
            return result;
        }"), Throws.InstanceOf<CsEvalException>());
    }

    [Test]
    public void Switch_EmptyFallThrough_Allowed()
    {
        var result = Eval(@"
        {
            var x = 1;
            var result = """";
            switch (x)
            {
                case 1:
                case 2: result = ""one or two""; break;
                default: result = ""other""; break;
            }
            return result;
        }");
        Assert.That(result, Is.EqualTo("one or two"));
    }

    // ═══════════════════════════════════════════════════════════════
    // Selection Statements — switch expression
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SwitchExpr_Basic()
    {
        var result = Eval(@"
        {
            var x = 2;
            return x switch { 1 => ""one"", 2 => ""two"", _ => ""other"" };
        }");
        Assert.That(result, Is.EqualTo("two"));
    }

    [Test]
    public void SwitchExpr_WithPatterns()
    {
        var result = Eval(@"
        {
            object val = 42;
            return val switch
            {
                int n when n > 100 => ""big"",
                int n => ""int:"" + n,
                string s => ""str:"" + s,
                _ => ""unknown""
            };
        }");
        Assert.That(result, Is.EqualTo("int:42"));
    }

    // ═══════════════════════════════════════════════════════════════
    // Iteration Statements — for loop
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void For_BasicSum()
        => Assert.That(Eval("{ var sum = 0; for (var i = 1; i <= 5; i++) { sum += i; } return sum; }"), Is.EqualTo(15));

    [Test]
    public void For_MultiInit()
        => Assert.That(Eval("{ var sum = 0; for (int i = 0, j = 10; i < 3; i++, j--) { sum += j; } return sum; }"), Is.EqualTo(27));

    [Test]
    public void For_EmptyParts_WithBreak()
        => Assert.That(Eval("{ var i = 0; for (;;) { i++; if (i >= 3) break; } return i; }"), Is.EqualTo(3));

    // ═══════════════════════════════════════════════════════════════
    // Iteration Statements — foreach
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Foreach_Array()
        => Assert.That(Eval("{ var sum = 0; foreach (var x in new int[] {1, 2, 3}) { sum += x; } return sum; }"), Is.EqualTo(6));

    [Test]
    public void Foreach_List()
    {
        var result = Eval(@"
        {
            var list = new List<int>();
            list.Add(10);
            list.Add(20);
            list.Add(30);
            var sum = 0;
            foreach (var x in list) { sum += x; }
            return sum;
        }");
        Assert.That(result, Is.EqualTo(60));
    }

    [Test]
    public void Foreach_String()
        => Assert.That(Eval(@"{ var count = 0; foreach (var c in ""hello"") { count++; } return count; }"), Is.EqualTo(5));

    // ═══════════════════════════════════════════════════════════════
    // Iteration Statements — while
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void While_Basic()
        => Assert.That(Eval("{ var i = 0; while (i < 3) { i++; } return i; }"), Is.EqualTo(3));

    [Test]
    public void While_Summation()
    {
        var result = Eval(@"
        {
            var sum = 0;
            var n = 1;
            while (n <= 100)
            {
                sum += n;
                n++;
            }
            return sum;
        }");
        Assert.That(result, Is.EqualTo(5050));
    }

    // ═══════════════════════════════════════════════════════════════
    // Iteration Statements — do-while
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void DoWhile_Basic()
        => Assert.That(Eval("{ var i = 0; do { i++; } while (i < 3); return i; }"), Is.EqualTo(3));

    [Test]
    public void DoWhile_AtLeastOnce()
        => Assert.That(Eval("{ var i = 0; do { i++; } while (false); return i; }"), Is.EqualTo(1));

    // ═══════════════════════════════════════════════════════════════
    // Iteration Statements — nested loops
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Nested_ForLoops()
    {
        var result = Eval(@"
        {
            var count = 0;
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    count++;
                }
            }
            return count;
        }");
        Assert.That(result, Is.EqualTo(9));
    }

    // ═══════════════════════════════════════════════════════════════
    // Iteration Statements — break and continue
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void BreakAndContinue()
    {
        var result = Eval(@"
        {
            var sum = 0;
            for (var i = 0; i < 10; i++)
            {
                if (i % 2 == 0) continue;
                if (i > 7) break;
                sum += i;
            }
            return sum;
        }");
        Assert.That(result, Is.EqualTo(16));
    }

    // ═══════════════════════════════════════════════════════════════
    // Iteration Statements — execution limits
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void MaxStatements_Terminates()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            Constraints = new ExecutionConstraints { MaxStatements = 10 }
        });
        Assert.That(() => engine.Evaluate("{ var i = 0; while (true) { i++; } return i; }"),
            Throws.InstanceOf<CsEvalExecutionLimitException>());
    }

    // ═══════════════════════════════════════════════════════════════
    // Research verification — goto backward
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Goto_Backward()
    {
        var result = Eval(@"
        {
            var i = 0;
            start:
            i++;
            if (i < 3) goto start;
            return i;
        }");
        Assert.That(result, Is.EqualTo(3));
    }
}
