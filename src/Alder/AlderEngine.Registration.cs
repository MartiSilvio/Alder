namespace Alder;

public sealed partial class AlderEngine
{
    /// <summary>
    /// Sets a variable that can be referenced by name in evaluated expressions.
    /// The value is tracked as <see cref="object"/> for binding purposes.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The variable value.</param>
    /// <returns>This engine instance, for method chaining.</returns>
    public AlderEngine SetVariable(string name, object? value)
    {
        ThrowIfDisposed();
        DefineOrStageVariable(name, value, typeof(object));
        return this;
    }

    /// <summary>
    /// Sets a strongly typed variable that can be referenced by name in evaluated expressions.
    /// The static type is preserved from <typeparamref name="T"/>, which allows more precise binding.
    /// </summary>
    /// <typeparam name="T">The type of the variable.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The variable value.</param>
    /// <returns>This engine instance, for method chaining.</returns>
    public AlderEngine SetVariable<T>(string name, T value)
    {
        ThrowIfDisposed();
        DefineOrStageVariable(name, value, typeof(T));
        return this;
    }

    /// <summary>
    /// Sets multiple variables from a dictionary.
    /// </summary>
    /// <param name="variables">A dictionary of variable names and values.</param>
    /// <returns>This engine instance, for method chaining.</returns>
    public AlderEngine SetVariables(IDictionary<string, object?> variables)
    {
        ThrowIfDisposed();
        DefineOrStageVariables(variables, typeof(object));
        return this;
    }

    /// <summary>
    /// Returns a snapshot of the modules registered on this engine.
    /// </summary>
    /// <returns>A dictionary mapping module names to their registration information.</returns>
    public IReadOnlyDictionary<string, RegisteredModule> GetRegisteredModules()
    {
        ThrowIfDisposed();
        var result = new Dictionary<string, RegisteredModule>(_config.Comparer);

        foreach (var (name, info) in _config.Modules)
        {
            result[name] = new RegisteredModule(info.Type, info.Instance, info.Members.Keys.ToArray());
        }

        return result;
    }

    /// <summary>
    /// Describes a module registered with the engine.
    /// </summary>
    /// <param name="Type">The .NET type that provides the module's methods and properties.</param>
    /// <param name="Instance">An optional pre-created instance for instance methods; <c>null</c> if the engine creates one on demand.</param>
    /// <param name="MemberNames">The member names exposed to expressions.</param>
    public sealed record RegisteredModule(Type Type, object? Instance, IReadOnlyList<string>? MemberNames);

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

    internal void SetTypedVariablesFromObject(object obj)
    {
        var entries = VariableBindingProjector.ProjectTypedVariables(obj);
        foreach (var (name, value, type) in entries)
            DefineOrStageVariable(name, value, type);
    }

    /// <summary>
    /// Sets multiple variables from a dictionary, using each value's runtime
    /// type for binding instead of erasing to <see cref="object"/>. Use this
    /// overload when injecting dynamically-sourced inputs (JSON payloads, tool
    /// arguments, user forms) so overload resolution and member access
    /// bind against the concrete types an expression actually needs.
    /// </summary>
    /// <param name="variables">Variable name/value pairs. Values of
    /// <see langword="null"/> bind as <see cref="object"/>.</param>
    /// <returns>This engine instance, for method chaining.</returns>
    public AlderEngine SetVariablesPreservingRuntimeTypes(IDictionary<string, object?> variables)
    {
        ThrowIfDisposed();
        foreach (var (name, value) in variables)
            DefineOrStageVariable(name, value, value?.GetType() ?? typeof(object));
        return this;
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
