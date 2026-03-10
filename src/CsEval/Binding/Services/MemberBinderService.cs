using CsEval.Binding.Plans;
using System.Reflection;

namespace CsEval.Binding.Services;

internal sealed class MemberBinderService
{
    public BoundMemberPlan BindMemberRead(Type targetType, string memberName, bool isStatic, bool isCaseSensitive)
    {
        var flags = BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var property = targetType.GetProperty(memberName, flags);
        if (property != null)
            return new BoundMemberPlan(targetType, memberName, property, IsMethodGroup: false, isStatic);

        var field = targetType.GetField(memberName, flags);
        if (field != null)
            return new BoundMemberPlan(targetType, memberName, field, IsMethodGroup: false, isStatic);

        var hasMethods = targetType
            .GetMethods(flags)
            .Any(method => string.Equals(
                method.Name,
                memberName,
                isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
        if (hasMethods)
            return new BoundMemberPlan(targetType, memberName, Member: null, IsMethodGroup: true, isStatic);

        throw new CsEvalException($"Member '{memberName}' was not found on type '{targetType.Name}'");
    }

    public BoundIndexPlan BindIndexRead(Type targetType, Type indexType)
    {
        if (targetType == typeof(string) && indexType == typeof(int))
            return new BoundIndexPlan(targetType, indexType, typeof(char), true);

        if (typeof(IList).IsAssignableFrom(targetType) && indexType == typeof(int))
        {
            var resultType = TryResolveListElementType(targetType, out var elementType)
                ? elementType
                : typeof(object);
            return new BoundIndexPlan(targetType, indexType, resultType, true);
        }

        if (indexType == typeof(string) &&
            TryResolveStringDictionaryValueType(targetType, out var dictionaryValueType))
        {
            return new BoundIndexPlan(targetType, indexType, dictionaryValueType, true);
        }

        var indexer = targetType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
        if (indexer != null)
        {
            var parameters = indexer.GetIndexParameters();
            if (parameters.Length == 1)
            {
                return new BoundIndexPlan(
                    targetType,
                    parameters[0].ParameterType,
                    indexer.PropertyType,
                    IsDirectCollectionAccess: false);
            }
        }

        throw new CsEvalException($"No indexer found on type '{targetType.Name}'");
    }

    private static bool TryResolveListElementType(Type targetType, out Type elementType)
    {
        if (targetType.IsArray)
        {
            elementType = targetType.GetElementType() ?? typeof(object);
            return true;
        }

        var directIndexer = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
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
        foreach (var interfaceType in type.GetInterfaces())
            yield return interfaceType;
    }
}
