using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compilation;

internal sealed partial class ExpressionCompilerUnit
{
    internal LinqExpression CompileLambda(LambdaExpr lambda)
    {
        var parameterNames = lambda.Parameters.Select(p => p.Name.Lexeme).ToList();

        // Create parameter list constant
        var listInit = LinqExpression.ListInit(
            LinqExpression.New(typeof(List<string>)),
            parameterNames.Select(p => LinqExpression.ElementInit(
                typeof(List<string>).GetMethod("Add")!,
                LinqExpression.Constant(p))));

        var closureParam = LinqExpression.Parameter(typeof(CsEvalContext), "closure");

        // Generic object-array delegate (fallback path)
        var argsParam = LinqExpression.Parameter(typeof(object?[]), "args");
        var genericAccessors = new List<LinqExpression>(parameterNames.Count);
        for (var i = 0; i < parameterNames.Count; i++)
            genericAccessors.Add(LinqExpression.ArrayIndex(argsParam, LinqExpression.Constant(i)));

        var genericBody = BuildCompiledLambdaBody(lambda, parameterNames, genericAccessors, closureParam);
        var compiledDelegate = LinqExpression.Lambda<Func<object?[], CsEvalContext, object?>>(
            genericBody,
            argsParam,
            closureParam);

        // Fast-path delegates for common LINQ selector/predicate arities
        LinqExpression fast0Expr = LinqExpression.Constant(null, typeof(Func<CsEvalContext, object?>));
        LinqExpression fast1Expr = LinqExpression.Constant(null, typeof(Func<object?, CsEvalContext, object?>));
        LinqExpression fast2Expr = LinqExpression.Constant(null, typeof(Func<object?, object?, CsEvalContext, object?>));

        if (parameterNames.Count == 0)
        {
            var body0 = BuildCompiledLambdaBody(lambda, parameterNames, [], closureParam);
            fast0Expr = LinqExpression.Lambda<Func<CsEvalContext, object?>>(body0, closureParam);
        }
        else if (parameterNames.Count == 1)
        {
            var arg0 = LinqExpression.Parameter(typeof(object), "arg0");
            var body1 = BuildCompiledLambdaBody(lambda, parameterNames, [arg0], closureParam);
            fast1Expr = LinqExpression.Lambda<Func<object?, CsEvalContext, object?>>(body1, arg0, closureParam);
        }
        else if (parameterNames.Count == 2)
        {
            var arg0 = LinqExpression.Parameter(typeof(object), "arg0");
            var arg1 = LinqExpression.Parameter(typeof(object), "arg1");
            var body2 = BuildCompiledLambdaBody(lambda, parameterNames, [arg0, arg1], closureParam);
            fast2Expr = LinqExpression.Lambda<Func<object?, object?, CsEvalContext, object?>>(body2, arg0, arg1, closureParam);
        }

        // Create CompiledLambdaValue(parameters, compiledBody, closure, fast0, fast1, fast2)
        return LinqExpression.New(
            CompilerContext.CompiledLambdaValueCtor,
            listInit,
            compiledDelegate,
            _ctx.CurrentContext,
            fast0Expr,
            fast1Expr,
            fast2Expr,
            LinqExpression.Constant(lambda, typeof(LambdaExpr)));
    }

    private LinqExpression BuildCompiledLambdaBody(
        LambdaExpr lambda,
        List<string> parameterNames,
        IReadOnlyList<LinqExpression> parameterAccessors,
        System.Linq.Expressions.ParameterExpression closureParam)
    {
        var paramMap = new Dictionary<string, LinqExpression>(parameterNames.Count, StringComparer.Ordinal);
        for (var i = 0; i < parameterNames.Count; i++)
            paramMap[parameterNames[i]] = parameterAccessors[i];

        var needsChildContext = lambda.Body is BlockExpr;
        var statements = new List<LinqExpression>();
        var blockVars = new List<System.Linq.Expressions.ParameterExpression>();

        System.Linq.Expressions.ParameterExpression? childContextVar = null;
        if (needsChildContext)
        {
            childContextVar = LinqExpression.Variable(typeof(CsEvalContext), "childContext");
            blockVars.Add(childContextVar);
            statements.Add(LinqExpression.Assign(
                childContextVar,
                LinqExpression.Call(closureParam, CompilerContext.CreateChildMethod)));
        }

        var savedContext = _ctx.CurrentContext;
        _ctx.CurrentContext = needsChildContext ? childContextVar! : closureParam;
        _ctx.PushLambdaParams(paramMap);

        var lambdaReturnLabel = LinqExpression.Label(typeof(object), "lambdaReturn");
        var lambdaReturnValue = LinqExpression.Variable(typeof(object), "lambdaReturnValue");
        blockVars.Add(lambdaReturnValue);
        _ctx.PushReturnContext(lambdaReturnLabel, lambdaReturnValue);

        try
        {
            var compiledBody = Compile(lambda.Body);
            statements.Add(LinqExpression.Assign(lambdaReturnValue, compiledBody));
        }
        finally
        {
            _ctx.CurrentContext = savedContext;
            _ctx.PopLambdaParams();
            _ctx.PopReturnContext();
        }

        statements.Add(LinqExpression.Label(lambdaReturnLabel, lambdaReturnValue));
        return LinqExpression.Block(typeof(object), blockVars, statements);
    }

    internal LinqExpression CompileArrayLiteral(ArrayLiteralExpr expr)
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
                var spreadValue = Compile(spread.Expression);
                statements.Add(LinqExpression.Call(CompilerContext.SpreadIntoListMethod, listVar, spreadValue));
            }
            else
            {
                statements.Add(LinqExpression.Call(listVar, CompilerContext.ListAddMethod, Compile(element)));
            }
        }

        statements.Add(LinqExpression.Call(CompilerContext.CreateTypedArrayMethod, listVar));
        return LinqExpression.Block(new[] { listVar }, statements);
    }

    internal LinqExpression CompileObjectLiteral(ObjectLiteralExpr expr) =>
        _extendedSyntax.CompileObjectLiteral(expr);

    internal LinqExpression CompileInterpolatedString(InterpolatedStringExpr expr)
    {
        var sbVar = LinqExpression.Variable(typeof(StringBuilder), "sb");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(sbVar, LinqExpression.New(CompilerContext.StringBuilderCtor))
        };

        foreach (var part in expr.Parts)
        {
            switch (part)
            {
                case TextPart text:
                    statements.Add(LinqExpression.Call(sbVar, CompilerContext.StringBuilderAppendMethod,
                        LinqExpression.Constant(text.Text)));
                    break;
                case ExpressionPart exprPart:
                    var value = Compile(exprPart.Expression);
                    if (exprPart.AlignmentSpecifier != null || exprPart.FormatSpecifier != null)
                    {
                        // Build format string like "{0,10:F2}" and call string.Format
                        var formatStr = "{0";
                        if (exprPart.AlignmentSpecifier != null) formatStr += "," + exprPart.AlignmentSpecifier;
                        if (exprPart.FormatSpecifier != null) formatStr += ":" + exprPart.FormatSpecifier;
                        formatStr += "}";
                        var formatted = LinqExpression.Call(
                            CompilerContext.StringFormatMethod,
                            LinqExpression.Constant(formatStr),
                            value);
                        statements.Add(LinqExpression.Call(sbVar, CompilerContext.StringBuilderAppendMethod, formatted));
                    }
                    else
                    {
                        var valueAsString = LinqExpression.Condition(
                            LinqExpression.Equal(value, LinqExpression.Constant(null, typeof(object))),
                            LinqExpression.Constant(""),
                            LinqExpression.Call(value, CompilerContext.ObjectToStringMethod));
                        statements.Add(LinqExpression.Call(sbVar, CompilerContext.StringBuilderAppendMethod, valueAsString));
                    }
                    break;
            }
        }

        statements.Add(LinqExpression.Convert(
            LinqExpression.Call(sbVar, CompilerContext.StringBuilderToStringMethod),
            typeof(object)));
        return LinqExpression.Block(new[] { sbVar }, statements);
    }

    internal LinqExpression CompileMemberAssign(MemberAssignExpr expr)
    {
        var target = Compile(expr.Object);
        var value = Compile(expr.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");

        return LinqExpression.Block(
            new[] { temp },
            LinqExpression.Assign(temp, value),
            LinqExpression.Call(CompilerContext.SetMemberMethod, target,
                LinqExpression.Constant(expr.Name.Lexeme), temp, _ctx.OptionsParam, _ctx.CurrentContext),
            temp);
    }

    internal LinqExpression CompileNullCoalesceAssign(NullCoalesceAssignExpr expr)
    {
        var name = expr.Name.Lexeme;
        var currentValue = CompileIdentifier(new IdentifierExpr(expr.Name));
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var result = LinqExpression.Variable(typeof(object), "result");

        var newValue = Compile(expr.Value);

        return LinqExpression.Block(
            new[] { temp, result },
            LinqExpression.Call(CompilerContext.CheckNullCoalesceAssignAllowedMethod,
                LinqExpression.Constant(name), _ctx.CurrentContext),
            LinqExpression.Assign(temp, currentValue),
            LinqExpression.IfThenElse(
                LinqExpression.NotEqual(temp, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Assign(result, temp),
                LinqExpression.Block(
                    LinqExpression.Call(CompilerContext.CheckAllowAssignmentMethod, _ctx.OptionsParam,
                        LinqExpression.Constant($"{name} ??= ...")),
                    LinqExpression.Assign(result, newValue),
                    LinqExpression.Call(_ctx.CurrentContext, CompilerContext.SetMethod,
                        LinqExpression.Constant(name), result))),
            result);
    }

    internal LinqExpression CompilePipeline(PipelineExpr expr) =>
        _extendedSyntax.CompilePipeline(expr);

    internal LinqExpression CompileRange(RangeExpr expr) =>
        _extendedSyntax.CompileRange(expr);

    internal LinqExpression CompileChainedComparison(Parsing.ChainedComparisonExpr expr) =>
        _extendedSyntax.CompileChainedComparison(expr);
}
