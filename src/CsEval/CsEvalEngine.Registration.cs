using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CsEval.Aot;
using CsEval.Attributes;
using CsEval.Diagnostics;
using CsEval.Runtime;

namespace CsEval;

public sealed partial class CsEvalEngine
{
    public CsEvalEngine SetVariable(string name, object? value)
    {
        // Non-generic SetVariable intentionally keeps the compile-time type as object.
        // This preserves C# unboxing semantics for object-typed values while still
        // clearing any previous strongly-typed metadata for the same variable name.
        DefineOrStageVariable(name, value, typeof(object));
        return this;
    }

    /// <summary>
    /// Sets a variable with compile-time type information, enabling optimized expression compilation.
    /// Use this overload when the variable type is known at call time for better performance.
    /// </summary>
    public CsEvalEngine SetVariable<T>(string name, T value)
    {
        DefineOrStageVariable(name, value, typeof(T));
        return this;
    }

    public CsEvalEngine SetVariables(IDictionary<string, object?> variables)
    {
        DefineOrStageVariables(variables, typeof(object));
        return this;
    }

    public CsEvalEngine RegisterFunction(string name, Func<object?[], object?> function)
    {
        EnsureNotFrozen();
        _functions[name] = function;
        return this;
    }

    [RequiresUnreferencedCode("Registering from assembly scans all types and members via reflection.")]
    public CsEvalEngine RegisterFromAssembly(Assembly assembly)
    {
        EnsureNotFrozen();
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            var isModule = type.GetCustomAttribute<CsEvalModuleAttribute>() != null;
            var hasGlobalFunctions = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Any(m => m.GetCustomAttribute<CsEvalFunctionAttribute>() != null);

            if (!isModule && !hasGlobalFunctions)
                continue;

            var hasStaticOnly = !isModule &&
                                type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                    .All(m => m.GetCustomAttribute<CsEvalFunctionAttribute>() != null);

            if (!hasStaticOnly && type.GetConstructor(Type.EmptyTypes) == null)
                continue;

            _registeredTypes.Add(new RegisteredType(type, null, null, ModuleMemberMetadata.Build(type, explicitOnly: false, _options.StringComparer)));
        }

        return this;
    }

    public CsEvalEngine RegisterFromType(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        object? instance = null)
    {
        EnsureNotFrozen();
        _registeredTypes.Add(new RegisteredType(type, instance, null, ModuleMemberMetadata.Build(type, explicitOnly: false, _options.StringComparer)));
        return this;
    }

    public CsEvalEngine RegisterFromType<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(T? instance = default) where T : class
    {
        return RegisterFromType(typeof(T), instance);
    }

    public CsEvalEngine RegisterModule(
        string moduleName,
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        bool explicitOnly = false,
        object? instance = null)
    {
        EnsureNotFrozen();
        if (!explicitOnly)
        {
            var moduleAttr = type.GetCustomAttribute<CsEvalModuleAttribute>();
            explicitOnly = moduleAttr?.ExplicitOnly ?? false;
        }
        var methods = ModuleMemberMetadata.Build(type, explicitOnly, _options.StringComparer);
        _registeredTypes.Add(new RegisteredType(type, instance, moduleName, methods));
        return this;
    }

    public CsEvalEngine RegisterModule<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(string moduleName, bool explicitOnly = false, T? instance = default) where T : class
    {
        return RegisterModule(moduleName, typeof(T), explicitOnly, instance);
    }

    public CsEvalEngine RegisterModule(
        string moduleName,
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        IReadOnlyDictionary<string, MemberInfo> members)
    {
        EnsureNotFrozen();
        _registeredTypes.Add(new RegisteredType(type, null, moduleName, members));
        return this;
    }

    public CsEvalEngine RegisterAssembly(Assembly assembly)
    {
        EnsureNotFrozen();
        _assemblies.Add(assembly);
        return this;
    }

    public CsEvalEngine RegisterNamespace(string namespaceName)
    {
        EnsureNotFrozen();
        _usingNamespaces.Add(namespaceName);
        return this;
    }

    public CsEvalEngine RegisterExtensionMethods(Type type)
    {
        EnsureNotFrozen();
        if (!_extensionTypes.Contains(type))
            _extensionTypes.Insert(0, type);
        return this;
    }

    public CsEvalEngine RegisterExtensionMethods<T>() => RegisterExtensionMethods(typeof(T));

    public CsEvalEngine UseGeneratedContext(CsEvalTypeContext context)
    {
        EnsureNotFrozen();
        _additionalContexts.Add(context);
        return this;
    }

    public CsEvalEngine ClearGeneratedContexts()
    {
        EnsureNotFrozen();
        _generatedContext = null;
        _additionalContexts.Clear();
        return this;
    }

    public IReadOnlyDictionary<string, RegisteredModule> GetRegisteredModules()
    {
        var config = GetOrCreateConfig();
        var result = new Dictionary<string, RegisteredModule>(_options.StringComparer);

        foreach (var (name, info) in config.Modules)
        {
            result[name] = new RegisteredModule(info.Type, info.Instance, info.Members);
        }

        return result;
    }

    public sealed record RegisteredModule(Type Type, object? Instance, IReadOnlyDictionary<string, MemberInfo>? Members);

    private void RegisterGlobalFunctions(RegisteredType reg)
    {
        var methods = reg.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<CsEvalFunctionAttribute>();
            if (attr == null) continue;

            var functionName = attr.Name ?? method.Name;
            var moduleInfo = method.IsStatic ? null : new ModuleInfo(reg.Type, reg.Instance, reg.Members);
            _functions[functionName] = CreateFunctionDelegate(method, moduleInfo);
        }
    }

    private static Func<object?[], object?> CreateFunctionDelegate(MethodInfo method, ModuleInfo? moduleInfo)
    {
        return args =>
        {
            var parameters = method.GetParameters();
            var finalArgs = PadWithDefaults(parameters, args, method.Name);
            return method.Invoke(moduleInfo?.Resolve(null), finalArgs);
        };
    }

    private static object?[] PadWithDefaults(ParameterInfo[] parameters, object?[] args, string callableName)
    {
        var result = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            if (i < args.Length)
            {
                result[i] = TypeHelpers.CoerceNumeric(args[i], parameters[i].ParameterType);
            }
            else if (parameters[i].HasDefaultValue)
            {
                result[i] = parameters[i].DefaultValue;
            }
            else
            {
                throw new CsEvalException(
                    DiagnosticDescriptors.MissingRequiredArgument,
                    parameters[i].Name,
                    callableName);
            }
        }

        return result;
    }

    private void EnsureNotFrozen([CallerMemberName] string? caller = null)
    {
        if (_frozenConfig != null)
            throw new InvalidOperationException(
                $"Cannot call {caller} after evaluation has started. " +
                $"Call {caller} before the first Evaluate().");
    }

    private void RegisterBuiltInModules()
    {
        var mathMembers = ModuleMemberMetadata.GetBuiltInMathMembers(_options.StringComparer);
        var convertMembers = ModuleMemberMetadata.GetBuiltInConvertMembers(_options.StringComparer);

        _registeredTypes.Add(new RegisteredType(typeof(Math), null, "Math", mathMembers));
        _registeredTypes.Add(new RegisteredType(typeof(Convert), null, "Convert", convertMembers));
    }

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

    private sealed record RegisteredType(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)]
        [property: DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)]
        Type Type,
        object? Instance,
        string? ModuleName,
        IReadOnlyDictionary<string, MemberInfo> Members);
}
