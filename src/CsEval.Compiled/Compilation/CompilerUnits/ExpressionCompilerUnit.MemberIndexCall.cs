using CsEval.Parsing;

namespace CsEval.Compiled.Compilation.CompilerUnits;

internal sealed partial class ExpressionCompilerUnit
{
    internal LinqExpression CompileMemberAccess(MemberAccessExpr m)
    {
        var directResult = _directEmit.TryEmitDirectMemberAccess(m);
        if (directResult != null)
            return directResult;

        var obj = Compile(m.Object);

        return LinqExpression.Call(
            CompilerContext.GetMemberMethod,
            obj,
            LinqExpression.Constant(m.Name.Lexeme),
            _ctx.OptionsParam,
            LinqExpression.Constant(m.NullSafe),
            _ctx.CurrentContext);
    }

    internal LinqExpression CompileIndexAccess(IndexAccessExpr expr)
    {
        var directResult = _directEmit.TryEmitDirectIndexAccess(expr);
        if (directResult != null)
            return directResult;

        var target = Compile(expr.Object);

        if (expr.NullSafe)
        {
            // arr?[i] - null-safe index access
            var targetVar = LinqExpression.Variable(typeof(object), "target");
            var index = Compile(expr.Index);
            return LinqExpression.Block(
                typeof(object),
                [targetVar],
                LinqExpression.Assign(targetVar, target),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    LinqExpression.Call(CompilerContext.GetIndexMethod, targetVar, index, _ctx.OptionsParam)));
        }

        var indexValue = Compile(expr.Index);
        return LinqExpression.Call(CompilerContext.GetIndexMethod, target, indexValue, _ctx.OptionsParam);
    }

    internal LinqExpression CompileSlice(SliceExpr expr)
    {
        var target = Compile(expr.Target);
        var start = expr.Start != null
            ? Compile(expr.Start)
            : LinqExpression.Constant(null, typeof(object));
        var end = expr.End != null
            ? Compile(expr.End)
            : LinqExpression.Constant(null, typeof(object));

        if (expr.Step != null)
        {
            var step = Compile(expr.Step);
            return LinqExpression.Call(CompilerContext.GetSliceStepMethod, target, start, end, step, _ctx.OptionsParam);
        }

        return LinqExpression.Call(CompilerContext.GetSliceMethod, target, start, end, _ctx.OptionsParam);
    }

    internal LinqExpression CompileCall(CallExpr call)
    {
        if (call.Callee is MemberAccessExpr ma)
        {
            var directResult = _directEmit.TryEmitDirectCall(call, ma);
            if (directResult != null) return directResult;
        }

        // Compile arguments into an object[] array, wrapping named arguments in NamedArg
        var argsVar = LinqExpression.Variable(typeof(object?[]), "args");
        var argsInit = LinqExpression.NewArrayInit(
            typeof(object),
            call.Arguments.Select(CompileArgument));

        var typeArgsExpr = call.TypeArguments != null
            ? LinqExpression.Constant(call.TypeArguments, typeof(IReadOnlyList<string>))
            : LinqExpression.Constant(null, typeof(IReadOnlyList<string>));

        // Check if any arguments are out parameters (need post-call variable definition)
        var hasOutArgs = call.Arguments.Any(a => a is OutArgExpr);

        LinqExpression callExpr;

        // Check if this is a member access call (target.Method(args))
        if (call.Callee is MemberAccessExpr memberAccess)
        {
            var target = Compile(memberAccess.Object);
            var methodName = memberAccess.Name.Lexeme;

            callExpr = LinqExpression.Call(
                CompilerContext.InvokeMemberCallMethod,
                target,
                LinqExpression.Constant(methodName),
                argsVar,
                LinqExpression.Constant(memberAccess.NullSafe),
                _ctx.CurrentContext,
                _ctx.OptionsParam,
                _ctx.CtParam,
                typeArgsExpr);
        }
        else if (call.Callee is IdentifierExpr calleeId &&
                 _ctx.TryGetLambdaParam(calleeId.Name.Lexeme, out var lambdaCallee))
        {
            // Lambda parameter used as callee: resolve from args[] and invoke as value
            callExpr = LinqExpression.Call(
                CompilerContext.InvokeCallMethod,
                lambdaCallee,
                argsVar,
                _ctx.CurrentContext,
                _ctx.OptionsParam,
                _ctx.CtParam,
                typeArgsExpr);
        }
        else if (call.Callee is IdentifierExpr calleeId2)
        {
            // Identifier call target: use direct dispatcher for both Standard and Extended modes.
            callExpr = LinqExpression.Call(
                CompilerContext.InvokeIdentifierCallMethod,
                LinqExpression.Constant(calleeId2.Name.Lexeme),
                argsVar,
                _ctx.CurrentContext,
                _ctx.OptionsParam,
                _ctx.CtParam,
                typeArgsExpr);
        }
        else
        {
            // General call: evaluate callee and invoke
            var callee = Compile(call.Callee);
            callExpr = LinqExpression.Call(
                CompilerContext.InvokeCallMethod,
                callee,
                argsVar,
                _ctx.CurrentContext,
                _ctx.OptionsParam,
                _ctx.CtParam,
                typeArgsExpr);
        }

        if (!hasOutArgs)
        {
            // Simple case: no out args, just call and return
            return LinqExpression.Block(
                new[] { argsVar },
                LinqExpression.Assign(argsVar, argsInit),
                callExpr);
        }

        // Out args case: call, then define out variables from modified args array
        var resultVar = LinqExpression.Variable(typeof(object), "callResult");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(argsVar, argsInit),
            LinqExpression.Assign(resultVar, callExpr)
        };

        // After the call, MethodInvoker.CopyBackOutArgs has replaced OutArgMarker entries
        // in argsVar with the actual values. Define variables for non-discard out args.
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            if (call.Arguments[i] is OutArgExpr { IsDiscard: false } outArg)
            {
                // Read the out value from argsVar[i]
                var outValue = LinqExpression.ArrayIndex(argsVar, LinqExpression.Constant(i));

                // Resolve type: if explicit type specified, use it; otherwise use runtime type
                LinqExpression typeExpr;
                if (outArg.TypeName != null)
                {
                    typeExpr = LinqExpression.Call(
                        _ctx.TypeResolverExpr,
                        CompilerContext.ResolveTypeMethod,
                        LinqExpression.Constant(outArg.TypeName));
                }
                else
                {
                    // typeof(object) — the runtime type will be set by the value itself
                    // Use a conditional: value?.GetType() ?? typeof(object)
                    var getTypeMethod = typeof(object).GetMethod(nameof(GetType))!;
                    typeExpr = LinqExpression.Condition(
                        LinqExpression.NotEqual(outValue, LinqExpression.Constant(null, typeof(object))),
                        LinqExpression.Call(outValue, getTypeMethod),
                        LinqExpression.Constant(typeof(object), typeof(Type)));
                }

                statements.Add(LinqExpression.Call(
                    _ctx.CurrentContext,
                    CompilerContext.DefineNewMethod,
                    LinqExpression.Constant(outArg.VariableName),
                    outValue,
                    typeExpr));
            }
        }

        // Return the call result
        statements.Add(resultVar);

        return LinqExpression.Block(
            new[] { argsVar, resultVar },
            statements);
    }

    /// <summary>
    /// Compiles an OutArgExpr to create an OutArgMarker at runtime.
    /// The marker flows through MethodInvoker which recognizes it for ByRef parameter handling.
    /// </summary>
    internal LinqExpression CompileOutArg(OutArgExpr outArg)
    {
        return LinqExpression.Convert(
            LinqExpression.New(
                CompilerContext.OutArgMarkerCtor,
                LinqExpression.Constant(outArg.VariableName),
                LinqExpression.Constant(outArg.TypeName, typeof(string)),
                LinqExpression.Constant(outArg.IsDiscard)),
            typeof(object));
    }

    private LinqExpression CompileArgument(Expr arg)
    {
        if (arg is NamedArgumentExpr namedArg)
        {
            // Wrap named argument in NamedArg: new NamedArg(name, value)
            return LinqExpression.Convert(
                LinqExpression.New(
                    CompilerContext.NamedArgCtor,
                    LinqExpression.Constant(namedArg.Name.Lexeme),
                    Compile(namedArg.Value)),
                typeof(object));
        }
        return Compile(arg);
    }
}
