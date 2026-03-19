using CsEval.Binding.BoundNodes;
using CsEval.Binding.Plans;
using CsEval.Binding.Services;
using CsEval.Parsing;
using CsEval.Runtime;
using System.Collections.Immutable;
using System.Reflection;

namespace CsEval.Binding;

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
        // Iterativize left-recursive member access chains to avoid stack overflow.
        // "a.b.c.d" parses as MemberAccess(MemberAccess(MemberAccess(a, b), c), d).
        // Collect the chain, bind the root, then fold bottom-up.
        var chain = new List<MemberAccessExpr>();
        Expr root = memberAccess;
        while (root is MemberAccessExpr ma)
        {
            chain.Add(ma);
            root = ma.Object;
        }

        var target = Bind(root, context);

        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var link = chain[i];
            target = BindSingleMemberAccess(target, link.Name.Lexeme, link.NullSafe, context);
            target = target with { Span = link.Span };
        }

        return (BoundMemberAccessExpr)target;
    }

    private BoundMemberAccessExpr BindSingleMemberAccess(BoundExpr target, string name, bool nullSafe, BindingContext context)
    {
        var (targetType, isStatic) = ResolveMemberTarget(target);

        var memberBinder = new MemberBinderService(context.RuntimeContext.TypeMetadata);
        BoundMemberPlan? plan;
        try
        {
            plan = memberBinder.BindMemberRead(targetType, name, isStatic, context.IsCaseSensitive);
        }
        catch (CsEvalException)
        {
            plan = null;
        }

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

        try
        {
            var memberBinder = new MemberBinderService(context.RuntimeContext.TypeMetadata);
            plan = memberBinder.BindIndexRead(target.StaticType, index.StaticType);
            staticType = plan.ResultType;
        }
        catch (CsEvalException)
        {
            // Keep index access in the bound pipeline with dynamic runtime dispatch when
            // static index planning is not possible (e.g., object-typed targets).
        }

        return new BoundIndexAccessExpr(target, index, plan, indexAccess.NullSafe, staticType);
    }

    private BoundExpr BindCall(CallExpr call, BindingContext context)
    {
        if (TryBindStaticModuleCall(call, context, out var staticModuleCall))
            return staticModuleCall;

        var callee = BindCallCallee(call.Callee, context);
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

            try
            {
                var plan = memberAccess.Plan.IsStatic && memberAccess.Target is BoundLiteralExpr { Value: Type staticDeclaringType }
                    ? callBinder.BindStaticCall(staticDeclaringType, memberAccess.MemberName, argumentTypes, context.IsCaseSensitive)
                    : callBinder.BindInstanceCall(memberAccess.Plan.DeclaringType, memberAccess.MemberName, argumentTypes, context.IsCaseSensitive);

                return new BoundCallExpr(callee, arguments, plan, plan.SelectedMethod.ReturnType);
            }
            catch (CsEvalException)
            {
                return new BoundInvokeExpr(callee, arguments, typeArguments, typeof(object));
            }
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

        BoundCallPlan callPlan;
        try
        {
            callPlan = callBinder.BindStaticCall(
                moduleInfo.Type,
                memberAccess.Name.Lexeme,
                argumentTypes,
                context.IsCaseSensitive) with { IsModuleCall = true };
        }
        catch (CsEvalException)
        {
            return false;
        }

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

        try
        {
            return BindMemberAccess(memberAccess, context);
        }
        catch (BindingNotSupportedException)
        {
            // Preserve call semantics for extension/dynamic method candidates by deferring
            // member resolution to invocation-time dispatch while staying in the bound pipeline.
            var target = Bind(memberAccess.Object, context);
            return new BoundMemberAccessExpr(
                target,
                memberAccess.Name.Lexeme,
                memberAccess.NullSafe,
                Plan: null,
                StaticType: typeof(object));
        }
    }

    private static (Type TargetType, bool IsStatic) ResolveMemberTarget(BoundExpr target)
    {
        if (target is BoundLiteralExpr { Value: Type staticTargetType })
            return (staticTargetType, true);

        return (target.StaticType, false);
    }
}
