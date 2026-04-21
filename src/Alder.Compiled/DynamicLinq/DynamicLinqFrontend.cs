using System.Linq.Expressions;
using Alder.Binding;
using Alder.Compiled.Compilation;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Compiled.DynamicLinq;

internal static class DynamicLinqFrontend
{
    internal static LambdaExpression ParsePredicate(
        AlderEngine engine,
        Type itType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values,
        string? itName)
    {
        var parameter = CreateItParameter(itType, itName);
        return ParseLambdaCore(engine, [parameter], typeof(bool), expression, values, enableImplicitReceiver: true);
    }

    internal static LambdaExpression ParseProjection(
        AlderEngine engine,
        Type itType,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values,
        string? itName)
    {
        var parameter = CreateItParameter(itType, itName);
        return ParseLambdaCore(engine, [parameter], resultType, expression, values, enableImplicitReceiver: true);
    }

    internal static LambdaExpression ParseLambda(
        AlderEngine engine,
        IReadOnlyList<ParameterExpression> parameters,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values)
    {
        var enableImplicitReceiver = parameters.Count == 1;
        return ParseLambdaCore(engine, parameters, resultType, expression, values, enableImplicitReceiver);
    }

    private static LambdaExpression ParseLambdaCore(
        AlderEngine engine,
        IReadOnlyList<ParameterExpression> parameters,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values,
        bool enableImplicitReceiver)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(expression);

        var access = engine.GetCompiledFeatureAccess();
        access.ThrowIfDisposed();
        try
        {
            var lexer = new Lexer(expression);
            var tokens = lexer.Tokenize();
            var parser = ExpressionParser.CreateForSubExpression(tokens, LanguageMode.Standard);
            var ast = parser.Parse();

            var providedParameters = parameters.ToArray();
            var providedNames = new string[providedParameters.Length];
            var providedTypes = new Type[providedParameters.Length];
            for (var i = 0; i < providedParameters.Length; i++)
            {
                var name = providedParameters[i].Name;
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("All provided parameters must have names.", nameof(parameters));

                providedNames[i] = name!;
                providedTypes[i] = providedParameters[i].Type;
            }

            var parameterScope = new Dictionary<string, ParameterExpression>(StringComparer.Ordinal);
            var (bodyAst, bindingNames, bindingTypes, scopeNames) =
                ResolveBindingShape(ast, providedNames, providedTypes);

            for (var i = 0; i < providedParameters.Length; i++)
                parameterScope[scopeNames[i]] = providedParameters[i];

            var engineVariables = access.CollectEngineVariables();
            if (values != null)
                foreach (var pair in values)
                    engineVariables[pair.Key] = pair.Value;

            var config = access.Config;
            var bindingRuntimeContext = new AlderContext(config);
            foreach (var pair in engineVariables)
                bindingRuntimeContext.Define(pair.Key, pair.Value, pair.Value?.GetType() ?? typeof(object));

            for (var i = 0; i < bindingNames.Length; i++)
                bindingRuntimeContext.Define(bindingNames[i], null, bindingTypes[i]);

            BindingReceiver? receiver = null;
            if (enableImplicitReceiver && bindingNames.Length == 1)
                receiver = new BindingReceiver(bindingTypes[0], bindingNames[0], EnableImplicitReceiver: true);

            var binder = new Alder.Binding.Binder();
            var boundBody = binder.Bind(bodyAst, new BindingContext(bindingRuntimeContext, receiver));
            var emitter = new ExpressionTreeEmitter(parameterScope, engineVariables, config.TypeResolver);
            var body = emitter.Emit(boundBody);
            body = CoerceResult(body, resultType);

            return Expression.Lambda(body, providedParameters);
        }
        catch (InsufficientExecutionStackException ex)
        {
            throw new AlderException(DiagnosticDescriptors.ExpressionNestingDepthExceeded, ex);
        }
    }

    private static (Expr Body, string[] BindingNames, Type[] BindingTypes, string[] ScopeNames) ResolveBindingShape(
        Expr ast,
        string[] providedNames,
        Type[] providedTypes)
    {
        if (ast is not LambdaExpr lambdaExpr)
            return (ast, providedNames, providedTypes, providedNames);

        if (lambdaExpr.Parameters.Count != providedNames.Length)
        {
            throw new AlderException(
                DiagnosticDescriptors.CantConvAnonMethParams,
                $"Func<{string.Join(", ", providedTypes.Select(static t => t.Name))}>");
        }

        var bindingNames = new string[providedNames.Length];
        var bindingTypes = new Type[providedNames.Length];
        for (var i = 0; i < providedNames.Length; i++)
        {
            bindingNames[i] = lambdaExpr.Parameters[i].Name.Lexeme;
            bindingTypes[i] = providedTypes[i];
        }

        return (lambdaExpr.Body, bindingNames, bindingTypes, bindingNames);
    }

    private static Expression CoerceResult(Expression body, Type? resultType)
    {
        if (resultType == null || body.Type == resultType)
            return body;

        try
        {
            return Expression.Convert(body, resultType);
        }
        catch (InvalidOperationException ex)
        {
            throw new AlderException(
                DiagnosticDescriptors.CantConvAnonMethReturnType,
                ex,
                resultType.Name);
        }
    }

    private static ParameterExpression CreateItParameter(Type itType, string? itName)
        => Expression.Parameter(itType, string.IsNullOrWhiteSpace(itName) ? "it" : itName);
}
