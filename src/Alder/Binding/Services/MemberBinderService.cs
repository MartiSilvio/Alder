using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Binding.Services;

internal enum MemberBindResult { NotFound, Property, Field, MethodGroup, StructuralMember }

internal sealed record IndexBindResult(
    Type TargetType,
    Type ResultType,
    bool IsDirectCollectionAccess);

internal sealed class MemberBinderService
{
    private readonly TypeMetadataProvider _typeMetadata;

    public MemberBinderService(TypeMetadataProvider typeMetadata)
    {
        _typeMetadata = typeMetadata;
    }

    public bool TryBindMemberRead(BoundType targetType, string memberName, bool isStatic, bool isCaseSensitive,
        out MemberBindResult result, out MemberInfo? member, out Type? resolvedType)
    {
        if (targetType.HasStructuralMembers)
        {
            var comparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            foreach (var kvp in targetType.MemberTypes!)
            {
                if (comparer.Equals(kvp.Key, memberName))
                {
                    resolvedType = kvp.Value;
                    result = MemberBindResult.StructuralMember;
                    member = null;
                    return true;
                }
            }
        }

        if (TryBindMemberReadFromClrType(targetType.ClrType, memberName, isStatic, isCaseSensitive,
                out result, out member, out resolvedType))
            return true;

        return false;
    }

    private bool TryBindMemberReadFromClrType(Type clrType, string memberName, bool isStatic, bool isCaseSensitive,
        out MemberBindResult result, out MemberInfo? member, out Type? resolvedType)
    {
        var flags = BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var property = _typeMetadata.GetProperty(clrType, memberName, flags);
        if (property != null)
        {
            result = MemberBindResult.Property;
            member = property;
            resolvedType = property.PropertyType;
            return true;
        }

        var field = _typeMetadata.GetField(clrType, memberName, flags);
        if (field != null)
        {
            result = MemberBindResult.Field;
            member = field;
            resolvedType = field.FieldType;
            return true;
        }

        var hasMethods = _typeMetadata.GetMethods(clrType, memberName, flags).Length > 0;
        if (hasMethods)
        {
            result = MemberBindResult.MethodGroup;
            member = null;
            resolvedType = null;
            return true;
        }

        result = MemberBindResult.NotFound;
        member = null;
        resolvedType = null;
        return false;
    }

    public MemberBindResult BindMemberRead(Type targetType, string memberName, bool isStatic, bool isCaseSensitive,
        out MemberInfo? member)
    {
        if (TryBindMemberReadFromClrType(targetType, memberName, isStatic, isCaseSensitive,
                out var result, out member, out _))
            return result;

        throw new AlderException(DiagnosticDescriptors.MemberNotFound, targetType.Name, memberName);
    }

    public bool TryBindIndexRead(Type targetType, Type indexType, out IndexBindResult? result)
    {
        if (targetType == typeof(string) && indexType == typeof(int))
        {
            result = new IndexBindResult(targetType, typeof(char), true);
            return true;
        }

        if (typeof(IList).IsAssignableFrom(targetType) && indexType == typeof(int))
        {
            var resultType = TryResolveListElementType(targetType, out var elementType)
                ? elementType
                : typeof(object);
            result = new IndexBindResult(targetType, resultType, true);
            return true;
        }

        if (indexType == typeof(string) &&
            TryResolveStringDictionaryValueType(targetType, out var dictionaryValueType))
        {
            result = new IndexBindResult(targetType, dictionaryValueType, true);
            return true;
        }

        var indexer = _typeMetadata.GetIndexer(targetType);
        if (indexer != null)
        {
            var parameters = indexer.GetIndexParameters();
            if (parameters.Length == 1)
            {
                result = new IndexBindResult(targetType, indexer.PropertyType, false);
                return true;
            }
        }

        result = null;
        return false;
    }

    public IndexBindResult BindIndexRead(Type targetType, Type indexType)
    {
        if (TryBindIndexRead(targetType, indexType, out var result))
            return result!;

        throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, targetType.Name);
    }

    private bool TryResolveListElementType(Type targetType, out Type elementType)
    {
        if (targetType.IsArray)
        {
            elementType = targetType.GetElementType() ?? typeof(object);
            return true;
        }

        var directIndexer = _typeMetadata.GetProperties(targetType, BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(property =>
            {
                if (!string.Equals(property.Name, "Item", StringComparison.Ordinal))
                    return false;
                var parameters = property.GetIndexParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(int);
            });
        if (directIndexer != null)
        {
            elementType = directIndexer.PropertyType;
            return true;
        }

        foreach (var candidate in EnumerateSelfAndInterfaces(targetType))
        {
            if (!candidate.IsGenericType)
                continue;

            var genericDef = candidate.GetGenericTypeDefinition();
            if (genericDef == typeof(IList<>) || genericDef == typeof(IReadOnlyList<>))
            {
                elementType = candidate.GetGenericArguments()[0];
                return true;
            }
        }

        elementType = typeof(object);
        return false;
    }

    private static bool TryResolveStringDictionaryValueType(Type targetType, out Type valueType)
    {
        foreach (var candidate in EnumerateSelfAndInterfaces(targetType))
        {
            if (!candidate.IsGenericType)
                continue;

            var genericDef = candidate.GetGenericTypeDefinition();
            if (genericDef != typeof(IDictionary<,>) &&
                genericDef != typeof(IReadOnlyDictionary<,>))
            {
                continue;
            }

            var args = candidate.GetGenericArguments();
            if (args[0] != typeof(string))
                continue;

            valueType = args[1];
            return true;
        }

        valueType = typeof(object);
        return false;
    }

    private static IEnumerable<Type> EnumerateSelfAndInterfaces(Type type)
    {
        yield return type;
        foreach (var interfaceType in RuntimeTypeIntrospection.GetInterfaces(type))
            yield return interfaceType;
    }
}
