using System.Collections.Concurrent;
using Alder.Diagnostics;

namespace Alder.Runtime;

internal static class TaskUnwrapper
{
    // JIT-path caches keyed by runtime type. Resolving Task<T>.Result and ValueTask<T>.AsTask
    // through RuntimeTypeIntrospection runs GetMethods/GetProperties (array allocation) plus a
    // LINQ scan, so without memoization every await re-pays that lookup. The cached closure still
    // reflection-invokes the member, exactly as before the AOT rewrite. The NativeAOT branches
    // below dispatch through generated code and are cached by that machinery, so these caches are
    // only consulted when dynamic code is supported. A null entry means "no such member" (e.g. a
    // non-generic Task has no Result), which is itself worth caching.
    private static readonly ConcurrentDictionary<Type, Func<object, object?>?> JitResultAccessorCache = new();
    private static readonly ConcurrentDictionary<Type, Func<object, Task>?> JitAsTaskAdapterCache = new();
    internal static ValueTask<object?> AwaitDynamic(object operand, AlderContext context)
    {
        if (operand is Task task)
        {
            if (task.Status == TaskStatus.RanToCompletion)
                return new ValueTask<object?>(GetTaskResult(task, context));
            return AwaitTaskSlow(task, context);
        }

        if (operand is ValueTask vt)
        {
            if (vt.IsCompleted)
                return new ValueTask<object?>((object?)null);
            return AwaitValueTaskSlow(vt);
        }

        // ValueTask<T> (and other Task-returning awaitables) expose a parameterless
        // AsTask() returning Task. Invoke it through generated dispatch (reflection
        // invoke is unavailable under NativeAOT), then await the resulting Task.
        var type = operand.GetType();
        if (TryConvertToTask(operand, type, context, out var asTask))
            return AwaitDynamic(asTask!, context);

        throw new AlderException(DiagnosticDescriptors.NotAwaitable, type.Name);
    }

    private static async ValueTask<object?> AwaitTaskSlow(Task task, AlderContext context)
    {
        await task.ConfigureAwait(false);
        return GetTaskResult(task, context);
    }

    private static async ValueTask<object?> AwaitValueTaskSlow(ValueTask vt)
    {
        await vt.ConfigureAwait(false);
        return null;
    }

    // Read Task<T>.Result the same way member access reads any property: through the
    // generated dispatch under NativeAOT, and through reflection otherwise. The original
    // code read Result via reflection (PropertyInfo.GetValue), which is dead under
    // NativeAOT — there is no dynamic invoke and the Result metadata is trimmed — so it
    // silently returned null for every awaited Task<T>. (The open-generic identity check
    // it also used, `GetGenericTypeDefinition() == typeof(Task<>)`, was sound and not the
    // cause.) A dispatch miss yields null: a non-generic Task has no Result, and neither
    // do the internal Task<VoidTaskResult> instances behind Task.CompletedTask /
    // Task.Delay; awaiting those is void.
    private static object? GetTaskResult(Task task, AlderContext context)
    {
        var type = task.GetType();

        if (!MethodDispatchCache.DynamicCodeSupported)
        {
            return TypedDispatchHelper.TryGetMember(context.Config, type, "Result", task, out var value)
                ? value
                : null;
        }

        var accessor = JitResultAccessorCache.GetOrAdd(type, static t =>
        {
            var prop = RuntimeTypeIntrospection.FindProperty(t, "Result", BindingFlags.Public | BindingFlags.Instance);
            return prop == null ? null : task => prop.GetValue(task);
        });
        return accessor?.Invoke(task);
    }

    private static bool TryConvertToTask(object operand, Type type, AlderContext context, out Task? asTask)
    {
        asTask = null;

        if (!MethodDispatchCache.DynamicCodeSupported)
        {
            if (TypedDispatchHelper.TryInvokeInstance(context.Config, type, "AsTask", operand, [], out var result)
                && result is Task dispatched)
            {
                asTask = dispatched;
                return true;
            }
            return false;
        }

        var adapter = JitAsTaskAdapterCache.GetOrAdd(type, static t =>
        {
            var method = RuntimeTypeIntrospection.FindMethod(
                t, "AsTask", BindingFlags.Public | BindingFlags.Instance, []);
            if (method == null || !typeof(Task).IsAssignableFrom(method.ReturnType))
                return null;
            return operand => (Task)method.Invoke(operand, null)!;
        });

        if (adapter == null)
            return false;

        asTask = adapter(operand);
        return true;
    }
}
