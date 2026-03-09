using CsEval.Parsing;
using CsEval.Runtime.Extensions;

namespace CsEval.Compiled.Compilation.CompilerUnits;

/// <summary>
/// Compiles Extended-mode syntax sugar (pipeline, range, chained comparison, object literals)
/// to Expression Trees. Receives shared state via CompilerContext.
/// </summary>
internal sealed class ExtendedSyntaxCompilerUnit
{
    private readonly CompilerContext _ctx;
    private readonly DirectEmitCompilerUnit _directEmit;

    private ExpressionCompilerUnit? _exprUnit;

    internal ExtendedSyntaxCompilerUnit(CompilerContext ctx, DirectEmitCompilerUnit directEmit)
    {
        _ctx = ctx;
        _directEmit = directEmit;
    }

    internal void SetExpressionUnit(ExpressionCompilerUnit exprUnit)
    {
        _exprUnit = exprUnit;
    }

    private LinqExpression Compile(Expr expr) => _exprUnit!.Compile(expr);

    private static readonly MethodInfo InvokePipelineMethod =
        typeof(PipelineOperator).GetMethod(nameof(PipelineOperator.InvokePipeline))!;

    /// <summary>
    /// Compiles a pipeline expression (x |> f) to a call to
    /// PipelineOperator.InvokePipeline(leftValue, rightCallable, context, options, ct).
    /// </summary>
    internal LinqExpression CompilePipeline(PipelineExpr expr)
    {
        if (expr.Right is IdentifierExpr rightIdentifier)
        {
            return LinqExpression.Call(
                CompilerContext.InvokePipelineIdentifierMethod,
                Compile(expr.Left),
                LinqExpression.Constant(rightIdentifier.Name.Lexeme),
                _ctx.CurrentContext,
                _ctx.OptionsParam,
                _ctx.CtParam);
        }

        var left = Compile(expr.Left);
        var right = Compile(expr.Right);

        return LinqExpression.Call(
            InvokePipelineMethod,
            left,
            right,
            _ctx.CurrentContext,
            _ctx.OptionsParam,
            _ctx.CtParam);
    }

    private static readonly MethodInfo GenerateRangeMethod =
        typeof(RangeHelpers).GetMethod(nameof(RangeHelpers.GenerateRange))!;

    private static readonly MethodInfo ConvertToInt32Method =
        typeof(Convert).GetMethod(nameof(Convert.ToInt32), [typeof(object)])!;

    /// <summary>
    /// Compiles a range expression (start..end or start..&lt;end) to a call to
    /// RangeHelpers.GenerateRange(int start, int end, bool exclusiveEnd).
    /// Returns IEnumerable&lt;int&gt; boxed as object.
    /// </summary>
    internal LinqExpression CompileRange(RangeExpr expr)
    {
        var start = Compile(expr.Start);
        var end = Compile(expr.End);

        var startInt = LinqExpression.Call(ConvertToInt32Method, start);
        var endInt = LinqExpression.Call(ConvertToInt32Method, end);

        var call = LinqExpression.Call(
            GenerateRangeMethod,
            startInt,
            endInt,
            LinqExpression.Constant(expr.ExclusiveEnd));

        return LinqExpression.Convert(call, typeof(object));
    }

    private static readonly MethodInfo PerformComparisonMethod =
        typeof(ChainedComparisonHelper).GetMethod(nameof(ChainedComparisonHelper.PerformComparison))!;

    /// <summary>
    /// Compiles a chained comparison (0 &lt; x &lt; 10) with lazy evaluation and short-circuit.
    /// Each operand is evaluated exactly once; short-circuits on first false comparison.
    /// </summary>
    internal LinqExpression CompileChainedComparison(ChainedComparisonExpr expr)
    {
        if (_directEmit.TryCompileDirectNumericChainedComparison(expr, out var direct))
            return direct;

        var resultLabel = LinqExpression.Label(typeof(object), "chainResult");
        var variables = new List<System.Linq.Expressions.ParameterExpression>();
        var body = new List<LinqExpression>();

        var v0 = LinqExpression.Variable(typeof(object), "v0");
        variables.Add(v0);
        body.Add(LinqExpression.Assign(v0, Compile(expr.Operands[0])));

        for (int i = 0; i < expr.Operators.Count; i++)
        {
            var vi = LinqExpression.Variable(typeof(object), $"v{i + 1}");
            variables.Add(vi);
            body.Add(LinqExpression.Assign(vi, Compile(expr.Operands[i + 1])));

            var prevVar = variables[i];
            var comparison = LinqExpression.Call(
                PerformComparisonMethod,
                prevVar,
                vi,
                LinqExpression.Constant(expr.Operators[i].Type),
                _ctx.OptionsParam);

            body.Add(LinqExpression.IfThen(
                LinqExpression.Not(comparison),
                LinqExpression.Return(resultLabel, LinqExpression.Constant(false, typeof(object)))));
        }

        body.Add(LinqExpression.Label(resultLabel, LinqExpression.Constant(true, typeof(object))));

        return LinqExpression.Block(typeof(object), variables.ToArray(), body.ToArray());
    }

    /// <summary>
    /// Compiles an anonymous object literal expression: new { Name = "John" }.
    /// Creates ExpandoObject with property spread support.
    /// </summary>
    internal LinqExpression CompileObjectLiteral(ObjectLiteralExpr expr)
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
                var spreadValue = Compile(spread.Expression);
                statements.Add(LinqExpression.Call(CompilerContext.SpreadIntoDictMethod, dictVar, spreadValue, _ctx.CurrentContext));
            }
            else
            {
                statements.Add(LinqExpression.Assign(
                    LinqExpression.Property(dictVar, dictItemProperty, LinqExpression.Constant(key.Lexeme)),
                    Compile(value)));
            }
        }

        statements.Add(LinqExpression.Convert(dictVar, typeof(object)));
        return LinqExpression.Block(new[] { dictVar }, statements);
    }
}
