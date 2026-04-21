using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Binding.Services;

/// <summary>
/// Resolves runtime-backed value shapes for binding without mixing that logic into lexical scope state.
/// </summary>
internal sealed class RuntimeBindingProbeService
{
    private readonly AlderContext _runtimeContext;

    public RuntimeBindingProbeService(AlderContext runtimeContext)
    {
        _runtimeContext = runtimeContext;
    }

    public bool TryGetValueType(string name, out BoundType type)
    {
        if (_runtimeContext.TryGetVariableType(name, out var declaredType) && declaredType != null)
        {
            type = new BoundType(declaredType);
            return true;
        }

        if (_runtimeContext.TryGet(name, out var runtimeValue) && runtimeValue != null)
        {
            if (runtimeValue is StructuralObjectValue structural)
            {
                var members = ImmutableDictionary.CreateBuilder<string, Type>();
                foreach (var member in structural.TypeInfo.Members)
                    members[member.Name] = member.Type;

                type = new BoundStructuralType(
                    structural.GetType(),
                    members.ToImmutable(),
                    structuralInfo: structural.TypeInfo);
                return true;
            }

            type = new BoundType(runtimeValue.GetType());
            return true;
        }

        type = BoundType.Unknown;
        return false;
    }
}
