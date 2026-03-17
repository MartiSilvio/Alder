namespace CsEval.Test.DocVerification;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class Phase03Plan02VerificationTests(CompilationMode mode)
{
    private CsEvalEngine Engine()
        => TestEngineFactory.Create(mode);

    private object? Eval(string expr) => Engine().Evaluate(expr);

    // ═══════════════════════════════════════════════════════════════
    // Jump Statements — return
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Return_WithValue()
        => Assert.That(Eval("{ return 42; }"), Is.EqualTo(42));

    [Test]
    public void Return_Expression()
        => Assert.That(Eval("{ var x = 10; return x * 2; }"), Is.EqualTo(20));

    [Test]
    public void Return_EarlyExit()
        => Assert.That(Eval("""
            {
                var x = 5;
                if (x > 3) return "big";
                return "small";
            }
            """), Is.EqualTo("big"));

    // ═══════════════════════════════════════════════════════════════
    // Jump Statements — break
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Break_InWhileLoop()
        => Assert.That(Eval("""
            {
                var i = 0;
                while (true) { if (i == 3) break; i++; }
                return i;
            }
            """), Is.EqualTo(3));

    [Test]
    public void Break_InForLoop()
        => Assert.That(Eval("""
            {
                var sum = 0;
                for (var i = 1; i <= 10; i++)
                {
                    if (i > 5) break;
                    sum += i;
                }
                return sum;
            }
            """), Is.EqualTo(15));

    // ═══════════════════════════════════════════════════════════════
    // Jump Statements — continue
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Continue_SkipsEvenNumbers()
        => Assert.That(Eval("""
            {
                var sum = 0;
                for (var i = 1; i <= 5; i++)
                {
                    if (i % 2 == 0) continue;
                    sum += i;
                }
                return sum;
            }
            """), Is.EqualTo(9));

    [Test]
    public void Continue_InWhile()
        => Assert.That(Eval("""
            {
                var count = 0;
                var i = 0;
                while (i < 10)
                {
                    i++;
                    if (i % 3 == 0) continue;
                    count++;
                }
                return count;
            }
            """), Is.EqualTo(7));

    // ═══════════════════════════════════════════════════════════════
    // Jump Statements — goto
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Goto_ForwardJump()
        => Assert.That(Eval("""
            {
                var x = 0;
                goto skip;
                x = 99;
                skip:
                return x;
            }
            """), Is.EqualTo(0));

    [Test]
    public void Goto_BackwardJump()
        => Assert.That(Eval("""
            {
                var count = 0;
                start:
                count++;
                if (count < 3) goto start;
                return count;
            }
            """), Is.EqualTo(3));

    [Test]
    public void Goto_Label_SkipsAssignment()
        => Assert.That(Eval("""
            {
                var result = 1;
                goto done;
                result = result * 100;
                done:
                return result;
            }
            """), Is.EqualTo(1));

    // ═══════════════════════════════════════════════════════════════
    // Jump Statements — goto case / goto default
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void GotoCase_InSwitch()
        => Assert.That(Eval("""
            {
                var result = "";
                switch (1)
                {
                    case 1: result = "one"; goto case 2;
                    case 2: result = result + "+two"; break;
                }
                return result;
            }
            """), Is.EqualTo("one+two"));

    [Test]
    public void GotoDefault_InSwitch()
        => Assert.That(Eval("""
            {
                var result = "";
                switch (1)
                {
                    case 1: result = "one"; goto default;
                    default: result = result + "+def"; break;
                }
                return result;
            }
            """), Is.EqualTo("one+def"));

    [Test]
    public void EmptyCase_FallThrough()
        => Assert.That(Eval("""
            {
                var result = "";
                switch (1)
                {
                    case 1:
                    case 2: result = "one or two"; break;
                    default: result = "other"; break;
                }
                return result;
            }
            """), Is.EqualTo("one or two"));

    // ═══════════════════════════════════════════════════════════════
    // Jump Statements — ControlFlowSignal in try
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Break_InsideTry_NotCaught()
        => Assert.That(Eval("""
            {
                var result = 0;
                for (var i = 0; i < 10; i++)
                {
                    try { if (i == 3) break; }
                    catch { result = -1; }
                    result = i;
                }
                return result;
            }
            """), Is.EqualTo(2));

    // ═══════════════════════════════════════════════════════════════
    // Exception Handling — try/catch
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void TryCatch_Basic()
        => Assert.That(Eval("""
            {
                var result = "";
                try { throw new System.Exception("test"); }
                catch (System.Exception ex) { result = ex.Message; }
                return result;
            }
            """), Is.EqualTo("test"));

    [Test]
    public void TypedCatch_FQN()
        => Assert.That(Eval("""
            {
                var r = "";
                try { throw new System.ArgumentException("bad"); }
                catch (System.ArgumentException ex) { r = ex.Message; }
                return r;
            }
            """), Is.EqualTo("bad"));

    [Test]
    public void Catch_Ordering()
        => Assert.That(Eval("""
            {
                var r = "";
                try { throw new System.ArgumentException("bad"); }
                catch (System.InvalidOperationException) { r = "invalid-op"; }
                catch (System.ArgumentException) { r = "arg"; }
                catch (System.Exception) { r = "general"; }
                return r;
            }
            """), Is.EqualTo("arg"));

    // ═══════════════════════════════════════════════════════════════
    // Exception Handling — bare catch
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void BareCatch_CatchesAll()
        => Assert.That(Eval("""
            {
                var r = "";
                try { throw new System.InvalidOperationException(); }
                catch (System.ArgumentException) { r = "arg"; }
                catch { r = "bare"; }
                return r;
            }
            """), Is.EqualTo("bare"));

    // ═══════════════════════════════════════════════════════════════
    // Exception Handling — when guards
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void WhenGuard_FallThrough()
        => Assert.That(Eval("""
            {
                var r = 0;
                try { throw new System.Exception("other"); }
                catch (System.Exception ex) when (ex.Message == "match") { r = 1; }
                catch (System.Exception) { r = 2; }
                return r;
            }
            """), Is.EqualTo(2));

    [Test]
    public void WhenGuard_Matches()
        => Assert.That(Eval("""
            {
                var r = 0;
                try { throw new System.Exception("test"); }
                catch (System.Exception ex) when (ex.Message == "test") { r = 1; }
                catch (System.Exception) { r = 2; }
                return r;
            }
            """), Is.EqualTo(1));

    // ═══════════════════════════════════════════════════════════════
    // Exception Handling — finally
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Finally_AfterNormalCompletion()
        => Assert.That(Eval("""
            {
                var x = 1;
                try { x = 2; }
                finally { x = x * 3; }
                return x;
            }
            """), Is.EqualTo(6));

    [Test]
    public void Finally_AfterCatch()
        => Assert.That(Eval("""
            {
                var x = 0;
                try { throw new System.Exception("e"); }
                catch { x = 1; }
                finally { x = x + 10; }
                return x;
            }
            """), Is.EqualTo(11));

    [Test]
    public void Finally_AfterPropagation()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Eval("""
            {
                try { throw new System.InvalidOperationException("oops"); }
                finally { var dummy = 1; }
            }
            """));
        Assert.That(ex!.Message, Is.EqualTo("oops"));
    }

    // ═══════════════════════════════════════════════════════════════
    // Exception Handling — try/finally (no catch)
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void TryFinally_NoCatch()
        => Assert.That(Eval("""
            {
                var x = 1;
                try { x = 2; }
                finally { x = x + 10; }
                return x;
            }
            """), Is.EqualTo(12));

    // ═══════════════════════════════════════════════════════════════
    // Exception Handling — throw and rethrow
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Throw_DerivedType()
        => Assert.That(Eval("""
            {
                var r = "";
                try { throw new System.InvalidOperationException("not allowed"); }
                catch (System.InvalidOperationException ex) { r = ex.Message; }
                return r;
            }
            """), Is.EqualTo("not allowed"));

    [Test]
    public void Rethrow_PreservesException()
        => Assert.That(Eval("""
            {
                try {
                    try { throw new System.Exception("inner"); }
                    catch { throw; }
                }
                catch (System.Exception ex) { return ex.Message; }
            }
            """), Is.EqualTo("inner"));

    // ═══════════════════════════════════════════════════════════════
    // Exception Handling — using statement
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Using_DisposesResource()
        => Assert.That(Eval("""
            {
                var result = "";
                using (var ms = new System.IO.MemoryStream())
                {
                    result = "len=" + ms.Length.ToString();
                }
                return result;
            }
            """), Is.EqualTo("len=0"));

    // ═══════════════════════════════════════════════════════════════
    // Checked/Unchecked — default wrapping
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Default_IntOverflow_Wraps()
        => Assert.That(Eval("int.MaxValue + 1"), Is.EqualTo(int.MinValue));

    // ═══════════════════════════════════════════════════════════════
    // Checked/Unchecked — checked expressions
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Checked_IntOverflow_Throws()
        => Assert.Throws<OverflowException>(() => Eval("checked(int.MaxValue + 1)"));

    [Test]
    public void Checked_LongOverflow_Throws()
        => Assert.Throws<OverflowException>(() => Eval("checked(long.MaxValue + 1L)"));

    [Test]
    public void Checked_CastOverflow_Throws()
        => Assert.Throws<OverflowException>(() => Eval("checked((byte)256)"));

    [Test]
    public void Checked_SafeArithmetic()
        => Assert.That(Eval("checked(100 + 200)"), Is.EqualTo(300));

    // ═══════════════════════════════════════════════════════════════
    // Checked/Unchecked — unchecked expressions
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Unchecked_IntOverflow_Wraps()
        => Assert.That(Eval("unchecked(int.MaxValue + 1)"), Is.EqualTo(int.MinValue));

    [Test]
    public void Unchecked_LongOverflow_Wraps()
        => Assert.That(Eval("unchecked(long.MaxValue + 1L)"), Is.EqualTo(long.MinValue));

    [Test]
    public void Unchecked_Cast_Truncates()
        => Assert.That(Eval("unchecked((byte)256)"), Is.EqualTo((byte)0));

    // ═══════════════════════════════════════════════════════════════
    // Checked/Unchecked — nesting
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Checked_Unchecked_Nested()
        => Assert.That(Eval("checked(unchecked(int.MaxValue + 1))"), Is.EqualTo(int.MinValue));

    [Test]
    public void Unchecked_Checked_Nested()
        => Assert.That(Eval("unchecked(checked(100 + 200))"), Is.EqualTo(300));
}
