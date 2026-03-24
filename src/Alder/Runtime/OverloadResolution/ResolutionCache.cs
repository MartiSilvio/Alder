using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Alder.Runtime;

/// <summary>
/// Caches overload resolution results for instance and static method calls.
/// Keyed by (declaring type, method name, argument type signature).
/// Excludes lambda, out, and named-arg calls since their resolution depends
/// on lambda body identity or argument structure that isn't captured in the key.
/// </summary>
internal static class ResolutionCache
{
    private readonly record struct ArgumentShape(
        ArgumentKind Kind,
        Type? RuntimeType,
        int LambdaArity);

    private readonly record struct ResolutionKey(
        Type DeclaringType,
        string MethodName,
        ImmutableArray<ArgumentShape> ArgShapes);

    private static readonly ConcurrentDictionary<ResolutionKey, ResolvedCall> Cache = new();
    private static readonly ConcurrentQueue<ResolutionKey> InsertionOrder = new();
    private const int MaxSize = 4096;

    public static bool TryGet(
        Type type,
        string methodName,
        ReadOnlySpan<ArgumentDescriptor> args,
        out ResolvedCall result)
    {
        result = default;

        if (!TryBuildKey(type, methodName, args, out var key))
            return false;

        return Cache.TryGetValue(key, out result);
    }

    public static void Set(
        Type type,
        string methodName,
        ReadOnlySpan<ArgumentDescriptor> args,
        ResolvedCall resolved)
    {
        if (!TryBuildKey(type, methodName, args, out var key))
            return;

        if (Cache.TryAdd(key, resolved))
        {
            InsertionOrder.Enqueue(key);
            while (Cache.Count > MaxSize && InsertionOrder.TryDequeue(out var oldest))
                Cache.TryRemove(oldest, out _);
        }
    }

    private static bool TryBuildKey(
        Type type,
        string methodName,
        ReadOnlySpan<ArgumentDescriptor> args,
        out ResolutionKey key)
    {
        key = default;
        var shapes = ImmutableArray.CreateBuilder<ArgumentShape>(args.Length);

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Kind is ArgumentKind.Lambda or ArgumentKind.Out)
                return false;
            if (args[i].Name != null)
                return false;

            shapes.Add(new ArgumentShape(
                args[i].Kind,
                args[i].StaticType,
                args[i].LambdaArity));
        }

        key = new ResolutionKey(type, methodName, shapes.ToImmutable());
        return true;
    }
}
