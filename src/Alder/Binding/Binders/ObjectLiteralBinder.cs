using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(ObjectLiteralExpr))]
internal static class ObjectLiteralBinder
{
    public static BoundExpr Bind(ObjectLiteralExpr expr, BindingContext context, BinderContext binder)
    {
        var properties = expr.Properties
            .Select(property =>
            {
                var (key, value) = property;
                return new BoundObjectLiteralProperty(
                    PropertyName: key.Lexeme,
                    Value: binder.Bind(value, context));
            })
            .ToImmutableArray();

        var members = ImmutableArray.CreateBuilder<StructuralObjectMember>(properties.Length);
        var memberTypes = ImmutableDictionary.CreateBuilder<string, Type>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            var name = property.PropertyName!;
            if (memberTypes.ContainsKey(name))
                throw new AlderException(DiagnosticDescriptors.AnonymousTypeDuplicateProperty, name);

            var propertyType = property.Value.StaticType.ClrType;
            memberTypes.Add(name, propertyType);
            members.Add(new StructuralObjectMember(name, propertyType));
        }

        var structuralInfo = StructuralObjectTypeFactory.GetOrCreate(members.ToImmutable());
        var staticType = new BoundStructuralType(
            structuralInfo.RuntimeType,
            memberTypes.ToImmutable(),
            structuralInfo: structuralInfo);

        return new BoundObjectLiteralExpr(properties, staticType);
    }
}
