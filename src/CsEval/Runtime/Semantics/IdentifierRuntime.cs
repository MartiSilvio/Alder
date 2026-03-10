using System.Linq;
using System.Runtime.CompilerServices;
using CsEval.Binding;
using CsEval.Diagnostics;
using CsEval.Interpretation;
using CsEval.Parsing;
using CsEval.Runtime.Extensions;

namespace CsEval.Runtime;

internal static class IdentifierRuntime
{
    public static object? ResolveIdentifier(string name, CsEvalContext context, CsEvalOptions options)
        => ResolveIdentifierCore(name, context, options);

    public static T ResolveIdentifierTyped<T>(string name, CsEvalContext context, CsEvalOptions options)
        => CoerceIdentifierValue<T>(ResolveIdentifierCore(name, context, options));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetVariableTyped<T>(string name, CsEvalContext context)
    {
        if (!context.TryGet(name, out var value))
            throw new CsEvalException(DiagnosticDescriptors.NameNotInContext, name);

        return CoerceIdentifierValue<T>(value);
    }

    public static object? InvokeIdentifierCall(
        string name,
        object?[] args,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken ct,
        IReadOnlyList<string>? typeArgs)
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

        if (context.Modules.TryGetValue(name, out var module))
            return MethodInvoker.InvokeCall(module, args, context, options, ct, typeArgs);

        if (hasVariable || context.TryGet(name, out variableValue))
            return MethodInvoker.InvokeCall(variableValue, args, context, options, ct, typeArgs);

        var callee = ResolveIdentifier(name, context, options);
        return MethodInvoker.InvokeCall(callee, args, context, options, ct, typeArgs);
    }

    public static object? InvokePipelineIdentifier(
        object? leftValue,
        string rightIdentifier,
        CsEvalContext context,
        CsEvalOptions options,
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

        if (context.Modules.TryGetValue(rightIdentifier, out var module))
        {
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.PipeGreater),
                TypeNameFormatter.Of(leftValue),
                module.GetType().Name);
        }

        if (hasVariable || context.TryGet(rightIdentifier, out variableValue))
        {
            if (!IsPipelineCallable(variableValue))
            {
                throw new CsEvalException(
                    DiagnosticDescriptors.BadBinaryOps,
                    TokenLexemes.GetCanonical(TokenType.PipeGreater),
                    TypeNameFormatter.Of(leftValue),
                    TypeNameFormatter.Of(variableValue));
            }

            return MethodInvoker.InvokeCall(variableValue, args, context, options, ct, null);
        }

        var callee = ResolveIdentifier(rightIdentifier, context, options);
        if (!IsPipelineCallable(callee))
        {
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.PipeGreater),
                TypeNameFormatter.Of(leftValue),
                TypeNameFormatter.Of(callee));
        }

        return MethodInvoker.InvokeCall(callee, args, context, options, ct, null);
    }

    public static object? InvokeBareMathOrCall(
        string name,
        object?[] args,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken ct,
        IReadOnlyList<string>? typeArgs) =>
        InvokeIdentifierCall(name, args, context, options, ct, typeArgs);

    public static void DefineOutVariables(
        object?[] invocationArgs,
        IReadOnlyList<OutVariableBinding> bindings,
        CsEvalContext context)
    {
        for (var i = 0; i < bindings.Count; i++)
        {
            var binding = bindings[i];
            if ((uint)binding.ArgumentIndex >= (uint)invocationArgs.Length)
                throw new CsEvalException($"Invalid out argument index '{binding.ArgumentIndex}'.");

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
        CsEvalContext context,
        CsEvalOptions options)
    {
        return new LambdaValue(parameterNames.ToList(), body, context, options);
    }

    public static object? GetLambdaArg(object?[] args, int index)
    {
        return index < args.Length ? args[index] : null;
    }

    private static object? ResolveIdentifierCore(string name, CsEvalContext context, CsEvalOptions options)
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

        return TypeHelpers.CoerceNumeric(value, targetType);
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
