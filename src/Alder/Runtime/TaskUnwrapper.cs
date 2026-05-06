using System.Collections.Concurrent;
using Alder.Diagnostics;

namespace Alder.Runtime;

internal static class TaskUnwrapper
{
    private static readonly ConcurrentDictionary<Type, Func<object, object?>?> TaskResultAccessorCache = new();
    private static readonly ConcurrentDictionary<Type, Func<object, Task>?> ValueTaskAsTaskCache = new();

    internal static ValueTask<object?> AwaitDynamic(object operand)
    {
        if (operand is Task task)
        {
            var accessor = GetTaskResultAccessor(task.GetType());
            if (task.Status == TaskStatus.RanToCompletion)
                return new ValueTask<object?>(accessor?.Invoke(task));
            return AwaitTaskSlow(task, accessor);
        }

        if (operand is ValueTask vt)
        {
            if (vt.IsCompleted)
                return new ValueTask<object?>((object?)null);
            return AwaitValueTaskSlow(vt);
        }

        var type = operand.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var asTask = GetValueTaskAsTaskAdapter(type);
            if (asTask != null)
                return AwaitDynamic(asTask(operand));
        }

        throw new AlderException(DiagnosticDescriptors.NotAwaitable, type.Name);
    }

    private static async ValueTask<object?> AwaitTaskSlow(Task task, Func<object, object?>? accessor)
    {
        await task.ConfigureAwait(false);
        return accessor?.Invoke(task);
    }

    private static async ValueTask<object?> AwaitValueTaskSlow(ValueTask vt)
    {
        await vt.ConfigureAwait(false);
        return null;
    }

    // Walk the type hierarchy to find Task<T> and cache per runtime type.
    // Uses PropertyInfo.GetValue for the Result property (AOT-safe, no MakeGenericMethod).
    private static Func<object, object?>? GetTaskResultAccessor(Type runtimeType)
    {
        return TaskResultAccessorCache.GetOrAdd(runtimeType, static type =>
        {
            var current = type;
            while (current != null && current != typeof(Task))
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var prop = RuntimeTypeIntrospection.FindProperty(current, "Result", BindingFlags.Public | BindingFlags.Instance);
                    if (prop != null)
                        return task => prop.GetValue(task);
                    return null;
                }
                current = current.BaseType;
            }
            return null;
        });
    }

    private static Func<object, Task>? GetValueTaskAsTaskAdapter(Type runtimeType)
    {
        return ValueTaskAsTaskCache.GetOrAdd(runtimeType, static type =>
        {
            var method = RuntimeTypeIntrospection.FindMethod(
                type,
                nameof(ValueTask<int>.AsTask),
                BindingFlags.Public | BindingFlags.Instance,
                []);

            if (method == null || !typeof(Task).IsAssignableFrom(method.ReturnType))
                return null;

            return boxed => (Task)method.Invoke(boxed, null)!;
        });
    }
}
