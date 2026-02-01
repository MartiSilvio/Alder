namespace CsEval.Test.Compilation;

/// <summary>
/// Tests to verify switch statement behavior parity between IL-compiled and tree-walking modes.
/// C# semantics: empty cases fall through; non-empty cases MUST have explicit break/return/throw.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class SwitchParityTests(CompilationMode mode)
{
    #region Basic Switch

    [Test]
    public void Switch_MatchesFirstCase()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 1;
            var res = """";
            switch (x) {
                case 1: res = ""one""; break;
                case 2: res = ""two""; break;
                case 3: res = ""three""; break;
            }
            return res;
        }");

        Assert.That(result, Is.EqualTo("one"));
    }

    [Test]
    public void Switch_MatchesMiddleCase()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 2;
            var res = """";
            switch (x) {
                case 1: res = ""one""; break;
                case 2: res = ""two""; break;
                case 3: res = ""three""; break;
            }
            return res;
        }");

        Assert.That(result, Is.EqualTo("two"));
    }

    [Test]
    public void Switch_MatchesLastCase()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 3;
            var res = """";
            switch (x) {
                case 1: res = ""one""; break;
                case 2: res = ""two""; break;
                case 3: res = ""three""; break;
            }
            return res;
        }");

        Assert.That(result, Is.EqualTo("three"));
    }

    [Test]
    public void Switch_NoMatchNoDefault_NoChange()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 99;
            var res = ""initial"";
            switch (x) {
                case 1: res = ""one""; break;
                case 2: res = ""two""; break;
            }
            return res;
        }");

        Assert.That(result, Is.EqualTo("initial"));
    }

    #endregion

    #region Default Case

    [Test]
    public void Switch_DefaultCase_ExecutesWhenNoMatch()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 99;
            var res = """";
            switch (x) {
                case 1: res = ""one""; break;
                case 2: res = ""two""; break;
                default: res = ""default""; break;
            }
            return res;
        }");

        Assert.That(result, Is.EqualTo("default"));
    }

    [Test]
    public void Switch_DefaultCase_NotExecutedWhenMatch()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 1;
            var res = """";
            switch (x) {
                case 1: res = ""one""; break;
                default: res = ""default""; break;
            }
            return res;
        }");

        Assert.That(result, Is.EqualTo("one"));
    }

    [Test]
    public void Switch_DefaultCase_InMiddle()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 99;
            var res = """";
            switch (x) {
                case 1: res = ""one""; break;
                default: res = ""default""; break;
                case 2: res = ""two""; break;
            }
            return res;
        }");

        Assert.That(result, Is.EqualTo("default"));
    }

    #endregion

    #region Empty Case Fallthrough (C# Behavior)

    [Test]
    public void Switch_EmptyCaseFallthrough_SingleEmpty()
    {
        // C# allows empty cases to fall through
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 1;
            var res = """";
            switch (x) {
                case 1:
                case 2: res = ""one or two""; break;
                case 3: res = ""three""; break;
            }
            return res;
        }");

        Assert.That(result, Is.EqualTo("one or two"));
    }

    [Test]
    public void Switch_EmptyCaseFallthrough_MultipleEmpty()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 2;
            var res = """";
            switch (x) {
                case 1:
                case 2:
                case 3: res = ""one two or three""; break;
                case 4: res = ""four""; break;
            }
            return res;
        }");

        Assert.That(result, Is.EqualTo("one two or three"));
    }

    [Test]
    public void Switch_EmptyCaseFallthrough_LastEmptyMatchesDefault()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 3;
            var res = """";
            switch (x) {
                case 1:
                case 2:
                case 3: res = ""one two or three""; break;
                case 4: res = ""four""; break;
            }
            return res;
        }");

        Assert.That(result, Is.EqualTo("one two or three"));
    }

    #endregion

    #region Non-Empty Case No Fallthrough (C# Behavior)

    [Test]
    public void Switch_NonEmptyCaseNoFallthrough_ThrowsError()
    {
        // C# requires explicit break/return/throw for non-empty cases (CS0163)
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(() => engine.Evaluate(@"
        {
            var x = 1;
            var sum = 0;
            switch (x) {
                case 1: sum += 10;
                case 2: sum += 20; break;
            }
            return sum;
        }"),
            Throws.TypeOf<CsEvalException>().With.Message.Contains("CS0163"));
    }

    #endregion

    #region Switch with Return

    [Test]
    public void Switch_Return_ExitsImmediately()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 2;
            switch (x) {
                case 1: return ""one"";
                case 2: return ""two"";
                case 3: return ""three"";
            }
            return ""none"";
        }");

        Assert.That(result, Is.EqualTo("two"));
    }

    [Test]
    public void Switch_Return_InDefault()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 99;
            switch (x) {
                case 1: return ""one"";
                default: return ""default"";
            }
            return ""unreachable"";
        }");

        Assert.That(result, Is.EqualTo("default"));
    }

    #endregion

    #region Switch Inside Loop

    [Test]
    public void Switch_InsideLoop_BreakOnlyExitsSwitch()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            for (var i = 0; i < 3; i++) {
                switch (i) {
                    case 0: sum += 1; break;
                    case 1: sum += 2; break;
                    case 2: sum += 3; break;
                }
            }
            return sum;
        }");

        Assert.That(result, Is.EqualTo(6)); // 1 + 2 + 3
    }

    [Test]
    public void Switch_InsideLoop_ContinueExitsLoop()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            for (var i = 0; i < 5; i++) {
                switch (i) {
                    case 2: continue;
                    default: sum += i; break;
                }
            }
            return sum;
        }");

        // 0 + 1 + (skip 2) + 3 + 4 = 8
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void Switch_InsideWhileLoop_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var i = 0;
            var sum = 0;
            while (i < 4) {
                switch (i) {
                    case 0: sum += 10; break;
                    case 1: sum += 20; break;
                    default: sum += 1; break;
                }
                i = i + 1;
            }
            return sum;
        }");

        // 10 + 20 + 1 + 1 = 32
        Assert.That(result, Is.EqualTo(32));
    }

    #endregion

    #region Switch with Expressions

    [Test]
    public void Switch_ExpressionValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 5;
            switch (x * 2) {
                case 10: return ""ten"";
                case 20: return ""twenty"";
                default: return ""other"";
            }
        }");

        Assert.That(result, Is.EqualTo("ten"));
    }

    [Test]
    public void Switch_ExpressionInCase()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("target", 15);
        var result = engine.Evaluate(@"
        {
            var x = 15;
            switch (x) {
                case target: return ""match"";
                default: return ""no match"";
            }
        }");

        Assert.That(result, Is.EqualTo("match"));
    }

    #endregion

    #region Switch with String

    [Test]
    public void Switch_StringValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var s = ""hello"";
            switch (s) {
                case ""hi"": return 1;
                case ""hello"": return 2;
                case ""hey"": return 3;
                default: return 0;
            }
        }");

        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void Switch_StringEmptyFallthrough()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var s = ""hi"";
            switch (s) {
                case ""hi"":
                case ""hello"": return ""greeting"";
                default: return ""other"";
            }
        }");

        Assert.That(result, Is.EqualTo("greeting"));
    }

    #endregion

    #region Nested Switch

    [Test]
    public void Switch_Nested()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 1;
            var y = 2;
            var res = """";
            switch (x) {
                case 1:
                    switch (y) {
                        case 1: res = ""x1y1""; break;
                        case 2: res = ""x1y2""; break;
                    }
                    break;
                case 2: res = ""x2""; break;
            }
            return res;
        }");

        Assert.That(result, Is.EqualTo("x1y2"));
    }

    #endregion
}
