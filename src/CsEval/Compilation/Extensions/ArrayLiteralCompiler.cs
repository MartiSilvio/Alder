using System.Linq.Expressions;
using CsEval.Parsing;

namespace CsEval.Compilation.Extensions;

/// <summary>
/// IL compiler for CsEval array literal expressions: [1, 2, 3].
/// Handles spread operator (..) within array elements via cached MethodInfo.
/// Called explicitly from ExpressionCompilerUnit.CompileArrayLiteral.
/// </summary>
internal static class ArrayLiteralCompiler
{
    internal static LinqExpression CompileArrayLiteral(
        ArrayLiteralExpr expr,
        CompilerContext ctx,
        Func<Expr, LinqExpression> compile)
    {
        var listVar = LinqExpression.Variable(typeof(List<object?>), "list");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(listVar, LinqExpression.New(CompilerContext.ListCtor))
        };

        foreach (var element in expr.Elements)
        {
            if (element is SpreadExpr spread)
            {
                var spreadValue = compile(spread.Expression);
                statements.Add(LinqExpression.Call(CompilerContext.SpreadIntoListMethod, listVar, spreadValue));
            }
            else
            {
                statements.Add(LinqExpression.Call(listVar, CompilerContext.ListAddMethod, compile(element)));
            }
        }

        statements.Add(LinqExpression.Call(CompilerContext.CreateTypedArrayMethod, listVar));
        return LinqExpression.Block(new[] { listVar }, statements);
    }
}
