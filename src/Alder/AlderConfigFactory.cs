using Alder.Aot;
using Alder.Attributes;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder;

internal static class AlderConfigFactory
{
    internal static AlderConfig Build(AlderOptions options)
    {
        var functions = new Dictionary<string, Func<object?[], object?>>(options.StringComparer);
        foreach (var kvp in options.Functions.RegisteredFunctions)
            functions[kvp.Key] = kvp.Value;

        var modules = new Dictionary<string, ModuleInfo>(options.StringComparer);

        foreach (var reg in options.Modules.RegisteredTypes)
        {
            var moduleName = reg.ModuleName ?? reg.Type.GetCustomAttribute<AlderModuleAttribute>()?.Name;
            if (moduleName != null)
            {
                modules[moduleName] = new ModuleInfo(reg.Type, reg.Instance, reg.Members);
            }
            else
            {
                RegisterGlobalFunctions(reg, functions);
            }
        }

        var typeResolver = TypeResolver.Create(
            [..options.Types.Assemblies],
            [..options.Types.Namespaces],
            true,
            options.StringComparer);

        var dispatchCatalog = BuildDispatchCatalog(options.Aot);
        var typeMetadata = new TypeMetadataProvider();

        return new AlderConfig(
            options.LanguageMode,
            options.Security.ToSecurityPolicy(),
            options.IsCaseSensitive,
            options.Constraints,
            options.Compiler,
            options.ExpressionCompiler,
            options.ServiceProvider,
            Runtime.Collections.FixedDictionary<string, Func<object?[], object?>>.Create(functions),
            Runtime.Collections.FixedDictionary<string, ModuleInfo>.Create(modules),
            [..options.Types.ExtensionTypes],
            typeMetadata,
            typeResolver,
            dispatchCatalog.TypeDispatch,
            dispatchCatalog.GenericStaticDispatch,
            dispatchCatalog.DelegateFactories,
            dispatchCatalog.RootedDelegateTypes);
    }

    private static void RegisterGlobalFunctions(
        AlderOptions.RegisteredType registration,
        Dictionary<string, Func<object?[], object?>> functions)
    {
        var methods = registration.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<AlderFunctionAttribute>();
            if (attr == null)
                continue;

            var functionName = attr.Name ?? method.Name;
            var moduleInfo = method.IsStatic ? null : new ModuleInfo(registration.Type, registration.Instance, registration.Members);
            functions[functionName] = CreateFunctionDelegate(method, moduleInfo);
        }
    }

    private static Func<object?[], object?> CreateFunctionDelegate(MethodInfo method, ModuleInfo? moduleInfo)
    {
        var parameters = MethodDispatchCache.GetParameters(method);

        return args =>
        {
            var finalArgs = PadWithDefaults(parameters, args, method.Name);
            return Alder.Runtime.MethodInvoker.InvokeMethodCore(method, moduleInfo?.Resolve(null), finalArgs);
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
                throw new AlderException(
                    DiagnosticDescriptors.MissingRequiredArgument,
                    parameters[i].Name,
                    callableName);
            }
        }

        return result;
    }

    private static DispatchCatalog BuildDispatchCatalog(AlderOptions.AotBuilder aotOptions)
    {
        Dictionary<Type, TypedDispatch>? typeDispatch = null;
        Dictionary<Type, List<GenericStaticDispatch>>? genericStaticDispatch = null;
        Dictionary<Type, Func<object, Delegate>>? delegateFactories = null;
        HashSet<RootedType>? rootedDelegateTypes = null;
        AddContext(aotOptions.BuiltInContext);

        foreach (var context in aotOptions.AdditionalContexts)
            AddContext(context);

        return new DispatchCatalog(
            typeDispatch != null ? Runtime.Collections.FixedDictionary<Type, TypedDispatch>.Create(typeDispatch) : null,
            genericStaticDispatch != null
                ? Runtime.Collections.FixedDictionary<Type, IReadOnlyList<GenericStaticDispatch>>.Create(
                    genericStaticDispatch.ToDictionary(
                        static kvp => kvp.Key,
                        static kvp => (IReadOnlyList<GenericStaticDispatch>)kvp.Value.ToArray()))
                : null,
            delegateFactories,
            rootedDelegateTypes);

        void AddContext(AlderTypeContext? context)
        {
            if (context == null)
                return;

            typeDispatch ??= new Dictionary<Type, TypedDispatch>();
            foreach (var metadata in context.GetTypeMetadata())
                typeDispatch[metadata.Type] = metadata;

            var genericDispatchers = context.GetGenericStaticDispatch();
            if (genericDispatchers != null)
            {
                genericStaticDispatch ??= new Dictionary<Type, List<GenericStaticDispatch>>();
                foreach (var dispatcher in genericDispatchers)
                {
                    if (!genericStaticDispatch.TryGetValue(dispatcher.DeclaringType, out var entries))
                    {
                        entries = [];
                        genericStaticDispatch[dispatcher.DeclaringType] = entries;
                    }

                    entries.Add(dispatcher);
                }
            }

            var factories = context.GetDelegateFactories();
            if (factories != null)
            {
                delegateFactories ??= new Dictionary<Type, Func<object, Delegate>>();
                foreach (var kvp in factories)
                    delegateFactories[kvp.Key.Type] = kvp.Value;
            }

            var contextRootedDelegateTypes = context.GetRootedDelegateTypes();
            if (contextRootedDelegateTypes != null)
            {
                rootedDelegateTypes ??= [];
                foreach (var delegateType in contextRootedDelegateTypes)
                    rootedDelegateTypes.Add(delegateType);
            }
        }
    }

    private readonly record struct DispatchCatalog(
        Runtime.Collections.FixedDictionary<Type, TypedDispatch>? TypeDispatch,
        Runtime.Collections.FixedDictionary<Type, IReadOnlyList<GenericStaticDispatch>>? GenericStaticDispatch,
        IReadOnlyDictionary<Type, Func<object, Delegate>>? DelegateFactories,
        IReadOnlyCollection<RootedType>? RootedDelegateTypes);
}
