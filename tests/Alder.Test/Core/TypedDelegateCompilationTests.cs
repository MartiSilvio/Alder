using Alder.Diagnostics;

namespace Alder.Test.Core;

[TestFixture]
public class TypedDelegateCompilationTests
{
    private delegate int IncrementDelegate(int value);

    private AlderEngine CreateEngine() => new(new AlderOptions().UseCompiler());

    #region Basic Func compilation

    [Test]
    public void Compile_FuncOneParam_ReturnsCorrectResult()
    {
        var engine = CreateEngine();
        var fn = engine.Compile<Func<int, int>>("x * 2", "x");

        Assert.That(fn(5), Is.EqualTo(10));
        Assert.That(fn(0), Is.EqualTo(0));
        Assert.That(fn(-3), Is.EqualTo(-6));
    }

    [Test]
    public void Compile_FuncTwoParams_ReturnsCorrectResult()
    {
        var engine = CreateEngine();
        var fn = engine.Compile<Func<int, int, int>>("a + b", "a", "b");

        Assert.That(fn(3, 4), Is.EqualTo(7));
        Assert.That(fn(100, -50), Is.EqualTo(50));
    }

    [Test]
    public void Compile_FuncThreeParams_ReturnsCorrectResult()
    {
        var engine = CreateEngine();
        var fn = engine.Compile<Func<int, int, int, int>>("x + y * z", "x", "y", "z");

        Assert.That(fn(1, 2, 3), Is.EqualTo(7));
    }

    [Test]
    public void Compile_FuncStringReturn_ReturnsCorrectResult()
    {
        var engine = CreateEngine();
        var fn = engine.Compile<Func<string, int, string>>(
            """$"Hello {name}, you are {age} years old" """, "name", "age");

        Assert.That(fn("Alice", 30), Is.EqualTo("Hello Alice, you are 30 years old"));
    }

    [Test]
    public void Compile_FuncBoolReturn_ReturnsCorrectResult()
    {
        var engine = CreateEngine();
        var fn = engine.Compile<Func<int, int, bool>>("a > b", "a", "b");

        Assert.That(fn(5, 3), Is.True);
        Assert.That(fn(3, 5), Is.False);
        Assert.That(fn(5, 5), Is.False);
    }

    [Test]
    public void Compile_FuncDoubleReturn_ReturnsCorrectResult()
    {
        var engine = CreateEngine();
        var fn = engine.Compile<Func<double, double, double>>("Math.Sqrt(x * x + y * y)", "x", "y");

        Assert.That(fn(3.0, 4.0), Is.EqualTo(5.0).Within(0.0001));
    }

    [Test]
    public void Compile_FuncDecimalReturn_ReturnsCorrectResult()
    {
        var engine = CreateEngine();
        var fn = engine.Compile<Func<decimal, decimal, decimal>>("price * quantity", "price", "quantity");

        Assert.That(fn(19.99m, 3m), Is.EqualTo(59.97m));
    }

    [Test]
    public void Compile_FuncMixedParamTypes_ReturnsCorrectResult()
    {
        var engine = CreateEngine();
        var fn = engine.Compile<Func<string, int, bool>>(
            "name.Length > minLength", "name", "minLength");

        Assert.That(fn("hello", 3), Is.True);
        Assert.That(fn("hi", 3), Is.False);
    }

    #endregion

    #region Multi-statement bodies

    [Test]
    public void Compile_MultiStatement_IfElse()
    {
        var engine = CreateEngine();
        var fn = engine.Compile<Func<int, string>>("""
            if (n % 15 == 0) return "FizzBuzz";
            else if (n % 3 == 0) return "Fizz";
            else if (n % 5 == 0) return "Buzz";
            else return n.ToString();
            """, "n");

        Assert.That(fn(15), Is.EqualTo("FizzBuzz"));
        Assert.That(fn(9), Is.EqualTo("Fizz"));
        Assert.That(fn(10), Is.EqualTo("Buzz"));
        Assert.That(fn(7), Is.EqualTo("7"));
    }

    [Test]
    public void Compile_MultiStatement_LocalVariables()
    {
        var engine = CreateEngine();
        var fn = engine.Compile<Func<decimal, decimal, decimal>>("""
            var subtotal = price * qty;
            var tax = subtotal * 0.08m;
            return subtotal + tax;
            """, "price", "qty");

        Assert.That(fn(100m, 2m), Is.EqualTo(216.00m));
    }

    [Test]
    public void Compile_MultiStatement_ForLoop()
    {
        var engine = CreateEngine();
        var fn = engine.Compile<Func<int, int>>("""
            var sum = 0;
            for (var i = 1; i <= n; i++)
                sum += i;
            return sum;
            """, "n");

        Assert.That(fn(10), Is.EqualTo(55));
        Assert.That(fn(100), Is.EqualTo(5050));
    }

    [Test]
    public void Compile_MultiStatement_WhileLoop()
    {
        var engine = CreateEngine();
        var fn = engine.Compile<Func<int, int>>("""
            var result = n;
            while (result > 9)
            {
                var sum = 0;
                var temp = result;
                while (temp > 0)
                {
                    sum += temp % 10;
                    temp /= 10;
                }
                result = sum;
            }
            return result;
            """, "n");

        Assert.That(fn(942), Is.EqualTo(6)); // 9+4+2=15, 1+5=6
    }

    [Test]
    public void Compile_MultiStatement_TryCatch()
    {
        var engine = CreateEngine();
        var fn = engine.Compile<Func<int, int, string>>("""
            try
            {
                return (a / b).ToString();
            }
            catch (DivideByZeroException)
            {
                return "undefined";
            }
            """, "a", "b");

        Assert.That(fn(10, 2), Is.EqualTo("5"));
        Assert.That(fn(10, 0), Is.EqualTo("undefined"));
    }

    #endregion

    #region Action delegates

    [Test]
    public void Compile_ActionOneParam_Executes()
    {
        var engine = CreateEngine();
        engine.SetVariable<int>("accumulator", 0);
        var action = engine.Compile<Action<int>>("accumulator = accumulator + value", "value");

        action(10);
        action(20);
        action(30);

        Assert.That(engine.Evaluate<int>("accumulator"), Is.EqualTo(60));
    }

    [Test]
    public void Compile_ActionTwoParams_Executes()
    {
        var engine = CreateEngine();
        engine.SetVariable<int>("result", 0);
        var action = engine.Compile<Action<int, int>>("result = a + b", "a", "b");

        action(3, 4);
        Assert.That(engine.Evaluate<int>("result"), Is.EqualTo(7));

        action(10, 20);
        Assert.That(engine.Evaluate<int>("result"), Is.EqualTo(30));
    }

    #endregion

    #region Repeated invocation (the whole point)

    [Test]
    public void Compile_RepeatedInvocation_DifferentValues()
    {
        var engine = CreateEngine();
        var fn = engine.Compile<Func<int, int>>("x * x", "x");

        var results = new List<int>();
        for (int i = 0; i < 100; i++)
            results.Add(fn(i));

        Assert.That(results[0], Is.EqualTo(0));
        Assert.That(results[10], Is.EqualTo(100));
        Assert.That(results[99], Is.EqualTo(9801));
    }

    [Test]
    public void Compile_FizzBuzz_FullRange()
    {
        var engine = CreateEngine();
        var fn = engine.Compile<Func<int, string>>("""
            if (i % 15 == 0) return $"{i} => FizzBuzz";
            else if (i % 3 == 0) return $"{i} => Fizz";
            else if (i % 5 == 0) return $"{i} => Buzz";
            else return $"{i} => {i}";
            """, "i");

        var answers = new List<string>();
        for (int i = 1; i <= 100; i++)
            answers.Add(fn(i));

        Assert.That(answers[0], Is.EqualTo("1 => 1"));
        Assert.That(answers[2], Is.EqualTo("3 => Fizz"));
        Assert.That(answers[4], Is.EqualTo("5 => Buzz"));
        Assert.That(answers[14], Is.EqualTo("15 => FizzBuzz"));
        Assert.That(answers.Count, Is.EqualTo(100));
    }

    #endregion

    #region Engine variable interaction

    [Test]
    public void Compile_DelegateCanAccessEngineVariables()
    {
        var engine = CreateEngine();
        engine.SetVariable<int>("baseRate", 100);
        var fn = engine.Compile<Func<double, double>>("baseRate * multiplier", "multiplier");

        Assert.That(fn(1.5), Is.EqualTo(150.0).Within(0.001));
    }

    [Test]
    public void Compile_EngineVariableChangesVisibleThroughDelegate()
    {
        var engine = CreateEngine();
        engine.SetVariable<int>("offset", 10);
        var fn = engine.Compile<Func<int, int>>("x + offset", "x");

        Assert.That(fn(5), Is.EqualTo(15));

        engine.SetVariable<int>("offset", 100);
        Assert.That(fn(5), Is.EqualTo(105));
    }

    [Test]
    public void Compile_DoesNotOverwriteExistingEngineVariableWithParameter()
    {
        var engine = CreateEngine();
        engine.SetVariable<int>("x", 99);

        var fn = engine.Compile<Func<int, int>>("x + 1", "x");

        Assert.That(engine.Evaluate<int>("x"), Is.EqualTo(99));
        Assert.That(fn(5), Is.EqualTo(6));
        Assert.That(engine.Evaluate<int>("x"), Is.EqualTo(99));
    }

    #endregion

    #region Object and collection parameters

    [Test]
    public void Compile_StringParam_MethodCalls()
    {
        var engine = CreateEngine();
        var fn = engine.Compile<Func<string, string>>("text.Trim().ToUpper()", "text");

        Assert.That(fn("  hello  "), Is.EqualTo("HELLO"));
    }

    [Test]
    public void Compile_ListParam_LinqOperations()
    {
        var engine = CreateEngine();
        var fn = engine.Compile<Func<List<int>, int>>("items.Where(x => x > 3).Count()", "items");

        Assert.That(fn([1, 2, 3, 4, 5]), Is.EqualTo(2));
        Assert.That(fn([10, 20, 30]), Is.EqualTo(3));
        Assert.That(fn([1, 2, 3]), Is.EqualTo(0));
    }

    #endregion

    #region Error handling

    [Test]
    public void Compile_WrongParamCount_Throws()
    {
        var engine = CreateEngine();

        var ex = Assert.Throws<AlderException>(() =>
            engine.Compile<Func<int, int>>("x * 2", "x", "y"));

        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0012));
    }

    [Test]
    public void Compile_TooFewParamNames_Throws()
    {
        var engine = CreateEngine();

        var ex = Assert.Throws<AlderException>(() =>
            engine.Compile<Func<int, int, int>>("a + b", "a"));

        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0012));
    }

    [Test]
    public void Compile_NoCompilerConfigured_Throws()
    {
        var engine = new AlderEngine(); // no compiler

        Assert.Throws<AlderException>(() =>
            engine.Compile<Func<int, int>>("x * 2", "x"));
    }

    [Test]
    public void Compile_InvalidExpression_Throws()
    {
        var engine = CreateEngine();

        Assert.Throws<AlderException>(() =>
            engine.Compile<Func<int, int>>("x +* 2", "x"));
    }

    [Test]
    public void Compile_FailureDoesNotLeakParameterVariable()
    {
        var engine = CreateEngine();

        Assert.Throws<AlderException>(() =>
            engine.Compile<Func<int, int>>("x +* 2", "x"));

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("x"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
    }

    [Test]
    public void Compile_CustomDelegateType_Works()
    {
        var engine = CreateEngine();

        var fn = engine.Compile<IncrementDelegate>("value + 1", "value");

        Assert.That(fn(41), Is.EqualTo(42));
    }

    #endregion

    #region IEnumerable parameter names overload

    [Test]
    public void Compile_IEnumerableParamNames_Works()
    {
        var engine = CreateEngine();
        var paramNames = new List<string> { "a", "b" };
        var fn = engine.Compile<Func<int, int, int>>("a + b", paramNames);

        Assert.That(fn(3, 7), Is.EqualTo(10));
    }

    #endregion

    #region Static AlderEval path

    [Test]
    public void AlderEvalCompile_ReturnsWorkingDelegate()
    {
        AlderEval.Reset();
        AlderEval.Configure(o => o.UseCompiler());

        try
        {
            var fn = AlderEvalCompileExtensions.Compile<Func<int, int>>("x * 3", "x");
            Assert.That(fn(7), Is.EqualTo(21));
        }
        finally
        {
            AlderEval.Reset();
        }
    }

    [Test]
    public void AlderEvalCompile_MultiStatement_Works()
    {
        AlderEval.Reset();
        AlderEval.Configure(o => o.UseCompiler());

        try
        {
            var fn = AlderEvalCompileExtensions.Compile<Func<int, string>>("""
                if (n > 0) return "positive";
                else if (n < 0) return "negative";
                else return "zero";
                """, "n");

            Assert.That(fn(5), Is.EqualTo("positive"));
            Assert.That(fn(-3), Is.EqualTo("negative"));
            Assert.That(fn(0), Is.EqualTo("zero"));
        }
        finally
        {
            AlderEval.Reset();
        }
    }

    #endregion

    #region String extension path

    [Test]
    public void StringCompile_ReturnsWorkingDelegate()
    {
        AlderEval.Reset();
        AlderEval.Configure(o => o.UseCompiler());

        try
        {
            var fn = "x + 1".Compile<Func<int, int>>("x");
            Assert.That(fn(99), Is.EqualTo(100));
        }
        finally
        {
            AlderEval.Reset();
        }
    }

    #endregion

    #region Real-world patterns

    [Test]
    public void Compile_PricingRule_RealisticBusinessLogic()
    {
        var engine = CreateEngine();
        var calculateDiscount = engine.Compile<Func<decimal, int, decimal>>("""
            if (quantity >= 100) return price * 0.80m;
            else if (quantity >= 50) return price * 0.90m;
            else if (quantity >= 10) return price * 0.95m;
            else return price;
            """, "price", "quantity");

        Assert.That(calculateDiscount(100m, 1), Is.EqualTo(100m));
        Assert.That(calculateDiscount(100m, 10), Is.EqualTo(95m));
        Assert.That(calculateDiscount(100m, 50), Is.EqualTo(90m));
        Assert.That(calculateDiscount(100m, 100), Is.EqualTo(80m));
    }

    [Test]
    public void Compile_ValidationRule_StringProcessing()
    {
        var engine = CreateEngine();
        var validate = engine.Compile<Func<string, bool>>("""
            return input != null
                && input.Length >= 3
                && input.Length <= 50
                && !input.StartsWith(" ")
                && !input.EndsWith(" ");
            """, "input");

        Assert.That(validate("hello"), Is.True);
        Assert.That(validate("ab"), Is.False);
        Assert.That(validate(" hello"), Is.False);
        Assert.That(validate("hello "), Is.False);
    }

    [Test]
    public void Compile_GradeCalculator_SwitchExpression()
    {
        var engine = CreateEngine();
        var getGrade = engine.Compile<Func<int, string>>("""
            return score switch
            {
                >= 90 => "A",
                >= 80 => "B",
                >= 70 => "C",
                >= 60 => "D",
                _ => "F"
            };
            """, "score");

        Assert.That(getGrade(95), Is.EqualTo("A"));
        Assert.That(getGrade(85), Is.EqualTo("B"));
        Assert.That(getGrade(75), Is.EqualTo("C"));
        Assert.That(getGrade(65), Is.EqualTo("D"));
        Assert.That(getGrade(55), Is.EqualTo("F"));
    }

    #endregion
}
