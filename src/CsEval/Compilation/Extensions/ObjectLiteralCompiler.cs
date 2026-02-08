using CsEval.Parsing;

namespace CsEval.Compilation.Extensions;

/// <summary>
/// IL compiler for anonymous object literal expressions: new { Name = "John" }.
/// Creates ExpandoObject with property spread support via cached MethodInfo.
/// Called explicitly from ExpressionCompilerUnit.CompileObjectLiteral.
/// </summary>
internal static class ObjectLiteralCompiler
{
    internal static LinqExpression CompileObjectLiteral(
        ObjectLiteralExpr expr,
        CompilerContext ctx,
        Func<Expr, LinqExpression> compile)
    {
        var dictVar = LinqExpression.Variable(typeof(IDictionary<string, object?>), "dict");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(dictVar, LinqExpression.New(CompilerContext.ExpandoObjectCtor))
        };

        var dictItemProperty = typeof(IDictionary<string, object?>).GetProperty("Item")!;

        foreach (var (key, value) in expr.Properties)
        {
            if (key.Type == TokenType.DotDot && value is SpreadExpr spread)
            {
                var spreadValue = compile(spread.Expression);
                statements.Add(LinqExpression.Call(CompilerContext.SpreadIntoDictMethod, dictVar, spreadValue, ctx.CurrentContext));
            }
            else
            {
                statements.Add(LinqExpression.Assign(
                    LinqExpression.Property(dictVar, dictItemProperty, LinqExpression.Constant(key.Lexeme)),
                    compile(value)));
            }
        }

        statements.Add(LinqExpression.Convert(dictVar, typeof(object)));
        return LinqExpression.Block(new[] { dictVar }, statements);
    }
}
