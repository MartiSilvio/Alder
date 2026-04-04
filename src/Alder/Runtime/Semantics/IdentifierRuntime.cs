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
        if (context.Functions.TryGetValue(name, out var function))
            return function(args);

        var hasVariable = context.TryGet(name, out var variableValue);

        if (context.Config.LanguageMode == LanguageMode.Extended &&
            !hasVariable &&
            BareMathNames.TryGetFunction(name, args.Length, out var mathFunc))
        {
            return mathFunc(args);
        }

        if (context.Config.LanguageMode == LanguageMode.Extended &&
            !hasVariable &&
            DateArithmeticSugar.TryInvokeClockFunction(name, args, context.Config.IsCaseSensitive, out var clockValue))
        {
            return clockValue;
        }

        if (context.Config.LanguageMode == LanguageMode.Extended &&
            !hasVariable &&
            AggregateBuiltins.TryInvoke(name, args, context.Config.IsCaseSensitive, out var aggregateResult))
        {
            return aggregateResult;
        }

        if (context.Modules.TryGetValue(name, out var module))
            return MethodInvoker.InvokeCall(module, args, context, typeArgs, ct);

        if (hasVariable || context.TryGet(name, out variableValue))
            return MethodInvoker.InvokeCall(variableValue, args, context, typeArgs, ct);

        var callee = ResolveIdentifier(name, context);
        return MethodInvoker.InvokeCall(callee, args, context, typeArgs, ct);
    }

    public static object? InvokePipelineIdentifier(
        object? leftValue,
        string rightIdentifier,
        AlderContext context,
        CancellationToken ct)
    {
        var args = new object?[] { leftValue };

        if (context.Functions.TryGetValue(rightIdentifier, out var function))
            return function(args);

        var hasVariable = context.TryGet(rightIdentifier, out var variableValue);

        if (context.Config.LanguageMode == LanguageMode.Extended &&
            !hasVariable &&
            BareMathNames.TryGetFunction(rightIdentifier, args.Length, out var mathFunc))
        {
            return mathFunc(args);
        }

        if (context.Config.LanguageMode == LanguageMode.Extended &&
            !hasVariable &&
            DateArithmeticSugar.TryInvokeClockFunction(rightIdentifier, args, context.Config.IsCaseSensitive, out var clockValue))
        {
            return clockValue;
        }

        if (context.Config.LanguageMode == LanguageMode.Extended &&
            !hasVariable &&
            AggregateBuiltins.TryInvoke(rightIdentifier, args, context.Config.IsCaseSensitive, out var aggregateResult))
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

            return MethodInvoker.InvokeCall(variableValue, args, context, null, ct);
        }

        var callee = ResolveIdentifier(rightIdentifier, context);
        if (!IsPipelineCallable(callee))
        {
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.PipeGreater),
                TypeNameFormatter.Of(leftValue),
                TypeNameFormatter.Of(callee));
        }

        return MethodInvoker.InvokeCall(callee, args, context, null, ct);
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

    private static bool IsPipelineCallable(object? value) => value is
        LambdaValue or
        CompiledLambdaValue or
        FunctionRef or
        Delegate or
        ModuleMethodRef or
        StaticMethodRef or
        MethodRef;
}
