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
        var isDirectCollectionAccess =
            (targetType == typeof(string) && indexType == typeof(int)) ||
            (typeof(IList).IsAssignableFrom(targetType) && indexType == typeof(int)) ||
            (typeof(IDictionary<string, object?>).IsAssignableFrom(targetType) && indexType == typeof(string));

        if (isDirectCollectionAccess)
            return new BoundIndexPlan(targetType, indexType, typeof(object), true);

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
}
