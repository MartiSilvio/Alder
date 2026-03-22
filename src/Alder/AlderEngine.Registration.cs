using Alder.Runtime;

namespace Alder;

public sealed partial class AlderEngine
{
    public AlderEngine SetVariable(string name, object? value)
    {
        DefineOrStageVariable(name, value, typeof(object));
        return this;
    }

    public AlderEngine SetVariable<T>(string name, T value)
    {
        DefineOrStageVariable(name, value, typeof(T));
        return this;
    }

    public AlderEngine SetVariables(IDictionary<string, object?> variables)
    {
        DefineOrStageVariables(variables, typeof(object));
        return this;
    }

    public IReadOnlyDictionary<string, RegisteredModule> GetRegisteredModules()
    {
        var result = new Dictionary<string, RegisteredModule>(_options.StringComparer);

        foreach (var (name, info) in _config.Modules)
        {
            result[name] = new RegisteredModule(info.Type, info.Instance, info.Members);
        }

        return result;
    }

    public sealed record RegisteredModule(Type Type, object? Instance, IReadOnlyDictionary<string, MemberInfo>? Members);

    private void DefineOrStageVariable(string name, object? value, Type inferredType)
    {
        if (_context != null)
        {
            _context.Define(name, value, inferredType);
            return;
        }

        lock (_contextInitLock)
        {
            if (_context != null)
                _context.Define(name, value, inferredType);
            else
                _pendingVariables[name] = new PendingVariable(value, inferredType);
        }
    }

    private void DefineOrStageVariables(IDictionary<string, object?> variables, Type inferredType)
    {
        if (_context != null)
        {
            foreach (var (name, value) in variables)
                _context.Define(name, value, inferredType);
            return;
        }

        lock (_contextInitLock)
        {
            if (_context != null)
            {
                foreach (var (name, value) in variables)
                    _context.Define(name, value, inferredType);
            }
            else
            {
                foreach (var (name, value) in variables)
                    _pendingVariables[name] = new PendingVariable(value, inferredType);
            }
        }
    }
}
