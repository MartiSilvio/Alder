using System.Diagnostics.CodeAnalysis;
using Alder.Aot;
using Alder.Attributes;
using Alder.Compilation;
using Alder.Runtime;
using Alder.Security;

namespace Alder;

/// <summary>
/// Selects the syntax accepted by <see cref="AlderEngine"/>.
/// </summary>
public enum LanguageMode
{
    /// <summary>
    /// Accepts standard C# syntax supported by Alder.
    /// Choose this mode when you want Alder to stay within its non-extended language set.
    /// </summary>
    Standard,

    /// <summary>
    /// Accepts the standard syntax plus Alder-specific extensions.
    /// This mode enables features such as chained comparisons, pipelines, and other extended syntax forms.
    /// </summary>
    Extended
}

/// <summary>
/// Configures an <see cref="AlderEngine"/> before it is created.
/// </summary>
public sealed class AlderOptions
{
    /// <summary>
    /// Controls identifier matching.
    /// The default is <c>true</c>, which matches normal C# case sensitivity.
    /// </summary>
    public bool IsCaseSensitive { get; set; } = true;

    /// <summary>
    /// Selects the accepted syntax.
    /// The default is <see cref="Alder.LanguageMode.Standard"/>.
    /// </summary>
    public LanguageMode LanguageMode { get; set; } = LanguageMode.Standard;

    /// <summary>
    /// Controls which runtime operations evaluation may perform.
    /// The default is <see cref="SecurityOptions.Trusted"/>.
    /// </summary>
    public SecurityOptions Security { get; set; } = SecurityOptions.Trusted();

    /// <summary>
    /// Sets runtime guardrails such as statement, loop, and timeout limits.
    /// </summary>
    public ExecutionConstraints Constraints { get; set; } = new();

    /// <summary>
    /// Selects the delegate compiler used by the compiled backend.
    /// </summary>
    public IExpressionCompiler ExpressionCompiler { get; set; } = DefaultExpressionCompiler.Instance;

    /// <summary>
    /// Optional service provider used when runtime resolution needs application services.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; set; }

    internal ICompiledProvider? Compiler { get; set; }

    internal StringComparer StringComparer => IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Configures module-style registrations whose members are surfaced to expressions.
    /// </summary>
    public ModuleBuilder Modules { get; }

    /// <summary>
    /// Configures standalone function registrations.
    /// </summary>
    public FunctionBuilder Functions { get; }

    /// <summary>
    /// Configures type resolution, imported namespaces, and extension methods.
    /// </summary>
    public TypeBuilder Types { get; }

    /// <summary>
    /// Configures AOT-generated type metadata and dispatch contexts.
    /// </summary>
    public AotBuilder Aot { get; }

    /// <summary>
    /// Creates a new options instance with the default runtime, security, and AOT settings.
    /// </summary>
    public AlderOptions()
    {
        Modules = new ModuleBuilder(this);
        Functions = new FunctionBuilder(this);
        Types = new TypeBuilder();
        Aot = new AotBuilder();
    }

    /// <summary>
    /// Registers module types whose members can be reached from expressions.
    /// </summary>
    public sealed class ModuleBuilder
    {
        private readonly AlderOptions _options;
        internal readonly List<RegisteredType> RegisteredTypes = [];

        internal ModuleBuilder(AlderOptions options) => _options = options;

        /// <summary>
        /// Registers a module under <paramref name="moduleName"/>.
        /// </summary>
        /// <typeparam name="T">The module type to register.</typeparam>
        /// <param name="moduleName">Name used inside expressions, for example <c>Math</c>.</param>
        /// <param name="explicitOnly">When <c>true</c>, only members marked with <see cref="AlderFunctionAttribute"/> are exposed.</param>
        /// <param name="instance">Optional pre-created instance. When <c>null</c>, Alder creates one on demand.</param>
        public ModuleBuilder Register<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.PublicProperties |
                DynamicallyAccessedMemberTypes.PublicFields)] T>(
            string moduleName, bool explicitOnly = false, T? instance = default) where T : class
        {
            return Register(moduleName, typeof(T), explicitOnly, instance);
        }

        /// <summary>
        /// Registers a module under <paramref name="moduleName"/>.
        /// </summary>
        /// <param name="moduleName">Name used inside expressions.</param>
        /// <param name="type">The module type to register.</param>
        /// <param name="explicitOnly">When <c>true</c>, only members marked with <see cref="AlderFunctionAttribute"/> are exposed.</param>
        /// <param name="instance">Optional pre-created instance. When <c>null</c>, Alder creates one on demand.</param>
        public ModuleBuilder Register(
            string moduleName,
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.PublicProperties |
                DynamicallyAccessedMemberTypes.PublicFields)] Type type,
            bool explicitOnly = false,
            object? instance = null)
        {
            if (!explicitOnly)
            {
                var moduleAttr = type.GetCustomAttribute<AlderModuleAttribute>();
                explicitOnly = moduleAttr?.ExplicitOnly ?? false;
            }
            var members = ModuleMemberMetadata.Build(type, explicitOnly, _options.StringComparer);
            RegisteredTypes.Add(new RegisteredType(type, instance, moduleName, members));
            return this;
        }

        /// <summary>
        /// Registers a module using a precomputed member map.
        /// Use this overload when member discovery already happened elsewhere.
        /// </summary>
        /// <param name="moduleName">Name used inside expressions.</param>
        /// <param name="type">The module type to register.</param>
        /// <param name="members">The members to expose, keyed by name. Each entry must contain either one or more methods, or a single property or field.</param>
        public ModuleBuilder Register(
            string moduleName,
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.PublicProperties |
                DynamicallyAccessedMemberTypes.PublicFields)] Type type,
            IReadOnlyDictionary<string, IReadOnlyCollection<MemberInfo>> members)
        {
            RegisteredTypes.Add(new RegisteredType(type, null, moduleName, ModuleMemberMetadata.BuildFromMemberMap(members, _options.StringComparer)));
            return this;
        }

        /// <summary>
        /// Registers a type using Alder attributes.
        /// Types marked with <see cref="AlderModuleAttribute"/> become modules.
        /// Other attributed members are registered as global functions.
        /// </summary>
        /// <param name="type">The type to register.</param>
        /// <param name="instance">Optional pre-created instance.</param>
        public ModuleBuilder RegisterFromType(
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.PublicProperties |
                DynamicallyAccessedMemberTypes.PublicFields)] Type type,
            object? instance = null)
        {
            var moduleAttr = type.GetCustomAttribute<AlderModuleAttribute>();
            var explicitOnly = moduleAttr?.ExplicitOnly ?? false;
            RegisteredTypes.Add(new RegisteredType(type, instance, null, ModuleMemberMetadata.Build(type, explicitOnly, _options.StringComparer)));
            return this;
        }

        /// <summary>
        /// Registers a type using Alder attributes.
        /// Types marked with <see cref="AlderModuleAttribute"/> become modules.
        /// Other attributed members are registered as global functions.
        /// </summary>
        /// <typeparam name="T">The type to register.</typeparam>
        /// <param name="instance">Optional pre-created instance.</param>
        public ModuleBuilder RegisterFromType<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.PublicProperties |
                DynamicallyAccessedMemberTypes.PublicFields)] T>(T? instance = default) where T : class
        {
            return RegisterFromType(typeof(T), instance);
        }

        /// <summary>
        /// Scans an assembly for Alder module and function attributes.
        /// This is convenient at startup, but it is reflection-heavy and trim-sensitive.
        /// </summary>
        /// <param name="assembly">The assembly to scan.</param>
        [RequiresUnreferencedCode("Registering from assembly scans all types and members via reflection.")]
        public ModuleBuilder RegisterFromAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                var isModule = type.GetCustomAttribute<AlderModuleAttribute>() != null;
                var hasGlobalFunctions = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Any(m => m.GetCustomAttribute<AlderFunctionAttribute>() != null);

                if (!isModule && !hasGlobalFunctions)
                    continue;

                var hasStaticOnly = !isModule &&
                                    type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                        .All(m => m.GetCustomAttribute<AlderFunctionAttribute>() != null);

                if (!hasStaticOnly && type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                RegisteredTypes.Add(new RegisteredType(type, null, null, ModuleMemberMetadata.Build(type, explicitOnly: false, _options.StringComparer)));
            }

            return this;
        }
    }

    /// <summary>
    /// Registers standalone delegate-based functions callable from expressions.
    /// </summary>
    public sealed class FunctionBuilder
    {
        internal readonly Dictionary<string, Func<object?[], object?>> RegisteredFunctions;

        internal FunctionBuilder(AlderOptions options)
        {
            RegisteredFunctions = new Dictionary<string, Func<object?[], object?>>(options.StringComparer);
        }

        /// <summary>
        /// Registers a function callable by <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name used inside expressions.</param>
        /// <param name="function">Delegate implementation. Alder passes arguments as <c>object?[]</c>.</param>
        public FunctionBuilder Register(string name, Func<object?[], object?> function)
        {
            RegisteredFunctions[name] = function;
            return this;
        }
    }

    /// <summary>
    /// Configures type resolution, namespace imports, and extension methods.
    /// </summary>
    public sealed class TypeBuilder
    {
        internal readonly List<Assembly> Assemblies = [];
        internal readonly List<string> Namespaces = [];
        internal readonly List<Type> ExtensionTypes = [typeof(Enumerable)];

        internal TypeBuilder() { }

        /// <summary>
        /// Adds an assembly to the type-resolution search set.
        /// Types from this assembly can then participate in resolution and construction.
        /// </summary>
        /// <param name="assembly">The assembly to add.</param>
        public TypeBuilder AddAssembly(Assembly assembly)
        {
            Assemblies.Add(assembly);
            return this;
        }

        /// <summary>
        /// Adds a namespace for unqualified type resolution.
        /// </summary>
        /// <param name="namespaceName">Namespace to import, for example <c>System.Collections.Generic</c>.</param>
        public TypeBuilder AddNamespace(string namespaceName)
        {
            Namespaces.Add(namespaceName);
            return this;
        }

        /// <summary>
        /// Adds a static type whose extension methods participate in binding.
        /// <see cref="Enumerable"/> is already included by default.
        /// </summary>
        /// <param name="type">A static type containing extension methods.</param>
        public TypeBuilder AddExtensionMethods(Type type)
        {
            if (!ExtensionTypes.Contains(type))
                ExtensionTypes.Insert(0, type);
            return this;
        }

        /// <inheritdoc cref="AddExtensionMethods(Type)"/>
        /// <typeparam name="T">A static type containing extension methods.</typeparam>
        public TypeBuilder AddExtensionMethods<T>() => AddExtensionMethods(typeof(T));
    }

    /// <summary>
    /// Configures AOT-generated type metadata and dispatch contexts.
    /// </summary>
    public sealed class AotBuilder
    {
        internal AlderTypeContext? BuiltInContext = AlderBuiltInContext.Default;
        internal readonly List<AlderTypeContext> AdditionalContexts = [];

        internal AotBuilder() { }

        /// <summary>
        /// Registers an additional AOT-generated type context.
        /// </summary>
        /// <param name="context">The generated type context to register.</param>
        public AotBuilder UseGeneratedContext(AlderTypeContext context)
        {
            AdditionalContexts.Add(context);
            return this;
        }

        /// <summary>
        /// Clears the built-in generated context and any additional contexts.
        /// Reflection-based dispatch remains available where the runtime permits it.
        /// </summary>
        public AotBuilder ClearBuiltInContext()
        {
            BuiltInContext = null;
            AdditionalContexts.Clear();
            return this;
        }
    }

    internal sealed record RegisteredType(
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
        IReadOnlyDictionary<string, ModuleMemberEntry> Members);
}

/// <summary>
/// Controls which runtime operations evaluation may perform.
/// Use <see cref="Trusted"/> for fully trusted evaluation, or construct an
/// instance with the exact operation flags the host wants to allow.
/// </summary>
public sealed record SecurityOptions
{
    /// <summary>
    /// Allows method calls.
    /// </summary>
    public bool AllowMethodCalls { get; init; }

    /// <summary>
    /// Allows instance property reads.
    /// </summary>
    public bool AllowPropertyRead { get; init; }

    /// <summary>
    /// Allows static property reads.
    /// </summary>
    public bool AllowStaticPropertyRead { get; init; }

    /// <summary>
    /// Allows static field reads.
    /// </summary>
    public bool AllowStaticFieldRead { get; init; }

    /// <summary>
    /// Allows variable assignment.
    /// </summary>
    public bool AllowAssignment { get; init; }

    /// <summary>
    /// Allows property writes.
    /// </summary>
    public bool AllowPropertySet { get; init; }

    /// <summary>
    /// Allows index writes such as <c>list[0] = x</c>.
    /// </summary>
    public bool AllowIndexSet { get; init; }

    /// <summary>
    /// Allows object construction through <c>new</c>.
    /// </summary>
    public bool AllowConstruction { get; init; }

    /// <summary>
    /// Types that remain allowed regardless of the broader flag set.
    /// </summary>
    public HashSet<Type>? TrustedTypes { get; init; }

    /// <summary>
    /// Namespaces that remain allowed regardless of the broader flag set.
    /// </summary>
    public HashSet<string>? TrustedNamespaces { get; init; }

    /// <summary>
    /// Types that remain denied regardless of the broader flag set.
    /// </summary>
    public HashSet<Type>? DeniedTypes { get; init; }

    /// <summary>
    /// Namespaces that remain denied regardless of the broader flag set.
    /// </summary>
    public HashSet<string>? DeniedNamespaces { get; init; }

    /// <summary>
    /// Maximum collection size allowed during evaluation.
    /// </summary>
    public int MaxCollectionSize { get; init; } = 10_000_000;

    /// <summary>
    /// Timeout applied to regex operations.
    /// </summary>
    public TimeSpan RegexTimeout { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Creates a fully permissive security policy for trusted code.
    /// </summary>
    public static SecurityOptions Trusted() => new()
    {
        AllowMethodCalls = true,
        AllowPropertyRead = true,
        AllowStaticPropertyRead = true,
        AllowStaticFieldRead = true,
        AllowAssignment = true,
        AllowPropertySet = true,
        AllowIndexSet = true,
        AllowConstruction = true
    };

    internal SecurityPolicy ToSecurityPolicy() => new SecurityPolicy.Builder
    {
        AllowMethodCalls = AllowMethodCalls,
        AllowPropertyRead = AllowPropertyRead,
        AllowStaticPropertyRead = AllowStaticPropertyRead,
        AllowStaticFieldRead = AllowStaticFieldRead,
        AllowAssignment = AllowAssignment,
        AllowPropertySet = AllowPropertySet,
        AllowIndexSet = AllowIndexSet,
        AllowConstruction = AllowConstruction,
        TrustedTypes = TrustedTypes,
        TrustedNamespaces = TrustedNamespaces,
        DeniedTypes = DeniedTypes,
        DeniedNamespaces = DeniedNamespaces,
        MaxCollectionSize = MaxCollectionSize,
        RegexTimeout = RegexTimeout,
    }.Build();
}

/// <summary>
/// Configures execution limits used to stop runaway evaluation.
/// </summary>
public sealed record ExecutionConstraints
{
    /// <summary>
    /// Maximum statement count before evaluation throws <see cref="AlderExecutionLimitException"/>.
    /// <c>null</c> means unlimited.
    /// </summary>
    public long? MaxStatements { get; init; }

    /// <summary>
    /// Maximum iterations allowed for a single loop before evaluation throws <see cref="AlderExecutionLimitException"/>.
    /// <c>null</c> means unlimited.
    /// </summary>
    public long? MaxLoopIterations { get; init; }

    /// <summary>
    /// Maximum wall-clock evaluation time before <see cref="AlderExecutionLimitException"/> is thrown.
    /// <c>null</c> means unlimited.
    /// </summary>
    public TimeSpan? MaxTimeout { get; init; }

}
