using System.Collections.Immutable;
using System.Reflection;
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

        if (callee is BoundMethodGroupExpr methodGroup &&
            arguments.All(static argument =>
                argument is not BoundNamedArgumentExpr &&
                argument is not BoundOutArgExpr))
        {
            var hasLambdas = arguments.Any(static a => a is BoundLambdaExpr);

            if (!hasLambdas)
            {
                var argumentTypes = arguments.Select(static argument => argument.StaticType.ClrType).ToArray();
                var callBinderService = new CallBinderService(context.RuntimeContext);

                var bound = methodGroup.IsStatic && methodGroup.Target is BoundLiteralExpr { Value: Type staticDeclaringType }
                    ? callBinderService.TryBindStaticCall(staticDeclaringType, methodGroup.MethodName, argumentTypes, context.IsCaseSensitive, out var callPlan)
                    : callBinderService.TryBindInstanceCall(methodGroup.DeclaringType, methodGroup.MethodName, argumentTypes, context.IsCaseSensitive, out callPlan);

                if (bound)
                    return new BoundResolvedCallExpr(callee, arguments, callPlan!.Resolution, callPlan.IsStaticCall, callPlan.IsModuleCall, new BoundType(callPlan.SelectedMethod.ReturnType));
            }
            else
            {
                var result = TryBindCallWithLambdas(callee, methodGroup, arguments, context, binder);
                if (result != null)
                    return result;
            }
        }

        return new BoundDynamicCallExpr(callee, arguments, typeArguments, BoundType.Unknown);
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

        var declaringType = methodGroup.IsStatic && methodGroup.Target is BoundLiteralExpr { Value: Type staticType }
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
