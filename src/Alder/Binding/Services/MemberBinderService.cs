using Alder.Binding.Plans;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Binding.Services;

internal sealed class MemberBinderService
{
    private readonly TypeMetadataProvider _typeMetadata;

    public MemberBinderService(TypeMetadataProvider typeMetadata)
    {
        _typeMetadata = typeMetadata;
    }

    public bool TryBindMemberRead(Type targetType, string memberName, bool isStatic, bool isCaseSensitive, out BoundMemberPlan? plan)
    {
        var flags = BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var property = _typeMetadata.GetProperty(targetType, memberName, flags);
        if (property != null)
        {
            plan = new BoundMemberPlan(targetType, memberName, property, IsMethodGroup: false, isStatic);
            return true;
        }

        var field = _typeMetadata.GetField(targetType, memberName, flags);
        if (field != null)
        {
            plan = new BoundMemberPlan(targetType, memberName, field, IsMethodGroup: false, isStatic);
            return true;
        }

        var hasMethods = _typeMetadata.GetMethods(targetType, memberName, flags).Length > 0;
        if (hasMethods)
        {
            plan = new BoundMemberPlan(targetType, memberName, Member: null, IsMethodGroup: true, isStatic);
            return true;
        }

        plan = null;
        return false;
    }

    public BoundMemberPlan BindMemberRead(Type targetType, string memberName, bool isStatic, bool isCaseSensitive)
    {
        if (TryBindMemberRead(targetType, memberName, isStatic, isCaseSensitive, out var plan))
            return plan!;

        throw new AlderException(DiagnosticDescriptors.MemberNotFound, targetType.Name, memberName);
    }

    public bool TryBindIndexRead(Type targetType, Type indexType, out BoundIndexPlan? plan)
    {
        if (targetType == typeof(string) && indexType == typeof(int))
        {
            plan = new BoundIndexPlan(targetType, indexType, typeof(char), true);
            return true;
        }

        if (typeof(IList).IsAssignableFrom(targetType) && indexType == typeof(int))
        {
            var resultType = TryResolveListElementType(targetType, out var elementType)
                ? elementType
                : typeof(object);
            plan = new BoundIndexPlan(targetType, indexType, resultType, true);
            return true;
        }

        if (indexType == typeof(string) &&
            TryResolveStringDictionaryValueType(targetType, out var dictionaryValueType))
        {
            plan = new BoundIndexPlan(targetType, indexType, dictionaryValueType, true);
            return true;
        }

        var indexer = _typeMetadata.GetIndexer(targetType);
        if (indexer != null)
        {
            var parameters = indexer.GetIndexParameters();
            if (parameters.Length == 1)
            {
                plan = new BoundIndexPlan(
                    targetType,
                    parameters[0].ParameterType,
                    indexer.PropertyType,
                    IsDirectCollectionAccess: false);
                return true;
            }
        }

        plan = null;
        return false;
    }

    public BoundIndexPlan BindIndexRead(Type targetType, Type indexType)
    {
        if (TryBindIndexRead(targetType, indexType, out var plan))
            return plan!;

        throw new AlderException(DiagnosticDescriptors.NoIndexerOnType, targetType.Name);
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
        foreach (var interfaceType in ReflectionRuntime.GetInterfaces(type))
            yield return interfaceType;
    }
}
