using System.Runtime.CompilerServices;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime.Extensions;

namespace Alder.Runtime.Semantics;

internal static class IdentifierRuntime
{
    public static object? ResolveIdentifier(string name, AlderContext context)
        => ResolveIdentifierCore(name, context);

    public static T ResolveIdentifierTyped<T>(string name, AlderContext context)
        => TypeHelpers.CoerceToType<T>(ResolveIdentifierCore(name, context));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetVariableTyped<T>(string name, AlderContext context)
    {
        if (!context.TryGet(name, out var value))
            throw new AlderException(DiagnosticDescriptors.NameNotInContext, name);

        return TypeHelpers.CoerceToType<T>(value);
    }

    public static object? InvokeIdentifierCall(
        string name,
        object?[] args,
        AlderContext context,
        IReadOnlyList<string>? typeArgs,
        CancellationToken ct = default)
    {
        if (TryInvokeDirectIdentifierCallable(name, args, context, out var result))
            return result;

        return MethodInvoker.InvokeCall(ResolveIdentifierCallable(name, context), args, context, typeArgs, ct);
    }

    public static object? InvokePipelineIdentifier(
        object? leftValue,
        string rightIdentifier,
        AlderContext context,
        CancellationToken ct)
    {
        var args = new object?[] { leftValue };

        if (TryInvokeDirectIdentifierCallable(rightIdentifier, args, context, out var result))
            return result;

        var callee = ResolveIdentifierCallable(rightIdentifier, context);
        return InvokeResolvedPipelineCallable(leftValue, callee, args, context, ct);
    }

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
        AlderContext context)
    {
        return new LambdaValue(parameterNames.ToList(), body, context);
    }

    public static object CreateIteratorLambdaValue(
        string[] parameterNames,
        Expr body,
        AlderContext context,
        string returnTypeName)
    {
        var lambda = new LambdaValue(parameterNames.ToList(), body, context);
        var returnType = context.TypeResolver.ResolveType(returnTypeName);
        if (returnType != null)
            lambda.IteratorElementType = TypeHelpers.GetEnumerableElementType(returnType);
        return lambda;
    }

    private static object? ResolveIdentifierCore(string name, AlderContext context)
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

        if (context.Config.LanguageMode == LanguageMode.Extended &&
            BareMathNames.TryGetConstant(name, out var constant))
            return constant;

        return context.Get(name);
    }

    private static bool TryInvokeDirectIdentifierCallable(
        string name,
        object?[] args,
        AlderContext context,
        out object? result)
    {
        if (context.Functions.TryGetValue(name, out var function))
        {
            result = function(args);
            return true;
        }

        var hasVariable = context.TryGet(name, out _);
        if (!hasVariable &&
            TryInvokeExtendedBuiltIn(name, args, context, out result))
        {
            return true;
        }

        result = null;
        return false;
    }

    private static bool TryInvokeExtendedBuiltIn(
        string name,
        object?[] args,
        AlderContext context,
        out object? result)
    {
        if (context.Config.LanguageMode == LanguageMode.Extended &&
            BareMathNames.TryGetFunction(name, args.Length, out var mathFunc))
        {
            result = mathFunc(args);
            return true;
        }

        if (context.Config.LanguageMode == LanguageMode.Extended &&
            DateArithmeticSugar.TryInvokeClockFunction(name, args, context.Config.IsCaseSensitive, out var clockValue))
        {
            result = clockValue;
            return true;
        }

        if (context.Config.LanguageMode == LanguageMode.Extended &&
            AggregateBuiltins.TryInvoke(name, args, context.Config.IsCaseSensitive, out var aggregateResult))
        {
            result = aggregateResult;
            return true;
        }

        result = null;
        return false;
    }

    private static object? ResolveIdentifierCallable(string name, AlderContext context)
    {
        if (context.Modules.TryGetValue(name, out var module))
            return module;

        if (context.TryGet(name, out var value))
            return value;

        return ResolveIdentifier(name, context);
    }

    private static object? InvokeResolvedPipelineCallable(
        object? leftValue,
        object? callee,
        object?[] args,
        AlderContext context,
        CancellationToken ct)
    {
        if (!MethodInvoker.IsCallable(callee))
        {
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.PipeGreater),
                TypeNameFormatter.Of(leftValue),
                TypeNameFormatter.Of(callee));
        }

        return MethodInvoker.InvokeCall(callee, args, context, null, ct);
    }
}
