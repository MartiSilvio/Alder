using System.Collections.Concurrent;
using System.Reflection;
using Alder.Diagnostics;

namespace Alder.Runtime;

internal static class TaskUnwrapper
{
    private static readonly ConcurrentDictionary<Type, Func<object, object?>?> TaskResultAccessorCache = new();

    private static readonly MethodInfo GetResultGenericMethod =
        typeof(TaskUnwrapper).GetMethod(nameof(GetTaskResult), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo AwaitValueTaskCoreMethod =
        typeof(TaskUnwrapper).GetMethod(nameof(AwaitValueTaskCore), BindingFlags.NonPublic | BindingFlags.Static)!;

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
            var method = AwaitValueTaskCoreMethod.MakeGenericMethod(type.GetGenericArguments()[0]);
            return (ValueTask<object?>)method.Invoke(null, [operand])!;
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

    // The runtime type of an async Task<T> is an internal subclass (AsyncStateMachineBox<T>),
    // not Task<T> directly. Walk the hierarchy to find Task<T> and cache per runtime type.
    private static Func<object, object?>? GetTaskResultAccessor(Type runtimeType)
    {
        return TaskResultAccessorCache.GetOrAdd(runtimeType, static type =>
        {
            var current = type;
            while (current != null && current != typeof(Task))
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var resultType = current.GetGenericArguments()[0];
                    var closed = GetResultGenericMethod.MakeGenericMethod(resultType);
                    return (Func<object, object?>)Delegate.CreateDelegate(typeof(Func<object, object?>), closed);
                }
                current = current.BaseType;
            }
            return null;
        });
    }

    private static async ValueTask<object?> AwaitValueTaskCore<T>(object boxed)
    {
        return await (ValueTask<T>)boxed;
    }

    private static object? GetTaskResult<T>(object boxedTask) => ((Task<T>)boxedTask).Result;
}
