using Alder.Test._Infrastructure;

namespace Alder.Test.Compliance;

/// <summary>
/// Third round — tests from agent findings.
/// Focuses on: for-loop continue + increment, compiled emitter divergences.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class CompiledEmitterConformanceTests(CompilationMode mode)
{
    private AlderEngine Engine(LanguageMode lang = LanguageMode.Standard)
        => TestEngineFactory.Create(mode, o => o.LanguageMode = lang);

    private object? Eval(string expr, LanguageMode lang = LanguageMode.Standard)
        => Engine(lang).Evaluate(expr);

    #region §13.9.4 For statement — continue MUST execute incrementors

    [Test]
    public void ForLoop_ContinueExecutesIncrement()
    {
        // §13.9.4: "A continue statement that exits the body of a for statement
        // causes control to be transferred to the for_iterator"
        // The incrementor (i++) must execute even when continue is hit
        var result = Eval(@"{
            var sum = 0;
            for (var i = 0; i < 5; i++)
            {
                if (i == 2) continue;
                sum += i;
            }
            return sum;
        }");
        // Expected: 0 + 1 + 3 + 4 = 8 (skips i=2, but i++ still runs)
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void ForLoop_ContinueExecutesIncrement_Complex()
    {
        // More rigorous: track increment execution via side effect
        var result = Eval(@"{
            var increments = 0;
            var body = 0;
            for (var i = 0; i < 5; i++)
            {
                increments++;
                if (i % 2 == 0) continue;
                body += i;
            }
            return increments * 100 + body;
        }");
        // increments: 5 (all iterations enter body before continue)
        // body: 1 + 3 = 4 (only odd values)
        Assert.That(result, Is.EqualTo(504));
    }

    [Test]
    public void ForLoop_ContinueWithMultipleIncrementors()
    {
        // For loop with multiple incrementors — continue should execute both
        var result = Eval(@"{
            var sum = 0;
            for (int i = 0, j = 10; i < 5; i++, j--)
            {
                if (i == 2) continue;
                sum += j;
            }
            return sum;
        }");
        // i=0,j=10: sum=10; i=1,j=9: sum=19; i=2,j=8: skip; i=3,j=7: sum=26; i=4,j=6: sum=32
        Assert.That(result, Is.EqualTo(32));
    }

    #endregion

    #region Compiled vs Interpreted parity — numeric promotion

    [Test]
    public void CompiledParity_IntDivision_TruncatesToInt()
    {
        var result = Eval(@"
            var x = 7;
            var y = 2;
            return x / y;
        ");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void CompiledParity_IntModulo()
    {
        var result = Eval(@"
            var x = 7;
            var y = 3;
            return x % y;
        ");
        Assert.That(result, Is.EqualTo(1));
    }

    #endregion

    #region ExpressionTreeEmitter coverage — ensure BoundExpressionEmitter used

    [Test]
    public void CompiledParity_NullConditional_MethodCall()
    {
        var result = Eval(@"
            string s = ""hello"";
            return s?.ToUpper();
        ");
        Assert.That(result, Is.EqualTo("HELLO"));
    }

    [Test]
    public void CompiledParity_NullConditional_NullCase()
    {
        var result = Eval(@"
            string s = null;
            return s?.ToUpper();
        ");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void CompiledParity_InterpolatedString()
    {
        var result = Eval(@"
            var x = 42;
            return $""value={x}"";
        ");
        Assert.That(result, Is.EqualTo("value=42"));
    }

    [Test]
    public void CompiledParity_SwitchStatement()
    {
        var result = Eval(@"{
            var x = 2;
            switch (x)
            {
                case 1: return ""one"";
                case 2: return ""two"";
                default: return ""other"";
            }
        }");
        Assert.That(result, Is.EqualTo("two"));
    }

    [Test]
    public void CompiledParity_TryCatchFinally()
    {
        var result = Eval(@"{
            var log = """";
            try
            {
                log += ""try:"";
                throw new System.InvalidOperationException();
            }
            catch (System.InvalidOperationException)
            {
                log += ""catch:"";
            }
            finally
            {
                log += ""finally"";
            }
            return log;
        }");
        Assert.That(result, Is.EqualTo("try:catch:finally"));
    }

    [Test]
    public void CompiledParity_CheckedArithmetic()
    {
        Assert.Throws<OverflowException>(() => Eval("checked(int.MaxValue + 1)"));
    }

    [Test]
    public void CompiledParity_UncheckedArithmetic()
    {
        var result = Eval("unchecked(int.MaxValue + 1)");
        Assert.That(result, Is.EqualTo(int.MinValue));
    }

    #endregion

    #region §12.12.9 Delegate equality operators — absent coverage

    [Test]
    public void DelegateEquality_SameReference()
    {
        var result = Eval(@"
            Func<int> f = () => 42;
            Func<int> g = f;
            return f == g;
        ");
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion

    #region §10.2.4 Implicit enum conversion — explicit cast is fine

    [Test]
    public void ExplicitEnumCast_NonZero_Works()
    {
        // Explicit cast (DayOfWeek)1 is always valid per §10.3.3
        var result = Eval("(System.DayOfWeek)1");
        Assert.That(result, Is.EqualTo(DayOfWeek.Monday));
    }

    #endregion
}
