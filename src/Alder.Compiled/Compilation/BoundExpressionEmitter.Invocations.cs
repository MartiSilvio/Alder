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
    private LinqExpression EmitPropertyAccess(BoundPropertyAccessExpr node)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return EmitPostfixChain(chain.Value);
        return EmitPropertyAccessWithTarget(node, Emit(node.Target));
    }

    private LinqExpression EmitPropertyAccessWithTarget(BoundPropertyAccessExpr node, LinqExpression emittedTarget)
    {
        var declaringType = node.Property.DeclaringType;
        if (declaringType == null || !TypeHelpers.IsValueTupleType(declaringType))
            return EmitDirectPropertyAccess(node, emittedTarget);

        return EmitDynamicGetMember(node.MemberName, node.NullSafe, emittedTarget);
    }

    private LinqExpression EmitFieldAccess(BoundFieldAccessExpr node)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return EmitPostfixChain(chain.Value);
        return EmitFieldAccessWithTarget(node, Emit(node.Target));
    }

    private LinqExpression EmitFieldAccessWithTarget(BoundFieldAccessExpr node, LinqExpression emittedTarget)
    {
        var declaringType = node.Field.DeclaringType;
        if (declaringType == null || !TypeHelpers.IsValueTupleType(declaringType))
            return EmitDirectFieldAccess(node, emittedTarget);

        return EmitDynamicGetMember(node.MemberName, node.NullSafe, emittedTarget);
    }

    private LinqExpression EmitMethodGroup(BoundMethodGroupExpr node)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return EmitPostfixChain(chain.Value);
        return EmitDynamicGetMember(node.MemberName, node.NullSafe, Emit(node.Target));
    }

    private LinqExpression EmitDynamicMemberAccess(BoundDynamicMemberAccessExpr node)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return EmitPostfixChain(chain.Value);
        return EmitDynamicGetMember(node.MemberName, node.NullSafe, Emit(node.Target));
    }

    private LinqExpression EmitDynamicGetMember(string memberName, bool nullSafe, LinqExpression emittedTarget)
    {
        return LinqExpression.Call(
            GetMemberMethod,
            EmitHelpers.AsObject(emittedTarget),
            LinqExpression.Constant(memberName),
            _configParam,
            LinqExpression.Constant(nullSafe),
            _contextParam);
    }

    private LinqExpression EmitMemberAccessBaseWithTarget(BoundMemberAccessBase ma, LinqExpression emittedTarget)
    {
        return ma switch
        {
            BoundPropertyAccessExpr prop => EmitPropertyAccessWithTarget(prop, emittedTarget),
            BoundFieldAccessExpr field => EmitFieldAccessWithTarget(field, emittedTarget),
            BoundMethodGroupExpr => EmitDynamicGetMember(ma.MemberName, ma.NullSafe, emittedTarget),
            BoundDynamicMemberAccessExpr => EmitDynamicGetMember(ma.MemberName, ma.NullSafe, emittedTarget),
            _ => throw new BindingNotSupportedException($"Unexpected member access type '{ma.GetType().Name}'")
        };
    }

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
        if (call.Callee is BoundMethodGroupExpr)
            return EmitDirectPlannedCall(call, emittedTarget);

        return EmitInvokeCore(call.Callee, call.Arguments, ImmutableArray<string>.Empty, emittedTarget);
    }

    private LinqExpression EmitDirectPropertyAccess(BoundPropertyAccessExpr node, LinqExpression emittedTarget)
    {
        var property = node.Property;
        var guardCheck = LinqExpression.Empty();

        if (node.IsStatic)
        {
            var access = LinqExpression.Property(null, property);
            var guarded = EmitHelpers.WrapGuardedValue(access, property.PropertyType, EmitHelpers.CreateMemberGuardContext(node.MemberName));
            return LinqExpression.Block(
                typeof(object),
                guardCheck,
                LinqExpression.Convert(guarded, typeof(object)));
        }

        var targetObjVar = LinqExpression.Variable(typeof(object), "memberTarget");
        var targetType = property.DeclaringType ?? property.ReflectedType!;
        var checkedTarget = LinqExpression.Call(
            EnsureMemberTargetNotNullMethod,
            targetObjVar,
            LinqExpression.Constant(node.MemberName));
        var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, targetType);
        var accessExpr = LinqExpression.Property(typedTarget, property);
        var guardedExpr = LinqExpression.Convert(
            EmitHelpers.WrapGuardedValue(accessExpr, property.PropertyType, EmitHelpers.CreateMemberGuardContext(node.MemberName)),
            typeof(object));

        if (node.NullSafe)
        {
            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                guardCheck,
                LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(emittedTarget)),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    guardedExpr));
        }

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar],
            guardCheck,
            LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(emittedTarget)),
            guardedExpr);
    }

    private LinqExpression EmitDirectFieldAccess(BoundFieldAccessExpr node, LinqExpression emittedTarget)
    {
        var field = node.Field;
        var guardCheck = LinqExpression.Empty();

        if (node.IsStatic)
        {
            var access = LinqExpression.Field(null, field);
            var guarded = EmitHelpers.WrapGuardedValue(access, field.FieldType, EmitHelpers.CreateMemberGuardContext(node.MemberName));
            return LinqExpression.Block(
                typeof(object),
                guardCheck,
                LinqExpression.Convert(guarded, typeof(object)));
        }

        var targetObjVar = LinqExpression.Variable(typeof(object), "fieldTarget");
        var targetType = field.DeclaringType ?? field.ReflectedType!;
        var checkedTarget = LinqExpression.Call(
            EnsureMemberTargetNotNullMethod,
            targetObjVar,
            LinqExpression.Constant(node.MemberName));
        var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, targetType);
        var accessExpr = LinqExpression.Field(typedTarget, field);
        var guardedExpr = LinqExpression.Convert(
            EmitHelpers.WrapGuardedValue(accessExpr, field.FieldType, EmitHelpers.CreateMemberGuardContext(node.MemberName)),
            typeof(object));

        if (node.NullSafe)
        {
            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                guardCheck,
                LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(emittedTarget)),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    guardedExpr));
        }

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar],
            guardCheck,
            LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(emittedTarget)),
            guardedExpr);
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
        return EmitInvokeCore(invoke.Callee, invoke.Arguments, invoke.TypeArguments);
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
                result = EmitInvokeCore(invoke.Callee, invoke.Arguments, invoke.TypeArguments, result);
            else
                result = EmitMemberAccessBaseWithTarget(seg.MemberAccess, result);
        }
        return result;
    }

    private LinqExpression EmitLambda(BoundLambdaExpr lambda)
    {
        var parameters = LinqExpression.NewArrayInit(
            typeof(string),
            lambda.Parameters.Select(static name => LinqExpression.Constant(name)));

        return LinqExpression.Call(
            CreateLambdaValueMethod,
            parameters,
            LinqExpression.Constant(lambda.Body, typeof(Expr)),
            _contextParam,
            _configParam);
    }

    private LinqExpression EmitPipeline(BoundPipelineExpr pipeline)
    {
        if (pipeline.Right is BoundIdentifierExpr rightIdentifier)
        {
            return LinqExpression.Call(
                InvokePipelineIdentifierMethod,
                EmitHelpers.AsObject(Emit(pipeline.Left)),
                LinqExpression.Constant(rightIdentifier.Name),
                _contextParam,
                _configParam,
                _ctParam);
        }

        return LinqExpression.Call(
            InvokePipelineMethod,
            EmitHelpers.AsObject(Emit(pipeline.Left)),
            EmitHelpers.AsObject(Emit(pipeline.Right)),
            _contextParam,
            _configParam,
            _ctParam);
    }

    private LinqExpression EmitArrayLiteral(BoundArrayLiteralExpr arrayLiteral)
    {
        var elementType = arrayLiteral.StaticType.ClrType.IsArray ? arrayLiteral.StaticType.ClrType.GetElementType() : null;
        if (elementType != null && elementType != typeof(object) &&
            !arrayLiteral.Elements.Any(static e => e is BoundSpreadExpr))
        {
            var elements = arrayLiteral.Elements.Select(
                element => EmitHelpers.EnsureTypedExpression(Emit(element), elementType));
            return EmitHelpers.AsObject(LinqExpression.NewArrayInit(elementType, elements));
        }

        var listVar = LinqExpression.Variable(typeof(List<object?>), "arr");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(listVar, LinqExpression.New(ListCtor))
        };

        for (var i = 0; i < arrayLiteral.Elements.Length; i++)
        {
            var element = arrayLiteral.Elements[i];
            if (element is BoundSpreadExpr spread)
            {
                statements.Add(LinqExpression.Call(
                    SpreadIntoListMethod,
                    listVar,
                    EmitHelpers.AsObject(Emit(spread.Expression))));
                continue;
            }

            statements.Add(LinqExpression.Call(listVar, ListAddMethod, EmitHelpers.AsObject(Emit(element))));
        }

        statements.Add(LinqExpression.Call(CreateTypedArrayMethod, listVar));
        return LinqExpression.Block(typeof(object), [listVar], statements);
    }

    private LinqExpression EmitObjectLiteral(BoundObjectLiteralExpr objectLiteral)
    {
        var dictVar = LinqExpression.Variable(typeof(IDictionary<string, object?>), "dict");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(dictVar, LinqExpression.New(ExpandoObjectCtor))
        };

        var itemProperty = typeof(IDictionary<string, object?>).GetProperty("Item")!;

        for (var i = 0; i < objectLiteral.Properties.Length; i++)
        {
            var property = objectLiteral.Properties[i];
            if (property.IsSpread)
            {
                statements.Add(LinqExpression.Call(
                    SpreadIntoDictMethod,
                    dictVar,
                    EmitHelpers.AsObject(Emit(property.Value)),
                    _contextParam));
                continue;
            }

            statements.Add(LinqExpression.Assign(
                LinqExpression.Property(dictVar, itemProperty, LinqExpression.Constant(property.PropertyName!)),
                EmitHelpers.AsObject(Emit(property.Value))));
        }

        statements.Add(LinqExpression.Convert(dictVar, typeof(object)));
        return LinqExpression.Block(typeof(object), [dictVar], statements);
    }

    private LinqExpression EmitInterpolatedString(BoundInterpolatedStringExpr interpolatedString)
    {
        var sbVar = LinqExpression.Variable(typeof(StringBuilder), "sb");
        var variables = new List<ParameterExpression> { sbVar };
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(sbVar, LinqExpression.New(StringBuilderCtor))
        };

        for (var i = 0; i < interpolatedString.Parts.Length; i++)
        {
            switch (interpolatedString.Parts[i])
            {
                case BoundInterpolatedTextPart textPart:
                    statements.Add(LinqExpression.Call(sbVar, StringBuilderAppendMethod, LinqExpression.Constant(textPart.Text)));
                    break;

                case BoundInterpolatedExpressionPart expressionPart:
                {
                    var valueVar = LinqExpression.Variable(typeof(object), $"interpValue{i}");
                    variables.Add(valueVar);
                    var format =
                        "{0" +
                        (expressionPart.AlignmentSpecifier != null ? "," + expressionPart.AlignmentSpecifier : string.Empty) +
                        (expressionPart.FormatSpecifier != null ? ":" + expressionPart.FormatSpecifier : string.Empty) +
                        "}";

                    statements.Add(LinqExpression.Assign(valueVar, EmitHelpers.AsObject(Emit(expressionPart.Expression))));
                    statements.Add(LinqExpression.Call(
                        sbVar,
                        StringBuilderAppendMethod,
                        expressionPart.AlignmentSpecifier != null || expressionPart.FormatSpecifier != null
                            ? LinqExpression.Call(StringFormatMethod, LinqExpression.Constant(format), valueVar)
                            : LinqExpression.Condition(
                                LinqExpression.Equal(valueVar, LinqExpression.Constant(null, typeof(object))),
                                LinqExpression.Constant(string.Empty),
                                LinqExpression.Call(valueVar, ObjectToStringMethod))));
                    break;
                }

                default:
                    throw new BindingNotSupportedException(
                        $"Bound interpolated part '{interpolatedString.Parts[i].GetType().Name}' is not implemented");
            }
        }

        statements.Add(LinqExpression.Call(sbVar, StringBuilderToStringMethod));
        return LinqExpression.Block(typeof(object), variables, statements);
    }

    private LinqExpression EmitNamedArgument(BoundNamedArgumentExpr namedArgument)
    {
        return LinqExpression.Convert(
            LinqExpression.New(
                NamedArgCtor,
                LinqExpression.Constant(namedArgument.Name),
                EmitHelpers.AsObject(Emit(namedArgument.Value))),
            typeof(object));
    }

    private static LinqExpression EmitOutArg(BoundOutArgExpr outArg)
    {
        return LinqExpression.Convert(
            LinqExpression.New(
                OutArgMarkerCtor,
                LinqExpression.Constant(outArg.VariableName),
                LinqExpression.Constant(outArg.TypeName, typeof(string)),
                LinqExpression.Constant(outArg.IsDiscard)),
            typeof(object));
    }

    private static LinqExpression EmitInvalidSpread()
    {
        throw new AlderException(DiagnosticDescriptors.SpreadOutsideLiteral);
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
