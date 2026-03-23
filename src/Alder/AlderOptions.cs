using System.Diagnostics.CodeAnalysis;
using Alder.Aot;
using Alder.Attributes;
using Alder.Compilation;
using Alder.Runtime;
using Alder.Security;

namespace Alder;

/// <summary>
/// Specifies the C# language mode for expression evaluation.
/// </summary>
public enum LanguageMode
{
    /// <summary>
    /// Standard C# expression semantics only.
    /// </summary>
    Standard,

    /// <summary>
    /// A superset of standard C# with additional operators, comparison chaining, ranges, slicing,
    /// collection comprehensions, <c>let..in</c> expressions, bare math functions, aggregate builtins,
    /// date/time sugar, the <c>it</c> implicit iterator, pipeline operator, and more.
    /// </summary>
    Extended
}

/// <summary>
/// Configuration options for an <see cref="AlderEngine"/> instance.
/// Pass to <see cref="AlderEngine(AlderOptions)"/> or the <see cref="AlderEngine(Action{AlderOptions})"/> overload.
/// </summary>
public sealed class AlderOptions
{
    /// <summary>
    /// Gets or sets whether identifier resolution is case-sensitive. Default is <c>true</c>.
    /// </summary>
    public bool IsCaseSensitive { get; set; } = true;

    /// <summary>
    /// Gets or sets the language mode. Default is <see cref="Alder.LanguageMode.Standard"/>.
    /// </summary>
    public LanguageMode LanguageMode { get; set; } = LanguageMode.Standard;

    /// <summary>
    /// Gets or sets the sandbox security policy controlling which runtime operations are allowed. Default is <see cref="SandboxOptions.Trusted"/>.
    /// </summary>
    public SandboxOptions Sandbox { get; set; } = SandboxOptions.Trusted();

    /// <summary>
    /// Gets or sets execution constraints such as statement limits, timeouts, and maximum expression depth.
    /// </summary>
    public ExecutionConstraints Constraints { get; set; } = new();

    /// <summary>
    /// Gets or sets the compiler used to compile LINQ expression trees to delegates.
    /// </summary>
    public IExpressionCompiler ExpressionCompiler { get; set; } = DefaultExpressionCompiler.Instance;

    /// <summary>
    /// Gets or sets an optional <see cref="IServiceProvider"/> for dependency injection within expressions.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; set; }

    internal ICompiledProvider? Compiler { get; set; }

    internal StringComparer StringComparer => IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
    internal StringComparison StringComparison => IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    /// <summary>
    /// Gets the builder for registering modules (classes whose methods and properties are exposed to expressions).
    /// </summary>
    public ModuleBuilder Modules { get; }

    /// <summary>
    /// Gets the builder for registering standalone functions callable from expressions.
    /// </summary>
    public FunctionBuilder Functions { get; }

    /// <summary>
    /// Gets the builder for configuring type resolution, assemblies, namespaces, and extension methods.
    /// </summary>
    public TypeBuilder Types { get; }

    /// <summary>
    /// Gets the builder for configuring AOT (ahead-of-time) compiled type metadata.
    /// </summary>
    public AotBuilder Aot { get; }

    /// <summary>
    /// Creates a new <see cref="AlderOptions"/> instance with default settings.
    /// </summary>
    public AlderOptions()
    {
        Modules = new ModuleBuilder(this);
        Functions = new FunctionBuilder(this);
        Types = new TypeBuilder();
        Aot = new AotBuilder();
    }

    /// <summary>
    /// Builder for registering module types whose public methods and properties are exposed to expressions.
    /// </summary>
    public sealed class ModuleBuilder
    {
        private readonly AlderOptions _options;
        internal readonly List<RegisteredType> RegisteredTypes = [];

        internal ModuleBuilder(AlderOptions options) => _options = options;

        /// <summary>
        /// Registers a module type under the specified name.
        /// </summary>
        /// <typeparam name="T">The module type to register.</typeparam>
        /// <param name="moduleName">The name used to access this module in expressions (e.g., <c>"Math"</c>).</param>
        /// <param name="explicitOnly">When <c>true</c>, only methods marked with <see cref="AlderFunctionAttribute"/> are exposed.</param>
        /// <param name="instance">An optional pre-created instance; if <c>null</c>, the engine creates one on demand.</param>
        /// <returns>This builder, for method chaining.</returns>
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
        /// Registers a module type under the specified name.
        /// </summary>
        /// <param name="moduleName">The name used to access this module in expressions.</param>
        /// <param name="type">The module type to register.</param>
        /// <param name="explicitOnly">When <c>true</c>, only methods marked with <see cref="AlderFunctionAttribute"/> are exposed.</param>
        /// <param name="instance">An optional pre-created instance; if <c>null</c>, the engine creates one on demand.</param>
        /// <returns>This builder, for method chaining.</returns>
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
        /// Registers a module type with a pre-built member dictionary.
        /// </summary>
        /// <param name="moduleName">The name used to access this module in expressions.</param>
        /// <param name="type">The module type to register.</param>
        /// <param name="members">The members to expose, keyed by name.</param>
        /// <returns>This builder, for method chaining.</returns>
        public ModuleBuilder Register(
            string moduleName,
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.PublicProperties |
                DynamicallyAccessedMemberTypes.PublicFields)] Type type,
            IReadOnlyDictionary<string, MemberInfo> members)
        {
            RegisteredTypes.Add(new RegisteredType(type, null, moduleName, members));
            return this;
        }

        /// <summary>
        /// Registers a type using its <see cref="AlderModuleAttribute"/> for the module name.
        /// If no attribute is present, the type's methods are registered as global functions.
        /// </summary>
        /// <param name="type">The type to register.</param>
        /// <param name="instance">An optional pre-created instance.</param>
        /// <returns>This builder, for method chaining.</returns>
        public ModuleBuilder RegisterFromType(
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.PublicProperties |
                DynamicallyAccessedMemberTypes.PublicFields)] Type type,
            object? instance = null)
        {
            RegisteredTypes.Add(new RegisteredType(type, instance, null, ModuleMemberMetadata.Build(type, explicitOnly: false, _options.StringComparer)));
            return this;
        }

        /// <summary>
        /// Registers a type using its <see cref="AlderModuleAttribute"/> for the module name.
        /// If no attribute is present, the type's methods are registered as global functions.
        /// </summary>
        /// <typeparam name="T">The type to register.</typeparam>
        /// <param name="instance">An optional pre-created instance.</param>
        /// <returns>This builder, for method chaining.</returns>
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
        /// Scans an assembly for types decorated with <see cref="AlderModuleAttribute"/> or containing
        /// methods decorated with <see cref="AlderFunctionAttribute"/>, and registers them.
        /// </summary>
        /// <param name="assembly">The assembly to scan.</param>
        /// <returns>This builder, for method chaining.</returns>
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
    /// Builder for registering standalone delegate-based functions callable from expressions.
    /// </summary>
    public sealed class FunctionBuilder
    {
        internal readonly Dictionary<string, Func<object?[], object?>> RegisteredFunctions;

        internal FunctionBuilder(AlderOptions options)
        {
            RegisteredFunctions = new Dictionary<string, Func<object?[], object?>>(options.StringComparer);
        }

        /// <summary>
        /// Registers a function that can be called by name from expressions.
        /// </summary>
        /// <param name="name">The function name as it appears in expressions.</param>
        /// <param name="function">The delegate to invoke. Receives arguments as an <c>object?[]</c>.</param>
        /// <returns>This builder, for method chaining.</returns>
        public FunctionBuilder Register(string name, Func<object?[], object?> function)
        {
            RegisteredFunctions[name] = function;
            return this;
        }
    }

    /// <summary>
    /// Builder for configuring type resolution — which assemblies, namespaces, and extension methods are available to expressions.
    /// </summary>
    public sealed class TypeBuilder
    {
        internal readonly List<Assembly> Assemblies = [];
        internal readonly List<string> Namespaces = [];
        internal readonly List<Type> ExtensionTypes = [typeof(Enumerable)];

        internal TypeBuilder() { }

        /// <summary>
        /// Adds an assembly for type resolution. Types from this assembly become constructible and accessible in expressions.
        /// </summary>
        /// <param name="assembly">The assembly to add.</param>
        /// <returns>This builder, for method chaining.</returns>
        public TypeBuilder AddAssembly(Assembly assembly)
        {
            Assemblies.Add(assembly);
            return this;
        }

        /// <summary>
        /// Adds a namespace for unqualified type resolution. Types in this namespace can be referenced without their full name.
        /// </summary>
        /// <param name="namespaceName">The namespace to add (e.g., <c>"System.Collections.Generic"</c>).</param>
        /// <returns>This builder, for method chaining.</returns>
        public TypeBuilder AddNamespace(string namespaceName)
        {
            Namespaces.Add(namespaceName);
            return this;
        }

        /// <summary>
        /// Adds a static type whose extension methods become available on matching types in expressions.
        /// <see cref="Enumerable"/> is included by default.
        /// </summary>
        /// <param name="type">A static type containing extension methods.</param>
        /// <returns>This builder, for method chaining.</returns>
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
    /// Builder for configuring AOT (ahead-of-time) type metadata generated by the Alder source generator.
    /// </summary>
    public sealed class AotBuilder
    {
        internal AlderTypeContext? BuiltInContext = AlderBuiltInContext.Default;
        internal readonly List<AlderTypeContext> AdditionalContexts = [];

        internal AotBuilder() { }

        /// <summary>
        /// Registers an AOT-generated type context containing pre-compiled metadata for member access and method dispatch.
        /// </summary>
        /// <param name="context">The generated type context to register.</param>
        /// <returns>This builder, for method chaining.</returns>
        public AotBuilder UseGeneratedContext(AlderTypeContext context)
        {
            AdditionalContexts.Add(context);
            return this;
        }

        /// <summary>
        /// Clears the built-in AOT context and all additional contexts, falling back to reflection-only dispatch.
        /// </summary>
        /// <returns>This builder, for method chaining.</returns>
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
        IReadOnlyDictionary<string, MemberInfo> Members);
}

/// <summary>
/// Controls which runtime operations are permitted during expression evaluation.
/// Use the factory methods <see cref="Trusted"/>, <see cref="Safe"/>, or <see cref="Strict"/> for common presets.
/// </summary>
public sealed record SandboxOptions
{
    /// <summary>
    /// Gets whether method calls on objects are allowed.
    /// </summary>
    public bool AllowMethodCalls { get; init; }

    /// <summary>
    /// Gets whether instance property reads are allowed.
    /// </summary>
    public bool AllowPropertyRead { get; init; }

    /// <summary>
    /// Gets whether static property reads are allowed.
    /// </summary>
    public bool AllowStaticPropertyRead { get; init; }

    /// <summary>
    /// Gets whether static field reads are allowed.
    /// </summary>
    public bool AllowStaticFieldRead { get; init; }

    /// <summary>
    /// Gets whether variable assignment is allowed.
    /// </summary>
    public bool AllowAssignment { get; init; }

    /// <summary>
    /// Gets whether property setters can be invoked.
    /// </summary>
    public bool AllowPropertySet { get; init; }

    /// <summary>
    /// Gets whether index setters (e.g., <c>list[0] = x</c>) can be invoked.
    /// </summary>
    public bool AllowIndexSet { get; init; }

    /// <summary>
    /// Gets whether object construction via <c>new</c> is allowed.
    /// </summary>
    public bool AllowConstruction { get; init; }

    /// <summary>
    /// Gets an optional set of types that are always allowed regardless of other restrictions.
    /// </summary>
    public HashSet<Type>? TrustedTypes { get; init; }

    /// <summary>
    /// Gets an optional set of namespaces whose types are always allowed regardless of other restrictions.
    /// </summary>
    public HashSet<string>? TrustedNamespaces { get; init; }

    /// <summary>
    /// Gets an optional set of types that are always denied regardless of other permissions.
    /// </summary>
    public HashSet<Type>? DeniedTypes { get; init; }

    /// <summary>
    /// Gets an optional set of namespaces whose types are always denied regardless of other permissions.
    /// </summary>
    public HashSet<string>? DeniedNamespaces { get; init; }

    /// <summary>
    /// Creates a fully permissive sandbox that allows all operations. Suitable for trusted code.
    /// </summary>
    /// <returns>A <see cref="SandboxOptions"/> with all permissions enabled.</returns>
    public static SandboxOptions Trusted() => new()
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

    /// <summary>
    /// Creates a sandbox that allows property access and assignment but disallows method calls, static access, and construction.
    /// </summary>
    /// <returns>A <see cref="SandboxOptions"/> with safe defaults.</returns>
    public static SandboxOptions Safe() => new()
    {
        AllowPropertyRead = true,
        AllowAssignment = true,
        AllowPropertySet = true,
        AllowIndexSet = true
    };

    /// <summary>
    /// Creates a minimal sandbox that only allows reading instance properties. No method calls, assignment, or construction.
    /// </summary>
    /// <returns>A <see cref="SandboxOptions"/> with the most restrictive settings.</returns>
    public static SandboxOptions Strict() => new()
    {
        AllowPropertyRead = true
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
    }.Build();
}

/// <summary>
/// Configures runtime execution limits to prevent runaway expressions.
/// </summary>
public sealed record ExecutionConstraints
{
    /// <summary>
    /// Gets the maximum number of statements that can be executed before the engine throws <see cref="AlderExecutionLimitException"/>.
    /// <c>null</c> means unlimited.
    /// </summary>
    public long? MaxStatements { get; init; }

    /// <summary>
    /// Gets the maximum number of iterations a single loop can execute before the engine throws <see cref="AlderExecutionLimitException"/>.
    /// <c>null</c> means unlimited.
    /// </summary>
    public long? MaxLoopIterations { get; init; }

    /// <summary>
    /// Gets the maximum wall-clock time an evaluation can run before the engine throws <see cref="AlderExecutionLimitException"/>.
    /// <c>null</c> means unlimited.
    /// </summary>
    public TimeSpan? MaxTimeout { get; init; }

    /// <summary>
    /// Gets the maximum nesting depth for expressions. Prevents stack overflows from deeply nested syntax trees. Default is 512.
    /// </summary>
    public int MaxExpressionDepth { get; init; } = 512;
}
