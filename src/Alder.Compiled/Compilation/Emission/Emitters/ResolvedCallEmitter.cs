using System.Collections.Immutable;
using System.Linq;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.ResolvedCall)]
internal static class ResolvedCallEmitter
{
    public static LinqExpression Emit(BoundResolvedCallExpr node, EmissionContext ctx)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return EmitPostfixChain(chain.Value, ctx);
        return EmitWithTarget(node, null, ctx);
    }

    internal static LinqExpression EmitWithTarget(BoundResolvedCallExpr call, LinqExpression? emittedTarget, EmissionContext ctx)
    {
        var callExpr = call.Callee is BoundMethodGroupExpr
            ? EmitDirectPlannedCall(call, emittedTarget, ctx)
            : DynamicCallEmitter.EmitInvokeCore(call.Callee, call.Arguments, ImmutableArray<string>.Empty, emittedTarget, ctx);

        return EmitCollectionSizeCheck(callExpr, ctx);
    }

    internal static LinqExpression EmitPostfixChain(PostfixChain.Chain chain, EmissionContext ctx)
    {
        var result = ctx.Emit(chain.Root);
        for (var i = chain.Segments.Count - 1; i >= 0; i--)
        {
            var seg = chain.Segments[i];
            if (seg.CallOrInvoke is BoundResolvedCallExpr call)
            {
                if (call.IsExtensionCall && call.Callee is BoundMethodGroupExpr { NullSafe: true })
                {
                    var receiverVar = LinqExpression.Variable(typeof(object), "nsExtRcv");
                    var innerResult = EmitWithTarget(call, receiverVar, ctx);
                    result = LinqExpression.Block(
                        typeof(object),
                        [receiverVar],
                        LinqExpression.Assign(receiverVar, EmitHelpers.AsObject(result)),
                        LinqExpression.Condition(
                            LinqExpression.Equal(receiverVar, LinqExpression.Constant(null, typeof(object))),
                            LinqExpression.Constant(null, typeof(object)),
                            EmitHelpers.AsObject(innerResult)));
                }
                else
                    result = EmitWithTarget(call, result, ctx);
            }
            else if (seg.CallOrInvoke is BoundDynamicCallExpr invoke)
                result = EmitCollectionSizeCheck(
                    DynamicCallEmitter.EmitInvokeCore(invoke.Callee, invoke.Arguments, invoke.TypeArguments, result, ctx),
                    ctx);
            else
                result = MemberAccessEmitter.EmitWithTarget(seg.MemberAccess, result, ctx);
        }
        return result;
    }

    internal static LinqExpression EmitCollectionSizeCheck(LinqExpression callResult, EmissionContext ctx)
    {
        var resultType = callResult.Type;
        if (resultType != typeof(object) && (resultType.IsValueType || resultType == typeof(string)))
            return callResult;

        var resultVar = LinqExpression.Variable(resultType, "callResult");
        var securityPolicyExpr = LinqExpression.Property(ctx.ConfigParam, SecurityPolicyProperty);

        return LinqExpression.Block(
            resultType,
            [resultVar],
            LinqExpression.Assign(resultVar, callResult),
            LinqExpression.Call(CheckCollectionSizeMethod, EmitHelpers.AsObject(resultVar), securityPolicyExpr),
            resultVar);
    }

    private static LinqExpression EmitDirectPlannedCall(BoundResolvedCallExpr call, LinqExpression? emittedTarget, EmissionContext ctx)
    {
        var memberAccess = (BoundMethodGroupExpr)call.Callee;
        if (!EmitHelpers.CanEmitDirectMethodCall(call, call.Arguments.Length))
            return DynamicCallEmitter.EmitInvokeCore(call.Callee, call.Arguments, ImmutableArray<string>.Empty, emittedTarget, ctx);

        var method = call.SelectedMethod;
        var parameters = MethodDispatchCache.GetParameters(method);
        var guardCheck = LinqExpression.Empty();
        var extensionReceiver = call.IsExtensionCall && emittedTarget != null ? emittedTarget : null;
        var args = EmitPlannedCallArguments(call, parameters, ctx, extensionReceiver);

        if (!call.IsExtensionCall)
        {
            if (ctx.PreferResolvedRuntimeDispatch)
            {
                var objectArgs = LinqExpression.NewArrayInit(typeof(object), args.Select(EmitHelpers.AsObject));
                var targetExpr = call.IsStaticCall
                    ? LinqExpression.Constant(null, typeof(object))
                    : EmitHelpers.AsObject(emittedTarget ?? ctx.Emit(memberAccess.Target));
                var runtimeCall = LinqExpression.Call(
                    InvokeResolvedMethodMethod,
                    LinqExpression.Constant(method, typeof(MethodInfo)),
                    targetExpr,
                    objectArgs,
                    ctx.ContextParam);

                if (method.ReturnType == typeof(void))
                {
                    return LinqExpression.Block(
                        typeof(object),
                        guardCheck,
                        runtimeCall,
                        LinqExpression.Constant(null, typeof(object)));
                }

                return memberAccess.NullSafe
                    ? runtimeCall
                    : LinqExpression.Block(
                        method.ReturnType,
                        guardCheck,
                        EmitHelpers.EnsureTypedExpression(runtimeCall, method.ReturnType));
            }

            return EmitDirectReflectionCall(call, emittedTarget, ctx, method, args, guardCheck, memberAccess);
        }

        return EmitDirectReflectionCall(call, emittedTarget, ctx, method, args, guardCheck, memberAccess);
    }

    private static LinqExpression EmitDirectReflectionCall(
        BoundResolvedCallExpr call,
        LinqExpression? emittedTarget,
        EmissionContext ctx,
        MethodInfo method,
        LinqExpression[] args,
        LinqExpression guardCheck,
        BoundMethodGroupExpr memberAccess)
    {
        if (call.IsStaticCall)
        {
            var staticCall = LinqExpression.Call(method, args);
            if (method.ReturnType == typeof(void))
                return LinqExpression.Block(guardCheck, staticCall, LinqExpression.Constant(null, typeof(object)));

            return LinqExpression.Block(
                method.ReturnType,
                guardCheck,
                EmitHelpers.WrapGuardedValue(staticCall, method.ReturnType, EmitHelpers.CreateMethodGuardContext(method.Name)));
        }

        var targetType = method.DeclaringType ?? memberAccess.DeclaringType;
        var targetObjVar = LinqExpression.Variable(typeof(object), "callTarget");
        var checkedTarget = LinqExpression.Call(
            EnsureCallTargetNotNullMethod,
            targetObjVar,
            LinqExpression.Constant(method.Name));
        var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, targetType);
        var instanceCall = LinqExpression.Call(typedTarget, method, args);

        if (memberAccess.NullSafe)
        {
            var nullSafeBody = method.ReturnType == typeof(void)
                ? (LinqExpression)LinqExpression.Block(
                    typeof(object),
                    guardCheck,
                    instanceCall,
                    LinqExpression.Constant(null, typeof(object)))
                : LinqExpression.Convert(
                    LinqExpression.Block(
                        method.ReturnType,
                        guardCheck,
                        EmitHelpers.WrapGuardedValue(instanceCall, method.ReturnType, EmitHelpers.CreateMethodGuardContext(method.Name))),
                    typeof(object));

            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(emittedTarget ?? ctx.Emit(memberAccess.Target))),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    nullSafeBody));
        }

        var targetVar = LinqExpression.Variable(targetType, "callTargetTyped");
        var assignTarget = LinqExpression.Assign(
            targetVar,
            EmitHelpers.EnsureTypedExpression(emittedTarget ?? ctx.Emit(memberAccess.Target), targetType));
        // Value-type targets can never be null once unboxed: non-nullable structs are always
        // present, and Nullable<T> method calls are well-defined on the default value.
        var ensureNonNullTarget = targetType.IsValueType
            ? (LinqExpression)LinqExpression.Empty()
            : LinqExpression.Call(
                EnsureCallTargetNotNullMethod,
                EmitHelpers.AsObject(targetVar),
                LinqExpression.Constant(method.Name));
        var directInstanceCall = LinqExpression.Call(targetVar, method, args);

        if (method.ReturnType == typeof(void))
        {
            return LinqExpression.Block(
                typeof(object),
                [targetVar],
                guardCheck,
                assignTarget,
                ensureNonNullTarget,
                directInstanceCall,
                LinqExpression.Constant(null, typeof(object)));
        }

        return LinqExpression.Block(
            method.ReturnType,
            [targetVar],
            guardCheck,
            assignTarget,
            ensureNonNullTarget,
            EmitHelpers.WrapGuardedValue(directInstanceCall, method.ReturnType, EmitHelpers.CreateMethodGuardContext(method.Name)));
    }

    private static LinqExpression[] EmitPlannedCallArguments(
        BoundResolvedCallExpr call, ParameterInfo[] parameters, EmissionContext ctx,
        LinqExpression? extensionReceiver = null)
    {
        var emitted = new LinqExpression[parameters.Length];
        var resolved = call.Resolution;
        var sources = resolved.ArgMap.Sources;
        var conversions = resolved.Conversions;

        for (var paramIdx = 0; paramIdx < sources.Length; paramIdx++)
        {
            var source = sources[paramIdx];
            switch (source.Kind)
            {
                case ParameterSourceKind.Argument:
                {
                    var argIdx = source.ArgumentIndex;
                    var conversion = conversions[argIdx];
                    if (call.IsExtensionCall && argIdx == 0)
                    {
                        var receiverExpr = extensionReceiver != null
                            ? EmitHelpers.AsObject(extensionReceiver)
                            : EmitHelpers.AsObject(ctx.Emit(call.Arguments[argIdx]));
                        var coerced = LinqExpression.Call(EnsureEnumerableMethod, receiverExpr);
                        emitted[paramIdx] = EmitHelpers.EnsureTypedExpression(coerced, conversion.TargetType);
                        break;
                    }
                    emitted[paramIdx] = EmitCallArgument(call.Arguments[argIdx], conversion, ctx);
                    break;
                }

                case ParameterSourceKind.Default:
                {
                    emitted[paramIdx] = EmitDefaultArgument(parameters[paramIdx]);
                    break;
                }

                case ParameterSourceKind.ParamsRange:
                {
                    var parameter = parameters[paramIdx];
                    var elementType = parameter.ParameterType.GetElementType()
                                     ?? throw new BindingNotSupportedException("Params parameter must be an array type.");
                    var args = new LinqExpression[source.ParamsCount];

                    for (var i = 0; i < source.ParamsCount; i++)
                    {
                        var argIdx = source.ParamsStartIndex + i;
                        var conversion = conversions[argIdx];
                        var convertedArg = EmitCallArgument(call.Arguments[argIdx], conversion, ctx);
                        args[i] = EmitHelpers.EnsureTypedExpression(convertedArg, elementType);
                    }

                    emitted[paramIdx] = LinqExpression.NewArrayInit(elementType, args);
                    break;
                }

                default:
                    throw new BindingNotSupportedException(
                        $"Parameter source kind '{source.Kind}' is not implemented");
            }
        }

        for (var i = 0; i < emitted.Length; i++)
        {
            if (emitted[i] == null)
                throw new BindingNotSupportedException($"No emitted argument for parameter index {i}.");
        }

        return emitted;
    }

    private static LinqExpression EmitDefaultArgument(ParameterInfo parameter)
    {
        var parameterType = parameter.ParameterType;
        var defaultValue = parameter.DefaultValue;

        if (defaultValue == Type.Missing || defaultValue == DBNull.Value)
            return LinqExpression.Default(parameterType);

        return LinqExpression.Constant(defaultValue, parameterType);
    }

    private static LinqExpression EmitCallArgument(BoundExpr argument, ArgumentConversion conversion, EmissionContext ctx)
    {
        var targetType = conversion.TargetType;
        var emittedArgument = ctx.Emit(argument);
        if (targetType == typeof(object))
            return EmitHelpers.AsObject(emittedArgument);

        if (emittedArgument.Type == targetType)
            return emittedArgument;

        if (conversion.Kind == ArgumentConversionKind.LambdaToDelegate)
        {
            var convertCall = LinqExpression.Call(
                LambdaDelegateTryConvertMethod,
                EmitHelpers.AsObject(emittedArgument),
                LinqExpression.Constant(targetType, typeof(Type)));
            return LinqExpression.Convert(convertCall, targetType);
        }

        if (emittedArgument.Type == typeof(object))
        {
            var coerced = LinqExpression.Call(
                CoerceNumericMethod,
                emittedArgument,
                LinqExpression.Constant(targetType, typeof(Type)));
            return LinqExpression.Convert(coerced, targetType);
        }

        return LinqExpression.Convert(emittedArgument, targetType);
    }

}
