using System.Runtime.CompilerServices;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime.Extensions;

namespace Alder.Runtime.Semantics;

internal static class IdentifierRuntime
{
    public static object? ResolveIdentifier(string name, AlderContext context, AlderOptions options)
        => ResolveIdentifierCore(name, context, options);

    public static T ResolveIdentifierTyped<T>(string name, AlderContext context, AlderOptions options)
        => CoerceIdentifierValue<T>(ResolveIdentifierCore(name, context, options));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetVariableTyped<T>(string name, AlderContext context)
    {
        if (!context.TryGet(name, out var value))
            throw new AlderException(DiagnosticDescriptors.NameNotInContext, name);

        return CoerceIdentifierValue<T>(value);
    }

    public static object? InvokeIdentifierCall(
        string name,
        object?[] args,
        AlderContext context,
        AlderOptions options,
        IReadOnlyList<string>? typeArgs,
        CancellationToken ct = default)
    {
        if (context.Functions.TryGetValue(name, out var function))
            return function(args);

        var hasVariable = context.TryGet(name, out var variableValue);

        if (options.LanguageMode == LanguageMode.Extended &&
            !hasVariable &&
            BareMathNames.TryGetFunction(name, args.Length, out var mathFunc))
        {
            return mathFunc(args);
        }

        if (options.LanguageMode == LanguageMode.Extended &&
            !hasVariable &&
            DateArithmeticSugar.TryInvokeClockFunction(name, args, options.IsCaseSensitive, out var clockValue))
        {
            return clockValue;
        }

        if (options.LanguageMode == LanguageMode.Extended &&
            !hasVariable &&
            AggregateBuiltins.TryInvoke(name, args, options.IsCaseSensitive, out var aggregateResult))
        {
            return aggregateResult;
        }

        if (context.Modules.TryGetValue(name, out var module))
            return MethodInvoker.InvokeCall(module, args, context, options, typeArgs, ct);

        if (hasVariable || context.TryGet(name, out variableValue))
            return MethodInvoker.InvokeCall(variableValue, args, context, options, typeArgs, ct);

        var callee = ResolveIdentifier(name, context, options);
        return MethodInvoker.InvokeCall(callee, args, context, options, typeArgs, ct);
    }

    public static object? InvokePipelineIdentifier(
        object? leftValue,
        string rightIdentifier,
        AlderContext context,
        AlderOptions options,
        CancellationToken ct)
    {
        var args = new object?[] { leftValue };

        if (context.Functions.TryGetValue(rightIdentifier, out var function))
            return function(args);

        var hasVariable = context.TryGet(rightIdentifier, out var variableValue);

        if (options.LanguageMode == LanguageMode.Extended &&
            !hasVariable &&
            BareMathNames.TryGetFunction(rightIdentifier, args.Length, out var mathFunc))
        {
            return mathFunc(args);
        }

        if (options.LanguageMode == LanguageMode.Extended &&
            !hasVariable &&
            DateArithmeticSugar.TryInvokeClockFunction(rightIdentifier, args, options.IsCaseSensitive, out var clockValue))
        {
            return clockValue;
        }

        if (options.LanguageMode == LanguageMode.Extended &&
            !hasVariable &&
            AggregateBuiltins.TryInvoke(rightIdentifier, args, options.IsCaseSensitive, out var aggregateResult))
        {
            return aggregateResult;
        }

        if (context.Modules.TryGetValue(rightIdentifier, out var module))
        {
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.PipeGreater),
                TypeNameFormatter.Of(leftValue),
                module.GetType().Name);
        }

        if (hasVariable || context.TryGet(rightIdentifier, out variableValue))
        {
            if (!IsPipelineCallable(variableValue))
            {
                throw new AlderException(
                    DiagnosticDescriptors.BadBinaryOps,
                    TokenLexemes.GetCanonical(TokenType.PipeGreater),
                    TypeNameFormatter.Of(leftValue),
                    TypeNameFormatter.Of(variableValue));
            }

            return MethodInvoker.InvokeCall(variableValue, args, context, options, null, ct);
        }

        var callee = ResolveIdentifier(rightIdentifier, context, options);
        if (!IsPipelineCallable(callee))
        {
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.PipeGreater),
                TypeNameFormatter.Of(leftValue),
                TypeNameFormatter.Of(callee));
        }

        return MethodInvoker.InvokeCall(callee, args, context, options, null, ct);
    }

    public static object? InvokeBareMathOrCall(
        string name,
        object?[] args,
        AlderContext context,
        AlderOptions options,
        IReadOnlyList<string>? typeArgs,
        CancellationToken ct = default) =>
        InvokeIdentifierCall(name, args, context, options, typeArgs, ct);

    public static void DefineOutVariables(
        object?[] invocationArgs,
        IReadOnlyList<OutVariableBinding> bindings,
        AlderContext context)
    {
        for (var i = 0; i < bindings.Count; i++)
        {
            var binding = bindings[i];
            if ((uint)binding.ArgumentIndex >= (uint)invocationArgs.Length)
                throw new AlderException(DiagnosticDescriptors.InvalidOutArgumentIndex, binding.ArgumentIndex);

            var outValue = invocationArgs[binding.ArgumentIndex];
            var variableType = binding.TypeName != null
                ? context.TypeResolver.ResolveType(binding.TypeName)
                : outValue?.GetType() ?? typeof(object);

            context.DefineNew(binding.VariableName, outValue, variableType);
        }
    }

    public static object CreateLambdaValue(
        string[] parameterNames,
        Expr body,
        AlderContext context,
        AlderOptions options)
    {
        return new LambdaValue(parameterNames.ToList(), body, context, options);
    }

    public static object? GetLambdaArg(object?[] args, int index)
    {
        return index < args.Length ? args[index] : null;
    }

    private static object? ResolveIdentifierCore(string name, AlderContext context, AlderOptions options)
    {
        if (context.Functions.TryGetValue(name, out var function))
            return new FunctionRef(name, function);

        if (context.Modules.TryGetValue(name, out var module))
            return module;

        if (context.TryGet(name, out var value))
            return value;

        var resolvedType = context.TypeResolver.TryResolveType(name);
        if (resolvedType != null)
            return resolvedType;

        if (context.TypeResolver.IsNamespaceOrPrefix(name))
            return new NamespaceRef(name);

        if (options.LanguageMode == LanguageMode.Extended &&
            BareMathNames.TryGetConstant(name, out var constant))
            return constant;

        return context.Get(name);
    }

    private static T CoerceIdentifierValue<T>(object? value)
    {
        if (value is T typedValue)
            return typedValue;

        value = CoerceNumericForTargetType<T>(value);
        if (value is T coercedTyped)
            return coercedTyped;

        return CastIdentifierValue<T>(value);
    }

    private static object? CoerceNumericForTargetType<T>(object? value)
    {
        var targetType = typeof(T);
        var numericTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (!TypeHelpers.IsArithmetic(numericTarget))
            return value;

        try
        {
            return TypeHelpers.CoerceNumeric(value, targetType);
        }
        catch (AlderException)
        {
            return value;
        }
    }

    private static T CastIdentifierValue<T>(object? value) => (T)value!;

    private static bool IsPipelineCallable(object? value) => value is
        LambdaValue or
        CompiledLambdaValue or
        FunctionRef or
        Delegate or
        ModuleMethodRef or
        StaticMethodRef or
        MethodRef;
}
