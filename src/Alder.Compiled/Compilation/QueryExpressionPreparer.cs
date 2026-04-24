using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compiled.DynamicLinq;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;
using Alder.Runtime.Collections;

namespace Alder.Compiled.Compilation;

internal static class QueryExpressionPreparer
{
    internal static PreparedQueryLambda PrepareParseAsExpression(
        AlderEngine engine,
        string expression,
        Type delegateType,
        Dictionary<string, object?>? additionalVariables)
    {
        var genericArgs = delegateType.GetGenericArguments();
        var paramTypes = genericArgs[..^1];
        var providedParameters = CreateParameterBindings(paramTypes);

        return PrepareCore(
            engine,
            expression,
            providedParameters,
            additionalVariables,
            enableImplicitReceiver: false,
            allowBodyOnly: paramTypes.Length == 0,
            parameterCountDisplay: delegateType.Name,
            lambdaRequiredExample: "x => x > 5",
            unwrapBodyOnlyReturn: true);
    }

    internal static DynamicQueryPlan PrepareDynamicQueryLambda(
        AlderEngine engine,
        IReadOnlyList<ParameterExpression> parameters,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values,
        bool enableImplicitReceiver,
        DynamicQueryLambdaKind expectedKind)
    {
        var providedParameters = new QueryParameterBinding[parameters.Count];
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var name = parameter.Name;
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("All provided parameters must have names.", nameof(parameters));

            providedParameters[i] = new QueryParameterBinding(name!, parameter.Type, parameter);
        }

        var valueMap = values == null
            ? null
            : values.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);

        return PrepareCore(
            engine,
            expression,
            providedParameters,
            valueMap,
            enableImplicitReceiver,
            allowBodyOnly: true,
            parameterCountDisplay: $"Func<{string.Join(", ", parameters.Select(static p => p.Type.Name))}>",
            lambdaRequiredExample: null,
            unwrapBodyOnlyReturn: false,
            expectedKind);
    }

    private static PreparedQueryLambda PrepareCore(
        AlderEngine engine,
        string expression,
        IReadOnlyList<QueryParameterBinding> providedParameters,
        IReadOnlyDictionary<string, object?>? additionalValues,
        bool enableImplicitReceiver,
        bool allowBodyOnly,
        string parameterCountDisplay,
        string? lambdaRequiredExample,
        bool unwrapBodyOnlyReturn)
    {
        var prepared = PrepareCore(
            engine,
            expression,
            providedParameters,
            additionalValues,
            enableImplicitReceiver,
            allowBodyOnly,
            parameterCountDisplay,
            lambdaRequiredExample,
            unwrapBodyOnlyReturn,
            InferKind(providedParameters.Count, expression));

        return new PreparedQueryLambda(
            prepared.BoundBody,
            [.. prepared.Parameters],
            new Dictionary<string, object?>(prepared.CapturedVariables, StringComparer.Ordinal));
    }

    private static DynamicQueryPlan PrepareCore(
        AlderEngine engine,
        string expression,
        IReadOnlyList<QueryParameterBinding> providedParameters,
        IReadOnlyDictionary<string, object?>? additionalValues,
        bool enableImplicitReceiver,
        bool allowBodyOnly,
        string parameterCountDisplay,
        string? lambdaRequiredExample,
        bool unwrapBodyOnlyReturn,
        DynamicQueryLambdaKind expectedKind)
    {
        var access = engine.GetCompiledFeatureAccess();
        access.ThrowIfDisposed();

        var lexer = new Lexer(expression);
        var tokens = lexer.Tokenize();
        var parser = ExpressionParser.CreateForSubExpression(tokens, LanguageMode.Standard);
        var ast = parser.Parse();

        Expr bodyAst;
        ParameterExpression[] lambdaParameters;

        if (ast is LambdaExpr lambdaExpr)
        {
            if (lambdaExpr.Parameters.Count != providedParameters.Count)
                throw new AlderException(DiagnosticDescriptors.CantConvAnonMethParams, parameterCountDisplay);

            lambdaParameters = CreateLambdaParametersFromLambda(providedParameters, lambdaExpr);

            bodyAst = lambdaExpr.Body;
        }
        else if (allowBodyOnly)
        {
            lambdaParameters = CreateBodyOnlyParameters(providedParameters);

            bodyAst = ast;
        }
        else
        {
            throw new AlderException(
                DiagnosticDescriptors.ParseAsExpressionRequiresLambda,
                lambdaRequiredExample ?? "x => x");
        }

        var engineVariables = access.CollectEngineVariables();
        if (additionalValues != null)
        {
            foreach (var (key, value) in additionalValues)
                engineVariables[key] = value;
        }

        var bindingContext = CreateQueryBindingRuntimeContext(access.Config);
        foreach (var (name, value) in engineVariables)
            bindingContext.Define(name, value, value?.GetType() ?? typeof(object));

        for (var i = 0; i < lambdaParameters.Length; i++)
            bindingContext.Define(lambdaParameters[i].Name!, null, lambdaParameters[i].Type);

        BindingReceiver? receiver = null;
        if (enableImplicitReceiver && lambdaParameters.Length == 1)
            receiver = new BindingReceiver(lambdaParameters[0].Type, lambdaParameters[0].Name!, EnableImplicitReceiver: true);

        var binder = new Binding.Binder();
        var boundBody = binder.Bind(bodyAst, new BindingContext(bindingContext, receiver));

        if (unwrapBodyOnlyReturn && ast is not LambdaExpr)
            boundBody = UnwrapReturnValue(boundBody);

        boundBody = access.RunCompilationPipeline(boundBody);
        var exportedLambda = Expression.Lambda(
            new QueryTreeExporter(lambdaParameters, engineVariables).Export(boundBody),
            lambdaParameters);
        return new DynamicQueryPlan(
            expectedKind,
            ClassifyResultShape(boundBody.StaticType.ClrType),
            boundBody.StaticType.ClrType,
            boundBody,
            lambdaParameters,
            engineVariables,
            exportedLambda);
    }

    private static DynamicQueryLambdaKind InferKind(int parameterCount, string expression)
    {
        if (parameterCount == 2)
            return DynamicQueryLambdaKind.BinarySelector;

        return expression.Contains("=>", StringComparison.Ordinal)
            ? DynamicQueryLambdaKind.Selector
            : DynamicQueryLambdaKind.Selector;
    }

    private static DynamicQueryResultShape ClassifyResultShape(Type resultType)
    {
        if (ImplementsGenericInterface(resultType, typeof(IGrouping<,>)))
            return DynamicQueryResultShape.Grouping;

        if (typeof(StructuralObjectValue).IsAssignableFrom(resultType))
            return DynamicQueryResultShape.StructuralObject;

        if (resultType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(resultType))
            return DynamicQueryResultShape.Collection;

        return DynamicQueryResultShape.Scalar;
    }

    private static bool ImplementsGenericInterface(Type type, Type genericInterface) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == genericInterface
        || type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterface);

    private static AlderContext CreateQueryBindingRuntimeContext(AlderConfig config)
    {
        if (!TryCreateEntityFrameworkHelperModule(config, out var efModule))
            return new AlderContext(config);

        var modules = new Dictionary<string, ModuleInfo>(config.Modules, config.Comparer)
        {
            ["EF"] = efModule
        };

        var queryConfig = new AlderConfig(
            config.LanguageMode,
            config.Security,
            config.IsCaseSensitive,
            config.Constraints,
            config.Compiler,
            config.ExpressionCompiler,
            config.ServiceProvider,
            config.Functions,
            FixedDictionary<string, ModuleInfo>.Create(modules),
            config.ExtensionTypes,
            config.TypeMetadata,
            config.TypeResolver,
            config.TypeDispatch,
            config.GenericStaticDispatch,
            config.DelegateFactories,
            config.ClosedDelegateTypes);

        return new AlderContext(queryConfig);
    }

    private static bool TryCreateEntityFrameworkHelperModule(
        AlderConfig config,
        out ModuleInfo module)
    {
        module = null!;

        if (config.Modules.ContainsKey("EF"))
            return false;

        var efAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(static assembly =>
                string.Equals(assembly.GetName().Name, "Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        var efType = efAssembly?.GetType("Microsoft.EntityFrameworkCore.EF", throwOnError: false, ignoreCase: false);
        if (efType == null)
            return false;

        module = new ModuleInfo(
            efType,
            instance: null,
            ModuleMemberMetadata.Build(efType, explicitOnly: false, config.Comparer));
        return true;
    }

    private static QueryParameterBinding[] CreateParameterBindings(IReadOnlyList<Type> parameterTypes)
    {
        var providedParameters = new QueryParameterBinding[parameterTypes.Count];
        for (var i = 0; i < parameterTypes.Count; i++)
            providedParameters[i] = new QueryParameterBinding($"arg{i}", parameterTypes[i], null);
        return providedParameters;
    }

    private static ParameterExpression[] CreateLambdaParametersFromLambda(
        IReadOnlyList<QueryParameterBinding> providedParameters,
        LambdaExpr lambdaExpr)
    {
        var parameters = new ParameterExpression[providedParameters.Count];
        for (var i = 0; i < providedParameters.Count; i++)
            parameters[i] = Expression.Parameter(providedParameters[i].Type, lambdaExpr.Parameters[i].Name.Lexeme);
        return parameters;
    }

    private static ParameterExpression[] CreateBodyOnlyParameters(IReadOnlyList<QueryParameterBinding> providedParameters)
    {
        var parameters = new ParameterExpression[providedParameters.Count];
        for (var i = 0; i < providedParameters.Count; i++)
            parameters[i] = providedParameters[i].ProvidedExpression
                ?? Expression.Parameter(providedParameters[i].Type, providedParameters[i].Name);
        return parameters;
    }

    private static BoundExpr UnwrapReturnValue(BoundExpr expr)
    {
        if (expr is BoundReturnExpr { Value: not null } ret)
            return ret.Value;
        if (expr is BoundBlockExpr block)
        {
            if (block.Statements.Length == 1 && block.Statements[0] is BoundReturnExpr { Value: not null } blockRet)
                return blockRet.Value;
            if (block.ReturnExpr != null)
                return block.ReturnExpr;
        }

        return expr;
    }

    private readonly record struct QueryParameterBinding(string Name, Type Type, ParameterExpression? ProvidedExpression);
}
