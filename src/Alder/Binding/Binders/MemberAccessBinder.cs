using System.Reflection;
using Alder.Binding.BoundNodes;
using Alder.Binding.Services;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class MemberAccessBinder : INodeBinder<MemberAccessExpr>
{
    public BoundExpr Bind(MemberAccessExpr expr, BindingContext context, BinderContext binder)
    {
        var memberChain = new List<MemberAccessExpr>();
        var callAfter = new List<CallExpr?>();
        CallExpr? pendingCall = null;
        Expr root = expr;

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

        BoundExpr target = binder.Bind(root, context);

        for (var i = memberChain.Count - 1; i > 0; i--)
        {
            var link = memberChain[i];
            target = BindSingleMemberAccess(target, link.Name.Lexeme, link.NullSafe, context);
            target = target with { Span = link.Span };

            if (callAfter[i] is { } callExpr)
                target = CallBinder.BindCallWithBoundCallee(target, callExpr, context, binder);
        }

        var outer = memberChain[0];
        var result = BindSingleMemberAccess(target, outer.Name.Lexeme, outer.NullSafe, context);
        return result with { Span = outer.Span };
    }

    internal static BoundMemberAccessBase BindSingleMemberAccess(BoundExpr target, string name, bool nullSafe, BindingContext context)
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

    internal static (BoundType TargetType, bool IsStatic) ResolveMemberTarget(BoundExpr target)
    {
        if (target is BoundLiteralExpr { Value: Type staticTargetType })
            return (new BoundType(staticTargetType), true);

        return (target.StaticType, false);
    }
}
