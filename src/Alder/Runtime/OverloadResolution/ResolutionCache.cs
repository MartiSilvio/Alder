using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Alder.Runtime;

/// <summary>
/// Caches overload resolution results for instance and static method calls.
/// The cache key is the declaring type, method name, and argument shape.
/// Calls with lambdas, named arguments, or out arguments are excluded because their resolution
/// depends on information that is not stable in the cache key.
/// </summary>
internal static class ResolutionCache
{
    private readonly record struct ArgumentShape(
        ArgumentKind Kind,
        Type? RuntimeType,
        int LambdaArity);

    private readonly struct ResolutionKey : IEquatable<ResolutionKey>
    {
        public readonly Type DeclaringType;
        public readonly string MethodName;
        public readonly ImmutableArray<ArgumentShape> ArgShapes;

        public ResolutionKey(Type declaringType, string methodName, ImmutableArray<ArgumentShape> argShapes)
        {
            DeclaringType = declaringType;
            MethodName = methodName;
            ArgShapes = argShapes;
        }

        public bool Equals(ResolutionKey other)
        {
            return DeclaringType == other.DeclaringType &&
                   MethodName == other.MethodName &&
                   ArgShapes.SequenceEqual(other.ArgShapes);
        }

        public override bool Equals(object? obj) => obj is ResolutionKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (DeclaringType?.GetHashCode() ?? 0) * 397;
                hash = (hash ^ (MethodName?.GetHashCode() ?? 0)) * 397;
                foreach (var shape in ArgShapes)
                    hash = (hash ^ shape.GetHashCode()) * 397;
                return hash;
            }
        }
    }

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
            if (args[i].Kind is ArgumentKind.Lambda or ArgumentKind.MethodGroup or ArgumentKind.Out)
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
