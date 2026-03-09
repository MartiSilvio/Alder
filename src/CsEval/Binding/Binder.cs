using CsEval.Binding.BoundNodes;
using CsEval.Binding.Plans;
using CsEval.Binding.Services;
using CsEval.Parsing;
using CsEval.Runtime;
using System.Collections.Immutable;
using System.Reflection;

namespace CsEval.Binding;

internal sealed class Binder
{
    public BoundExpr Bind(Expr expr, BindingContext context)
    {
        ArgumentNullException.ThrowIfNull(expr);
        ArgumentNullException.ThrowIfNull(context);

        return expr switch
        {
            LiteralExpr literal => BoundLiteralExpr.FromValue(literal.Value),
            IdentifierExpr identifier => BindIdentifier(identifier, context),
            TypeReferenceExpr typeReference => BindTypeReference(typeReference, context),
            BinaryExpr binary => BindBinary(binary, context),
            MemberAccessExpr memberAccess => BindMemberAccess(memberAccess, context),
            IndexAccessExpr indexAccess => BindIndexAccess(indexAccess, context),
            CallExpr call => BindCall(call, context),
            _ => throw new BindingNotSupportedException(
                $"Binding for expression type '{expr.GetType().Name}' is not implemented")
        };
    }

    private static BoundLiteralExpr BindTypeReference(TypeReferenceExpr typeReference, BindingContext context)
    {
        var resolvedType = context.RuntimeContext.TypeResolver.ResolveType(typeReference.TypeToken.Lexeme);
        return new BoundLiteralExpr(resolvedType, typeof(Type));
    }

    private static BoundExpr BindIdentifier(IdentifierExpr identifier, BindingContext context)
    {
        var name = identifier.Name.Lexeme;
        var resolvedType = context.RuntimeContext.TypeResolver.TryResolveType(name);
        if (resolvedType != null)
            return new BoundLiteralExpr(resolvedType, typeof(Type));

        context.TryGetVariableType(name, out var staticType);
        return new BoundIdentifierExpr(name, staticType);
    }

    private BoundBinaryExpr BindBinary(BinaryExpr binary, BindingContext context)
    {
        var left = Bind(binary.Left, context);
        var right = Bind(binary.Right, context);
        var resultType = InferBinaryResultType(binary.Op.Type, left.StaticType, right.StaticType);

        return new BoundBinaryExpr(binary.Op.Type, left, right, resultType);
    }

    private static Type InferBinaryResultType(TokenType op, Type leftType, Type rightType)
    {
        if (op == TokenType.Plus && (leftType == typeof(string) || rightType == typeof(string)))
            return typeof(string);

        if (TypeHelpers.IsArithmetic(leftType) && TypeHelpers.IsArithmetic(rightType))
            return leftType == rightType ? leftType : typeof(object);

        return typeof(object);
    }

    private BoundMemberAccessExpr BindMemberAccess(MemberAccessExpr memberAccess, BindingContext context)
    {
        var target = Bind(memberAccess.Object, context);
        var (targetType, isStatic) = ResolveMemberTarget(target);

        var memberBinder = new MemberBinderService();
        BoundMemberPlan plan;
        try
        {
            plan = memberBinder.BindMemberRead(targetType, memberAccess.Name.Lexeme, isStatic, context.IsCaseSensitive);
        }
        catch (CsEvalException ex)
        {
            throw new BindingNotSupportedException(ex.Message);
        }

        var staticType = plan.Member switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            _ => typeof(object)
        };

        return new BoundMemberAccessExpr(target, memberAccess.Name.Lexeme, plan, staticType);
    }

    private BoundIndexAccessExpr BindIndexAccess(IndexAccessExpr indexAccess, BindingContext context)
    {
        var target = Bind(indexAccess.Object, context);
        var index = Bind(indexAccess.Index, context);

        var memberBinder = new MemberBinderService();
        var plan = memberBinder.BindIndexRead(target.StaticType, index.StaticType);

        return new BoundIndexAccessExpr(target, index, plan, plan.ResultType);
    }

    private BoundCallExpr BindCall(CallExpr call, BindingContext context)
    {
        var callee = Bind(call.Callee, context);
        var arguments = call.Arguments
            .Select(argument => Bind(argument, context))
            .ToImmutableArray();

        if (callee is BoundMemberAccessExpr memberAccess && memberAccess.Plan.IsMethodGroup)
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
            catch (CsEvalException ex)
            {
                throw new BindingNotSupportedException(ex.Message);
            }
        }

        throw new BindingNotSupportedException("Only method-group call binding is currently supported");
    }

    private static (Type TargetType, bool IsStatic) ResolveMemberTarget(BoundExpr target)
    {
        if (target is BoundLiteralExpr { Value: Type staticTargetType })
            return (staticTargetType, true);

        return (target.StaticType, false);
    }
}
