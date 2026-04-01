namespace Alder;

public sealed partial class AlderEngine
{
    /// <summary>
    /// Sets a variable that can be referenced by name in evaluated expressions.
    /// The variable's type is inferred as <see cref="object"/>.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The variable value.</param>
    /// <returns>This engine instance, for method chaining.</returns>
    public AlderEngine SetVariable(string name, object? value)
    {
        DefineOrStageVariable(name, value, typeof(object));
        return this;
    }

    /// <summary>
    /// Sets a strongly-typed variable that can be referenced by name in evaluated expressions.
    /// The variable's type is inferred from <typeparamref name="T"/>, enabling type-aware binding.
    /// </summary>
    /// <typeparam name="T">The type of the variable.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The variable value.</param>
    /// <returns>This engine instance, for method chaining.</returns>
    public AlderEngine SetVariable<T>(string name, T value)
    {
        DefineOrStageVariable(name, value, typeof(T));
        return this;
    }

    /// <summary>
    /// Sets multiple variables from a dictionary. Each key-value pair becomes a variable accessible in expressions.
    /// </summary>
    /// <param name="variables">A dictionary of variable names and values.</param>
    /// <returns>This engine instance, for method chaining.</returns>
    public AlderEngine SetVariables(IDictionary<string, object?> variables)
    {
        DefineOrStageVariables(variables, typeof(object));
        return this;
    }

    /// <summary>
    /// Returns a snapshot of all registered modules and their metadata.
    /// </summary>
    /// <returns>A dictionary mapping module names to their registration information.</returns>
    public IReadOnlyDictionary<string, RegisteredModule> GetRegisteredModules()
    {
        var result = new Dictionary<string, RegisteredModule>(_config.Comparer);

        foreach (var (name, info) in _config.Modules)
        {
            result[name] = new RegisteredModule(info.Type, info.Instance, info.Members);
        }

        return result;
    }

    /// <summary>
    /// Describes a module registered with the engine, including its backing type, optional singleton instance, and exposed members.
    /// </summary>
    /// <param name="Type">The .NET type that provides the module's methods and properties.</param>
    /// <param name="Instance">An optional pre-created instance for instance methods; <c>null</c> if the engine creates one on demand.</param>
    /// <param name="Members">The members exposed to expressions, keyed by name.</param>
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
