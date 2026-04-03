using System.Collections.Concurrent;
using System.Reflection;
using Alder.Diagnostics;

namespace Alder.Runtime;

internal static class TaskUnwrapper
{
    private static readonly ConcurrentDictionary<Type, Func<object, ValueTask<object?>>> AwaitDelegateCache = new();

    private static readonly MethodInfo AwaitTaskCoreMethod =
        typeof(TaskUnwrapper).GetMethod(nameof(AwaitTaskCore), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo AwaitValueTaskCoreMethod =
        typeof(TaskUnwrapper).GetMethod(nameof(AwaitValueTaskCore), BindingFlags.NonPublic | BindingFlags.Static)!;

    internal static ValueTask<object?> AwaitDynamic(object operand)
    {
        if (operand is Task task)
        {
            var taskType = task.GetType();
            if (taskType.IsGenericType && taskType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var awaiter = GetOrCreateAwaiter(taskType);
                return awaiter(operand);
            }

            return AwaitPlainTask(task);
        }

        if (operand is ValueTask vt)
            return AwaitPlainValueTask(vt);

        var type = operand.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var awaiter = GetOrCreateAwaiter(type);
            return awaiter(operand);
        }

        throw new AlderException(DiagnosticDescriptors.NotAwaitable, type.Name);
    }

    private static Func<object, ValueTask<object?>> GetOrCreateAwaiter(Type taskType)
    {
        return AwaitDelegateCache.GetOrAdd(taskType, static type =>
        {
            var genericDef = type.GetGenericTypeDefinition();
            var resultType = type.GetGenericArguments()[0];

            MethodInfo coreMethod;
            if (genericDef == typeof(Task<>))
                coreMethod = AwaitTaskCoreMethod.MakeGenericMethod(resultType);
            else if (genericDef == typeof(ValueTask<>))
                coreMethod = AwaitValueTaskCoreMethod.MakeGenericMethod(resultType);
            else
                throw new AlderException(DiagnosticDescriptors.NotAwaitable, type.Name);

            return (Func<object, ValueTask<object?>>)Delegate.CreateDelegate(
                typeof(Func<object, ValueTask<object?>>), coreMethod);
        });
    }

    private static async ValueTask<object?> AwaitTaskCore<T>(object boxedTask)
    {
        return await (Task<T>)boxedTask;
    }

    private static async ValueTask<object?> AwaitValueTaskCore<T>(object boxedValueTask)
    {
        return await (ValueTask<T>)boxedValueTask;
    }

    private static async ValueTask<object?> AwaitPlainTask(Task task)
    {
        await task;
        return null;
    }

    private static async ValueTask<object?> AwaitPlainValueTask(ValueTask valueTask)
    {
        await valueTask;
        return null;
    }
}
