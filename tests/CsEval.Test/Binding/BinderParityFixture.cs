namespace CsEval.Test;

public sealed record ExecutionParityScenario(
    string Name,
    string Expression,
    IReadOnlyDictionary<string, object?> Variables)
{
    public Dictionary<string, object?> CloneVariables()
    {
        var clone = new Dictionary<string, object?>(Variables.Count, StringComparer.Ordinal);
        foreach (var pair in Variables)
            clone[pair.Key] = pair.Value;
        return clone;
    }

    public override string ToString() => Name;
}

internal static class BinderParityFixture
{
    public static IEnumerable<TestCaseData> StandardScenarios()
    {
        yield return CreateScenario(
            "Standard/MathMix",
            "Math.Abs(x - y) + Math.Max(y, z)",
            ("x", -5),
            ("y", 2),
            ("z", 9));

        yield return CreateScenario(
            "Standard/StringChain",
            "text.Trim().ToUpper().Substring(0, 3)",
            ("text", "  alpha  "));

        yield return CreateScenario(
            "Standard/PredicateCount",
            "numbers.Where((n) => n > threshold).Count()",
            ("numbers", new List<int> { 1, 3, 5, 9, 10 }),
            ("threshold", 4));
    }

    public static IEnumerable<TestCaseData> ExtendedScenarios()
    {
        yield return CreateScenario(
            "Extended/PipelineFunction",
            "x |> inc",
            ("x", 41));

        yield return CreateScenario(
            "Extended/NotInAlias",
            "x not in numbers",
            ("x", 42),
            ("numbers", new List<int> { 1, 2, 3, 5, 8, 13 }));

        yield return CreateScenario(
            "Extended/RegexMatch",
            "text =~ \"^alp\"",
            ("text", "alpha"));

        yield return CreateScenario(
            "Extended/ImplicitItWhereCount",
            "numbers.Where(it > threshold).Count()",
            ("numbers", new List<int> { 1, 3, 5, 9, 10 }),
            ("threshold", 4));

        yield return CreateScenario(
            "Extended/DateArithmeticSugar",
            "start + 30.days - start",
            ("start", new DateTime(2026, 1, 1)));

        yield return CreateScenario(
            "Extended/LetInExpression",
            "let x = 5 in x * x");

        yield return CreateScenario(
            "Extended/ScopeFunctionLet",
            "7.let(x => x * x)");

        yield return CreateScenario(
            "Extended/IfExpression",
            "if (value > 0) value else -value",
            ("value", -7));

        yield return CreateScenario(
            "Extended/Comprehension",
            "[x * x for x in 1..10 if x % 2 == 0]");

        yield return CreateScenario(
            "Extended/LetInDestructuring",
            "let { Name, Age } = person in Name + \":\" + Age",
            ("person", new Dictionary<string, object?> { ["Name"] = "Ada", ["Age"] = 20 }));
    }

    public static CsEvalEngine CreateEngine(
        CompilationMode mode,
        LanguageMode languageMode,
        ExecutionParityScenario scenario)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            LanguageMode = languageMode
        });

        engine.RegisterFunction("inc", args => Convert.ToInt32(args[0]) + 1);
        foreach (var pair in scenario.Variables)
            engine.SetVariable(pair.Key, pair.Value);

        return engine;
    }

    private static TestCaseData CreateScenario(
        string name,
        string expression,
        params (string Name, object? Value)[] variables)
    {
        var variableMap = new Dictionary<string, object?>(variables.Length, StringComparer.Ordinal);
        foreach (var (variableName, variableValue) in variables)
            variableMap[variableName] = variableValue;

        return new TestCaseData(
                new ExecutionParityScenario(name, expression, variableMap))
            .SetName(name.Replace('/', '_'));
    }
}
