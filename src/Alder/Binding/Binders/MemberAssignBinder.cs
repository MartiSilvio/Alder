using Alder.Binding.BoundNodes;
using Alder.Binding.Services;
using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(MemberAssignExpr))]
internal static class MemberAssignBinder
{
    public static BoundExpr Bind(MemberAssignExpr expr, BindingContext context, BinderContext binder)
    {
        var target = binder.Bind(expr.Object, context);
        var value = binder.Bind(expr.Value, context);
        var (resolvedMember, declaringType) = ResolveMemberForAssignment(target.StaticType.ClrType, expr.Name.Lexeme, context);
        // §12.21.2 / CS0200: assigning to a property with no setter is a bind-time error.
        if (resolvedMember is PropertyInfo property && property.SetMethod == null)
            throw new AlderException(DiagnosticDescriptors.ReadonlyPropertyAssignment, $"{property.DeclaringType?.Name}.{property.Name}");
        return new BoundMemberAssignExpr(target, expr.Name.Lexeme, resolvedMember, declaringType, value, value.StaticType);
    }

    internal static (MemberInfo? ResolvedMember, Type? DeclaringType) ResolveMemberForAssignment(Type targetType, string memberName, BindingContext context)
    {
        if (targetType == typeof(object))
            return (null, null);

        var memberBinder = new MemberBinderService(context.RuntimeContext.TypeMetadata);
        if (!memberBinder.TryBindMemberRead(new BoundType(targetType), memberName, false, context.IsCaseSensitive,
                out var result, out var member, out _))
            return (null, null);

        return result is MemberBindResult.Property or MemberBindResult.Field
            ? (member, targetType)
            : (null, null);
    }

    internal static Type ExtractMemberType(MemberInfo? member) => member switch
    {
        PropertyInfo property => property.PropertyType,
        FieldInfo field => field.FieldType,
        _ => typeof(object)
    };
}
