using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compiled.Compilation.CompilerUnits;

internal sealed class ExpressionMemberCallCompiler
{
    private readonly ExpressionCompilerUnit _owner;

    internal ExpressionMemberCallCompiler(ExpressionCompilerUnit owner)
    {
        _owner = owner;
    }

    internal LinqExpression CompileMemberAccess(MemberAccessExpr m)
    {
        var directResult = _owner.DirectEmit.TryEmitDirectMemberAccess(m);
        if (directResult != null)
            return directResult;

        var obj = _owner.Compile(m.Object);

        return LinqExpression.Call(
            CompilerReflectionCache.GetMemberMethod,
            obj,
            LinqExpression.Constant(m.Name.Lexeme),
            _owner.Context.OptionsParam,
            LinqExpression.Constant(m.NullSafe),
            _owner.Context.CurrentContext);
    }

    internal LinqExpression CompileIndexAccess(IndexAccessExpr expr)
    {
        var directResult = _owner.DirectEmit.TryEmitDirectIndexAccess(expr);
        if (directResult != null)
            return directResult;

        var target = _owner.Compile(expr.Object);

        if (expr.NullSafe)
        {
            // arr?[i] - null-safe index access
            var targetVar = LinqExpression.Variable(typeof(object), "target");
            var index = _owner.Compile(expr.Index);
            return LinqExpression.Block(
                typeof(object),
                [targetVar],
                LinqExpression.Assign(targetVar, target),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    LinqExpression.Call(CompilerReflectionCache.GetIndexMethod, targetVar, index, _owner.Context.OptionsParam)));
        }

        var indexValue = _owner.Compile(expr.Index);
        return LinqExpression.Call(CompilerReflectionCache.GetIndexMethod, target, indexValue, _owner.Context.OptionsParam);
    }

    internal LinqExpression CompileSlice(SliceExpr expr)
    {
        var target = _owner.Compile(expr.Target);
        var start = expr.Start != null
            ? _owner.Compile(expr.Start)
            : LinqExpression.Constant(null, typeof(object));
        var end = expr.End != null
            ? _owner.Compile(expr.End)
            : LinqExpression.Constant(null, typeof(object));

        if (expr.Step != null)
        {
            var step = _owner.Compile(expr.Step);
            return LinqExpression.Call(CompilerReflectionCache.GetSliceStepMethod, target, start, end, step, _owner.Context.OptionsParam);
        }

        return LinqExpression.Call(CompilerReflectionCache.GetSliceMethod, target, start, end, _owner.Context.OptionsParam);
    }

    internal LinqExpression CompileCall(CallExpr call)
    {
        if (call.Callee is MemberAccessExpr ma)
        {
            var directResult = _owner.DirectEmit.TryEmitDirectCall(call, ma);
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

        var outBindings = CollectOutBindings(call.Arguments);
        var hasOutArgs = outBindings.Length > 0;

        LinqExpression callExpr;

        // Check if this is a member access call (target.Method(args))
        if (call.Callee is MemberAccessExpr memberAccess)
        {
            var target = _owner.Compile(memberAccess.Object);
            var methodName = memberAccess.Name.Lexeme;

            callExpr = LinqExpression.Call(
                CompilerReflectionCache.InvokeMemberCallMethod,
                target,
                LinqExpression.Constant(methodName),
                argsVar,
                LinqExpression.Constant(memberAccess.NullSafe),
                _owner.Context.CurrentContext,
                _owner.Context.OptionsParam,
                _owner.Context.CtParam,
                typeArgsExpr);
        }
        else if (call.Callee is IdentifierExpr calleeId &&
                 _owner.Context.TryGetLambdaParam(calleeId.Name.Lexeme, out var lambdaCallee))
        {
            // Lambda parameter used as callee: resolve from args[] and invoke as value
            callExpr = LinqExpression.Call(
                CompilerReflectionCache.InvokeCallMethod,
                lambdaCallee,
                argsVar,
                _owner.Context.CurrentContext,
                _owner.Context.OptionsParam,
                _owner.Context.CtParam,
                typeArgsExpr);
        }
        else if (call.Callee is IdentifierExpr calleeId2)
        {
            // Identifier call target: use direct dispatcher for both Standard and Extended modes.
            callExpr = LinqExpression.Call(
                CompilerReflectionCache.InvokeIdentifierCallMethod,
                LinqExpression.Constant(calleeId2.Name.Lexeme),
                argsVar,
                _owner.Context.CurrentContext,
                _owner.Context.OptionsParam,
                _owner.Context.CtParam,
                typeArgsExpr);
        }
        else
        {
            // General call: evaluate callee and invoke
            var callee = _owner.Compile(call.Callee);
            callExpr = LinqExpression.Call(
                CompilerReflectionCache.InvokeCallMethod,
                callee,
                argsVar,
                _owner.Context.CurrentContext,
                _owner.Context.OptionsParam,
                _owner.Context.CtParam,
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
            LinqExpression.Assign(resultVar, callExpr),
            LinqExpression.Call(
                CompilerReflectionCache.DefineOutVariablesMethod,
                argsVar,
                LinqExpression.Constant(outBindings, typeof(IReadOnlyList<OutVariableBinding>)),
                _owner.Context.CurrentContext)
        };

        // Return the call result
        statements.Add(resultVar);

        return LinqExpression.Block(
            new[] { argsVar, resultVar },
            statements);
    }

    private static OutVariableBinding[] CollectOutBindings(IReadOnlyList<Expr> arguments)
    {
        if (arguments.Count == 0)
            return [];

        List<OutVariableBinding>? bindings = null;
        for (var i = 0; i < arguments.Count; i++)
        {
            if (arguments[i] is OutArgExpr { IsDiscard: false } outArg)
            {
                bindings ??= [];
                bindings.Add(new OutVariableBinding(i, outArg.VariableName, outArg.TypeName));
            }
        }

        return bindings?.ToArray() ?? [];
    }

    /// <summary>
    /// Compiles an OutArgExpr to create an OutArgMarker at runtime.
    /// The marker flows through MethodInvoker which recognizes it for ByRef parameter handling.
    /// </summary>
    internal LinqExpression CompileOutArg(OutArgExpr outArg)
    {
        return LinqExpression.Convert(
            LinqExpression.New(
                CompilerReflectionCache.OutArgMarkerCtor,
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
                    CompilerReflectionCache.NamedArgCtor,
                    LinqExpression.Constant(namedArg.Name.Lexeme),
                    _owner.Compile(namedArg.Value)),
                typeof(object));
        }
        return _owner.Compile(arg);
    }
}
