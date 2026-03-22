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
    public int MaxArrayLength { get; }
    public TimeSpan RegexTimeout { get; }

    internal bool IsTrusted { get; }

    private readonly FixedSet<Type>? _allowedTypes;
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
        MaxArrayLength = b.MaxArrayLength;
        RegexTimeout = b.RegexTimeout;

        _allowedTypes = b.AllowedTypes?.Count > 0 ? FixedSet<Type>.Create(b.AllowedTypes) : null;
        _deniedTypes = FixedSet<Type>.Create(b.DeniedTypes ?? DefaultDeniedTypes);
        _deniedNamespaces = FixedSet<string>.Create(b.DeniedNamespaces ?? DefaultDeniedNamespaces);

        IsTrusted = AllowMethodCalls && AllowPropertyRead && AllowStaticPropertyRead &&
                    AllowStaticFieldRead && AllowAssignment && AllowPropertySet &&
                    AllowIndexSet && AllowConstruction && _allowedTypes == null;
    }

    public bool IsTypeAllowed(Type type)
    {
        if (_deniedTypes.Contains(type))
            return false;

        var ns = type.Namespace;
        if (ns != null)
        {
            foreach (var denied in _deniedNamespaces)
            {
                if (ns.Equals(denied, StringComparison.Ordinal) ||
                    ns.StartsWith(denied + ".", StringComparison.Ordinal))
                    return false;
            }
        }

        return _allowedTypes == null || _allowedTypes.Contains(type);
    }

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
        DeniedTypes = new HashSet<Type>(),
        DeniedNamespaces = new HashSet<string>()
    }.Build());
    public static SecurityPolicy Trusted => _trusted.Value;

    private static readonly Lazy<SecurityPolicy> _safe = new(() => new Builder
    {
        AllowPropertyRead = true,
        AllowStaticPropertyRead = true,
        AllowStaticFieldRead = true,
        AllowAssignment = true,
        AllowPropertySet = true,
        AllowIndexSet = true
    }.Build());
    public static SecurityPolicy Safe => _safe.Value;

    private static readonly Lazy<SecurityPolicy> _strict = new(() => new Builder
    {
        AllowPropertyRead = true,
        AllowStaticPropertyRead = true,
        AllowStaticFieldRead = true
    }.Build());
    public static SecurityPolicy Strict => _strict.Value;

    private static readonly HashSet<Type> DefaultDeniedTypes = BuildDefaultDeniedTypes();

    private static HashSet<Type> BuildDefaultDeniedTypes()
    {
        var types = new HashSet<Type>
        {
            typeof(AppDomain),
            typeof(Environment),
            typeof(GC),
            typeof(Console),
        };
        TryAddType(types, "System.Threading.Thread, System.Threading.Thread");
        TryAddType(types, "System.Threading.ThreadPool, System.Threading.ThreadPool");
        TryAddType(types, "System.Diagnostics.Process, System.Diagnostics.Process");
        TryAddType(types, "System.Diagnostics.ProcessStartInfo, System.Diagnostics.Process");
        TryAddType(types, "System.Runtime.InteropServices.Marshal, System.Runtime.InteropServices");
        return types;
    }

    private static void TryAddType(HashSet<Type> set, string assemblyQualifiedName)
    {
        var type = Type.GetType(assemblyQualifiedName);
        if (type != null) set.Add(type);
    }

    private static readonly HashSet<string> DefaultDeniedNamespaces = new()
    {
        "System.Reflection",
        "System.Reflection.Emit",
        "System.Runtime.InteropServices",
        "System.IO",
        "System.Net",
        "System.Net.Http",
        "System.Net.Sockets",
        "System.Security",
        "System.Diagnostics",
        "System.CodeDom",
        "System.Runtime.Loader"
    };

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
        public int MaxArrayLength { get; set; } = 10_000_000;
        public TimeSpan RegexTimeout { get; set; } = TimeSpan.FromSeconds(1);
        public HashSet<Type>? AllowedTypes { get; set; }
        public HashSet<Type>? DeniedTypes { get; set; }
        public HashSet<string>? DeniedNamespaces { get; set; }

        public SecurityPolicy Build() => new(this);
    }
}
