using System.Linq.Expressions;
using System.Reflection;
using Alder.Binding;
using Alder.Compiled.Compilation;

namespace Alder.Compiled.DynamicLinq;

public sealed class DynamicQueryPlan
{
    internal DynamicQueryPlan(
        DynamicQueryLambdaKind kind,
        DynamicQueryResultShape resultShape,
        Type resultType,
        BoundExpr boundBody,
        IReadOnlyList<ParameterExpression> parameters,
        IReadOnlyDictionary<string, object?> capturedVariables,
        LambdaExpression exportedLambda)
    {
        Kind = kind;
        ResultShape = resultShape;
        ResultType = resultType;
        BoundBody = boundBody;
        Parameters = parameters;
        CapturedVariables = capturedVariables;
        ExportedLambda = exportedLambda;
    }

    public Type ResultType { get; }

    public LambdaExpression Lambda => ExportedLambda;

    internal DynamicQueryLambdaKind Kind { get; }

    internal DynamicQueryResultShape ResultShape { get; }

    internal BoundExpr BoundBody { get; }

    internal IReadOnlyList<ParameterExpression> Parameters { get; }

    internal IReadOnlyDictionary<string, object?> CapturedVariables { get; }

    internal LambdaExpression ExportedLambda { get; }

    public LambdaExpression ToLambdaExpression(Type? resultType = null)
        => Expression.Lambda(
            CoerceLambdaBody(ExportedLambda.Body, resultType ?? ExportedLambda.ReturnType),
            ExportedLambda.Parameters);

    public Expression<TDelegate> ToExpression<TDelegate>()
        where TDelegate : Delegate
    {
        var invoke = typeof(TDelegate).GetMethod(nameof(Action.Invoke))
            ?? throw new InvalidOperationException($"{typeof(TDelegate)} is not a delegate type.");
        ValidateDelegateParameters(invoke);
        return Expression.Lambda<TDelegate>(
            CoerceLambdaBody(ExportedLambda.Body, invoke.ReturnType),
            ExportedLambda.Parameters);
    }

    public TDelegate Compile<TDelegate>()
        where TDelegate : Delegate
        => ToExpression<TDelegate>().Compile();

    private void ValidateDelegateParameters(MethodInfo invoke)
    {
        var expected = invoke.GetParameters();
        if (expected.Length != ExportedLambda.Parameters.Count)
            throw new InvalidOperationException("Plan parameter count does not match the requested delegate type.");

        for (var i = 0; i < expected.Length; i++)
        {
            if (expected[i].ParameterType != ExportedLambda.Parameters[i].Type)
                throw new InvalidOperationException("Plan parameter types do not match the requested delegate type.");
        }
    }

    private static Expression CoerceLambdaBody(Expression body, Type resultType)
    {
        if (body.Type == resultType)
            return body;

        if (!body.Type.IsValueType && resultType.IsAssignableFrom(body.Type))
            return body;

        return Expression.Convert(body, resultType);
    }
}
