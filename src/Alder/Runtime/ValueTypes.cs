using Alder.Binding;
using Alder.Parsing;

namespace Alder.Runtime;

/// <summary>
/// Reference to a registered function, used by IL-compiled and interpreted expressions.
/// </summary>
internal sealed record FunctionRef(string Name, Func<object?[], object?> Function)
{
    public object? Invoke(object?[] args) => Function(args);
}

internal sealed class LambdaValue
{
    public List<string> Parameters { get; }
    public Expr Body { get; }
    public AlderContext Closure { get; }
    public AlderConfig Config { get; }

    private (Type[] ArgTypes, BoundExpr BoundBody)? _bindingCache;

    public LambdaValue(List<string> Parameters, Expr Body, AlderContext Closure, AlderConfig Config)
    {
        this.Parameters = Parameters;
        this.Body = Body;
        this.Closure = Closure;
        this.Config = Config;
    }

    internal BoundExpr GetOrBindBody(AlderContext childContext)
    {
        var argTypes = new Type[Parameters.Count];
        for (var i = 0; i < Parameters.Count; i++)
        {
            if (childContext.TryGet(Parameters[i], out var value) && value != null)
                argTypes[i] = value.GetType();
            else
                argTypes[i] = typeof(object);
        }

        if (_bindingCache is var (cachedTypes, cachedBody) && TypesMatch(cachedTypes, argTypes))
            return cachedBody;

        var binder = new Binding.Binder();
        var bound = binder.Bind(Body, new BindingContext(childContext));
        _bindingCache = (argTypes, bound);
        return bound;
    }

    private static bool TypesMatch(Type[] a, Type[] b)
    {
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }
}

/// <summary>
/// Compiled lambda with IL-compiled body delegate.
/// </summary>
internal sealed record CompiledLambdaValue(
    List<string> Parameters,
    Func<object?[], AlderContext, object?> CompiledBody,
    AlderContext Closure,
    Func<AlderContext, object?>? CompiledBody0 = null,
    Func<object?, AlderContext, object?>? CompiledBody1 = null,
    Func<object?, object?, AlderContext, object?>? CompiledBody2 = null,
    LambdaExpr? Source = null);

internal sealed record MethodRef(object Target, string MethodName);

internal sealed record StaticMethodRef(Type Type, string MethodName);

internal sealed record ModuleMethodRef(ModuleInfo Module, IServiceProvider? ServiceProvider, MethodInfo Method);

/// <summary>
/// Wraps a System.Range to indicate inclusive-end semantics for iteration.
/// When used as an array index, unwraps to the raw Range (exclusive-end).
/// When iterated, includes the end value.
/// </summary>
internal sealed record InclusiveRange(Range Value);

/// <summary>
/// Sentinel for partially-resolved namespace paths during FQN type access.
/// Flows through MemberAccess chains until TypeResolver resolves a full type name.
/// Example: IdentifierExpr("System") -> NamespaceRef("System") -> member "Linq" -> NamespaceRef("System.Linq") -> member "Enumerable" -> Type
/// </summary>
internal sealed record NamespaceRef(string Path);

/// <summary>
/// Wraps a ValueTuple to carry named element metadata at runtime.
/// Allows `.Name` access on tuples created with named elements like (Name: "test", Value: 42).
/// </summary>
internal sealed class NamedTupleValue(object tuple, IReadOnlyDictionary<string, int> nameToIndex)
    : System.Runtime.CompilerServices.ITuple
{
    public object Tuple { get; } = tuple;
    private readonly System.Runtime.CompilerServices.ITuple _ituple = (System.Runtime.CompilerServices.ITuple)tuple;

    public int Length => _ituple.Length;
    public object? this[int index] => _ituple[index];

    public bool TryGetIndex(string name, out int index)
    {
        return nameToIndex.TryGetValue(name, out index);
    }

    internal NamedTupleValue WithModifiedValues(string[] names, object?[] values)
    {
        var elements = new object?[Length];
        for (var i = 0; i < Length; i++)
            elements[i] = this[i];

        for (var i = 0; i < names.Length; i++)
        {
            if (!TryGetIndex(names[i], out var idx))
                throw new AlderException(
                    Diagnostics.DiagnosticDescriptors.MemberNotFound, "tuple", names[i]);
            elements[idx] = values[i];
        }

        return new NamedTupleValue(
            Semantics.ConstructionRuntime.CreateTuple(elements),
            nameToIndex);
    }

    // Delegate all other behavior to the underlying tuple
    public override string ToString() => Tuple.ToString()!;
    public override int GetHashCode() => Tuple.GetHashCode();
    public override bool Equals(object? obj) =>
        obj is NamedTupleValue other ? Tuple.Equals(other.Tuple) : Tuple.Equals(obj);
}

/// <summary>
/// Wrapper for a named argument value. Used to pass parameter name information
/// through the method invocation stack.
/// </summary>
internal sealed record NamedArg(string Name, object? Value);

/// <summary>
/// Marker for out parameter arguments. Flows through the method invocation stack
/// so MethodInvoker can detect ByRef parameters and set up the args array correctly.
/// After method invocation, the engine reads modified values from the args array
/// and defines variables in the current scope.
/// </summary>
internal sealed record OutArgMarker(string VariableName, string? TypeName, bool IsDiscard);

/// <summary>
/// Immutable metadata for defining out variables after method invocation.
/// </summary>
internal readonly record struct OutVariableBinding(int ArgumentIndex, string VariableName, string? TypeName);
