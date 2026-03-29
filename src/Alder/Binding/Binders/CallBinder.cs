using System.Collections.Immutable;
using System.Reflection;
using Alder.Binding.BoundNodes;
using Alder.Binding.Services;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class CallBinder : INodeBinder<CallExpr>
{
    public BoundExpr Bind(CallExpr expr, BindingContext context, BinderContext binder)
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
                argument is not BoundLambdaExpr &&
                argument is not BoundNamedArgumentExpr &&
                argument is not BoundOutArgExpr))
        {
            var argumentTypes = arguments.Select(static argument => argument.StaticType.ClrType).ToArray();
            var callBinder = new CallBinderService(context.RuntimeContext);

            var bound = methodGroup.IsStatic && methodGroup.Target is BoundLiteralExpr { Value: Type staticDeclaringType }
                ? callBinder.TryBindStaticCall(staticDeclaringType, methodGroup.MethodName, argumentTypes, context.IsCaseSensitive, out var callPlan)
                : callBinder.TryBindInstanceCall(methodGroup.DeclaringType, methodGroup.MethodName, argumentTypes, context.IsCaseSensitive, out callPlan);

            if (bound)
                return new BoundResolvedCallExpr(callee, arguments, callPlan!.Resolution, callPlan.IsStaticCall, callPlan.IsModuleCall, new BoundType(callPlan.SelectedMethod.ReturnType));

            return new BoundDynamicCallExpr(callee, arguments, typeArguments, BoundType.Unknown);
        }

        return new BoundDynamicCallExpr(callee, arguments, typeArguments, BoundType.Unknown);
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
