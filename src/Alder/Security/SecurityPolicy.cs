using Alder.Runtime.Collections;

namespace Alder.Security;

public sealed class SecurityPolicy
{
    public bool AllowMethodCalls { get; }
    public bool AllowPropertyRead { get; }
    public bool AllowStaticPropertyRead { get; }
    public bool AllowStaticFieldRead { get; }
    public bool AllowAssignment { get; }
    public bool AllowPropertySet { get; }
    public bool AllowIndexSet { get; }
    public bool AllowConstruction { get; }
    public int MaxCollectionSize { get; }
    public TimeSpan RegexTimeout { get; }

    private readonly FixedSet<Type> _trustedTypes;
    private readonly FixedSet<string> _trustedNamespaces;
    private readonly FixedSet<Type> _deniedTypes;
    private readonly FixedSet<string> _deniedNamespaces;

    private SecurityPolicy(Builder b)
    {
        AllowMethodCalls = b.AllowMethodCalls;
        AllowPropertyRead = b.AllowPropertyRead;
        AllowStaticPropertyRead = b.AllowStaticPropertyRead;
        AllowStaticFieldRead = b.AllowStaticFieldRead;
        AllowAssignment = b.AllowAssignment;
        AllowPropertySet = b.AllowPropertySet;
        AllowIndexSet = b.AllowIndexSet;
        AllowConstruction = b.AllowConstruction;
        MaxCollectionSize = b.MaxCollectionSize;
        RegexTimeout = b.RegexTimeout;

        _trustedTypes = b.TrustedTypes != null ? FixedSet<Type>.Create(b.TrustedTypes) : FixedSet<Type>.Empty;
        _trustedNamespaces = b.TrustedNamespaces != null ? FixedSet<string>.Create(b.TrustedNamespaces) : FixedSet<string>.Empty;
        _deniedTypes = FixedSet<Type>.Create(b.DeniedTypes ?? DefaultDeniedTypes);
        _deniedNamespaces = FixedSet<string>.Create(b.DeniedNamespaces ?? DefaultDeniedNamespaces);

    }

    public bool IsTypeAllowed(Type type)
    {
        if (HardDenied.Contains(type))
            return false;

        if (_trustedTypes.Contains(type) || InNamespace(type, _trustedNamespaces))
            return true;

        if (_deniedTypes.Contains(type) || InNamespace(type, _deniedNamespaces))
            return false;

        return true;
    }

    private static bool InNamespace(Type type, FixedSet<string> set)
    {
        if (set.Count == 0) return false;
        var ns = type.Namespace;
        if (ns == null) return false;

        foreach (var entry in set)
        {
            if (ns.Equals(entry, StringComparison.Ordinal) ||
                ns.StartsWith(entry + ".", StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    #region Presets

    public static SecurityPolicy Trusted => _trusted.Value;
    public static SecurityPolicy Safe => _safe.Value;
    public static SecurityPolicy Strict => _strict.Value;

    private static readonly Lazy<SecurityPolicy> _trusted = new(() => new Builder
    {
        AllowMethodCalls = true,
        AllowPropertyRead = true,
        AllowStaticPropertyRead = true,
        AllowStaticFieldRead = true,
        AllowAssignment = true,
        AllowPropertySet = true,
        AllowIndexSet = true,
        AllowConstruction = true,
    }.Build());

    private static readonly Lazy<SecurityPolicy> _safe = new(() => new Builder
    {
        AllowPropertyRead = true,
        AllowStaticPropertyRead = true,
        AllowStaticFieldRead = true,
        AllowAssignment = true,
        AllowPropertySet = true,
        AllowIndexSet = true,
    }.Build());

    private static readonly Lazy<SecurityPolicy> _strict = new(() => new Builder
    {
        AllowPropertyRead = true,
        AllowStaticPropertyRead = true,
        AllowStaticFieldRead = true,
    }.Build());

    #endregion

    #region Default deny lists

    private static readonly HashSet<Type> HardDenied = new()
    {
        typeof(AlderEngine),
        typeof(AlderOptions),
        typeof(AlderExpression),
    };

    private static readonly HashSet<Type> DefaultDeniedTypes = BuildDefaultDeniedTypes();

    private static HashSet<Type> BuildDefaultDeniedTypes()
    {
        var types = new HashSet<Type>
        {
            typeof(Activator),
            typeof(AppDomain),
            typeof(Console),
            typeof(Delegate),
            typeof(Environment),
            typeof(GC),
            typeof(MulticastDelegate),
            typeof(WeakReference),
            typeof(WeakReference<>),
        };
        AddIfAvailable(types, "System.Threading.Thread, System.Threading.Thread");
        AddIfAvailable(types, "System.Threading.ThreadPool, System.Threading.ThreadPool");
        AddIfAvailable(types, "System.Diagnostics.Process, System.Diagnostics.Process");
        AddIfAvailable(types, "System.Diagnostics.ProcessStartInfo, System.Diagnostics.Process");
        AddIfAvailable(types, "System.Runtime.InteropServices.Marshal, System.Runtime.InteropServices");
        return types;
    }

    private static void AddIfAvailable(HashSet<Type> set, string assemblyQualifiedName)
    {
        var type = Type.GetType(assemblyQualifiedName);
        if (type != null) set.Add(type);
    }

    private static readonly HashSet<string> DefaultDeniedNamespaces = new()
    {
        // Code generation & dynamic compilation
        "System.CodeDom",
        "System.Linq.Expressions",
        "System.Reflection",
        "System.Reflection.Emit",
        "System.Runtime.CompilerServices",
        "System.Runtime.Loader",
        "System.Runtime.Serialization",
        "Microsoft.CSharp",

        // OS & process access
        "System.Diagnostics",
        "System.IO",
        "System.ServiceProcess",
        "System.Management",
        "Microsoft.Win32",

        // Network
        "System.Net",
        "System.Net.Http",
        "System.Net.Mail",
        "System.Net.NetworkInformation",
        "System.Net.Sockets",

        // Threading
        "System.Threading",

        // Security & interop
        "System.Runtime.InteropServices",
        "System.Security",

        // Data access
        "System.Data",
        "Microsoft.Data",

        // Configuration & composition
        "System.Configuration",
        "System.ComponentModel",
        "System.ComponentModel.Composition",
        "System.Composition",
        "System.DirectoryServices",
        "System.Resources",
    };

    #endregion

    public sealed class Builder
    {
        public bool AllowMethodCalls { get; set; }
        public bool AllowPropertyRead { get; set; }
        public bool AllowStaticPropertyRead { get; set; }
        public bool AllowStaticFieldRead { get; set; }
        public bool AllowAssignment { get; set; }
        public bool AllowPropertySet { get; set; }
        public bool AllowIndexSet { get; set; }
        public bool AllowConstruction { get; set; }
        public int MaxCollectionSize { get; set; } = 10_000_000;
        public TimeSpan RegexTimeout { get; set; } = TimeSpan.FromSeconds(1);

        public HashSet<Type>? TrustedTypes { get; set; }
        public HashSet<string>? TrustedNamespaces { get; set; }
        public HashSet<Type>? DeniedTypes { get; set; }
        public HashSet<string>? DeniedNamespaces { get; set; }

        public SecurityPolicy Build() => new(this);
    }
}
