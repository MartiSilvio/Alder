using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Binding.Services;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(CallExpr))]
internal static class CallBinder
{
    public static BoundExpr Bind(CallExpr expr, BindingContext context, BinderContext binder)
    {
        if (TryBindStaticModuleCall(expr, context, binder, out var staticModuleCall))
            return staticModuleCall;

        var callee = BindCallCallee(expr.Callee, context, binder);
        return BindCallWithBoundCallee(callee, expr, context, binder);
    }

    internal static BoundExpr BindCallWithBoundCallee(BoundExpr callee, CallExpr call, BindingContext context, BinderContext binder)
    {
        var arguments = call.Arguments
            .Select(argument => binder.Bind(argument, context))
            .ToImmutableArray();
        var typeArguments = call.TypeArguments?.ToImmutableArray() ?? ImmutableArray<string>.Empty;

        var hasSpecialArgs = arguments.Any(static argument =>
            argument is BoundNamedArgumentExpr or BoundOutArgExpr);

        if (callee is BoundMethodGroupExpr methodGroup && !hasSpecialArgs)
        {
            var hasLambdasOrMethodGroups = arguments.Any(static a => a is BoundLambdaExpr or BoundMethodGroupExpr);

            if (!hasLambdasOrMethodGroups)
            {
                var argumentTypes = arguments.Select(static argument => argument.StaticType.ClrType).ToArray();
                var callBinderService = new CallBinderService(context.RuntimeContext);

                var bound = methodGroup.IsStatic
                    ? callBinderService.TryBindStaticCall(methodGroup.DeclaringType, methodGroup.MethodName, argumentTypes, context.IsCaseSensitive, out var callPlan)
                    : callBinderService.TryBindInstanceCall(methodGroup.DeclaringType, methodGroup.MethodName, argumentTypes, context.IsCaseSensitive, out callPlan);

                if (bound)
                {
                    var returnType = callPlan!.SelectedMethod.ReturnType;
                    var boundReturnType = TypeHelpers.IsValueTupleType(returnType)
                        ? CreateTupleAwareBoundType(returnType, arguments.Insert(0, methodGroup.Target))
                        : new BoundType(returnType);
                    return new BoundResolvedCallExpr(callee, arguments, callPlan.Resolution, callPlan.IsStaticCall, callPlan.IsModuleCall, boundReturnType);
                }

                if (!methodGroup.IsStatic && methodGroup.DeclaringType != typeof(object))
                {
                    var ext = TryBindExtensionCall(
                        methodGroup.Target, methodGroup.DeclaringType, methodGroup.MethodName,
                        methodGroup.NullSafe, arguments, argumentTypes, context);
                    if (ext != null) return ext;
                }
            }
            else
            {
                var result = TryBindCallWithLambdas(callee, methodGroup, arguments, context, binder);
                if (result != null)
                    return result;

                if (!methodGroup.IsStatic && methodGroup.DeclaringType != typeof(object))
                {
                    var ext = TryBindExtensionCallWithLambdas(
                        methodGroup.Target, methodGroup.DeclaringType, methodGroup.MethodName,
                        methodGroup.NullSafe, arguments, context, binder);
                    if (ext != null) return ext;
                }
            }
        }

        if (callee is BoundDynamicMemberAccessExpr dynAccess && !hasSpecialArgs &&
            dynAccess.Target.StaticType is not BoundUnknownType)
        {
            var targetType = dynAccess.Target.StaticType.ClrType;
            var hasLambdasOrMethodGroups = arguments.Any(static a => a is BoundLambdaExpr or BoundMethodGroupExpr);

            if (!hasLambdasOrMethodGroups)
            {
                var argumentTypes = arguments.Select(static a => a.StaticType.ClrType).ToArray();
                var ext = TryBindExtensionCall(
                    dynAccess.Target, targetType, dynAccess.MemberName,
                    dynAccess.NullSafe, arguments, argumentTypes, context);
                if (ext != null) return ext;
            }
            else
            {
                var ext = TryBindExtensionCallWithLambdas(
                    dynAccess.Target, targetType, dynAccess.MemberName,
                    dynAccess.NullSafe, arguments, context, binder);
                if (ext != null) return ext;
            }
        }

        if (callee.StaticType is not BoundUnknownType)
        {
            var calleeType = callee.StaticType.ClrType;
            if (typeof(Delegate).IsAssignableFrom(calleeType))
            {
                var invokeMethod = calleeType.GetMethod("Invoke");
                if (invokeMethod != null)
                {
                    var returnType = invokeMethod.ReturnType;
                    return new BoundDynamicCallExpr(callee, arguments, typeArguments,
                        returnType == typeof(void) ? BoundType.Void : new BoundType(returnType));
                }
            }
        }

        return new BoundDynamicCallExpr(callee, arguments, typeArguments, BoundType.Unknown);
    }

    private static BoundExpr? TryBindExtensionCall(
        BoundExpr targetExpr,
        Type targetType,
        string methodName,
        bool nullSafe,
        ImmutableArray<BoundExpr> arguments,
        Type[] argumentTypes,
        BindingContext context)
    {
        var service = new CallBinderService(context.RuntimeContext);
        if (!service.TryBindExtensionCall(targetType, methodName, argumentTypes, context.IsCaseSensitive, out var plan))
            return null;

        return BuildExtensionCallExpr(targetExpr, methodName, nullSafe, arguments, plan!, context);
    }

    private static BoundExpr? TryBindExtensionCallWithLambdas(
        BoundExpr targetExpr,
        Type targetType,
        string methodName,
        bool nullSafe,
        ImmutableArray<BoundExpr> arguments,
        BindingContext context,
        BinderContext binder)
    {
        var userDescriptors = BuildDescriptorsForLambdasAndMethodGroups(arguments);

        var service = new CallBinderService(context.RuntimeContext);
        if (!service.TryBindExtensionCallWithDescriptors(
                targetType, methodName, userDescriptors, context.IsCaseSensitive, out var plan))
            return null;

        var extensionType = plan!.SelectedMethod.DeclaringType ?? targetType;
        var callee = new BoundMethodGroupExpr(
            targetExpr, extensionType, methodName, nullSafe, IsStatic: true, BoundType.Unknown);

        var allArguments = ImmutableArray.CreateBuilder<BoundExpr>(arguments.Length + 1);
        allArguments.Add(targetExpr);
        allArguments.AddRange(arguments);
        var fullArguments = allArguments.ToImmutable();

        var typedArguments = TryBindLambdaAndMethodGroupArguments(fullArguments, plan.Resolution, context, binder);
        if (typedArguments == null)
            return null;

        return new BoundResolvedCallExpr(
            callee, typedArguments.Value, plan.Resolution,
            plan.IsStaticCall, plan.IsModuleCall,
            CreateTupleAwareBoundType(plan.SelectedMethod.ReturnType, typedArguments.Value),
            IsExtensionCall: true);
    }

    private static BoundResolvedCallExpr BuildExtensionCallExpr(
        BoundExpr targetExpr,
        string methodName,
        bool nullSafe,
        ImmutableArray<BoundExpr> userArguments,
        CallBindResult plan,
        BindingContext context)
    {
        var extensionType = plan.SelectedMethod.DeclaringType ?? typeof(object);
        var callee = new BoundMethodGroupExpr(
            targetExpr, extensionType, methodName, nullSafe, IsStatic: true, BoundType.Unknown);

        var allArguments = ImmutableArray.CreateBuilder<BoundExpr>(userArguments.Length + 1);
        allArguments.Add(targetExpr);
        allArguments.AddRange(userArguments);

        var fullArguments = allArguments.ToImmutable();
        return new BoundResolvedCallExpr(
            callee, fullArguments, plan.Resolution,
            plan.IsStaticCall, plan.IsModuleCall,
            CreateTupleAwareBoundType(plan.SelectedMethod.ReturnType, fullArguments),
            IsExtensionCall: true);
    }

    private static BoundExpr? TryBindCallWithLambdas(
        BoundExpr callee,
        BoundMethodGroupExpr methodGroup,
        ImmutableArray<BoundExpr> arguments,
        BindingContext context,
        BinderContext binder)
    {
        var descriptors = BuildDescriptorsForLambdasAndMethodGroups(arguments);

        var flags = BindingFlags.Public |
                    (methodGroup.IsStatic ? BindingFlags.Static : BindingFlags.Instance);
        if (!context.IsCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = context.RuntimeContext.TypeMetadata.GetMethods(methodGroup.DeclaringType, methodGroup.MethodName, flags);
        var callBinderService = new CallBinderService(context.RuntimeContext);

        if (!callBinderService.TryBindWithDescriptors(methods, descriptors, methodGroup.IsStatic, out var callPlan))
            return null;

        var resolution = callPlan!.Resolution;
        var typedArguments = TryBindLambdaAndMethodGroupArguments(arguments, resolution, context, binder);
        if (typedArguments == null)
            return null;

        return new BoundResolvedCallExpr(
            callee, typedArguments.Value, resolution,
            callPlan.IsStaticCall, callPlan.IsModuleCall,
            CreateTupleAwareBoundType(callPlan.SelectedMethod.ReturnType, typedArguments.Value));
    }

    private static ImmutableArray<BoundExpr>? TryBindLambdaArguments(
        ImmutableArray<BoundExpr> arguments,
        ResolvedCall resolution,
        BindingContext context,
        BinderContext binder)
    {
        var conversions = resolution.Conversions;
        var result = arguments.ToBuilder();

        for (var i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] is not BoundLambdaExpr lambda)
                continue;

            if (i >= conversions.Length || conversions[i].Kind != ArgumentConversionKind.LambdaToDelegate)
                return null;

            var delegateType = conversions[i].TargetType;
            var invokeMethod = delegateType.GetMethod("Invoke");
            if (invokeMethod == null)
                return null;

            var invokeParams = invokeMethod.GetParameters();
            if (invokeParams.Length != lambda.Parameters.Length)
                return null;

            var savedLocalCount = context.LocalCount;
            BoundExpr? boundBody = null;
            ImmutableArray<BoundTypedLambdaParameter> typedParamsImmutable = default;
            try
            {
                var lambdaScope = context.CreateChildScope();
                var typedParams = ImmutableArray.CreateBuilder<BoundTypedLambdaParameter>(lambda.Parameters.Length);
                var lambdaParamIds = new HashSet<int>();

                for (var p = 0; p < lambda.Parameters.Length; p++)
                {
                    var paramType = invokeParams[p].ParameterType;
                    var boundParamType = CreateTupleAwareBoundType(paramType, arguments);
                    var paramId = lambdaScope.DeclareLocal(lambda.Parameters[p], boundParamType);
                    lambdaParamIds.Add(paramId);
                    typedParams.Add(new BoundTypedLambdaParameter(lambda.Parameters[p], paramType));
                }

                boundBody = binder.Bind(lambda.Body, lambdaScope);
                if (boundBody.HasErrors)
                {
                    context.LocalCount = savedLocalCount;
                    return null;
                }

                if (CapturesOuterLocals(boundBody, lambdaParamIds))
                {
                    context.LocalCount = savedLocalCount;
                    return null;
                }

                typedParamsImmutable = typedParams.ToImmutable();
            }
            catch (Exception ex) when (ex is AlderException or BindingNotSupportedException or InvalidOperationException)
            {
                context.LocalCount = savedLocalCount;
                return null;
            }

            // §10.7.1 / CS0029: an expression-bodied lambda's body must implicitly convert to the
            // delegate's return type. `object`-typed bodies defer to runtime because Alder uses
            // object as the fallback for dynamic/untyped expressions and Roslyn would have a
            // sharper static type at this point.
            if (invokeMethod.ReturnType != typeof(void)
                && boundBody.StaticType is not BoundUnknownType
                && boundBody.StaticType.ClrType != typeof(object)
                && !TypeHelpers.CanImplicitlyConvert(boundBody.StaticType.ClrType, invokeMethod.ReturnType))
            {
                throw new AlderException(DiagnosticDescriptors.NoImplicitConversion,
                    boundBody.StaticType.ClrType.Name, invokeMethod.ReturnType.Name);
            }

            result[i] = new BoundTypedLambdaExpr(
                typedParamsImmutable,
                boundBody,
                delegateType,
                new BoundType(delegateType));
        }

        return result.ToImmutable();
    }

    private static bool CapturesOuterLocals(BoundExpr body, HashSet<int> lambdaLocalIds)
    {
        if (body is BoundIdentifierExpr { LocalId: { } id } && !lambdaLocalIds.Contains(id))
            return true;

        var captures = false;
        body.EnumerateChildren(child =>
        {
            if (!captures)
                captures = CapturesOuterLocals(child, lambdaLocalIds);
        });
        return captures;
    }

    /// <summary>
    /// §7.3: Tuple element names are compile-time metadata erased at runtime.
    /// When a type is a ValueTuple, search the bound tree for upstream lambdas that
    /// produced a BoundStructuralType with element names and propagate them.
    /// Used for both lambda parameter types (Where's p.squared → p.Item2) and
    /// return types (First().value resolves via the propagated names).
    /// </summary>
    private static BoundType CreateTupleAwareBoundType(Type type, ImmutableArray<BoundExpr> arguments)
    {
        if (!TypeHelpers.IsValueTupleType(type))
            return new BoundType(type);

        foreach (var arg in arguments)
        {
            var names = FindTupleElementNames(type, arg);
            if (!names.IsDefaultOrEmpty)
                return BoundStructuralType.FromElementNames(type, names);
        }

        return new BoundType(type);
    }

    private static ImmutableArray<string?> FindTupleElementNames(Type tupleType, BoundExpr expr)
    {
        if (expr is BoundTypedLambdaExpr { Body.StaticType: BoundStructuralType st }
            && st.ClrType == tupleType
            && !st.TupleElementNames.IsDefaultOrEmpty)
        {
            return st.TupleElementNames;
        }

        if (expr is BoundResolvedCallExpr call)
        {
            // Check typed lambdas at this call level first (nearest producer wins)
            foreach (var callArg in call.Arguments)
            {
                if (callArg is BoundTypedLambdaExpr { Body.StaticType: BoundStructuralType s }
                    && s.ClrType == tupleType
                    && !s.TupleElementNames.IsDefaultOrEmpty)
                {
                    return s.TupleElementNames;
                }
            }

            // Recurse into non-lambda source expressions (chained calls like Select().OrderBy())
            foreach (var callArg in call.Arguments)
            {
                if (callArg is not BoundTypedLambdaExpr)
                {
                    var names = FindTupleElementNames(tupleType, callArg);
                    if (!names.IsDefaultOrEmpty)
                        return names;
                }
            }
        }

        return default;
    }


    private static ArgumentDescriptor[] BuildDescriptorsForLambdasAndMethodGroups(ImmutableArray<BoundExpr> arguments)
    {
        var descriptors = new ArgumentDescriptor[arguments.Length];
        for (var i = 0; i < arguments.Length; i++)
        {
            descriptors[i] = arguments[i] switch
            {
                BoundLambdaExpr lambda => ArgumentDescriptor.ForLambda(lambda.Parameters.Length, lambda),
                BoundMethodGroupExpr mg => ArgumentDescriptor.ForMethodGroup(1, new StaticMethodRef(mg.DeclaringType, mg.MethodName)),
                _ => ArgumentDescriptor.ForType(arguments[i].StaticType.ClrType)
            };
        }
        return descriptors;
    }

    /// <summary>
    /// Method groups don't need bind-time transformation (the runtime LambdaDelegateConverter
    /// handles the conversion). If lambdas are also present, delegates to TryBindLambdaArguments
    /// which types their parameters from the resolved delegate signature.
    /// </summary>
    private static ImmutableArray<BoundExpr>? TryBindLambdaAndMethodGroupArguments(
        ImmutableArray<BoundExpr> arguments,
        ResolvedCall resolution,
        BindingContext context,
        BinderContext binder)
    {
        var hasLambdas = arguments.Any(static a => a is BoundLambdaExpr);
        if (hasLambdas)
            return TryBindLambdaArguments(arguments, resolution, context, binder);

        // Method groups only, no bind-time processing needed
        return arguments;
    }

    private static bool TryBindStaticModuleCall(CallExpr call, BindingContext context, BinderContext binder, out BoundExpr boundCall)
    {
        boundCall = null!;
        if (call.Callee is not MemberAccessExpr { Object: IdentifierExpr moduleIdentifier } memberAccess)
            return false;

        var moduleName = moduleIdentifier.Name.Lexeme;
        if (context.RuntimeContext.Functions.ContainsKey(moduleName))
            return false;

        if (!context.RuntimeContext.Modules.TryGetValue(moduleName, out var moduleInfo))
            return false;

        if (moduleInfo.Instance != null ||
            !moduleInfo.Type.IsAbstract ||
            !moduleInfo.Type.IsSealed)
        {
            return false;
        }

        if (!moduleInfo.Members.TryGetValue(memberAccess.Name.Lexeme, out var moduleMember) ||
            moduleMember is not MethodInfo)
            return false;

        var arguments = call.Arguments
            .Select(argument => binder.Bind(argument, context))
            .ToImmutableArray();
        if (arguments.Any(static argument => argument is BoundLambdaExpr))
            return false;

        var argumentTypes = arguments.Select(static argument => argument.StaticType.ClrType).ToArray();
        var callBinderService = new CallBinderService(context.RuntimeContext);

        if (!callBinderService.TryBindStaticCall(
                moduleInfo.Type,
                memberAccess.Name.Lexeme,
                argumentTypes,
                context.IsCaseSensitive,
                out var moduleCallPlan))
        {
            return false;
        }

        var callResult = moduleCallPlan! with { IsModuleCall = true };

        var callee = new BoundMethodGroupExpr(
            new BoundTypeRefExpr(moduleInfo.Type, new BoundType(typeof(Type))),
            moduleInfo.Type,
            memberAccess.Name.Lexeme,
            memberAccess.NullSafe,
            IsStatic: true,
            BoundType.Unknown);

        boundCall = new BoundResolvedCallExpr(callee, arguments, callResult.Resolution, callResult.IsStaticCall, callResult.IsModuleCall, CreateTupleAwareBoundType(callResult.SelectedMethod.ReturnType, arguments));
        return true;
    }

    private static BoundExpr BindCallCallee(Expr callee, BindingContext context, BinderContext binder)
    {
        if (callee is not MemberAccessExpr memberAccess)
            return binder.Bind(callee, context);

        return binder.Bind(memberAccess, context);
    }
}
