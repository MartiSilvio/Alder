using System.Collections;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation;

internal sealed partial class BoundExpressionEmitter
{
    private LinqExpression EmitDynamicIndexAccess(BoundDynamicIndexAccessExpr indexAccess)
    {
        var targetExpr = EmitHelpers.AsObject(Emit(indexAccess.Target));
        var indexExpr = EmitHelpers.AsObject(Emit(indexAccess.Index));

        if (!indexAccess.NullSafe)
        {
            return LinqExpression.Call(
                GetIndexMethod,
                targetExpr,
                indexExpr,
                _configParam,
                _contextParam);
        }

        var targetVar = LinqExpression.Variable(typeof(object), "indexTarget");
        return LinqExpression.Block(
            typeof(object),
            [targetVar],
            LinqExpression.Assign(targetVar, targetExpr),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    LinqExpression.Call(GetIndexMethod, targetVar, indexExpr, _configParam, _contextParam)));
    }

    private LinqExpression EmitIndexAccess(BoundResolvedIndexAccessExpr indexAccess)
    {
        if (indexAccess.IsDirectCollectionAccess)
            return EmitDirectCollectionIndexAccess(indexAccess);

        var targetExpr = EmitHelpers.AsObject(Emit(indexAccess.Target));
        var indexExpr = EmitHelpers.AsObject(Emit(indexAccess.Index));

        if (!indexAccess.NullSafe)
        {
            return LinqExpression.Call(
                GetIndexMethod,
                targetExpr,
                indexExpr,
                _configParam,
                _contextParam);
        }

        var targetVar = LinqExpression.Variable(typeof(object), "indexTarget");
        return LinqExpression.Block(
            typeof(object),
            [targetVar],
            LinqExpression.Assign(targetVar, targetExpr),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    LinqExpression.Call(GetIndexMethod, targetVar, indexExpr, _configParam, _contextParam)));
    }

    private LinqExpression EmitCall(BoundResolvedCallExpr call)
    {
        var chain = PostfixChain.TryCollect(call);
        if (chain != null) return EmitPostfixChain(chain.Value);
        return EmitCallWithTarget(call, null);
    }

    private LinqExpression EmitCallWithTarget(BoundResolvedCallExpr call, LinqExpression? emittedTarget)
    {
        var callExpr = call.Callee is BoundMethodGroupExpr
            ? EmitDirectPlannedCall(call, emittedTarget)
            : EmitInvokeCore(call.Callee, call.Arguments, ImmutableArray<string>.Empty, emittedTarget);

        return EmitCollectionSizeCheck(callExpr);
    }

    private LinqExpression EmitCollectionSizeCheck(LinqExpression callResult)
    {
        var resultType = callResult.Type;
        if (resultType != typeof(object) && (resultType.IsValueType || resultType == typeof(string)))
            return callResult;

        var resultVar = LinqExpression.Variable(typeof(object), "callResult");
        var securityPolicyExpr = LinqExpression.Property(_configParam, SecurityPolicyProperty);

        return LinqExpression.Block(
            typeof(object),
            [resultVar],
            LinqExpression.Assign(resultVar, EmitHelpers.AsObject(callResult)),
            LinqExpression.Call(CheckCollectionSizeMethod, resultVar, securityPolicyExpr),
            resultVar);
    }

    private LinqExpression EmitDirectCollectionIndexAccess(BoundResolvedIndexAccessExpr indexAccess)
    {
        if (indexAccess.TargetType == typeof(string))
            return EmitDirectStringIndexAccess(indexAccess);

        if (typeof(IList).IsAssignableFrom(indexAccess.TargetType))
            return EmitDirectListIndexAccess(indexAccess);

        return LinqExpression.Call(
            GetIndexMethod,
            EmitHelpers.AsObject(Emit(indexAccess.Target)),
            EmitHelpers.AsObject(Emit(indexAccess.Index)),
            _configParam,
            _contextParam);
    }

    private LinqExpression EmitDirectStringIndexAccess(BoundResolvedIndexAccessExpr indexAccess)
    {
        var targetObjVar = LinqExpression.Variable(typeof(object), "indexTarget");
        var typedTarget = EmitHelpers.EnsureTypedExpression(
            LinqExpression.Call(EnsureIndexTargetNotNullMethod, targetObjVar),
            typeof(string));
        var indexExpr = BuildNormalizedIntIndex(indexAccess, LinqExpression.Property(typedTarget, StringLengthProperty));
        var charExpr = LinqExpression.Property(typedTarget, StringCharsProperty, indexExpr);
        var valueExpr = LinqExpression.Convert(charExpr, typeof(object));

        if (indexAccess.NullSafe)
        {
            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(Emit(indexAccess.Target))),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    valueExpr));
        }

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar],
            LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(Emit(indexAccess.Target))),
            valueExpr);
    }

    private LinqExpression EmitDirectListIndexAccess(BoundResolvedIndexAccessExpr indexAccess)
    {
        var targetObjVar = LinqExpression.Variable(typeof(object), "listTarget");
        var checkedTarget = LinqExpression.Call(EnsureIndexTargetNotNullMethod, targetObjVar);

        LinqExpression typedTarget;
        LinqExpression countExpr;
        LinqExpression valueExpr;
        Type valueType;

        if (EmitHelpers.TryGetIntIndexer(indexAccess.TargetType, out var indexer) &&
            EmitHelpers.TryGetCountProperty(indexAccess.TargetType, out var countProperty))
        {
            typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, indexAccess.TargetType);
            countExpr = LinqExpression.Property(typedTarget, countProperty);
            var indexExpr = BuildNormalizedIntIndex(indexAccess, countExpr);
            valueExpr = LinqExpression.Property(typedTarget, indexer, indexExpr);
            valueType = indexer.PropertyType;
        }
        else
        {
            typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, typeof(IList));
            countExpr = LinqExpression.Property(
                EmitHelpers.EnsureTypedExpression(typedTarget, typeof(ICollection)),
                ICollectionCountProperty);
            var indexExpr = BuildNormalizedIntIndex(indexAccess, countExpr);
            valueExpr = LinqExpression.Property(typedTarget, IListIndexerProperty, indexExpr);
            valueType = typeof(object);
        }

        var guardedValueExpr = LinqExpression.Convert(
            EmitHelpers.WrapGuardedValue(valueExpr, valueType, "index access"),
            typeof(object));

        if (indexAccess.NullSafe)
        {
            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(Emit(indexAccess.Target))),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    guardedValueExpr));
        }

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar],
            LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(Emit(indexAccess.Target))),
            guardedValueExpr);
    }

    private LinqExpression BuildNormalizedIntIndex(BoundResolvedIndexAccessExpr indexAccess, LinqExpression lengthExpression)
    {
        if (indexAccess.Index is BoundLiteralExpr { Value: int literalIndex and >= 0 })
            return LinqExpression.Constant(literalIndex, typeof(int));

        var rawIndex = LinqExpression.Call(ConvertToInt32ObjectMethod, EmitHelpers.AsObject(Emit(indexAccess.Index)));
        var languageMode = LinqExpression.Property(_configParam, nameof(AlderConfig.LanguageMode));
        return LinqExpression.Call(NormalizeIndexMethod, rawIndex, lengthExpression, languageMode);
    }

    private LinqExpression EmitDirectPlannedCall(BoundResolvedCallExpr call, LinqExpression? emittedTarget = null)
    {
        var memberAccess = (BoundMethodGroupExpr)call.Callee;
        if (!EmitHelpers.CanEmitDirectMethodCall(call, call.Arguments.Length))
            return EmitInvokeCore(call.Callee, call.Arguments, ImmutableArray<string>.Empty, emittedTarget);

        var method = call.SelectedMethod;
        var parameters = MethodDispatchCache.GetParameters(method);
        var guardCheck = LinqExpression.Empty();
        var args = EmitPlannedCallArguments(call, parameters);

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
                LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(emittedTarget ?? Emit(memberAccess.Target))),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    nullSafeBody));
        }

        var targetVar = LinqExpression.Variable(targetType, "callTargetTyped");
        var assignTarget = LinqExpression.Assign(
            targetVar,
            EmitHelpers.EnsureTypedExpression(emittedTarget ?? Emit(memberAccess.Target), targetType));
        var ensureNonNullTarget = IsNonNullableValueType(targetType)
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

    private static bool IsNonNullableValueType(Type type)
    {
        return type.IsValueType && Nullable.GetUnderlyingType(type) == null;
    }


    private LinqExpression[] EmitPlannedCallArguments(BoundResolvedCallExpr call, ParameterInfo[] parameters)
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
                    emitted[paramIdx] = EmitCallArgument(call.Arguments[argIdx], conversion.TargetType);
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
                        var convertedArg = EmitCallArgument(call.Arguments[argIdx], conversion.TargetType);
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

    private LinqExpression EmitCallArgument(BoundExpr argument, Type targetType)
    {
        var emittedArgument = Emit(argument);
        if (targetType == typeof(object))
            return EmitHelpers.AsObject(emittedArgument);

        if (emittedArgument.Type == targetType)
            return emittedArgument;

        if (emittedArgument.Type == typeof(object))
        {
            var coerced = LinqExpression.Call(
                CompilerReflectionCache.CoerceNumericMethod,
                emittedArgument,
                LinqExpression.Constant(targetType, typeof(Type)));
            return LinqExpression.Convert(coerced, targetType);
        }

        return LinqExpression.Convert(emittedArgument, targetType);
    }


    private LinqExpression EmitInvoke(BoundDynamicCallExpr invoke)
    {
        var chain = PostfixChain.TryCollect(invoke);
        if (chain != null) return EmitPostfixChain(chain.Value);
        return EmitCollectionSizeCheck(
            EmitInvokeCore(invoke.Callee, invoke.Arguments, invoke.TypeArguments));
    }

    private LinqExpression EmitPostfixChain(PostfixChain.Chain chain)
    {
        var result = Emit(chain.Root);
        for (var i = chain.Segments.Count - 1; i >= 0; i--)
        {
            var seg = chain.Segments[i];
            if (seg.CallOrInvoke is BoundResolvedCallExpr call)
                result = EmitCallWithTarget(call, result);
            else if (seg.CallOrInvoke is BoundDynamicCallExpr invoke)
                result = EmitCollectionSizeCheck(
                    EmitInvokeCore(invoke.Callee, invoke.Arguments, invoke.TypeArguments, result));
            else
                result = Emission.Emitters.MemberAccessEmitter.EmitWithTarget(seg.MemberAccess, result, _emissionCtx);
        }
        return result;
    }

    private LinqExpression EmitInvokeCore(
        BoundExpr callee,
        ImmutableArray<BoundExpr> arguments,
        ImmutableArray<string> typeArguments,
        LinqExpression? emittedCalleeTarget = null)
    {
        var argsVar = LinqExpression.Variable(typeof(object?[]), "args");
        var argsInit = LinqExpression.NewArrayInit(
            typeof(object),
            arguments.Select(argument => EmitHelpers.AsObject(Emit(argument))));
        var emittedTypeArguments = EmitTypeArguments(typeArguments);
        var outBindings = EmitHelpers.CollectOutBindings(arguments);

        LinqExpression invokeExpr;
        if (callee is BoundIdentifierExpr identifier)
        {
            invokeExpr = LinqExpression.Call(
                InvokeIdentifierCallMethod,
                LinqExpression.Constant(identifier.Name),
                argsVar,
                _contextParam,
                _configParam,
                emittedTypeArguments,
                _ctParam);
        }
        else if (callee is BoundMemberAccessBase memberAccess)
        {
            invokeExpr = LinqExpression.Call(
                InvokeMemberCallMethod,
                EmitHelpers.AsObject(emittedCalleeTarget ?? Emit(memberAccess.Target)),
                LinqExpression.Constant(memberAccess.MemberName),
                argsVar,
                LinqExpression.Constant(memberAccess.NullSafe),
                _contextParam,
                _configParam,
                emittedTypeArguments,
                _ctParam);
        }
        else
        {
            invokeExpr = LinqExpression.Call(
                InvokeCallMethod,
                EmitHelpers.AsObject(Emit(callee)),
                argsVar,
                _contextParam,
                _configParam,
                emittedTypeArguments,
                _ctParam);
        }

        if (outBindings.Length == 0)
        {
            return LinqExpression.Block(
                new[] { argsVar },
                LinqExpression.Assign(argsVar, argsInit),
                invokeExpr);
        }

        var resultVar = LinqExpression.Variable(typeof(object), "invokeResult");
        return LinqExpression.Block(
            new[] { argsVar, resultVar },
            LinqExpression.Assign(argsVar, argsInit),
            LinqExpression.Assign(resultVar, invokeExpr),
            LinqExpression.Call(
                DefineOutVariablesMethod,
                argsVar,
                LinqExpression.Constant(outBindings, typeof(IReadOnlyList<OutVariableBinding>)),
                _contextParam),
            resultVar);
    }

    private static LinqExpression EmitTypeArguments(ImmutableArray<string> typeArguments)
    {
        if (typeArguments.IsDefaultOrEmpty)
            return LinqExpression.Constant(null, typeof(IReadOnlyList<string>));

        return LinqExpression.Constant(typeArguments.ToArray(), typeof(IReadOnlyList<string>));
    }
}
