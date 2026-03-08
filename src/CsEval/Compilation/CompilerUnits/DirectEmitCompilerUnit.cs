using System.Reflection;
using CsEval.Interpretation;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compilation;

/// <summary>
/// Compiles expression nodes to typed IL when the operand types are known at compile time,
/// bypassing runtime dispatch. Falls back to null when types are unknown or conditions aren't met,
/// letting ExpressionCompilerUnit use runtime dispatch instead.
/// </summary>
internal sealed partial class DirectEmitCompilerUnit
{
    private readonly CompilerContext _ctx;
    private ExpressionCompilerUnit? _exprUnit;

    private static readonly MethodInfo NormalizeIndexMethod =
        typeof(MemberAccess).GetMethod(nameof(MemberAccess.NormalizeIndex), [typeof(int), typeof(int), typeof(LanguageMode)])!;
    private static readonly MethodInfo ConvertToInt32ObjectMethod =
        typeof(Convert).GetMethod(nameof(Convert.ToInt32), [typeof(object)])!;
    private const string DirectIndexAccessGuardContext = "direct index access";

    internal DirectEmitCompilerUnit(CompilerContext ctx)
    {
        _ctx = ctx;
    }

    internal void SetExpressionUnit(ExpressionCompilerUnit exprUnit)
    {
        _exprUnit = exprUnit;
    }

    private LinqExpression Compile(Expr expr) => _exprUnit!.Compile(expr);

    private (LinqExpression Expression, Type KnownType) CompileTyped(Expr expr) => _exprUnit!.CompileTyped(expr);

    /// <summary>
    /// Resolves the target type, binding flags, and static/instance classification for
    /// direct-emit compilation from an AST object expression. Shared by TryEmitDirectCall
    /// and TryEmitDirectMemberAccess to eliminate duplicated type-resolution logic.
    /// </summary>
    private (Type TargetType, BindingFlags Flags, bool IsStatic)? ResolveDirectEmitTarget(Expr objectExpr)
    {
        var objectType = _ctx.TypeInferrer.Infer(objectExpr);

        Type? targetType;
        BindingFlags flags;
        bool isStatic;

        if (objectType == typeof(Type) && objectExpr is TypeReferenceExpr typeRef)
        {
            targetType = _ctx.Context.TypeResolver.TryResolveType(typeRef.TypeToken.Lexeme);
            flags = BindingFlags.Public | BindingFlags.Static;
            isStatic = true;
        }
        else if (objectType == typeof(Type) && objectExpr is IdentifierExpr idExpr)
        {
            targetType = _ctx.Context.TypeResolver.TryResolveType(idExpr.Name.Lexeme);
            flags = BindingFlags.Public | BindingFlags.Static;
            isStatic = true;
        }
        else if (objectType != typeof(object) && !objectType.IsArray)
        {
            targetType = objectType;
            flags = BindingFlags.Public | BindingFlags.Instance;
            isStatic = false;
        }
        else return null;

        if (!_ctx.Options.IsCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        if (targetType == null) return null;

        return (targetType, flags, isStatic);
    }

    internal bool TryFoldPureConstantExpression(Expr expr, out LinqExpression folded)
    {
        folded = null!;
        if (!IsPureConstantExpression(expr))
            return false;

        try
        {
            var evaluator = new Evaluator(_ctx.Context, _ctx.Options, CancellationToken.None);
            var value = evaluator.Evaluate(expr);
            folded = LinqExpression.Constant(value, typeof(object));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPureConstantExpression(Expr expr) => expr switch
    {
        LiteralExpr => true,
        UnaryExpr u => IsPureConstantExpression(u.Right),
        BinaryExpr b => IsPureConstantExpression(b.Left) && IsPureConstantExpression(b.Right),
        LogicalExpr l => IsPureConstantExpression(l.Left) && IsPureConstantExpression(l.Right),
        ConditionalExpr c => IsPureConstantExpression(c.Condition) &&
                             IsPureConstantExpression(c.ThenBranch) &&
                             IsPureConstantExpression(c.ElseBranch),
        NullCoalesceExpr n => IsPureConstantExpression(n.Left) && IsPureConstantExpression(n.Right),
        CastExpr c => IsPureConstantExpression(c.Expression),
        CheckedExpr c => IsPureConstantExpression(c.Expression),
        _ => false
    };


    internal LinqExpression? TryEmitDirectMemberAccess(MemberAccessExpr m)
    {
        if (m.Object is MemberAccessExpr or CallExpr or IndexAccessExpr)
            return TryEmitDirectChain(m);

        {
            var resolved = ResolveDirectEmitTarget(m.Object);
            if (resolved == null) return null;
            var (targetType, flags, isStatic) = resolved.Value;

            var prop = _ctx.Context.TypeCache.GetProperty(targetType, m.Name.Lexeme, flags);
            if (prop != null)
            {
                if (isStatic && !_ctx.Options.Sandbox.AllowStaticPropertyRead) return null;
                if (!isStatic && !_ctx.Options.Sandbox.AllowPropertyRead) return null;
                return EmitDirectAccess(m, isStatic, obj => LinqExpression.Property(obj, prop));
            }

            var field = _ctx.Context.TypeCache.GetField(targetType, m.Name.Lexeme, flags);
            if (field != null)
            {
                if (isStatic && !_ctx.Options.Sandbox.AllowStaticFieldRead) return null;
                if (!isStatic && !_ctx.Options.Sandbox.AllowPropertyRead) return null;
                return EmitDirectAccess(m, isStatic, obj => LinqExpression.Field(obj, field));
            }

            return null;
        }
    }

    private LinqExpression EmitDirectAccess(
        MemberAccessExpr m, bool isStatic, Func<LinqExpression?, LinqExpression> buildAccess)
    {
        if (isStatic)
            return LinqExpression.Convert(WrapGuard(buildAccess(null)), typeof(object));

        if (!m.NullSafe)
        {
            var obj = Compile(m.Object);
            var objectType = _ctx.TypeInferrer.Infer(m.Object);
            var typedObj = LinqExpression.Convert(obj, objectType);
            return LinqExpression.Convert(WrapGuard(buildAccess(typedObj)), typeof(object));
        }

        var objVar = LinqExpression.Variable(typeof(object), "nullSafeTarget");
        var objectType2 = _ctx.TypeInferrer.Infer(m.Object);
        var typedVar = LinqExpression.Convert(objVar, objectType2);
        return LinqExpression.Block(
            typeof(object),
            [objVar],
            LinqExpression.Assign(objVar, Compile(m.Object)),
                LinqExpression.Condition(
                    LinqExpression.Equal(objVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    LinqExpression.Convert(WrapGuard(buildAccess(typedVar)), typeof(object))));

        LinqExpression WrapGuard(LinqExpression value) =>
            WrapGuardedValue(value, value.Type, CreateMemberGuardContext(m.Name.Lexeme));
    }

    internal LinqExpression? TryEmitDirectCall(CallExpr call, MemberAccessExpr memberAccess)
    {
        if (memberAccess.Object is MemberAccessExpr or CallExpr or IndexAccessExpr)
            return TryEmitDirectChain(call);

        {
            if (call.TypeArguments is { Count: > 0 }) return null;
            if (call.Arguments.Any(a => a is NamedArgumentExpr or OutArgExpr)) return null;
            if (memberAccess.NullSafe) return null;
            if (!_ctx.Options.Sandbox.AllowMethodCalls) return null;

            Type? targetType;
            BindingFlags flags;
            bool isStatic;

        // Built-in static modules (e.g., Math, Convert): bind directly to static methods.
        if (memberAccess.Object is IdentifierExpr moduleId &&
            !_ctx.Context.Functions.ContainsKey(moduleId.Name.Lexeme) &&
            _ctx.Context.Modules.TryGetValue(moduleId.Name.Lexeme, out var moduleInfo) &&
            moduleInfo.Instance == null &&
            moduleInfo.Type is { IsAbstract: true, IsSealed: true })
        {
            targetType = moduleInfo.Type;
            flags = BindingFlags.Public | BindingFlags.Static;
            isStatic = true;

            if (!_ctx.Options.IsCaseSensitive)
                flags |= BindingFlags.IgnoreCase;
        }
        else
        {
            var resolved = ResolveDirectEmitTarget(memberAccess.Object);
            if (resolved == null) return null;
            (targetType, flags, isStatic) = resolved.Value;
        }

            if (targetType == null) return null;

        var argTypes = new Type[call.Arguments.Count];
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            argTypes[i] = _ctx.TypeInferrer.Infer(call.Arguments[i]);
            if (argTypes[i] == typeof(object) || argTypes[i].IsArray) return null;
        }

            var method = MethodResolver.TryResolveMethod(targetType, memberAccess.Name.Lexeme, argTypes, flags);
            if (method == null) return null;

        var parameters = method.GetParameters();
        var typedArgs = new LinqExpression[call.Arguments.Count];
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            var (compiled, _) = CompileTyped(call.Arguments[i]);
            if (compiled.Type == parameters[i].ParameterType)
            {
                typedArgs[i] = compiled;
            }
            else if (compiled.Type == typeof(object))
            {
                var coerced = LinqExpression.Call(
                    CompilerContext.CoerceNumericMethod,
                    compiled,
                    LinqExpression.Constant(parameters[i].ParameterType, typeof(Type)));
                typedArgs[i] = LinqExpression.Convert(coerced, parameters[i].ParameterType);
            }
            else
            {
                typedArgs[i] = LinqExpression.Convert(compiled, parameters[i].ParameterType);
            }
        }

        LinqExpression directCall;
        if (isStatic)
        {
            directCall = LinqExpression.Call(method, typedArgs);
        }
        else
        {
            var target = Compile(memberAccess.Object);
            var typedTarget = LinqExpression.Convert(target, targetType);
            directCall = LinqExpression.Call(typedTarget, method, typedArgs);
        }

        if (method.ReturnType == typeof(void))
            return LinqExpression.Block(directCall, LinqExpression.Constant(null, typeof(object)));

            return LinqExpression.Convert(
                WrapGuardedValue(
                    directCall,
                    method.ReturnType,
                    CreateMethodGuardContext(method.Name)),
                typeof(object));
        }
    }


    private static LinqExpression EnsureObjectExpression(LinqExpression expression) =>
        expression.Type == typeof(object)
            ? expression
            : LinqExpression.Convert(expression, typeof(object));

    private static LinqExpression EnsureTypedExpression(LinqExpression expression, Type targetType)
    {
        if (expression.Type == targetType)
            return expression;

        return LinqExpression.Convert(expression, targetType);
    }

    private static LinqExpression ConvertExpressionForParameter(LinqExpression expression, Type parameterType)
    {
        if (expression.Type == parameterType)
            return expression;

        return parameterType == typeof(object)
            ? EnsureObjectExpression(expression)
            : LinqExpression.Convert(expression, parameterType);
    }

    private static LinqExpression WrapGuardedValue(
        LinqExpression value,
        Type valueType,
        string context)
    {
        if (!TypeHelpers.RequiresReflectionLeakGuard(valueType))
            return value;

        return LinqExpression.Call(
            CompilerContext.GetGuardReflectionLeakTypedMethod(valueType),
            value,
            LinqExpression.Constant(context));
    }

    private static string CreateMemberGuardContext(string memberName) => $"direct member access {memberName}";

    private static string CreateMethodGuardContext(string methodName) => $"direct method call {methodName}";

}
