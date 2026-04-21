using Flee.PublicTypes;
using NCalc;

namespace Alder.Benchmarks;

public readonly record struct ParityResult(bool IsSuccess, string Message);

public static class BenchmarkParityVerifier
{
    public static ParityResult VerifyCrossEngineScenario(CrossEngineScenario scenario, BenchmarkData data)
    {
        try
        {
            var expected = scenario.Native(data);
            var interpreted = EvaluateAlder(data, scenario.AlderExpr, CompilationMode.Interpreted);
            var compiled = EvaluateAlder(data, scenario.AlderExpr, CompilationMode.Compiled);
            var compiledFec = EvaluateAlder(data, scenario.AlderExpr, CompilationMode.CompiledFec);
            var roslyn = EvaluateRoslyn(data, scenario.RoslynExpr);
            var ncalc = EvaluateNCalc(data, scenario.NCalcExpr);
            var dynamicExpresso = EvaluateDynamicExpresso(data, scenario.DExpressoExpr);
            var flee = EvaluateFlee(data, scenario.FleeExpr);

            if (!AreEquivalent(expected, interpreted))
                return Failure(scenario.Name, "Alder Interpreted", expected, interpreted);
            if (!AreEquivalent(expected, compiled))
                return Failure(scenario.Name, "Alder Compiled", expected, compiled);
            if (!AreEquivalent(expected, compiledFec))
                return Failure(scenario.Name, "Alder CompiledFec", expected, compiledFec);
            if (!AreEquivalent(expected, roslyn))
                return Failure(scenario.Name, "Roslyn", expected, roslyn);
            if (!AreEquivalent(expected, ncalc))
                return Failure(scenario.Name, "NCalc", expected, ncalc);
            if (!AreEquivalent(expected, dynamicExpresso))
                return Failure(scenario.Name, "DynamicExpresso", expected, dynamicExpresso);
            if (!AreEquivalent(expected, flee))
                return Failure(scenario.Name, "Flee", expected, flee);

            return new ParityResult(true, $"{scenario.Name}: all engines aligned.");
        }
        catch (Exception ex)
        {
            return new ParityResult(false, $"{scenario.Name}: {ex.GetType().Name} - {ex.Message}");
        }
    }

    public static ParityResult VerifyAlderScenario(AlderScenario scenario, BenchmarkData data)
    {
        try
        {
            var expected = scenario.Native(data);
            var interpreted = EvaluateAlder(data, scenario.AlderExpr, CompilationMode.Interpreted);
            var compiled = EvaluateAlder(data, scenario.AlderExpr, CompilationMode.Compiled);
            var roslyn = EvaluateRoslyn(data, scenario.RoslynExpr);

            if (!AreEquivalent(expected, interpreted))
                return Failure(scenario.Name, "Alder Interpreted", expected, interpreted);
            if (!AreEquivalent(expected, compiled))
                return Failure(scenario.Name, "Alder Compiled", expected, compiled);
            if (!AreEquivalent(expected, roslyn))
                return Failure(scenario.Name, "Roslyn", expected, roslyn);

            return new ParityResult(true, $"{scenario.Name}: Alder and Roslyn aligned.");
        }
        catch (Exception ex)
        {
            return new ParityResult(false, $"{scenario.Name}: {ex.GetType().Name} - {ex.Message}");
        }
    }

    public static ParityResult VerifyExtendedScenario(ExtendedScenario scenario, BenchmarkData data)
    {
        try
        {
            foreach (var mode in new[] { CompilationMode.Interpreted, CompilationMode.Compiled, CompilationMode.CompiledFec })
            {
                using var extEngine = BenchmarkBase.CreateEngine(mode, data, LanguageMode.Extended, ConfigureExtended);
                using var stdEngine = BenchmarkBase.CreateEngine(mode, data, LanguageMode.Standard, ConfigureExtended);

                var extResult = extEngine.Evaluate(scenario.ExtendedExpr);
                var stdResult = stdEngine.Evaluate(scenario.StandardExpr);

                if (!AreEquivalent(stdResult, extResult))
                    return Failure(scenario.Name, $"Extended vs Standard ({mode})", stdResult, extResult);
            }

            return new ParityResult(true, $"{scenario.Name}: extended syntax matches standard.");
        }
        catch (Exception ex)
        {
            return new ParityResult(false, $"{scenario.Name}: {ex.GetType().Name} - {ex.Message}");
        }
    }

    public static ParityResult VerifyDynamicLinqScenario(DynamicLinqScenario scenario, BenchmarkData data)
    {
        try
        {
            var expected = scenario.Native(data);
            var alder = scenario.Alder(data);
            var dynLinq = scenario.DynLinqCore(data);

            if (!AreEquivalent(expected, alder))
                return Failure(scenario.Name, "Alder DynamicLINQ", expected, alder);
            if (!AreEquivalent(expected, dynLinq))
                return Failure(scenario.Name, "System.Linq.Dynamic.Core", expected, dynLinq);

            return new ParityResult(true, $"{scenario.Name}: Dynamic LINQ aligned.");
        }
        catch (Exception ex)
        {
            return new ParityResult(false, $"{scenario.Name}: {ex.GetType().Name} - {ex.Message}");
        }
    }

    internal static void ApplyNCalcParameters(Expression expression, BenchmarkData data)
    {
        expression.Parameters["x"] = data.X;
        expression.Parameters["y"] = data.Y;
        expression.Parameters["z"] = data.Z;
        expression.Parameters["value"] = data.Value;
        expression.Parameters["text"] = data.Text;
    }

    private static void ConfigureExtended(AlderOptions opts)
    {
        opts.Functions.Register("inc", args => Convert.ToInt32(args[0]) + 1);
    }

    private static object? EvaluateAlder(BenchmarkData data, string expression, CompilationMode mode)
    {
        using var engine = BenchmarkBase.CreateEngine(mode, data);
        return engine.Evaluate(expression);
    }

    private static object? EvaluateRoslyn(BenchmarkData data, string expression)
    {
        return BenchmarkBase.EvaluateRoslynAsync(expression, data).GetAwaiter().GetResult();
    }

    private static object? EvaluateNCalc(BenchmarkData data, string expression)
    {
        var ncalc = new Expression(expression);
        ApplyNCalcParameters(ncalc, data);
        return ncalc.Evaluate();
    }

    private static object? EvaluateDynamicExpresso(BenchmarkData data, string expression)
    {
        var interpreter = BenchmarkBase.CreateDynamicExpressoInterpreter(data);
        var lambda = interpreter.Parse(expression);
        return lambda.Invoke();
    }

    private static object? EvaluateFlee(BenchmarkData data, string expression)
    {
        var context = BenchmarkBase.CreateFleeContext(data);
        IDynamicExpression compiled = context.CompileDynamic(expression);
        return compiled.Evaluate();
    }

    private static ParityResult Failure(string scenario, string engineName, object? expected, object? actual)
        => new(false, $"{scenario}: {engineName} mismatch. expected={Format(expected)}, actual={Format(actual)}");

    internal static bool AreEquivalent(object? expected, object? actual)
    {
        if (expected is null || actual is null)
            return Equals(expected, actual);

        if (expected is string es && actual is string sa)
            return string.Equals(es, sa, StringComparison.Ordinal);

        if (expected is bool eb && actual is bool ba)
            return eb == ba;

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
