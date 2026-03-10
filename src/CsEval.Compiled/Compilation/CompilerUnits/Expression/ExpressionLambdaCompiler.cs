using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compiled.Compilation.CompilerUnits;

internal sealed class ExpressionLambdaCompiler
{
    private readonly ExpressionCompilerUnit _owner;

    internal ExpressionLambdaCompiler(ExpressionCompilerUnit owner)
    {
        _owner = owner;
    }

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
            CompilerReflectionCache.CompiledLambdaValueCtor,
            listInit,
            compiledDelegate,
            _owner.Context.CurrentContext,
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
                LinqExpression.Call(closureParam, CompilerReflectionCache.CreateChildMethod)));
        }

        var savedContext = _owner.Context.CurrentContext;
        _owner.Context.CurrentContext = needsChildContext ? childContextVar! : closureParam;
        _owner.Context.PushLambdaParams(paramMap);

        var lambdaReturnLabel = LinqExpression.Label(typeof(object), "lambdaReturn");
        var lambdaReturnValue = LinqExpression.Variable(typeof(object), "lambdaReturnValue");
        blockVars.Add(lambdaReturnValue);
        _owner.Context.PushReturnContext(lambdaReturnLabel, lambdaReturnValue);

        try
        {
            var compiledBody = _owner.Compile(lambda.Body);
            statements.Add(LinqExpression.Assign(lambdaReturnValue, compiledBody));
        }
        finally
        {
            _owner.Context.CurrentContext = savedContext;
            _owner.Context.PopLambdaParams();
            _owner.Context.PopReturnContext();
        }

        statements.Add(LinqExpression.Label(lambdaReturnLabel, lambdaReturnValue));
        return LinqExpression.Block(typeof(object), blockVars, statements);
    }

    internal LinqExpression CompileArrayLiteral(ArrayLiteralExpr expr)
    {
        var listVar = LinqExpression.Variable(typeof(List<object?>), "list");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(listVar, LinqExpression.New(CompilerReflectionCache.ListCtor))
        };

        foreach (var element in expr.Elements)
        {
            if (element is SpreadExpr spread)
            {
                var spreadValue = _owner.Compile(spread.Expression);
                statements.Add(LinqExpression.Call(CompilerReflectionCache.SpreadIntoListMethod, listVar, spreadValue));
            }
            else
            {
                statements.Add(LinqExpression.Call(listVar, CompilerReflectionCache.ListAddMethod, _owner.Compile(element)));
            }
        }

        statements.Add(LinqExpression.Call(CompilerReflectionCache.CreateTypedArrayMethod, listVar));
        return LinqExpression.Block(new[] { listVar }, statements);
    }

    internal LinqExpression CompileObjectLiteral(ObjectLiteralExpr expr) =>
        _owner.ExtendedSyntax.CompileObjectLiteral(expr);

    internal LinqExpression CompileInterpolatedString(InterpolatedStringExpr expr)
    {
        var sbVar = LinqExpression.Variable(typeof(StringBuilder), "sb");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(sbVar, LinqExpression.New(CompilerReflectionCache.StringBuilderCtor))
        };

        foreach (var part in expr.Parts)
        {
            switch (part)
            {
                case TextPart text:
                    statements.Add(LinqExpression.Call(sbVar, CompilerReflectionCache.StringBuilderAppendMethod,
                        LinqExpression.Constant(text.Text)));
                    break;
                case ExpressionPart exprPart:
                    var value = _owner.Compile(exprPart.Expression);
                    if (exprPart.AlignmentSpecifier != null || exprPart.FormatSpecifier != null)
                    {
                        // Build format string like "{0,10:F2}" and call string.Format
                        var formatStr = "{0";
                        if (exprPart.AlignmentSpecifier != null) formatStr += "," + exprPart.AlignmentSpecifier;
                        if (exprPart.FormatSpecifier != null) formatStr += ":" + exprPart.FormatSpecifier;
                        formatStr += "}";
                        var formatted = LinqExpression.Call(
                            CompilerReflectionCache.StringFormatMethod,
                            LinqExpression.Constant(formatStr),
                            value);
                        statements.Add(LinqExpression.Call(sbVar, CompilerReflectionCache.StringBuilderAppendMethod, formatted));
                    }
                    else
                    {
                        var valueAsString = LinqExpression.Condition(
                            LinqExpression.Equal(value, LinqExpression.Constant(null, typeof(object))),
                            LinqExpression.Constant(""),
                            LinqExpression.Call(value, CompilerReflectionCache.ObjectToStringMethod));
                        statements.Add(LinqExpression.Call(sbVar, CompilerReflectionCache.StringBuilderAppendMethod, valueAsString));
                    }
                    break;
            }
        }

        statements.Add(LinqExpression.Convert(
            LinqExpression.Call(sbVar, CompilerReflectionCache.StringBuilderToStringMethod),
            typeof(object)));
        return LinqExpression.Block(new[] { sbVar }, statements);
    }

    internal LinqExpression CompileMemberAssign(MemberAssignExpr expr)
    {
        var target = _owner.Compile(expr.Object);
        var value = _owner.Compile(expr.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");

        return LinqExpression.Block(
            new[] { temp },
            LinqExpression.Assign(temp, value),
            LinqExpression.Call(CompilerReflectionCache.SetMemberMethod, target,
                LinqExpression.Constant(expr.Name.Lexeme), temp, _owner.Context.OptionsParam, _owner.Context.CurrentContext),
            temp);
    }

    internal LinqExpression CompileNullCoalesceAssign(NullCoalesceAssignExpr expr)
    {
        var name = expr.Name.Lexeme;
        var currentValue = _owner.CompileIdentifier(new IdentifierExpr(expr.Name));
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var result = LinqExpression.Variable(typeof(object), "result");

        var newValue = _owner.Compile(expr.Value);

        return LinqExpression.Block(
            new[] { temp, result },
            LinqExpression.Call(CompilerReflectionCache.CheckNullCoalesceAssignAllowedMethod,
                LinqExpression.Constant(name), _owner.Context.CurrentContext),
            LinqExpression.Assign(temp, currentValue),
            LinqExpression.IfThenElse(
                LinqExpression.NotEqual(temp, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Assign(result, temp),
                LinqExpression.Block(
                    LinqExpression.Call(CompilerReflectionCache.CheckAllowAssignmentMethod, _owner.Context.OptionsParam,
                        LinqExpression.Constant($"{name} ??= ...")),
                    LinqExpression.Assign(result, newValue),
                    LinqExpression.Call(_owner.Context.CurrentContext, CompilerReflectionCache.SetMethod,
                        LinqExpression.Constant(name), result))),
            result);
    }

    internal LinqExpression CompilePipeline(PipelineExpr expr) =>
        _owner.ExtendedSyntax.CompilePipeline(expr);

    internal LinqExpression CompileRange(RangeExpr expr) =>
        _owner.ExtendedSyntax.CompileRange(expr);

    internal LinqExpression CompileChainedComparison(Parsing.ChainedComparisonExpr expr) =>
        _owner.ExtendedSyntax.CompileChainedComparison(expr);
}
