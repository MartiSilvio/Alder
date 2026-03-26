using System.Collections.Immutable;
using System.Reflection;
using Alder.Binding.BoundNodes;
using Alder.Binding.Services;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding;

internal sealed partial class Binder
{
    private static BoundLambdaExpr BindLambda(LambdaExpr lambda)
    {
        return new BoundLambdaExpr(
            [..lambda.Parameters.Select(static parameter => parameter.Name.Lexeme)],
            lambda.Body,
            new BoundType(typeof(LambdaValue)));
    }

    private BoundExpr BindPipeline(PipelineExpr pipeline, BindingContext context)
    {
        if (pipeline.Right is IdentifierExpr rightIdentifier)
        {
            var call = new CallExpr(rightIdentifier, [pipeline.Left]);
            return BindCall(call, context);
        }

        if (pipeline.Right is CallExpr rightCall)
        {
            var args = new List<Expr>(rightCall.Arguments.Count + 1) { pipeline.Left };
            args.AddRange(rightCall.Arguments);
            var call = new CallExpr(rightCall.Callee, args, rightCall.TypeArguments);
            return BindCall(call, context);
        }

        return new BoundPipelineExpr(
            Bind(pipeline.Left, context),
            Bind(pipeline.Right, context),
            BoundType.Unknown);
    }

    private BoundNamedArgumentExpr BindNamedArgument(NamedArgumentExpr namedArgument, BindingContext context)
    {
        var value = Bind(namedArgument.Value, context);
        return new BoundNamedArgumentExpr(namedArgument.Name.Lexeme, value, BoundType.Unknown);
    }

    private static BoundOutArgExpr BindOutArg(OutArgExpr outArg)
    {
        return new BoundOutArgExpr(outArg.VariableName, outArg.TypeName, outArg.IsDiscard, BoundType.Unknown);
    }

    private BoundMemberAccessBase BindMemberAccess(MemberAccessExpr memberAccess, BindingContext context)
    {
        var memberChain = new List<MemberAccessExpr>();
        var callAfter = new List<CallExpr?>();
        CallExpr? pendingCall = null;
        Expr root = memberAccess;

        while (root is MemberAccessExpr ma)
        {
            memberChain.Add(ma);
            callAfter.Add(pendingCall);
            pendingCall = null;

            if (ma.Object is CallExpr call && call.Callee is MemberAccessExpr)
            {
                pendingCall = call;
                root = call.Callee;
            }
            else
            {
                root = ma.Object;
            }
        }

        BoundExpr target = Bind(root, context);

        for (var i = memberChain.Count - 1; i > 0; i--)
        {
            var link = memberChain[i];
            target = BindSingleMemberAccess(target, link.Name.Lexeme, link.NullSafe, context);
            target = target with { Span = link.Span };

            if (callAfter[i] is { } callExpr)
                target = BindCallWithBoundCallee(target, callExpr, context);
        }

        var outer = memberChain[0];
        var result = BindSingleMemberAccess(target, outer.Name.Lexeme, outer.NullSafe, context);
        return result with { Span = outer.Span };
    }

    private BoundMemberAccessBase BindSingleMemberAccess(BoundExpr target, string name, bool nullSafe, BindingContext context)
    {
        var (targetBoundType, isStatic) = ResolveMemberTarget(target);

        var memberBinder = new MemberBinderService(context.RuntimeContext.TypeMetadata);
        memberBinder.TryBindMemberRead(targetBoundType, name, isStatic, context.IsCaseSensitive,
            out var bindResult, out var member, out var resolvedType);

        if (resolvedType == null &&
            target is BoundIdentifierExpr identifier &&
            context.RuntimeContext.Modules.TryGetValue(identifier.Name, out var moduleInfo))
        {
            memberBinder.TryBindMemberRead(new BoundType(moduleInfo.Type), name, true, context.IsCaseSensitive,
                out _, out _, out resolvedType);
        }

        var staticType = resolvedType != null
            ? new BoundType(resolvedType)
            : BoundType.Unknown;

        return bindResult switch
        {
            MemberBindResult.Property => new BoundPropertyAccessExpr(
                target, (PropertyInfo)member!, nullSafe, isStatic, staticType),
            MemberBindResult.Field => new BoundFieldAccessExpr(
                target, (FieldInfo)member!, nullSafe, isStatic, staticType),
            MemberBindResult.MethodGroup => new BoundMethodGroupExpr(
                target, targetBoundType.ClrType, name, nullSafe, isStatic, staticType),
            _ => new BoundDynamicMemberAccessExpr(target, name, nullSafe, staticType)
        };
    }

    private BoundExpr BindIndexAccess(IndexAccessExpr indexAccess, BindingContext context)
    {
        var target = Bind(indexAccess.Object, context);
        var index = Bind(indexAccess.Index, context);

        var memberBinder = new MemberBinderService(context.RuntimeContext.TypeMetadata);
        if (memberBinder.TryBindIndexRead(target.StaticType.ClrType, index.StaticType.ClrType, out var indexPlan))
        {
            return new BoundResolvedIndexAccessExpr(
                target, index,
                indexPlan!.TargetType, indexPlan.ResultType, indexPlan.IsDirectCollectionAccess,
                indexAccess.NullSafe, new BoundType(indexPlan.ResultType));
        }

        return new BoundDynamicIndexAccessExpr(target, index, indexAccess.NullSafe, BoundType.Unknown);
    }

    private BoundExpr BindCall(CallExpr call, BindingContext context)
    {
        if (TryBindStaticModuleCall(call, context, out var staticModuleCall))
            return staticModuleCall;

        var callee = BindCallCallee(call.Callee, context);
        return BindCallWithBoundCallee(callee, call, context);
    }

    private BoundExpr BindCallWithBoundCallee(BoundExpr callee, CallExpr call, BindingContext context)
    {
        var arguments = call.Arguments
            .Select(argument => Bind(argument, context))
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

    private bool TryBindStaticModuleCall(CallExpr call, BindingContext context, out BoundExpr boundCall)
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
            .Select(argument => Bind(argument, context))
            .ToImmutableArray();
        if (arguments.Any(static argument => argument is BoundLambdaExpr))
            return false;

        var argumentTypes = arguments.Select(static argument => argument.StaticType.ClrType).ToArray();
        var callBinder = new CallBinderService(context.RuntimeContext);

        if (!callBinder.TryBindStaticCall(
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

    private BoundExpr BindCallCallee(Expr callee, BindingContext context)
    {
        if (callee is not MemberAccessExpr memberAccess)
            return Bind(callee, context);

        return BindMemberAccess(memberAccess, context);
    }

    private static (BoundType TargetType, bool IsStatic) ResolveMemberTarget(BoundExpr target)
    {
        if (target is BoundLiteralExpr { Value: Type staticTargetType })
            return (new BoundType(staticTargetType), true);

        return (target.StaticType, false);
    }
}
