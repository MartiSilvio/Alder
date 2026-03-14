namespace CsEval.Runtime;

internal static class LambdaDelegateFactory
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
            throw new InvalidOperationException($"Unsupported delegate arity: {arity}");

        var factory = factories[arity];
        if (genericArgs.Length > 0)
            factory = RuntimeGenericFactory.CloseGenericMethod(factory, genericArgs);
        return (Delegate)factory.Invoke(null, [lambda])!;
    }

    private static MethodInfo GetFactory(string name) =>
        typeof(LambdaDelegateFactory).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;

    private static TResult CastResult<TResult>(object? value)
    {
        if (value is TResult typed)
            return typed;

        if (value is LambdaValue or CompiledLambdaValue)
        {
            var converted = LambdaDelegateConverter.TryConvert(value, typeof(TResult));
            if (converted != null)
                return (TResult)(object)converted;
        }

        return (TResult)value!;
    }

    private static Delegate CreateCompiledAction0(CompiledLambdaValue lambda) =>
        new Action(() => _ = MethodInvoker.InvokeCompiledLambda0(lambda));

    private static Delegate CreateCompiledAction1<T0>(CompiledLambdaValue lambda) =>
        new Action<T0>((arg0) => _ = MethodInvoker.InvokeCompiledLambda1(lambda, arg0));

    private static Delegate CreateCompiledAction2<T0, T1>(CompiledLambdaValue lambda) =>
        new Action<T0, T1>((arg0, arg1) => _ = MethodInvoker.InvokeCompiledLambda2(lambda, arg0, arg1));

    private static Delegate CreateCompiledAction3<T0, T1, T2>(CompiledLambdaValue lambda) =>
        new Action<T0, T1, T2>((arg0, arg1, arg2) => _ = MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2]));

    private static Delegate CreateCompiledAction4<T0, T1, T2, T3>(CompiledLambdaValue lambda) =>
        new Action<T0, T1, T2, T3>((arg0, arg1, arg2, arg3) => _ = MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3]));

    private static Delegate CreateCompiledAction5<T0, T1, T2, T3, T4>(CompiledLambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4>((arg0, arg1, arg2, arg3, arg4) => _ = MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4]));

    private static Delegate CreateCompiledAction6<T0, T1, T2, T3, T4, T5>(CompiledLambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5>((arg0, arg1, arg2, arg3, arg4, arg5) => _ = MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5]));

    private static Delegate CreateCompiledAction7<T0, T1, T2, T3, T4, T5, T6>(CompiledLambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6>((arg0, arg1, arg2, arg3, arg4, arg5, arg6) => _ = MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6]));

    private static Delegate CreateCompiledAction8<T0, T1, T2, T3, T4, T5, T6, T7>(CompiledLambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6, T7>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7) => _ = MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7]));

    private static Delegate CreateCompiledAction9<T0, T1, T2, T3, T4, T5, T6, T7, T8>(CompiledLambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6, T7, T8>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => _ = MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8]));

    private static Delegate CreateCompiledAction10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(CompiledLambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) => _ = MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9]));

    private static Delegate CreateCompiledAction11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(CompiledLambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10) => _ = MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10]));

    private static Delegate CreateCompiledAction12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(CompiledLambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11) => _ = MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11]));

    private static Delegate CreateCompiledAction13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(CompiledLambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12) => _ = MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12]));

    private static Delegate CreateCompiledAction14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(CompiledLambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13) => _ = MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13]));

    private static Delegate CreateCompiledAction15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(CompiledLambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14) => _ = MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14]));

    private static Delegate CreateCompiledAction16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(CompiledLambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15) => _ = MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15]));

    private static Delegate CreateCompiledFunc0<TResult>(CompiledLambdaValue lambda) =>
        new Func<TResult>(() => CastResult<TResult>(MethodInvoker.InvokeCompiledLambda0(lambda)));

    private static Delegate CreateCompiledFunc1<T0, TResult>(CompiledLambdaValue lambda) =>
        new Func<T0, TResult>((arg0) => CastResult<TResult>(MethodInvoker.InvokeCompiledLambda1(lambda, arg0)));

    private static Delegate CreateCompiledFunc2<T0, T1, TResult>(CompiledLambdaValue lambda) =>
        new Func<T0, T1, TResult>((arg0, arg1) => CastResult<TResult>(MethodInvoker.InvokeCompiledLambda2(lambda, arg0, arg1)));

    private static Delegate CreateCompiledFunc3<T0, T1, T2, TResult>(CompiledLambdaValue lambda) =>
        new Func<T0, T1, T2, TResult>((arg0, arg1, arg2) => CastResult<TResult>(MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2])));

    private static Delegate CreateCompiledFunc4<T0, T1, T2, T3, TResult>(CompiledLambdaValue lambda) =>
        new Func<T0, T1, T2, T3, TResult>((arg0, arg1, arg2, arg3) => CastResult<TResult>(MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3])));

    private static Delegate CreateCompiledFunc5<T0, T1, T2, T3, T4, TResult>(CompiledLambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, TResult>((arg0, arg1, arg2, arg3, arg4) => CastResult<TResult>(MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4])));

    private static Delegate CreateCompiledFunc6<T0, T1, T2, T3, T4, T5, TResult>(CompiledLambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, TResult>((arg0, arg1, arg2, arg3, arg4, arg5) => CastResult<TResult>(MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5])));

    private static Delegate CreateCompiledFunc7<T0, T1, T2, T3, T4, T5, T6, TResult>(CompiledLambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6) => CastResult<TResult>(MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6])));

    private static Delegate CreateCompiledFunc8<T0, T1, T2, T3, T4, T5, T6, T7, TResult>(CompiledLambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, T7, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7) => CastResult<TResult>(MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7])));

    private static Delegate CreateCompiledFunc9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TResult>(CompiledLambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => CastResult<TResult>(MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8])));

    private static Delegate CreateCompiledFunc10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(CompiledLambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) => CastResult<TResult>(MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9])));

    private static Delegate CreateCompiledFunc11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(CompiledLambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10) => CastResult<TResult>(MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10])));

    private static Delegate CreateCompiledFunc12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(CompiledLambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11) => CastResult<TResult>(MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11])));

    private static Delegate CreateCompiledFunc13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(CompiledLambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12) => CastResult<TResult>(MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12])));

    private static Delegate CreateCompiledFunc14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(CompiledLambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13) => CastResult<TResult>(MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13])));

    private static Delegate CreateCompiledFunc15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(CompiledLambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14) => CastResult<TResult>(MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14])));

    private static Delegate CreateCompiledFunc16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(CompiledLambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15) => CastResult<TResult>(MethodInvoker.InvokeCompiledLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15])));

    private static Delegate CreateInterpretedAction0(LambdaValue lambda) =>
        new Action(() => _ = MethodInvoker.InvokeLambda(lambda, EmptyArgs, lambda.Closure));

    private static Delegate CreateInterpretedAction1<T0>(LambdaValue lambda) =>
        new Action<T0>((arg0) => _ = MethodInvoker.InvokeLambda(lambda, [arg0], lambda.Closure));

    private static Delegate CreateInterpretedAction2<T0, T1>(LambdaValue lambda) =>
        new Action<T0, T1>((arg0, arg1) => _ = MethodInvoker.InvokeLambda(lambda, [arg0, arg1], lambda.Closure));

    private static Delegate CreateInterpretedAction3<T0, T1, T2>(LambdaValue lambda) =>
        new Action<T0, T1, T2>((arg0, arg1, arg2) => _ = MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2], lambda.Closure));

    private static Delegate CreateInterpretedAction4<T0, T1, T2, T3>(LambdaValue lambda) =>
        new Action<T0, T1, T2, T3>((arg0, arg1, arg2, arg3) => _ = MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3], lambda.Closure));

    private static Delegate CreateInterpretedAction5<T0, T1, T2, T3, T4>(LambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4>((arg0, arg1, arg2, arg3, arg4) => _ = MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4], lambda.Closure));

    private static Delegate CreateInterpretedAction6<T0, T1, T2, T3, T4, T5>(LambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5>((arg0, arg1, arg2, arg3, arg4, arg5) => _ = MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5], lambda.Closure));

    private static Delegate CreateInterpretedAction7<T0, T1, T2, T3, T4, T5, T6>(LambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6>((arg0, arg1, arg2, arg3, arg4, arg5, arg6) => _ = MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6], lambda.Closure));

    private static Delegate CreateInterpretedAction8<T0, T1, T2, T3, T4, T5, T6, T7>(LambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6, T7>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7) => _ = MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7], lambda.Closure));

    private static Delegate CreateInterpretedAction9<T0, T1, T2, T3, T4, T5, T6, T7, T8>(LambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6, T7, T8>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => _ = MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8], lambda.Closure));

    private static Delegate CreateInterpretedAction10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(LambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) => _ = MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9], lambda.Closure));

    private static Delegate CreateInterpretedAction11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(LambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10) => _ = MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10], lambda.Closure));

    private static Delegate CreateInterpretedAction12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(LambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11) => _ = MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11], lambda.Closure));

    private static Delegate CreateInterpretedAction13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(LambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12) => _ = MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12], lambda.Closure));

    private static Delegate CreateInterpretedAction14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(LambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13) => _ = MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13], lambda.Closure));

    private static Delegate CreateInterpretedAction15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(LambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14) => _ = MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14], lambda.Closure));

    private static Delegate CreateInterpretedAction16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(LambdaValue lambda) =>
        new Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15) => _ = MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15], lambda.Closure));

    private static Delegate CreateInterpretedFunc0<TResult>(LambdaValue lambda) =>
        new Func<TResult>(() => CastResult<TResult>(MethodInvoker.InvokeLambda(lambda, EmptyArgs, lambda.Closure)));

    private static Delegate CreateInterpretedFunc1<T0, TResult>(LambdaValue lambda) =>
        new Func<T0, TResult>((arg0) => CastResult<TResult>(MethodInvoker.InvokeLambda(lambda, [arg0], lambda.Closure)));

    private static Delegate CreateInterpretedFunc2<T0, T1, TResult>(LambdaValue lambda) =>
        new Func<T0, T1, TResult>((arg0, arg1) => CastResult<TResult>(MethodInvoker.InvokeLambda(lambda, [arg0, arg1], lambda.Closure)));

    private static Delegate CreateInterpretedFunc3<T0, T1, T2, TResult>(LambdaValue lambda) =>
        new Func<T0, T1, T2, TResult>((arg0, arg1, arg2) => CastResult<TResult>(MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2], lambda.Closure)));

    private static Delegate CreateInterpretedFunc4<T0, T1, T2, T3, TResult>(LambdaValue lambda) =>
        new Func<T0, T1, T2, T3, TResult>((arg0, arg1, arg2, arg3) => CastResult<TResult>(MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3], lambda.Closure)));

    private static Delegate CreateInterpretedFunc5<T0, T1, T2, T3, T4, TResult>(LambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, TResult>((arg0, arg1, arg2, arg3, arg4) => CastResult<TResult>(MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4], lambda.Closure)));

    private static Delegate CreateInterpretedFunc6<T0, T1, T2, T3, T4, T5, TResult>(LambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, TResult>((arg0, arg1, arg2, arg3, arg4, arg5) => CastResult<TResult>(MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5], lambda.Closure)));

    private static Delegate CreateInterpretedFunc7<T0, T1, T2, T3, T4, T5, T6, TResult>(LambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6) => CastResult<TResult>(MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6], lambda.Closure)));

    private static Delegate CreateInterpretedFunc8<T0, T1, T2, T3, T4, T5, T6, T7, TResult>(LambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, T7, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7) => CastResult<TResult>(MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7], lambda.Closure)));

    private static Delegate CreateInterpretedFunc9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TResult>(LambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => CastResult<TResult>(MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8], lambda.Closure)));

    private static Delegate CreateInterpretedFunc10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(LambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) => CastResult<TResult>(MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9], lambda.Closure)));

    private static Delegate CreateInterpretedFunc11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(LambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10) => CastResult<TResult>(MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10], lambda.Closure)));

    private static Delegate CreateInterpretedFunc12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(LambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11) => CastResult<TResult>(MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11], lambda.Closure)));

    private static Delegate CreateInterpretedFunc13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(LambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12) => CastResult<TResult>(MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12], lambda.Closure)));

    private static Delegate CreateInterpretedFunc14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(LambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13) => CastResult<TResult>(MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13], lambda.Closure)));

    private static Delegate CreateInterpretedFunc15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(LambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14) => CastResult<TResult>(MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14], lambda.Closure)));

    private static Delegate CreateInterpretedFunc16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(LambdaValue lambda) =>
        new Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15) => CastResult<TResult>(MethodInvoker.InvokeLambda(lambda, [arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15], lambda.Closure)));

}
