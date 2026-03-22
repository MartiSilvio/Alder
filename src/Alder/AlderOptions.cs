using System.Diagnostics.CodeAnalysis;
using Alder.Aot;
using Alder.Attributes;
using Alder.Compilation;
using Alder.Runtime;
using Alder.Security;

namespace Alder;

public enum LanguageMode
{
    Standard,
    Extended
}

public sealed class AlderOptions
{
    public bool IsCaseSensitive { get; set; } = true;

    public ExecutionConstraints? Constraints { get; set; }

    public SandboxOptions Sandbox { get; set; } = SandboxOptions.Trusted();

    internal SecurityPolicy Security => Sandbox.ToSecurityPolicy();

    internal ICompiledProvider? Compiler { get; set; }

    public IServiceProvider? ServiceProvider { get; set; }

    public LanguageMode LanguageMode { get; set; } = LanguageMode.Standard;

    public IExpressionCompiler ExpressionCompiler { get; set; } = DefaultExpressionCompiler.Instance;

    internal int MaxExpressionDepth => Constraints?.MaxExpressionDepth ?? ExecutionConstraints.DefaultMaxExpressionDepth;
    internal StringComparer StringComparer => IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
    internal StringComparison StringComparison => IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    public ModuleBuilder Modules { get; }
    public FunctionBuilder Functions { get; }
    public TypeBuilder Types { get; }
    public AotBuilder Aot { get; }

    public AlderOptions()
    {
        Modules = new ModuleBuilder(this);
        Functions = new FunctionBuilder(this);
        Types = new TypeBuilder();
        Aot = new AotBuilder();
    }

    public sealed class ModuleBuilder
    {
        private readonly AlderOptions _options;
        internal readonly List<RegisteredType> RegisteredTypes = [];

        internal ModuleBuilder(AlderOptions options) => _options = options;

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

        public ModuleBuilder RegisterFromType<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.PublicProperties |
                DynamicallyAccessedMemberTypes.PublicFields)] T>(T? instance = default) where T : class
        {
            return RegisterFromType(typeof(T), instance);
        }

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

    public sealed class FunctionBuilder
    {
        private readonly AlderOptions _options;
        internal readonly Dictionary<string, Func<object?[], object?>> RegisteredFunctions;

        internal FunctionBuilder(AlderOptions options)
        {
            _options = options;
            RegisteredFunctions = new Dictionary<string, Func<object?[], object?>>(options.StringComparer);
        }

        public FunctionBuilder Register(string name, Func<object?[], object?> function)
        {
            RegisteredFunctions[name] = function;
            return this;
        }
    }

    public sealed class TypeBuilder
    {
        internal readonly List<Assembly> Assemblies = [];
        internal readonly List<string> Namespaces = [];
        internal readonly List<Type> ExtensionTypes = [typeof(Enumerable)];

        internal TypeBuilder() { }

        public TypeBuilder AddAssembly(Assembly assembly)
        {
            Assemblies.Add(assembly);
            return this;
        }

        public TypeBuilder AddNamespace(string namespaceName)
        {
            Namespaces.Add(namespaceName);
            return this;
        }

        public TypeBuilder AddExtensionMethods(Type type)
        {
            if (!ExtensionTypes.Contains(type))
                ExtensionTypes.Insert(0, type);
            return this;
        }

        public TypeBuilder AddExtensionMethods<T>() => AddExtensionMethods(typeof(T));
    }

    public sealed class AotBuilder
    {
        internal AlderTypeContext? BuiltInContext = AlderBuiltInContext.Default;
        internal readonly List<AlderTypeContext> AdditionalContexts = [];

        internal AotBuilder() { }

        public AotBuilder UseGeneratedContext(AlderTypeContext context)
        {
            AdditionalContexts.Add(context);
            return this;
        }

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

public sealed record SandboxOptions
{
    public bool AllowMethodCalls { get; init; }

    public bool AllowPropertyRead { get; init; }

    public bool AllowStaticPropertyRead { get; init; }

    public bool AllowStaticFieldRead { get; init; }

    public bool AllowAssignment { get; init; }

    public bool AllowPropertySet { get; init; }

    public bool AllowIndexSet { get; init; }

    public bool AllowConstruction { get; init; }

    public HashSet<Type>? TrustedTypes { get; init; }

    public HashSet<string>? TrustedNamespaces { get; init; }

    public HashSet<Type>? DeniedTypes { get; init; }

    public HashSet<string>? DeniedNamespaces { get; init; }

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

    public static SandboxOptions Safe() => new()
    {
        AllowPropertyRead = true,
        AllowAssignment = true,
        AllowPropertySet = true,
        AllowIndexSet = true
    };

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

public sealed class ExecutionConstraints
{
    internal const int DefaultMaxExpressionDepth = 512;
    public const long DefaultMaxLoopIterations = 1_000_000;

    public long? MaxStatements { get; set; }

    public long MaxLoopIterations { get; set; } = DefaultMaxLoopIterations;

    public TimeSpan? MaxTimeout { get; set; }

    public int MaxExpressionDepth { get; set; } = DefaultMaxExpressionDepth;
}
