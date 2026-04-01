using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Binding.Services;
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
            var hasLambdas = arguments.Any(static a => a is BoundLambdaExpr);

            if (!hasLambdas)
            {
                var argumentTypes = arguments.Select(static argument => argument.StaticType.ClrType).ToArray();
                var callBinderService = new CallBinderService(context.RuntimeContext);

                var bound = methodGroup is { IsStatic: true, Target: BoundLiteralExpr { Value: Type staticDeclaringType } }
                    ? callBinderService.TryBindStaticCall(staticDeclaringType, methodGroup.MethodName, argumentTypes, context.IsCaseSensitive, out var callPlan)
                    : callBinderService.TryBindInstanceCall(methodGroup.DeclaringType, methodGroup.MethodName, argumentTypes, context.IsCaseSensitive, out callPlan);

                if (bound)
                    return new BoundResolvedCallExpr(callee, arguments, callPlan!.Resolution, callPlan.IsStaticCall, callPlan.IsModuleCall, new BoundType(callPlan.SelectedMethod.ReturnType));

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
            var hasLambdas = arguments.Any(static a => a is BoundLambdaExpr);

            if (!hasLambdas)
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
        var userDescriptors = new ArgumentDescriptor[arguments.Length];
        for (var i = 0; i < arguments.Length; i++)
        {
            userDescriptors[i] = arguments[i] is BoundLambdaExpr lambda
                ? ArgumentDescriptor.ForLambda(lambda.Parameters.Length)
                : ArgumentDescriptor.ForType(arguments[i].StaticType.ClrType);
        }

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

        var typedArguments = TryBindLambdaArguments(fullArguments, plan.Resolution, context, binder);
        if (typedArguments == null)
            return null;

        return new BoundResolvedCallExpr(
            callee, typedArguments.Value, plan.Resolution,
            plan.IsStaticCall, plan.IsModuleCall,
            new BoundType(plan.SelectedMethod.ReturnType),
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

        return new BoundResolvedCallExpr(
            callee, allArguments.ToImmutable(), plan.Resolution,
            plan.IsStaticCall, plan.IsModuleCall,
            new BoundType(plan.SelectedMethod.ReturnType),
            IsExtensionCall: true);
    }

    private static BoundExpr? TryBindCallWithLambdas(
        BoundExpr callee,
        BoundMethodGroupExpr methodGroup,
        ImmutableArray<BoundExpr> arguments,
        BindingContext context,
        BinderContext binder)
    {
        var descriptors = new ArgumentDescriptor[arguments.Length];
        for (var i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] is BoundLambdaExpr lambda)
                descriptors[i] = ArgumentDescriptor.ForLambda(lambda.Parameters.Length);
            else
                descriptors[i] = ArgumentDescriptor.ForType(arguments[i].StaticType.ClrType);
        }

        var flags = BindingFlags.Public |
                    (methodGroup.IsStatic ? BindingFlags.Static : BindingFlags.Instance);
        if (!context.IsCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var declaringType = methodGroup is { IsStatic: true, Target: BoundLiteralExpr { Value: Type staticType } }
            ? staticType
            : methodGroup.DeclaringType;

        var methods = context.RuntimeContext.TypeMetadata.GetMethods(declaringType, methodGroup.MethodName, flags);
        var callBinderService = new CallBinderService(context.RuntimeContext);

        if (!callBinderService.TryBindWithDescriptors(methods, descriptors, methodGroup.IsStatic, out var callPlan))
            return null;

        var resolution = callPlan!.Resolution;
        var typedArguments = TryBindLambdaArguments(arguments, resolution, context, binder);
        if (typedArguments == null)
            return null;

        return new BoundResolvedCallExpr(
            callee, typedArguments.Value, resolution,
            callPlan.IsStaticCall, callPlan.IsModuleCall,
            new BoundType(callPlan.SelectedMethod.ReturnType));
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
            try
            {
                var lambdaScope = context.CreateChildScope();
                var typedParams = ImmutableArray.CreateBuilder<BoundTypedLambdaParameter>(lambda.Parameters.Length);
                var lambdaParamIds = new HashSet<int>();

                for (var p = 0; p < lambda.Parameters.Length; p++)
                {
                    var paramType = invokeParams[p].ParameterType;
                    var paramId = lambdaScope.DeclareLocal(lambda.Parameters[p], new BoundType(paramType));
                    lambdaParamIds.Add(paramId);
                    typedParams.Add(new BoundTypedLambdaParameter(lambda.Parameters[p], paramType));
                }

                var boundBody = binder.Bind(lambda.Body, lambdaScope);
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

                result[i] = new BoundTypedLambdaExpr(
                    typedParams.ToImmutable(),
                    boundBody,
                    delegateType,
                    new BoundType(delegateType));
            }
            catch (Exception ex) when (ex is AlderException or BindingNotSupportedException or InvalidOperationException)
            {
                context.LocalCount = savedLocalCount;
                return null;
            }
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
            new BoundLiteralExpr(moduleInfo.Type, new BoundType(typeof(Type))),
            moduleInfo.Type,
            memberAccess.Name.Lexeme,
            memberAccess.NullSafe,
            IsStatic: true,
            BoundType.Unknown);

        boundCall = new BoundResolvedCallExpr(callee, arguments, callResult.Resolution, callResult.IsStaticCall, callResult.IsModuleCall, new BoundType(callResult.SelectedMethod.ReturnType));
        return true;
    }

    private static BoundExpr BindCallCallee(Expr callee, BindingContext context, BinderContext binder)
    {
        if (callee is not MemberAccessExpr memberAccess)
            return binder.Bind(callee, context);

        return binder.Bind(memberAccess, context);
    }
}
