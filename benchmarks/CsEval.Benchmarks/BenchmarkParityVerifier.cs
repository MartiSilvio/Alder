using DynamicExpresso;
using Flee.PublicTypes;
using NCalc;

namespace CsEval.Benchmarks;

public readonly record struct ParityResult(bool IsSuccess, string Message);

public static class BenchmarkParityVerifier
{
    public static ParityResult VerifyComparableScenario(ComparableScenario scenario, BenchmarkGlobalData globals)
    {
        try
        {
            var expected = scenario.NativeEvaluator(globals);
            var interpreted = EvaluateCsEval(globals, scenario.CsEvalExpression, CompilationMode.Interpreted);
            var compiled = EvaluateCsEval(globals, scenario.CsEvalExpression, CompilationMode.StrictCompiled);
            var roslyn = EvaluateRoslyn(globals, scenario.RoslynExpression);
            var ncalc = EvaluateNCalc(globals, scenario.NCalcExpression);
            var dynamicExpresso = EvaluateDynamicExpresso(globals, scenario.DynamicExpressoExpression);
            var flee = EvaluateFlee(globals, scenario.FleeExpression);

            if (!AreEquivalent(expected, interpreted))
                return Failure(scenario, "CsEval Interpreted", expected, interpreted);
            if (!AreEquivalent(expected, compiled))
                return Failure(scenario, "CsEval Compiled", expected, compiled);
            if (!AreEquivalent(expected, roslyn))
                return Failure(scenario, "Roslyn Script", expected, roslyn);
            if (!AreEquivalent(expected, ncalc))
                return Failure(scenario, "NCalc", expected, ncalc);
            if (!AreEquivalent(expected, dynamicExpresso))
                return Failure(scenario, "Dynamic Expresso", expected, dynamicExpresso);
            if (!AreEquivalent(expected, flee))
                return Failure(scenario, "Flee", expected, flee);

            return new ParityResult(true, $"{scenario.Name} aligned across engines.");
        }
        catch (Exception ex)
        {
            return new ParityResult(false, $"{scenario.Name} parity verification failed: {ex.GetType().Name} - {ex.Message}");
        }
    }

    public static ParityResult VerifyAdvancedScenario(AdvancedScenario scenario, BenchmarkGlobalData globals)
    {
        try
        {
            var interpreted = EvaluateCsEval(globals, scenario.CsEvalExpression, CompilationMode.Interpreted);
            var compiled = EvaluateCsEval(globals, scenario.CsEvalExpression, CompilationMode.StrictCompiled);
            var roslyn = EvaluateRoslyn(globals, scenario.RoslynExpression);
            var dynamicExpresso = EvaluateDynamicExpresso(globals, scenario.DynamicExpressoExpression);
            var flee = EvaluateFlee(globals, scenario.FleeExpression);

            if (!AreEquivalent(roslyn, interpreted))
                return Failure(scenario, "CsEval Interpreted", roslyn, interpreted);
            if (!AreEquivalent(roslyn, compiled))
                return Failure(scenario, "CsEval Compiled", roslyn, compiled);
            if (!AreEquivalent(roslyn, dynamicExpresso))
                return Failure(scenario, "Dynamic Expresso", roslyn, dynamicExpresso);
            if (!AreEquivalent(roslyn, flee))
                return Failure(scenario, "Flee", roslyn, flee);

            return new ParityResult(true, $"{scenario.Name} aligned between CsEval and Roslyn.");
        }
        catch (Exception ex)
        {
            return new ParityResult(false, $"{scenario.Name} advanced parity failed: {ex.GetType().Name} - {ex.Message}");
        }
    }

    public static ParityResult VerifyExtendedParityScenario(ExtendedParityScenario scenario, BenchmarkGlobalData globals)
    {
        try
        {
            using var extInterpreted = BenchmarkBase.CreateEngine(
                CompilationMode.Interpreted,
                globals,
                LanguageMode.Extended);
            using var extCompiled = BenchmarkBase.CreateEngine(
                CompilationMode.StrictCompiled,
                globals,
                LanguageMode.Extended);
            using var stdInterpreted = BenchmarkBase.CreateEngine(
                CompilationMode.Interpreted,
                globals,
                LanguageMode.Standard);
            using var stdCompiled = BenchmarkBase.CreateEngine(
                CompilationMode.StrictCompiled,
                globals,
                LanguageMode.Standard);

            ConfigureExtendedParityEngine(extInterpreted);
            ConfigureExtendedParityEngine(extCompiled);
            ConfigureExtendedParityEngine(stdInterpreted);
            ConfigureExtendedParityEngine(stdCompiled);

            var extI = extInterpreted.Evaluate(scenario.ExtendedExpression);
            var extC = extCompiled.Evaluate(scenario.ExtendedExpression);
            var stdI = stdInterpreted.Evaluate(scenario.StandardExpression);
            var stdC = stdCompiled.Evaluate(scenario.StandardExpression);

            if (!AreEquivalent(stdI, extI))
                return Failure(scenario, "Extended Interpreted", stdI, extI);
            if (!AreEquivalent(stdC, extC))
                return Failure(scenario, "Extended Compiled", stdC, extC);

            return new ParityResult(true, $"{scenario.Name} extended syntax aligns with normal syntax.");
        }
        catch (Exception ex)
        {
            return new ParityResult(false, $"{scenario.Name} extended parity failed: {ex.GetType().Name} - {ex.Message}");
        }
    }

    public static ParityResult VerifyLinqScenario(LinqScenario scenario, BenchmarkGlobalData globals)
    {
        try
        {
            var expected = scenario.NativeEvaluator(globals);
            var interpreted = EvaluateCsEval(globals, scenario.CsEvalExpression, CompilationMode.Interpreted);
            var compiled = EvaluateCsEval(globals, scenario.CsEvalExpression, CompilationMode.StrictCompiled);
            var roslyn = EvaluateRoslyn(globals, scenario.RoslynExpression);

            if (!AreEquivalent(expected, interpreted))
                return Failure(scenario, "CsEval Interpreted", expected, interpreted);
            if (!AreEquivalent(expected, compiled))
                return Failure(scenario, "CsEval Compiled", expected, compiled);
            if (!AreEquivalent(expected, roslyn))
                return Failure(scenario, "Roslyn Script", expected, roslyn);

            return new ParityResult(true, $"{scenario.Name} aligned across engines.");
        }
        catch (Exception ex)
        {
            return new ParityResult(false, $"{scenario.Name} LINQ parity failed: {ex.GetType().Name} - {ex.Message}");
        }
    }


    public static ParityResult VerifyCompilationScenario(CompilationScenario scenario, BenchmarkGlobalData globals)
    {
        try
        {
            using var interpretedEngine = BenchmarkBase.CreateEngine(CompilationMode.Interpreted, globals);
            using var compiledEngine = BenchmarkBase.CreateEngine(CompilationMode.StrictCompiled, globals);

            interpretedEngine.Parse(scenario.CsEvalExpression);
            compiledEngine.Parse(scenario.CsEvalExpression);

            var script = BenchmarkBase.CreateRoslynScript(scenario.RoslynExpression);
            script.Compile();

            _ = new Expression(scenario.NCalcExpression);

            var interpreter = BenchmarkBase.CreateDynamicExpressoInterpreter(globals);
            interpreter.Parse(scenario.DynamicExpressoExpression);

            var fleeContext = BenchmarkBase.CreateFleeContext(globals);
            _ = fleeContext.CompileDynamic(scenario.FleeExpression);

            return new ParityResult(true, $"{scenario.Name} compiles/parses across engines.");
        }
        catch (Exception ex)
        {
            return new ParityResult(false, $"{scenario.Name} compilation parity failed: {ex.GetType().Name} - {ex.Message}");
        }
    }

    private static ParityResult Failure(ComparableScenario scenario, string engineName, object? expected, object? actual)
        => new(false, $"{scenario.Name}: {engineName} mismatch. expected={Format(expected)}, actual={Format(actual)}");

    private static ParityResult Failure(AdvancedScenario scenario, string engineName, object? expected, object? actual)
        => new(false, $"{scenario.Name}: {engineName} mismatch. expected={Format(expected)}, actual={Format(actual)}");

    private static ParityResult Failure(ExtendedParityScenario scenario, string engineName, object? expected, object? actual)
        => new(false, $"{scenario.Name}: {engineName} mismatch. expected={Format(expected)}, actual={Format(actual)}");

    private static ParityResult Failure(LinqScenario scenario, string engineName, object? expected, object? actual)
        => new(false, $"{scenario.Name}: {engineName} mismatch. expected={Format(expected)}, actual={Format(actual)}");


    private static void ConfigureExtendedParityEngine(CsEvalEngine engine)
    {
        engine.RegisterFunction("inc", args => Convert.ToInt32(args[0]) + 1);
    }

    private static object? EvaluateCsEval(BenchmarkGlobalData globals, string expression, CompilationMode mode)
    {
        using var engine = BenchmarkBase.CreateEngine(mode, globals);
        return engine.Evaluate(expression);
    }

    private static object? EvaluateRoslyn(BenchmarkGlobalData globals, string expression)
    {
        return BenchmarkBase.EvaluateRoslynAsync(expression, globals).GetAwaiter().GetResult();
    }

    private static object? EvaluateNCalc(BenchmarkGlobalData globals, string expression)
    {
        var ncalc = new Expression(expression);
        ApplyNCalcParameters(ncalc, globals);
        return ncalc.Evaluate();
    }

    private static object? EvaluateDynamicExpresso(BenchmarkGlobalData globals, string expression)
    {
        var interpreter = BenchmarkBase.CreateDynamicExpressoInterpreter(globals);
        var lambda = interpreter.Parse(expression);
        return lambda.Invoke();
    }

    private static object? EvaluateFlee(BenchmarkGlobalData globals, string expression)
    {
        var context = BenchmarkBase.CreateFleeContext(globals);
        IDynamicExpression compiled = context.CompileDynamic(expression);
        return compiled.Evaluate();
    }

    internal static void ApplyNCalcParameters(Expression expression, BenchmarkGlobalData globals)
    {
        expression.Parameters["x"] = globals.X;
        expression.Parameters["y"] = globals.Y;
        expression.Parameters["z"] = globals.Z;
        expression.Parameters["value"] = globals.Value;
        expression.Parameters["text"] = globals.Text;
    }

    private static bool AreEquivalent(object? expected, object? actual)
    {
        if (expected is null || actual is null)
            return Equals(expected, actual);

        if (expected is string expectedString && actual is string actualString)
            return string.Equals(expectedString, actualString, StringComparison.Ordinal);

        if (expected is bool expectedBool && actual is bool actualBool)
            return expectedBool == actualBool;

        if (IsNumeric(expected) && IsNumeric(actual))
        {
            var expectedDecimal = Convert.ToDecimal(expected);
            var actualDecimal = Convert.ToDecimal(actual);
            return decimal.Abs(expectedDecimal - actualDecimal) <= 0.000_001m;
        }

        return Equals(expected, actual);
    }

    private static bool IsNumeric(object value) =>
        value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private static string Format(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"\"{s}\"",
            _ => $"{value} ({value.GetType().Name})"
        };
    }
}
