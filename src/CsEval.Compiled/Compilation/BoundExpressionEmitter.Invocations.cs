using System.Collections;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using CsEval.Binding;
using CsEval.Binding.BoundNodes;
using CsEval.Binding.Plans;
using CsEval.Diagnostics;
using CsEval.Parsing;
using CsEval.Runtime;
using static CsEval.Compiled.Compilation.BoundRuntimeMethodCache;

namespace CsEval.Compiled.Compilation;

internal sealed partial class BoundExpressionEmitter
{
    private LinqExpression EmitMemberAccess(BoundMemberAccessExpr memberAccess)
    {
        if (memberAccess.Plan?.Member is PropertyInfo property)
        {
            var declaringType = property.DeclaringType;
            if (declaringType == null || !IsValueTupleType(declaringType))
                return EmitDirectPropertyAccess(memberAccess, property);
        }

        if (memberAccess.Plan?.Member is FieldInfo field)
        {
            var declaringType = field.DeclaringType;
            if (declaringType == null || !IsValueTupleType(declaringType))
                return EmitDirectFieldAccess(memberAccess, field);
        }

        var target = EmitHelpers.AsObject(Emit(memberAccess.Target));
        return LinqExpression.Call(
            GetMemberMethod,
            target,
            LinqExpression.Constant(memberAccess.MemberName),
            _optionsParam,
            LinqExpression.Constant(memberAccess.NullSafe),
            _contextParam);
    }

    private LinqExpression EmitIndexAccess(BoundIndexAccessExpr indexAccess)
    {
        if (indexAccess.Plan?.IsDirectCollectionAccess == true)
            return EmitDirectCollectionIndexAccess(indexAccess);

        var targetExpr = EmitHelpers.AsObject(Emit(indexAccess.Target));
        var indexExpr = EmitHelpers.AsObject(Emit(indexAccess.Index));

        if (!indexAccess.NullSafe)
        {
            return LinqExpression.Call(
                GetIndexMethod,
                targetExpr,
                indexExpr,
                _optionsParam,
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
                    LinqExpression.Call(GetIndexMethod, targetVar, indexExpr, _optionsParam, _contextParam)));
    }

    private LinqExpression EmitCall(BoundCallExpr call)
    {
        if (call.Callee is BoundMemberAccessExpr { Plan: not null } memberAccess)
            return EmitDirectPlannedCall(call, memberAccess);

        return EmitInvokeCore(call.Callee, call.Arguments, ImmutableArray<string>.Empty);
    }

    private LinqExpression EmitDirectPropertyAccess(BoundMemberAccessExpr memberAccess, PropertyInfo property)
    {
        var plan = memberAccess.Plan!;
        var guardCheck = EmitMemberReadGuard(memberAccess, isField: false);

        if (plan.IsStatic)
        {
            var access = LinqExpression.Property(null, property);
            var guarded = EmitHelpers.WrapGuardedValue(access, property.PropertyType, EmitHelpers.CreateMemberGuardContext(memberAccess.MemberName));
            return LinqExpression.Block(
                typeof(object),
                guardCheck,
                LinqExpression.Convert(guarded, typeof(object)));
        }

        var targetObjVar = LinqExpression.Variable(typeof(object), "memberTarget");
        var targetType = property.DeclaringType ?? plan.DeclaringType;
        var checkedTarget = LinqExpression.Call(
            EnsureMemberTargetNotNullMethod,
            targetObjVar,
            LinqExpression.Constant(memberAccess.MemberName));
        var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, targetType);
        var accessExpr = LinqExpression.Property(typedTarget, property);
        var guardedExpr = LinqExpression.Convert(
            EmitHelpers.WrapGuardedValue(accessExpr, property.PropertyType, EmitHelpers.CreateMemberGuardContext(memberAccess.MemberName)),
            typeof(object));

        if (memberAccess.NullSafe)
        {
            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                guardCheck,
                LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(Emit(memberAccess.Target))),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    guardedExpr));
        }

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar],
            guardCheck,
            LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(Emit(memberAccess.Target))),
            guardedExpr);
    }

    private LinqExpression EmitDirectFieldAccess(BoundMemberAccessExpr memberAccess, FieldInfo field)
    {
        var plan = memberAccess.Plan!;
        var guardCheck = EmitMemberReadGuard(memberAccess, isField: true);

        if (plan.IsStatic)
        {
            var access = LinqExpression.Field(null, field);
            var guarded = EmitHelpers.WrapGuardedValue(access, field.FieldType, EmitHelpers.CreateMemberGuardContext(memberAccess.MemberName));
            return LinqExpression.Block(
                typeof(object),
                guardCheck,
                LinqExpression.Convert(guarded, typeof(object)));
        }

        var targetObjVar = LinqExpression.Variable(typeof(object), "fieldTarget");
        var targetType = field.DeclaringType ?? plan.DeclaringType;
        var checkedTarget = LinqExpression.Call(
            EnsureMemberTargetNotNullMethod,
            targetObjVar,
            LinqExpression.Constant(memberAccess.MemberName));
        var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, targetType);
        var accessExpr = LinqExpression.Field(typedTarget, field);
        var guardedExpr = LinqExpression.Convert(
            EmitHelpers.WrapGuardedValue(accessExpr, field.FieldType, EmitHelpers.CreateMemberGuardContext(memberAccess.MemberName)),
            typeof(object));

        if (memberAccess.NullSafe)
        {
            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                guardCheck,
                LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(Emit(memberAccess.Target))),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    guardedExpr));
        }

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar],
            guardCheck,
            LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(Emit(memberAccess.Target))),
            guardedExpr);
    }

    private LinqExpression EmitDirectCollectionIndexAccess(BoundIndexAccessExpr indexAccess)
    {
        var plan = indexAccess.Plan;
        if (plan == null)
            throw new BindingNotSupportedException("Direct collection index emission requires an index plan.");

        if (plan.TargetType == typeof(string))
            return EmitDirectStringIndexAccess(indexAccess);

        if (typeof(IList).IsAssignableFrom(plan.TargetType))
            return EmitDirectListIndexAccess(indexAccess);

        return LinqExpression.Call(
            GetIndexMethod,
            EmitHelpers.AsObject(Emit(indexAccess.Target)),
            EmitHelpers.AsObject(Emit(indexAccess.Index)),
            _optionsParam,
            _contextParam);
    }

    private LinqExpression EmitDirectStringIndexAccess(BoundIndexAccessExpr indexAccess)
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

    private LinqExpression EmitDirectListIndexAccess(BoundIndexAccessExpr indexAccess)
    {
        var plan = indexAccess.Plan
                   ?? throw new BindingNotSupportedException("Direct list index emission requires an index plan.");
        var targetObjVar = LinqExpression.Variable(typeof(object), "listTarget");
        var checkedTarget = LinqExpression.Call(EnsureIndexTargetNotNullMethod, targetObjVar);

        LinqExpression typedTarget;
        LinqExpression countExpr;
        LinqExpression valueExpr;
        Type valueType;

        if (EmitHelpers.TryGetIntIndexer(plan.TargetType, out var indexer) &&
            EmitHelpers.TryGetCountProperty(plan.TargetType, out var countProperty))
        {
            typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, plan.TargetType);
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

    private LinqExpression BuildNormalizedIntIndex(BoundIndexAccessExpr indexAccess, LinqExpression lengthExpression)
    {
        if (indexAccess.Index is BoundLiteralExpr { Value: int literalIndex and >= 0 })
            return LinqExpression.Constant(literalIndex, typeof(int));

        var rawIndex = LinqExpression.Call(ConvertToInt32ObjectMethod, EmitHelpers.AsObject(Emit(indexAccess.Index)));
        var languageMode = LinqExpression.Property(_optionsParam, nameof(CsEvalOptions.LanguageMode));
        return LinqExpression.Call(NormalizeIndexMethod, rawIndex, lengthExpression, languageMode);
    }

    private LinqExpression EmitDirectPlannedCall(BoundCallExpr call, BoundMemberAccessExpr memberAccess)
    {
        if (!EmitHelpers.CanEmitDirectMethodCall(call.Plan, call.Arguments.Length))
            return EmitInvokeCore(call.Callee, call.Arguments, ImmutableArray<string>.Empty);

        var method = call.Plan.SelectedMethod;
        var parameters = MethodDispatchCache.GetParameters(method);
        var guardCheck = EmitMethodCallGuard(method, call.Plan.IsStaticCall, call.Plan.IsModuleCall);
        var args = EmitPlannedCallArguments(call, parameters);

        if (call.Plan.IsStaticCall)
        {
            var staticCall = LinqExpression.Call(method, args);
            if (method.ReturnType == typeof(void))
                return LinqExpression.Block(guardCheck, staticCall, LinqExpression.Constant(null, typeof(object)));

            return LinqExpression.Block(
                method.ReturnType,
                guardCheck,
                EmitHelpers.WrapGuardedValue(staticCall, method.ReturnType, EmitHelpers.CreateMethodGuardContext(method.Name)));
        }

        var targetType = method.DeclaringType ?? memberAccess.Plan!.DeclaringType;
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
                LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(Emit(memberAccess.Target))),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    nullSafeBody));
        }

        var targetVar = LinqExpression.Variable(targetType, "callTargetTyped");
        var assignTarget = LinqExpression.Assign(
            targetVar,
            EmitHelpers.EnsureTypedExpression(Emit(memberAccess.Target), targetType));
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

    private static bool IsValueTupleType(Type type)
    {
        return type is { IsValueType: true, IsGenericType: true } &&
               type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true;
    }

    private LinqExpression[] EmitPlannedCallArguments(BoundCallExpr call, ParameterInfo[] parameters)
    {
        var emitted = new LinqExpression[parameters.Length];
        var conversions = call.Plan.ArgumentConversions;

        foreach (var binding in call.Plan.ParameterBindings)
        {
            switch (binding.Kind)
            {
                case BoundParameterBindingKind.Argument:
                {
                    var sourceIndex = binding.SourceArgumentIndex;
                    var conversion = conversions[sourceIndex];
                    emitted[binding.ParameterIndex] = EmitCallArgument(call.Arguments[sourceIndex], conversion.TargetType);
                    break;
                }

                case BoundParameterBindingKind.DefaultValue:
                {
                    emitted[binding.ParameterIndex] = EmitDefaultArgument(parameters[binding.ParameterIndex]);
                    break;
                }

                case BoundParameterBindingKind.ParamsArray:
                {
                    var parameter = parameters[binding.ParameterIndex];
                    var elementType = parameter.ParameterType.GetElementType()
                                     ?? throw new BindingNotSupportedException("Params parameter must be an array type.");
                    var args = new LinqExpression[binding.SourceArgumentCount];

                    for (var i = 0; i < binding.SourceArgumentCount; i++)
                    {
                        var sourceIndex = binding.SourceArgumentIndex + i;
                        var conversion = conversions[sourceIndex];
                        var convertedArg = EmitCallArgument(call.Arguments[sourceIndex], conversion.TargetType);
                        args[i] = EmitHelpers.EnsureTypedExpression(convertedArg, elementType);
                    }

                    emitted[binding.ParameterIndex] = LinqExpression.NewArrayInit(elementType, args);
                    break;
                }

                default:
                    throw new BindingNotSupportedException(
                        $"Bound parameter binding kind '{binding.Kind}' is not implemented");
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

    private LinqExpression EmitMethodCallGuard(MethodInfo method, bool isStaticCall, bool isModuleCall)
    {
        return LinqExpression.Call(
            EnsureMethodCallsAllowedMethod,
            _optionsParam,
            LinqExpression.Constant(method.Name),
            isStaticCall
                ? LinqExpression.Constant(method.DeclaringType, typeof(Type))
                : LinqExpression.Constant(null, typeof(Type)),
            LinqExpression.Constant(isModuleCall));
    }

    private LinqExpression EmitMemberReadGuard(BoundMemberAccessExpr memberAccess, bool isField)
    {
        var plan = memberAccess.Plan!;
        return LinqExpression.Call(
            EnsureMemberReadAllowedMethod,
            _optionsParam,
            LinqExpression.Constant(memberAccess.MemberName),
            LinqExpression.Constant(plan.IsStatic),
            LinqExpression.Constant(isField),
            LinqExpression.Constant(plan.DeclaringType, typeof(Type)));
    }

    private LinqExpression EmitInvoke(BoundInvokeExpr invoke)
    {
        return EmitInvokeCore(invoke.Callee, invoke.Arguments, invoke.TypeArguments);
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
            _optionsParam);
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
                _optionsParam,
                _ctParam);
        }

        return LinqExpression.Call(
            InvokePipelineMethod,
            EmitHelpers.AsObject(Emit(pipeline.Left)),
            EmitHelpers.AsObject(Emit(pipeline.Right)),
            _contextParam,
            _optionsParam,
            _ctParam);
    }

    private LinqExpression EmitArrayLiteral(BoundArrayLiteralExpr arrayLiteral)
    {
        var elementType = arrayLiteral.StaticType.IsArray ? arrayLiteral.StaticType.GetElementType() : null;
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
        throw new CsEvalException(DiagnosticDescriptors.SpreadOutsideLiteral);
    }

    private LinqExpression EmitInvokeCore(
        BoundExpr callee,
        ImmutableArray<BoundExpr> arguments,
        ImmutableArray<string> typeArguments)
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
                _optionsParam,
                _ctParam,
                emittedTypeArguments);
        }
        else if (callee is BoundMemberAccessExpr memberAccess)
        {
            invokeExpr = LinqExpression.Call(
                InvokeMemberCallMethod,
                EmitHelpers.AsObject(Emit(memberAccess.Target)),
                LinqExpression.Constant(memberAccess.MemberName),
                argsVar,
                LinqExpression.Constant(memberAccess.NullSafe),
                _contextParam,
                _optionsParam,
                _ctParam,
                emittedTypeArguments);
        }
        else
        {
            invokeExpr = LinqExpression.Call(
                InvokeCallMethod,
                EmitHelpers.AsObject(Emit(callee)),
                argsVar,
                _contextParam,
                _optionsParam,
                _ctParam,
                emittedTypeArguments);
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