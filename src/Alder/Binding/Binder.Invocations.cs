using System.Collections.Immutable;
using System.Reflection;
using Alder.Binding.BoundNodes;
using Alder.Binding.Plans;
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
            typeof(LambdaValue));
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
            typeof(object));
    }

    private BoundNamedArgumentExpr BindNamedArgument(NamedArgumentExpr namedArgument, BindingContext context)
    {
        var value = Bind(namedArgument.Value, context);
        return new BoundNamedArgumentExpr(namedArgument.Name.Lexeme, value, typeof(object));
    }

    private static BoundOutArgExpr BindOutArg(OutArgExpr outArg)
    {
        return new BoundOutArgExpr(outArg.VariableName, outArg.TypeName, outArg.IsDiscard, typeof(object));
    }

    private BoundMemberAccessExpr BindMemberAccess(MemberAccessExpr memberAccess, BindingContext context)
    {
        // Iterativize left-recursive member/call chains to avoid stack overflow.
        // "a.b.c.d" parses as MemberAccess(MemberAccess(MemberAccess(a, b), c), d).
        // "a.B().C()" interleaves MemberAccess and CallExpr nodes.
        // Collect the full postfix spine, bind the root, then fold bottom-up.
        //
        // When MA("c").Object is CallExpr(callee: MA("B")), the call belongs to MA("B")
        // (the callee), not MA("c") (the consumer of the result). We defer recording the
        // call via pendingCall until we reach the callee member access.
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

    private BoundMemberAccessExpr BindSingleMemberAccess(BoundExpr target, string name, bool nullSafe, BindingContext context)
    {
        var (targetType, isStatic) = ResolveMemberTarget(target);

        var memberBinder = new MemberBinderService(context.RuntimeContext.TypeMetadata);
        memberBinder.TryBindMemberRead(targetType, name, isStatic, context.IsCaseSensitive, out var plan);

        var staticType = plan?.Member switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            _ => typeof(object)
        };

        return new BoundMemberAccessExpr(target, name, nullSafe, plan, staticType);
    }

    private BoundIndexAccessExpr BindIndexAccess(IndexAccessExpr indexAccess, BindingContext context)
    {
        var target = Bind(indexAccess.Object, context);
        var index = Bind(indexAccess.Index, context);

        BoundIndexPlan? plan = null;
        var staticType = typeof(object);

        var memberBinder = new MemberBinderService(context.RuntimeContext.TypeMetadata);
        if (memberBinder.TryBindIndexRead(target.StaticType, index.StaticType, out var indexPlan))
        {
            plan = indexPlan;
            staticType = indexPlan!.ResultType;
        }

        return new BoundIndexAccessExpr(target, index, plan, indexAccess.NullSafe, staticType);
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

        if (callee is BoundMemberAccessExpr { Plan.IsMethodGroup: true } memberAccess &&
            arguments.All(static argument =>
                argument is not BoundLambdaExpr &&
                argument is not BoundNamedArgumentExpr &&
                argument is not BoundOutArgExpr))
        {
            var argumentTypes = arguments.Select(static argument => argument.StaticType).ToArray();
            var callBinder = new CallBinderService(context.RuntimeContext);

            var bound = memberAccess.Plan.IsStatic && memberAccess.Target is BoundLiteralExpr { Value: Type staticDeclaringType }
                ? callBinder.TryBindStaticCall(staticDeclaringType, memberAccess.MemberName, argumentTypes, context.IsCaseSensitive, out var callPlan)
                : callBinder.TryBindInstanceCall(memberAccess.Plan.DeclaringType, memberAccess.MemberName, argumentTypes, context.IsCaseSensitive, out callPlan);

            if (bound)
                return new BoundCallExpr(callee, arguments, callPlan!, callPlan!.SelectedMethod.ReturnType);

            return new BoundInvokeExpr(callee, arguments, typeArguments, typeof(object));
        }

        return new BoundInvokeExpr(callee, arguments, typeArguments, typeof(object));
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

        var argumentTypes = arguments.Select(static argument => argument.StaticType).ToArray();
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

        var callPlan = moduleCallPlan! with { IsModuleCall = true };

        var callee = new BoundMemberAccessExpr(
            new BoundLiteralExpr(moduleInfo.Type, typeof(Type)),
            memberAccess.Name.Lexeme,
            memberAccess.NullSafe,
            new BoundMemberPlan(
                moduleInfo.Type,
                memberAccess.Name.Lexeme,
                Member: null,
                IsMethodGroup: true,
                IsStatic: true),
            typeof(object));

        boundCall = new BoundCallExpr(callee, arguments, callPlan, callPlan.SelectedMethod.ReturnType);
        return true;
    }

    private BoundExpr BindCallCallee(Expr callee, BindingContext context)
    {
        if (callee is not MemberAccessExpr memberAccess)
            return Bind(callee, context);

        return BindMemberAccess(memberAccess, context);
    }

    private static (Type TargetType, bool IsStatic) ResolveMemberTarget(BoundExpr target)
    {
        if (target is BoundLiteralExpr { Value: Type staticTargetType })
            return (staticTargetType, true);

        return (target.StaticType, false);
    }
}
