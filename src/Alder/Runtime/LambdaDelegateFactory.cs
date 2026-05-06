using System.Collections.Concurrent;

namespace Alder.Runtime;

// Factory methods are in LambdaDelegateFactory.generated.cs (scripts/generate-delegate-factories.sh)
internal static partial class LambdaDelegateFactory
{
    private static readonly object?[] EmptyArgs = [];

    private static readonly MethodInfo[] CompiledActionFactories =
    [
        GetFactory(nameof(CreateCompiledAction0)),
        GetFactory(nameof(CreateCompiledAction1)),
        GetFactory(nameof(CreateCompiledAction2)),
        GetFactory(nameof(CreateCompiledAction3)),
        GetFactory(nameof(CreateCompiledAction4)),
        GetFactory(nameof(CreateCompiledAction5)),
        GetFactory(nameof(CreateCompiledAction6)),
        GetFactory(nameof(CreateCompiledAction7)),
        GetFactory(nameof(CreateCompiledAction8)),
        GetFactory(nameof(CreateCompiledAction9)),
        GetFactory(nameof(CreateCompiledAction10)),
        GetFactory(nameof(CreateCompiledAction11)),
        GetFactory(nameof(CreateCompiledAction12)),
        GetFactory(nameof(CreateCompiledAction13)),
        GetFactory(nameof(CreateCompiledAction14)),
        GetFactory(nameof(CreateCompiledAction15)),
        GetFactory(nameof(CreateCompiledAction16)),
    ];

    private static readonly MethodInfo[] CompiledFuncFactories =
    [
        GetFactory(nameof(CreateCompiledFunc0)),
        GetFactory(nameof(CreateCompiledFunc1)),
        GetFactory(nameof(CreateCompiledFunc2)),
        GetFactory(nameof(CreateCompiledFunc3)),
        GetFactory(nameof(CreateCompiledFunc4)),
        GetFactory(nameof(CreateCompiledFunc5)),
        GetFactory(nameof(CreateCompiledFunc6)),
        GetFactory(nameof(CreateCompiledFunc7)),
        GetFactory(nameof(CreateCompiledFunc8)),
        GetFactory(nameof(CreateCompiledFunc9)),
        GetFactory(nameof(CreateCompiledFunc10)),
        GetFactory(nameof(CreateCompiledFunc11)),
        GetFactory(nameof(CreateCompiledFunc12)),
        GetFactory(nameof(CreateCompiledFunc13)),
        GetFactory(nameof(CreateCompiledFunc14)),
        GetFactory(nameof(CreateCompiledFunc15)),
        GetFactory(nameof(CreateCompiledFunc16)),
    ];

    private static readonly MethodInfo[] InterpretedActionFactories =
    [
        GetFactory(nameof(CreateInterpretedAction0)),
        GetFactory(nameof(CreateInterpretedAction1)),
        GetFactory(nameof(CreateInterpretedAction2)),
        GetFactory(nameof(CreateInterpretedAction3)),
        GetFactory(nameof(CreateInterpretedAction4)),
        GetFactory(nameof(CreateInterpretedAction5)),
        GetFactory(nameof(CreateInterpretedAction6)),
        GetFactory(nameof(CreateInterpretedAction7)),
        GetFactory(nameof(CreateInterpretedAction8)),
        GetFactory(nameof(CreateInterpretedAction9)),
        GetFactory(nameof(CreateInterpretedAction10)),
        GetFactory(nameof(CreateInterpretedAction11)),
        GetFactory(nameof(CreateInterpretedAction12)),
        GetFactory(nameof(CreateInterpretedAction13)),
        GetFactory(nameof(CreateInterpretedAction14)),
        GetFactory(nameof(CreateInterpretedAction15)),
        GetFactory(nameof(CreateInterpretedAction16)),
    ];

    private static readonly MethodInfo[] InterpretedFuncFactories =
    [
        GetFactory(nameof(CreateInterpretedFunc0)),
        GetFactory(nameof(CreateInterpretedFunc1)),
        GetFactory(nameof(CreateInterpretedFunc2)),
        GetFactory(nameof(CreateInterpretedFunc3)),
        GetFactory(nameof(CreateInterpretedFunc4)),
        GetFactory(nameof(CreateInterpretedFunc5)),
        GetFactory(nameof(CreateInterpretedFunc6)),
        GetFactory(nameof(CreateInterpretedFunc7)),
        GetFactory(nameof(CreateInterpretedFunc8)),
        GetFactory(nameof(CreateInterpretedFunc9)),
        GetFactory(nameof(CreateInterpretedFunc10)),
        GetFactory(nameof(CreateInterpretedFunc11)),
        GetFactory(nameof(CreateInterpretedFunc12)),
        GetFactory(nameof(CreateInterpretedFunc13)),
        GetFactory(nameof(CreateInterpretedFunc14)),
        GetFactory(nameof(CreateInterpretedFunc15)),
        GetFactory(nameof(CreateInterpretedFunc16)),
    ];

    internal static Delegate CreateCompiledDelegate(
        CompiledLambdaValue lambda,
        Type delegateType,
        Type[] paramTypes,
        Type returnType)
    {
        if (returnType == typeof(void))
            return InvokeFactory(CompiledActionFactories, paramTypes.Length, paramTypes, lambda);

        var genericArgs = new Type[paramTypes.Length + 1];
        Array.Copy(paramTypes, genericArgs, paramTypes.Length);
        genericArgs[^1] = returnType;
        return InvokeFactory(CompiledFuncFactories, paramTypes.Length, genericArgs, lambda);
    }

    internal static Delegate CreateInterpretedDelegate(
        LambdaValue lambda,
        Type delegateType,
        Type[] paramTypes,
        Type returnType)
    {
        if (returnType == typeof(void))
            return InvokeFactory(InterpretedActionFactories, paramTypes.Length, paramTypes, lambda);

        var genericArgs = new Type[paramTypes.Length + 1];
        Array.Copy(paramTypes, genericArgs, paramTypes.Length);
        genericArgs[^1] = returnType;
        return InvokeFactory(InterpretedFuncFactories, paramTypes.Length, genericArgs, lambda);
    }

    private static Delegate InvokeFactory(MethodInfo[] factories, int arity, Type[] genericArgs, object lambda)
    {
        if ((uint)arity >= (uint)factories.Length)
            throw new AlderException(Diagnostics.DiagnosticDescriptors.UnsupportedDelegateArity, arity);

        var factory = factories[arity];
        if (genericArgs.Length > 0)
            factory = RuntimeGenericClosure.CloseMethod(factory, genericArgs);
        return (Delegate)factory.Invoke(null, [lambda])!;
    }

    private static MethodInfo GetFactory(string name) =>
        typeof(LambdaDelegateFactory).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;

    internal static TResult CastResult<TResult>(object? value)
    {
        if (value is TResult typed)
            return typed;

        // NamedTupleValue wraps a real ValueTuple — unwrap before casting so .NET
        // delegates (e.g. LINQ's Func<T,TResult>) receive the underlying ValueTuple.
        if (value is NamedTupleValue named)
        {
            if (named.Tuple is TResult unwrapped)
                return unwrapped;
            // TResult is object — keep the NamedTupleValue so element names are preserved
            // through LINQ chains (e.g. Select → Where with named element access).
            if (typeof(TResult) == typeof(object))
                return (TResult)value!;
        }

        // Async lambda returns Task<object?> but the delegate expects Task<T>.
        // MakeGenericMethod is safe here: CastResult is only called from factory methods
        // which are closed with concrete types by the JIT or AOT delegate factories.
        if (value is Task<object?> objectTask && typeof(TResult).IsGenericType
            && typeof(TResult).GetGenericTypeDefinition() == typeof(Task<>))
        {
            return (TResult)(object)MapTaskResult(objectTask, typeof(TResult).GetGenericArguments()[0]);
        }

        if (value is LambdaValue or CompiledLambdaValue)
        {
            var converted = LambdaDelegateConverter.TryConvert(value, typeof(TResult));
            if (converted != null)
                return (TResult)(object)converted;
        }

        return (TResult)value!;
    }

    private static readonly ConcurrentDictionary<Type, Func<Task<object?>, object>> TaskMapperCache = new();

    private static readonly MethodInfo MapTaskCoreMethod =
        typeof(LambdaDelegateFactory).GetMethod(nameof(MapTaskCore), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static object MapTaskResult(Task<object?> source, Type resultType)
    {
        var mapper = TaskMapperCache.GetOrAdd(resultType, static type =>
        {
            var closed = RuntimeGenericClosure.CloseMethod(MapTaskCoreMethod, [type]);
            return (Func<Task<object?>, object>)Delegate.CreateDelegate(
                typeof(Func<Task<object?>, object>), closed);
        });
        return mapper(source);
    }

    private static async Task<T> MapTaskCore<T>(Task<object?> source)
    {
        var result = await source.ConfigureAwait(false);
        return TypeHelpers.CoerceToType<T>(result);
    }
}
