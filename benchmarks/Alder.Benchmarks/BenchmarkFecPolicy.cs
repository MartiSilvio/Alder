namespace Alder.Benchmarks;

public static class BenchmarkFecPolicy
{
    public const string UnsupportedReasonCode = "n/a-fec-unsupported-expression";

    private static readonly HashSet<string> SupportedCaseIds = new(StringComparer.Ordinal)
    {
        "Arithmetic/Constant",
        "Arithmetic/Variables",
        "Arithmetic/Modulo",
        "Boolean/Composite",
        "Boolean/MixedPredicate",
        "Conditional/Ternary",
        "Equality/Nested",
        "Functions/MathMix",
        "String/Concatenation",
        "String/ConditionalConcat",
        "Stress/SmallBranching",
        "Stress/BigBoolean",

        "Advanced/NestedMath",
        "Advanced/NestedConditional",
        "Advanced/StringPredicate",
        "Advanced/CollectionProperties",
        "Advanced/ObjectGraphAccess",
        "Advanced/StringChain",
        "Advanced/StringInterpolation",
        "Advanced/NullConditional",
        "Advanced/TupleCreation",

        "Invocation/OverloadResolution_Int",
        "Invocation/OverloadResolution_Long",
        "Invocation/ImplicitConversion",
        "Invocation/ParamsExpansion",
        "Invocation/OptionalArgument",
        "Invocation/ChainedInstance",

        "BusinessRules/ProductPredicate",

        "Extended/BareMath",
        "Extended/Pipeline",
        "Extended/ChainedComparison",
        "Extended/PowerOperator",
        "Extended/InOperator",
        "Extended/NotIn",
        "Extended/Like",
        "Extended/Regex"
    };

    public static bool IsUnsupportedExpression(string caseId, string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return false;

        return !SupportedCaseIds.Contains(caseId);
    }

    public static bool IsSupportedExpression(string caseId, string? expression) =>
        !IsUnsupportedExpression(caseId, expression);
}
