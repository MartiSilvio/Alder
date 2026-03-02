using CsEval.Parsing;

namespace CsEval.Runtime;

/// <summary>
/// Reference to a registered function, used by IL-compiled and interpreted expressions.
/// </summary>
public sealed record FunctionRef(string Name, Func<object?[], object?> Function)
{
    public object? Invoke(object?[] args) => Function(args);
}

internal sealed record LambdaValue(List<string> Parameters, Expr Body, CsEvalContext Closure, CsEvalOptions? Options = null);

/// <summary>
/// Compiled lambda with IL-compiled body delegate.
/// </summary>
internal sealed record CompiledLambdaValue(
    List<string> Parameters,
    Func<object?[], CsEvalContext, object?> CompiledBody,
    CsEvalContext Closure);

internal sealed record MethodRef(object Target, string MethodName);

internal sealed record StaticMethodRef(Type Type, string MethodName);

internal sealed record ModuleMethodRef(ModuleInfo Module, IServiceProvider? ServiceProvider, MethodInfo Method);

/// <summary>
/// Sentinel for partially-resolved namespace paths during FQN type access.
/// Flows through MemberAccess chains until TypeResolver resolves a full type name.
/// Example: IdentifierExpr("System") -> NamespaceRef("System") -> member "Linq" -> NamespaceRef("System.Linq") -> member "Enumerable" -> Type
/// </summary>
internal sealed record NamespaceRef(string Path);

/// <summary>
/// Wrapper for a named argument value. Used to pass parameter name information
/// through the method invocation stack.
/// </summary>
internal sealed record NamedArg(string Name, object? Value);

/// <summary>
/// Marker for out parameter arguments. Flows through the method invocation stack
/// so MethodInvoker can detect ByRef parameters and set up the args array correctly.
/// After method invocation, the evaluator reads modified values from the args array
/// and defines variables in the current scope.
/// </summary>
internal sealed record OutArgMarker(string VariableName, string? TypeName, bool IsDiscard);
