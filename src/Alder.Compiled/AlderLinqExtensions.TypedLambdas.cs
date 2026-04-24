using System.Linq.Expressions;
using Alder.Compiled.DynamicLinq;

namespace Alder.Compiled;

public static partial class AlderLinqExtensions
{
    private static Expression CoerceLambdaBody(Expression body, Type resultType)
    {
        if (body.Type == resultType)
            return body;

        if (!body.Type.IsValueType && resultType.IsAssignableFrom(body.Type))
            return body;

        return Expression.Convert(body, resultType);
    }

    private static Func<T, bool> CompilePredicate<T>(Expression<Func<T, bool>> predicateExpr)
    {
        ArgumentNullException.ThrowIfNull(predicateExpr);
        return predicateExpr.Compile();
    }

    private static Func<T, TResult> CompileSelector<T, TResult>(Expression<Func<T, TResult>> selectorExpr)
    {
        ArgumentNullException.ThrowIfNull(selectorExpr);
        return selectorExpr.Compile();
    }
}
