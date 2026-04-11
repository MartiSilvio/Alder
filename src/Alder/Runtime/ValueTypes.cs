using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Runtime;

/// <summary>
/// References a registered global function.
/// This wrapper gives the interpreter and compiled backend a shared runtime representation.
/// </summary>
internal sealed record FunctionRef(string Name, Func<object?[], object?> Function)
{
    public object? Invoke(object?[] args) => Function(args);
}

/// <summary>
/// Represents a runtime lambda value together with its closure.
/// The binder emits lambdas as syntax plus captured scope, and this runtime wrapper preserves both until invocation.
/// </summary>
internal sealed class LambdaValue
{
    public List<string> Parameters { get; }
    public Expr Body { get; }
    public AlderContext Closure { get; }
    public AlderConfig Config => Closure.Config;
    public bool IsAsync { get; }
    public bool IsIterator => _isIterator ??= ContainsYield(Body);
    public Type? IteratorElementType { get; set; }

    private bool? _isIterator;
    // Cache bindings by lambda argument types. Closure values still flow from the captured context at execution time.
    private (Type[] ArgTypes, BoundExpr BoundBody)? _bindingCache;

    public LambdaValue(BoundLambdaExpr node, AlderContext closure)
    {
        Parameters = node.Parameters.ToList();
        Body = node.Body;
        Closure = closure;
        IsAsync = node.IsAsync;

        if (node.ReturnTypeName != null)
        {
            var returnType = closure.TypeResolver.ResolveType(node.ReturnTypeName);
            if (returnType != null)
                IteratorElementType = TypeHelpers.GetEnumerableElementType(returnType);
        }
    }

    internal LambdaValue(List<string> parameters, Expr body, AlderContext closure)
    {
        Parameters = parameters;
        Body = body;
        Closure = closure;
    }

    private static bool ContainsYield(Expr expr) => expr switch
    {
        YieldReturnExpr or YieldBreakExpr => true,
        BlockExpr block => block.Statements.Any(ContainsYield)
                           || (block.ReturnExpr != null && ContainsYield(block.ReturnExpr)),
        IfStatementExpr ife => ife.ThenStatements.Any(ContainsYield)
                               || (ife.ElseStatements != null && ife.ElseStatements.Any(ContainsYield)),
        WhileStatementExpr we => we.Body.Any(ContainsYield),
        ForStatementExpr fe => fe.Body.Any(ContainsYield),
        ForEachStatementExpr fae => fae.Body.Any(ContainsYield),
        DoWhileStatementExpr dw => dw.Body.Any(ContainsYield),
        SwitchStatementExpr sw => sw.Cases.Any(static c => c.Statements.Any(ContainsYield)),
        TryCatchFinallyExpr tc => tc.TryBody.Any(ContainsYield)
                                  || tc.CatchClauses.Any(static c => c.Body.Any(ContainsYield))
                                  || (tc.FinallyBody != null && tc.FinallyBody.Any(ContainsYield)),
        UsingStatementExpr us => ContainsYield(us.Body),
        LockStatementExpr lk => ContainsYield(lk.Body),
        _ => false
    };

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
/// Represents a lambda whose body has already been compiled to delegates.
/// Specialized delegate slots avoid array allocation in the common low-arity cases.
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
/// Wraps a <see cref="Range"/> to preserve Alder's inclusive-end iteration semantics.
/// The raw <see cref="Range"/> still flows through unchanged when .NET indexing semantics are required.
/// </summary>
internal sealed record InclusiveRange(Range Value);

/// <summary>
/// Carries a partially-resolved namespace path during fully qualified type access.
/// Member-access evaluation extends this sentinel until <see cref="TypeResolver"/> can resolve a final type.
/// </summary>
internal sealed record NamespaceRef(string Path);

/// <summary>
/// Wraps a <see cref="ValueTuple"/> together with named-element metadata.
/// Runtime tuple instances do not retain member names, so Alder carries them separately when tuple element names matter.
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

    // Structural tuple behavior still comes from the underlying ValueTuple instance.
    public override string ToString() => Tuple.ToString()!;
    public override int GetHashCode() => Tuple.GetHashCode();
    public override bool Equals(object? obj) =>
        obj is NamedTupleValue other ? Tuple.Equals(other.Tuple) : Tuple.Equals(obj);
}

/// <summary>
/// Wraps a named argument value so parameter-name information survives through invocation preparation.
/// </summary>
internal sealed record NamedArg(string Name, object? Value);

/// <summary>
/// Marks an <c>out</c> argument as it moves through invocation preparation.
/// The runtime uses this marker to allocate the reflection argument array and to publish the post-call value back into scope.
/// </summary>
internal sealed record OutArgMarker(string VariableName, string? TypeName, bool IsDiscard);

/// <summary>
/// Immutable metadata used when publishing <c>out</c> variables after method invocation completes.
/// </summary>
internal readonly record struct OutVariableBinding(int ArgumentIndex, string VariableName, string? TypeName);
