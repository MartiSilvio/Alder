using System.Linq.Expressions;
using Alder.Diagnostics;

namespace Alder.Test.Compilation;

[TestFixture]
public class ExpressionTreeTests
{
    private AlderEngine _engine = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new AlderEngine();
    }


    [Test]
    public void Arithmetic_Addition()
    {
        var expr = _engine.ParseAsExpression<Func<int, int>>("x => x + 1");
        var compiled = expr.Compile();

        Assert.That(compiled(5), Is.EqualTo(6));
        Assert.That(compiled(0), Is.EqualTo(1));
        Assert.That(compiled(-1), Is.EqualTo(0));
    }

    [Test]
    public void Arithmetic_CombinedMultiplyAndAdd()
    {
        var expr = _engine.ParseAsExpression<Func<int, int>>("x => x * 2 + 1");
        var compiled = expr.Compile();

        Assert.That(compiled(3), Is.EqualTo(7));
        Assert.That(compiled(0), Is.EqualTo(1));
    }

    [Test]
    public void Arithmetic_DivisionWithPromotion()
    {
        var expr = _engine.ParseAsExpression<Func<int, double>>("x => x / 2.0");
        var compiled = expr.Compile();

        Assert.That(compiled(5), Is.EqualTo(2.5));
        Assert.That(compiled(10), Is.EqualTo(5.0));
    }

    [Test]
    public void Arithmetic_Modulo()
    {
        var expr = _engine.ParseAsExpression<Func<int, int>>("x => x % 3");
        var compiled = expr.Compile();

        Assert.That(compiled(10), Is.EqualTo(1));
        Assert.That(compiled(9), Is.EqualTo(0));
    }

    [Test]
    public void Arithmetic_MultiParameterAddition()
    {
        var expr = _engine.ParseAsExpression<Func<int, int, int>>("(x, y) => x + y");
        var compiled = expr.Compile();

        Assert.That(compiled(3, 4), Is.EqualTo(7));
        Assert.That(compiled(-1, 1), Is.EqualTo(0));
    }

    [Test]
    public void Arithmetic_DoubleSubtraction()
    {
        var expr = _engine.ParseAsExpression<Func<double, double, double>>("(x, y) => x - y");
        var compiled = expr.Compile();

        Assert.That(compiled(5.5, 2.5), Is.EqualTo(3.0));
        Assert.That(compiled(0.0, 1.0), Is.EqualTo(-1.0));
    }



    [Test]
    public void Comparison_GreaterThan()
    {
        var expr = _engine.ParseAsExpression<Func<int, bool>>("x => x > 5");
        var compiled = expr.Compile();

        Assert.That(compiled(10), Is.True);
        Assert.That(compiled(5), Is.False);
        Assert.That(compiled(3), Is.False);
    }

    [Test]
    public void Comparison_Equality()
    {
        var expr = _engine.ParseAsExpression<Func<int, bool>>("x => x == 0");
        var compiled = expr.Compile();

        Assert.That(compiled(0), Is.True);
        Assert.That(compiled(1), Is.False);
    }

    [Test]
    public void Comparison_NotEqualNull()
    {
        var expr = _engine.ParseAsExpression<Func<string, bool>>("x => x != null");
        var compiled = expr.Compile();

        Assert.That(compiled("hello"), Is.True);
        Assert.That(compiled(null!), Is.False);
    }

    [Test]
    public void Comparison_LessThanOrEqual()
    {
        var expr = _engine.ParseAsExpression<Func<int, bool>>("x => x <= 100");
        var compiled = expr.Compile();

        Assert.That(compiled(100), Is.True);
        Assert.That(compiled(99), Is.True);
        Assert.That(compiled(101), Is.False);
    }

    [Test]
    public void Comparison_CombinedWithLogical()
    {
        var expr = _engine.ParseAsExpression<Func<int, bool>>("x => x >= 0 && x <= 100");
        var compiled = expr.Compile();

        Assert.That(compiled(50), Is.True);
        Assert.That(compiled(0), Is.True);
        Assert.That(compiled(100), Is.True);
        Assert.That(compiled(-1), Is.False);
        Assert.That(compiled(101), Is.False);
    }



    [Test]
    public void Logical_And()
    {
        var expr = _engine.ParseAsExpression<Func<int, bool>>("x => x > 0 && x < 10");
        var compiled = expr.Compile();

        Assert.That(compiled(5), Is.True);
        Assert.That(compiled(0), Is.False);
        Assert.That(compiled(10), Is.False);
    }

    [Test]
    public void Logical_Or()
    {
        var expr = _engine.ParseAsExpression<Func<int, bool>>("x => x < 0 || x > 100");
        var compiled = expr.Compile();

        Assert.That(compiled(-1), Is.True);
        Assert.That(compiled(101), Is.True);
        Assert.That(compiled(50), Is.False);
    }

    [Test]
    public void Logical_Not()
    {
        var expr = _engine.ParseAsExpression<Func<bool, bool>>("x => !x");
        var compiled = expr.Compile();

        Assert.That(compiled(true), Is.False);
        Assert.That(compiled(false), Is.True);
    }



    [Test]
    public void Unary_Negate()
    {
        var expr = _engine.ParseAsExpression<Func<int, int>>("x => -x");
        var compiled = expr.Compile();

        Assert.That(compiled(5), Is.EqualTo(-5));
        Assert.That(compiled(-3), Is.EqualTo(3));
        Assert.That(compiled(0), Is.EqualTo(0));
    }

    [Test]
    public void Unary_Identity()
    {
        var expr = _engine.ParseAsExpression<Func<int, int>>("x => +x");
        var compiled = expr.Compile();

        Assert.That(compiled(5), Is.EqualTo(5));
        Assert.That(compiled(-3), Is.EqualTo(-3));
    }



    [Test]
    public void MemberAccess_StringLength()
    {
        var expr = _engine.ParseAsExpression<Func<string, int>>("x => x.Length");
        var compiled = expr.Compile();

        Assert.That(compiled("hello"), Is.EqualTo(5));
        Assert.That(compiled(string.Empty), Is.EqualTo(0));
    }

    [Test]
    public void MemberAccess_PropertyWithComparison()
    {
        var expr = _engine.ParseAsExpression<Func<string, int, bool>>("(x, y) => x.Length > y");
        var compiled = expr.Compile();

        Assert.That(compiled("hello", 3), Is.True);
        Assert.That(compiled("hi", 5), Is.False);
    }

    [Test]
    public void MemberAccess_TypedObjectProperty()
    {
        var expr = _engine.ParseAsExpression<Func<TestPerson, string>>("x => x.Name");
        var compiled = expr.Compile();

        Assert.That(compiled(new TestPerson { Name = "Alice" }), Is.EqualTo("Alice"));
    }

    [Test]
    public void MemberAccess_TypedObjectIntProperty()
    {
        var expr = _engine.ParseAsExpression<Func<TestPerson, int>>("x => x.Age");
        var compiled = expr.Compile();

        Assert.That(compiled(new TestPerson { Age = 25 }), Is.EqualTo(25));
    }



    [Test]
    public void MethodCall_IntToString()
    {
        var expr = _engine.ParseAsExpression<Func<int, string>>("x => x.ToString()");
        var compiled = expr.Compile();

        Assert.That(compiled(42), Is.EqualTo("42"));
    }

    [Test]
    public void MethodCall_StringToUpper()
    {
        var expr = _engine.ParseAsExpression<Func<string, string>>("x => x.ToUpper()");
        var compiled = expr.Compile();

        Assert.That(compiled("hello"), Is.EqualTo("HELLO"));
    }

    [Test]
    public void MethodCall_ChainedMemberAndMethod()
    {
        var expr = _engine.ParseAsExpression<Func<TestPerson, bool>>(
            "x => x.Name.StartsWith(\"A\")");
        var compiled = expr.Compile();

        Assert.That(compiled(new TestPerson { Name = "Alice" }), Is.True);
        Assert.That(compiled(new TestPerson { Name = "Bob" }), Is.False);
    }

    [Test]
    public void MethodCall_StringContains()
    {
        var expr = _engine.ParseAsExpression<Func<TestPerson, bool>>(
            "x => x.Name.Contains(\"test\")");
        var compiled = expr.Compile();

        Assert.That(compiled(new TestPerson { Name = "testing" }), Is.True);
        Assert.That(compiled(new TestPerson { Name = "Alice" }), Is.False);
    }



    [Test]
    public void Literal_IntegerConstant()
    {
        var expr = _engine.ParseAsExpression<Func<int, int>>("x => 42");
        var compiled = expr.Compile();

        Assert.That(compiled(0), Is.EqualTo(42));
        Assert.That(compiled(999), Is.EqualTo(42));
    }

    [Test]
    public void Literal_BooleanConstant()
    {
        var expr = _engine.ParseAsExpression<Func<int, bool>>("x => true");
        var compiled = expr.Compile();

        Assert.That(compiled(0), Is.True);
    }

    [Test]
    public void Literal_StringConstant()
    {
        var expr = _engine.ParseAsExpression<Func<int, string>>("x => \"hello\"");
        var compiled = expr.Compile();

        Assert.That(compiled(0), Is.EqualTo("hello"));
    }



    [Test]
    public void Conditional_AbsoluteValue()
    {
        var expr = _engine.ParseAsExpression<Func<int, int>>("x => x > 0 ? x : -x");
        var compiled = expr.Compile();

        Assert.That(compiled(5), Is.EqualTo(5));
        Assert.That(compiled(-3), Is.EqualTo(3));
        Assert.That(compiled(0), Is.EqualTo(0));
    }

    [Test]
    public void Conditional_NullCheck()
    {
        var expr = _engine.ParseAsExpression<Func<string, string>>(
            "x => x != null ? x : \"default\"");
        var compiled = expr.Compile();

        Assert.That(compiled("hello"), Is.EqualTo("hello"));
        Assert.That(compiled(null!), Is.EqualTo("default"));
    }



    [Test]
    public void NullCoalesce_StringFallback()
    {
        var expr = _engine.ParseAsExpression<Func<string, string>>("x => x ?? \"default\"");
        var compiled = expr.Compile();

        Assert.That(compiled("hello"), Is.EqualTo("hello"));
        Assert.That(compiled(null!), Is.EqualTo("default"));
    }

    [Test]
    public void NullCoalesce_NullableValueType()
    {
        var expr = _engine.ParseAsExpression<Func<int?, int>>("x => x ?? 0");
        var compiled = expr.Compile();

        Assert.That(compiled(42), Is.EqualTo(42));
        Assert.That(compiled(null), Is.EqualTo(0));
    }



    [Test]
    public void Cast_IntToDouble()
    {
        var expr = _engine.ParseAsExpression<Func<int, double>>("x => (double)x");
        var compiled = expr.Compile();

        Assert.That(compiled(5), Is.EqualTo(5.0));
    }

    [Test]
    public void Cast_DoubleToInt()
    {
        var expr = _engine.ParseAsExpression<Func<double, int>>("x => (int)x");
        var compiled = expr.Compile();

        Assert.That(compiled(5.9), Is.EqualTo(5));
        Assert.That(compiled(3.1), Is.EqualTo(3));
    }

    [Test]
    public void Cast_IntToLong()
    {
        var expr = _engine.ParseAsExpression<Func<int, long>>("x => (long)x");
        var compiled = expr.Compile();

        Assert.That(compiled(42), Is.EqualTo(42L));
    }



    [Test]
    public void TypeCheck_IsString_True()
    {
        var expr = _engine.ParseAsExpression<Func<object, bool>>("x => x is string");
        var compiled = expr.Compile();

        Assert.That(compiled("hello"), Is.True);
        Assert.That(compiled(42), Is.False);
    }

    [Test]
    public void TypeCheck_IsString_WithDifferentTypes()
    {
        var expr = _engine.ParseAsExpression<Func<object, bool>>("x => x is int");
        var compiled = expr.Compile();

        Assert.That(compiled(42), Is.True);
        Assert.That(compiled("hello"), Is.False);
        Assert.That(compiled(3.14), Is.False);
    }



    [Test]
    public void IndexAccess_Array()
    {
        var expr = _engine.ParseAsExpression<Func<int[], int>>("x => x[0]");
        var compiled = expr.Compile();

        Assert.That(compiled([10, 20, 30]), Is.EqualTo(10));
    }

    [Test]
    public void IndexAccess_ArraySecondElement()
    {
        var expr = _engine.ParseAsExpression<Func<int[], int>>("x => x[1]");
        var compiled = expr.Compile();

        Assert.That(compiled([10, 20, 30]), Is.EqualTo(20));
    }

    [Test]
    public void IndexAccess_List()
    {
        var expr = _engine.ParseAsExpression<Func<List<string>, string>>("x => x[0]");
        var compiled = expr.Compile();

        Assert.That(compiled(["first", "second"]), Is.EqualTo("first"));
    }



    [Test]
    public void ObjectCreation_DateTime()
    {
        var expr = _engine.ParseAsExpression<Func<int, DateTime>>(
            "x => new DateTime(x, 1, 1)");
        var compiled = expr.Compile();

        Assert.That(compiled(2024), Is.EqualTo(new DateTime(2024, 1, 1)));
        Assert.That(compiled(2000), Is.EqualTo(new DateTime(2000, 1, 1)));
    }



    [Test]
    public void EngineVariable_IntegerCapturedAsConstant()
    {
        _engine.SetVariable("minAge", 18);

        var expr = _engine.ParseAsExpression<Func<int, bool>>("x => x > minAge");
        var compiled = expr.Compile();

        Assert.That(compiled(20), Is.True);
        Assert.That(compiled(15), Is.False);
        Assert.That(compiled(18), Is.False);
    }

    [Test]
    public void EngineVariable_StringCapturedAsConstant()
    {
        _engine.SetVariable("prefix", "Mr. ");

        var expr = _engine.ParseAsExpression<Func<string, string>>(
            "x => prefix + x");
        var compiled = expr.Compile();

        Assert.That(compiled("Smith"), Is.EqualTo("Mr. Smith"));
    }

    [Test]
    public void EngineVariable_MultipleVariables()
    {
        _engine.SetVariable("min", 10);
        _engine.SetVariable("max", 100);

        var expr = _engine.ParseAsExpression<Func<int, bool>>(
            "x => x >= min && x <= max");
        var compiled = expr.Compile();

        Assert.That(compiled(50), Is.True);
        Assert.That(compiled(5), Is.False);
        Assert.That(compiled(101), Is.False);
    }



    [Test]
    public void NumericPromotion_IntPlusDouble()
    {
        var expr = _engine.ParseAsExpression<Func<int, double>>("x => x + 1.0");
        var compiled = expr.Compile();

        Assert.That(compiled(5), Is.EqualTo(6.0));
    }

    [Test]
    public void NumericPromotion_MixedTypeParameters()
    {
        var expr = _engine.ParseAsExpression<Func<int, double, double>>(
            "(x, y) => x + y");
        var compiled = expr.Compile();

        Assert.That(compiled(3, 2.5), Is.EqualTo(5.5));
    }

    [Test]
    public void NumericPromotion_IntMultipliedByDouble()
    {
        var expr = _engine.ParseAsExpression<Func<int, double>>("x => x * 0.5");
        var compiled = expr.Compile();

        Assert.That(compiled(10), Is.EqualTo(5.0));
    }



    [Test]
    public void StringConcat_TwoStrings()
    {
        var expr = _engine.ParseAsExpression<Func<string, string, string>>(
            "(a, b) => a + b");
        var compiled = expr.Compile();

        Assert.That(compiled("hello", " world"), Is.EqualTo("hello world"));
    }

    [Test]
    public void StringConcat_WithLiteral()
    {
        var expr = _engine.ParseAsExpression<Func<string, string, string>>(
            "(a, b) => a + \" \" + b");
        var compiled = expr.Compile();

        Assert.That(compiled("hello", "world"), Is.EqualTo("hello world"));
    }



    [Test]
    public void MultiParam_ThreeIntegers()
    {
        var expr = _engine.ParseAsExpression<Func<int, int, int, int>>(
            "(a, b, c) => a + b + c");
        var compiled = expr.Compile();

        Assert.That(compiled(1, 2, 3), Is.EqualTo(6));
    }

    [Test]
    public void MultiParam_MixedTypes()
    {
        var expr = _engine.ParseAsExpression<Func<int, string, bool>>(
            "(x, name) => x > 5 && name.Length > 0");
        var compiled = expr.Compile();

        Assert.That(compiled(10, "Alice"), Is.True);
        Assert.That(compiled(3, "Alice"), Is.False);
        Assert.That(compiled(10, ""), Is.False);
    }



    [Test]
    public void StandardModeEnforcement_ExtendedEngineStillUsesStandard()
    {
        var extendedEngine = new AlderEngine(new AlderOptions
        {
            LanguageMode = LanguageMode.Extended
        });

        // ParseAsExpression always uses Standard mode -- extended features
        // like ** (exponentiation) should not be available
        var expr = extendedEngine.ParseAsExpression<Func<int, bool>>("x => x > 5");
        var compiled = expr.Compile();

        Assert.That(compiled(10), Is.True);
        Assert.That(compiled(3), Is.False);
    }

    [Test]
    public void StandardModeEnforcement_ExtendedOperatorRejected()
    {
        var extendedEngine = new AlderEngine(new AlderOptions
        {
            LanguageMode = LanguageMode.Extended
        });

        // ** is an Extended operator -- should not be available in expression trees
        Assert.That(
            () => extendedEngine.ParseAsExpression<Func<int, int>>("x => x ** 2"),
            Throws.Exception);
    }



    [Test]
    public void TryParse_UnsupportedBlock_ReturnsFalseWithDiagnostic()
    {
        var success = _engine.TryParseAsExpression<Func<int, int>>(
            "x => { return x + 1; }", out var result, out var diagnostics);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
        Assert.That(diagnostics, Has.Count.GreaterThan(0));
        Assert.That(diagnostics[0].Message, Does.Contain("block"));
    }

    [Test]
    public void ParseAsExpression_UnsupportedBlock_ThrowsAlderException()
    {
        var ex = Assert.Throws<AlderException>(
            () => _engine.ParseAsExpression<Func<int, int>>("x => { return x + 1; }"));

        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS7053));
        Assert.That(ex.Message, Does.Contain("block"));
    }

    [Test]
    public void TryParse_UnsupportedAssignment_ReturnsFalseWithDiagnostic()
    {
        var success = _engine.TryParseAsExpression<Func<int, int>>(
            "x => x = 5", out var result, out var diagnostics);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
        Assert.That(diagnostics, Has.Count.GreaterThan(0));
        Assert.That(diagnostics[0].Message, Does.Contain("assignment"));
    }

    [Test]
    public void TryParse_UnsupportedNestedLambda_ReturnsFalseWithDiagnostic()
    {
        // Nested lambda not supported in v1
        var success = _engine.TryParseAsExpression<Func<int, Func<int, int>>>(
            "x => y => x + y", out var result, out var diagnostics);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
        Assert.That(diagnostics, Has.Count.GreaterThan(0));
        Assert.That(diagnostics[0].Message, Does.Contain("nested lambda").IgnoreCase);
    }

    [Test]
    public void TryParse_UnsupportedInterpolatedString_ReturnsFalseWithDiagnostic()
    {
        var success = _engine.TryParseAsExpression<Func<int, string>>(
            "x => $\"value is {x}\"", out var result, out var diagnostics);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
        Assert.That(diagnostics, Has.Count.GreaterThan(0));
        Assert.That(diagnostics[0].Message, Does.Contain("interpolated string"));
    }

    [Test]
    public void TryParse_UnsupportedNamedArgument_ReturnsSpecificDiagnosticWithCode()
    {
        var success = _engine.TryParseAsExpression<Func<int, int>>(
            "x => Math.Max(val1: x, val2: 2)", out var result, out var diagnostics);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
        Assert.That(diagnostics, Has.Count.GreaterThan(0));
        Assert.That(diagnostics[0].Code, Is.EqualTo(DiagnosticCode.CS7053));
        Assert.That(diagnostics[0].Message, Does.Contain("named argument").IgnoreCase);
    }

    [Test]
    public void TryParse_SuccessfulExpression_ReturnsTrueWithEmptyDiagnostics()
    {
        var success = _engine.TryParseAsExpression<Func<int, bool>>(
            "x => x > 5", out var result, out var diagnostics);

        Assert.That(success, Is.True);
        Assert.That(result, Is.Not.Null);
        Assert.That(diagnostics, Has.Count.EqualTo(0));

        var compiled = result!.Compile();
        Assert.That(compiled(10), Is.True);
    }



    [Test]
    public void ParameterMismatch_TooFewLambdaParams()
    {
        // Lambda has 1 param but Func<int,int,int> expects 2
        Assert.That(
            () => _engine.ParseAsExpression<Func<int, int, int>>("x => x + 1"),
            Throws.InstanceOf<AlderException>()
                .With.Message.Contains("parameter"));
    }

    [Test]
    public void ParameterMismatch_TooManyLambdaParams()
    {
        // Lambda has 2 params but Func<int,int> expects 1
        Assert.That(
            () => _engine.ParseAsExpression<Func<int, int>>("(x, y) => x + y"),
            Throws.InstanceOf<AlderException>()
                .With.Message.Contains("parameter"));
    }



    [Test]
    public void NonLambdaInput_BareExpression_Throws()
    {
        Assert.That(
            () => _engine.ParseAsExpression<Func<int, int>>("x + 1"),
            Throws.InstanceOf<AlderException>()
                .With.Message.Contains("lambda"));
    }



    [Test]
    public void ExpressionTree_ProducesParameterExpression()
    {
        var expr = _engine.ParseAsExpression<Func<int, bool>>("x => x > 5");

        Assert.That(expr.Parameters, Has.Count.EqualTo(1));
        Assert.That(expr.Parameters[0].Name, Is.EqualTo("x"));
        Assert.That(expr.Parameters[0].Type, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void ExpressionTree_BodyIsCorrectNodeType()
    {
        var expr = _engine.ParseAsExpression<Func<int, bool>>("x => x > 5");

        Assert.That(expr.Body.NodeType, Is.EqualTo(ExpressionType.GreaterThan));
    }

    [Test]
    public void ExpressionTree_EngineVariableIsCapturedAsConstant()
    {
        _engine.SetVariable("threshold", 10);

        var expr = _engine.ParseAsExpression<Func<int, bool>>("x => x > threshold");

        // Walk the tree to find the ConstantExpression for the engine variable
        var binary = (BinaryExpression)expr.Body;
        Assert.That(binary.Right, Is.InstanceOf<ConstantExpression>());
        Assert.That(((ConstantExpression)binary.Right).Value, Is.EqualTo(10));
    }

    [Test]
    public void ExpressionTree_NoOpaqueRuntimeHelperCalls()
    {
        var expr = _engine.ParseAsExpression<Func<int, bool>>("x => x > 5");

        // The tree should contain only standard expression nodes
        // -- no calls to Alder runtime helpers
        var visitor = new OpaqueCallDetector();
        visitor.Visit(expr);

        Assert.That(visitor.FoundOpaqueCalls, Is.False,
            "Expression tree should not contain opaque runtime helper calls");
    }



    [Test]
    public void CompiledCorrectness_ComplexArithmeticExpression()
    {
        var expr = _engine.ParseAsExpression<Func<int, int, int>>(
            "(x, y) => (x + y) * 2 - x");
        var compiled = expr.Compile();

        Assert.That(compiled(3, 4), Is.EqualTo(11)); // (3+4)*2 - 3 = 11
        Assert.That(compiled(0, 0), Is.EqualTo(0));
    }

    [Test]
    public void CompiledCorrectness_ChainedMethodCalls()
    {
        var expr = _engine.ParseAsExpression<Func<string, string>>(
            "x => x.Trim().ToUpper()");
        var compiled = expr.Compile();

        Assert.That(compiled("  hello  "), Is.EqualTo("HELLO"));
    }

    [Test]
    public void CompiledCorrectness_NestedConditional()
    {
        var expr = _engine.ParseAsExpression<Func<int, string>>(
            "x => x > 0 ? \"positive\" : x < 0 ? \"negative\" : \"zero\"");
        var compiled = expr.Compile();

        Assert.That(compiled(5), Is.EqualTo("positive"));
        Assert.That(compiled(-3), Is.EqualTo("negative"));
        Assert.That(compiled(0), Is.EqualTo("zero"));
    }

    [Test]
    public void CompiledCorrectness_MemberAccessChain()
    {
        var expr = _engine.ParseAsExpression<Func<TestPerson, int>>(
            "x => x.Name.Length");
        var compiled = expr.Compile();

        Assert.That(compiled(new TestPerson { Name = "Alice" }), Is.EqualTo(5));
        Assert.That(compiled(new TestPerson { Name = "" }), Is.EqualTo(0));
    }



    [Test]
    public void Bitwise_And()
    {
        var expr = _engine.ParseAsExpression<Func<int, int>>("x => x & 0xFF");
        var compiled = expr.Compile();

        Assert.That(compiled(0x1234), Is.EqualTo(0x34));
    }

    [Test]
    public void Bitwise_Or()
    {
        var expr = _engine.ParseAsExpression<Func<int, int>>("x => x | 1");
        var compiled = expr.Compile();

        Assert.That(compiled(0), Is.EqualTo(1));
        Assert.That(compiled(2), Is.EqualTo(3));
    }

    [Test]
    public void Bitwise_Xor()
    {
        var expr = _engine.ParseAsExpression<Func<int, int>>("x => x ^ 0xFF");
        var compiled = expr.Compile();

        Assert.That(compiled(0x0F), Is.EqualTo(0xF0));
    }



    [Test]
    public void StaticMethod_MathAbs()
    {
        var expr = _engine.ParseAsExpression<Func<int, int>>("x => Math.Abs(x)");
        var compiled = expr.Compile();

        Assert.That(compiled(-5), Is.EqualTo(5));
        Assert.That(compiled(3), Is.EqualTo(3));
    }

    [Test]
    public void StaticMethod_MathMax()
    {
        var expr = _engine.ParseAsExpression<Func<int, int, int>>(
            "(x, y) => Math.Max(x, y)");
        var compiled = expr.Compile();

        Assert.That(compiled(3, 7), Is.EqualTo(7));
        Assert.That(compiled(10, 5), Is.EqualTo(10));
    }



    [Test]
    public void ZeroParam_ConstantExpression()
    {
        var expr = _engine.ParseAsExpression<Func<int>>("() => 42");
        var compiled = expr.Compile();

        Assert.That(compiled(), Is.EqualTo(42));
    }



    public class TestPerson
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    /// <summary>
    /// Expression visitor that detects calls to opaque Alder runtime helpers.
    /// An EF-compatible expression tree should contain only standard BCL types.
    /// </summary>
    private class OpaqueCallDetector : ExpressionVisitor
    {
        public bool FoundOpaqueCalls { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var declaringType = node.Method.DeclaringType;
            if (declaringType != null)
            {
                var ns = declaringType.Namespace ?? "";
                if (ns.StartsWith("Alder", StringComparison.Ordinal))
                {
                    FoundOpaqueCalls = true;
                }
            }
            return base.VisitMethodCall(node);
        }
    }



    [Test]
    public void Evaluate_Func_ReturnsUsableDelegate()
    {
        var doubler = _engine.Evaluate<Func<int, int>>("x => x * 2");

        Assert.That(doubler, Is.Not.Null);
        Assert.That(doubler!(5), Is.EqualTo(10));
        Assert.That(doubler(0), Is.EqualTo(0));
        Assert.That(doubler(-3), Is.EqualTo(-6));
    }

    [Test]
    public void Evaluate_Func_MultiParam()
    {
        var add = _engine.Evaluate<Func<int, int, int>>("(a, b) => a + b");

        Assert.That(add, Is.Not.Null);
        Assert.That(add!(3, 4), Is.EqualTo(7));
        Assert.That(add(0, 0), Is.EqualTo(0));
        Assert.That(add(-1, 1), Is.EqualTo(0));
    }

    [Test]
    public void Evaluate_Func_BoolReturn()
    {
        var isPositive = _engine.Evaluate<Func<int, bool>>("x => x > 0");

        Assert.That(isPositive, Is.Not.Null);
        Assert.That(isPositive!(5), Is.True);
        Assert.That(isPositive(0), Is.False);
        Assert.That(isPositive(-1), Is.False);
    }

    [Test]
    public void Evaluate_Func_ParameterMismatch_Throws()
    {
        var ex = Assert.Throws<AlderException>(() =>
            _engine.Evaluate<Func<int, int, int>>("x => x * 2"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0123));
    }



    [Test]
    public void CompileExpression_ReturnsNativeDelegate()
    {
        var greaterThan5 = _engine.CompileExpression<Func<int, bool>>("x => x > 5");

        Assert.That(greaterThan5(10), Is.True);
        Assert.That(greaterThan5(5), Is.False);
        Assert.That(greaterThan5(3), Is.False);
    }

    [Test]
    public void CompileExpression_MatchesParseAsExpression()
    {
        var compiled = _engine.CompileExpression<Func<int, int>>("x => x * x + 1");
        var fromTree = _engine.ParseAsExpression<Func<int, int>>("x => x * x + 1").Compile();

        for (var i = -5; i <= 5; i++)
            Assert.That(compiled(i), Is.EqualTo(fromTree(i)));
    }

    [Test]
    public void CompileExpression_MultiParam()
    {
        var multiply = _engine.CompileExpression<Func<double, double, double>>("(a, b) => a * b");

        Assert.That(multiply(3.0, 4.0), Is.EqualTo(12.0));
        Assert.That(multiply(0.5, 2.0), Is.EqualTo(1.0));
    }



    [Test]
    public void ParseAsExpression_WorksWithIQueryable_Where()
    {
        var data = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }.AsQueryable();
        var predicate = _engine.ParseAsExpression<Func<int, bool>>("x => x > 5");

        var results = data.Where(predicate).ToList();

        Assert.That(results, Is.EqualTo(new[] { 6, 7, 8, 9, 10 }));
    }

    [Test]
    public void ParseAsExpression_WorksWithIQueryable_Select()
    {
        var data = new[] { 1, 2, 3, 4, 5 }.AsQueryable();
        var projection = _engine.ParseAsExpression<Func<int, int>>("x => x * x");

        var results = data.Select(projection).ToList();

        Assert.That(results, Is.EqualTo(new[] { 1, 4, 9, 16, 25 }));
    }

    [Test]
    public void ParseAsExpression_WorksWithIQueryable_OrderBy()
    {
        var data = new[] { 5, 3, 1, 4, 2 }.AsQueryable();
        var keySelector = _engine.ParseAsExpression<Func<int, int>>("x => x");

        var results = data.OrderBy(keySelector).ToList();

        Assert.That(results, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void ParseAsExpression_WorksWithIQueryable_ComplexObject()
    {
        var people = new[]
        {
            new TestPerson { Name = "Alice", Age = 30 },
            new TestPerson { Name = "Bob", Age = 25 },
            new TestPerson { Name = "Charlie", Age = 35 },
            new TestPerson { Name = "Diana", Age = 28 }
        }.AsQueryable();

        var filter = _engine.ParseAsExpression<Func<TestPerson, bool>>("p => p.Age >= 30");
        var results = people.Where(filter).Select(p => p.Name).ToList();

        Assert.That(results, Is.EqualTo(new[] { "Alice", "Charlie" }));
    }

    [Test]
    public void ParseAsExpression_WorksWithIQueryable_ChainedOperations()
    {
        var data = new[] { 10, 3, 7, 1, 8, 5, 2, 9, 4, 6 }.AsQueryable();

        var predicate = _engine.ParseAsExpression<Func<int, bool>>("x => x >= 5");
        var transform = _engine.ParseAsExpression<Func<int, int>>("x => x * 2");

        var results = data.Where(predicate).OrderBy(x => x).Select(transform).ToList();

        Assert.That(results, Is.EqualTo(new[] { 10, 12, 14, 16, 18, 20 }));
    }

}
