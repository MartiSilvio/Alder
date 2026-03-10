using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compiled.Compilation.CompilerUnits;

internal sealed partial class DirectEmitCompilerUnit
{
    internal LinqExpression? TryEmitDirectIndexAccess(IndexAccessExpr expr)
    {
        if (!_ctx.Options.Sandbox.AllowPropertyRead)
            return null;

        var (targetExpr, targetType) = CompileTyped(expr.Object);
        if (targetType == typeof(object) &&
            expr.Object is IdentifierExpr identifier &&
            !_ctx.Context.Functions.ContainsKey(identifier.Name.Lexeme) &&
            !_ctx.Context.Modules.ContainsKey(identifier.Name.Lexeme) &&
            _ctx.Context.TryGetVariableType(identifier.Name.Lexeme, out var metadataType) &&
            metadataType != null &&
            metadataType != typeof(object))
        {
            targetType = metadataType;
            var typedVariableGetter = CompilerReflectionCache.GetVariableTypedMethodFor(targetType);
            targetExpr = LinqExpression.Call(
                typedVariableGetter,
                LinqExpression.Constant(identifier.Name.Lexeme),
                _ctx.CurrentContext);
        }
        var (indexExpr, _) = CompileTyped(expr.Index);

        if (targetType == typeof(object))
            return null;

        if (targetType == typeof(string))
        {
            var stringTarget = EnsureTypedExpression(targetExpr, typeof(string));
            return EmitDirectIndexAccessCore(
                expr,
                stringTarget,
                typeof(string),
                t => LinqExpression.Property(t, nameof(string.Length)),
                (t, i) => LinqExpression.Property(t, "Chars", i),
                typeof(char));
        }

        if (targetType.IsArray && targetType.GetArrayRank() == 1)
        {
            var elementType = targetType.GetElementType();
            if (elementType == null)
                return null;

            var arrayTarget = EnsureTypedExpression(targetExpr, targetType);
            return EmitDirectIndexAccessCore(
                expr,
                arrayTarget,
                targetType,
                LinqExpression.ArrayLength,
                LinqExpression.ArrayIndex,
                elementType);
        }

        var listInterface = GetGenericIListInterface(targetType);
        if (listInterface == null)
            return null;

        var itemType = listInterface.GetGenericArguments()[0];
        if (!TryGetListIndexProperties(listInterface, targetType, out var itemProperty, out var countProperty))
            return null;

        var typedTarget = EnsureTypedExpression(targetExpr, listInterface);
        return EmitDirectIndexAccessCore(
            expr,
            typedTarget,
            listInterface,
            t => LinqExpression.Property(t, countProperty),
            (t, i) => LinqExpression.Property(t, itemProperty, i),
            itemType,
            allowFastConstantIndex: true);

        LinqExpression EmitDirectIndexAccessCore(
            IndexAccessExpr indexExprNode,
            LinqExpression rawTarget,
            Type rawTargetType,
            Func<LinqExpression, LinqExpression> countFactory,
            Func<LinqExpression, LinqExpression, LinqExpression> accessFactory,
            Type resultType,
            bool allowFastConstantIndex = false)
        {
            var indexValue = indexExpr.Type == typeof(int)
                ? indexExpr
                : LinqExpression.Call(
                    ConvertToInt32ObjectMethod,
                    EnsureObjectExpression(indexExpr));
            var fastConstantIndex = default(int);
            var hasFastConstantIndex = allowFastConstantIndex &&
                TryGetDirectFastConstantIndex(indexExprNode.Index, out fastConstantIndex);

            if (indexExprNode.NullSafe)
            {
                var targetVar = LinqExpression.Variable(typeof(object), "target");
                var typedTarget = LinqExpression.Convert(targetVar, rawTargetType);
                var targetAccess = hasFastConstantIndex
                    ? accessFactory(typedTarget, LinqExpression.Constant(fastConstantIndex))
                    : BuildNormalizedIndexAccess(indexValue, countFactory, accessFactory, typedTarget);
                return LinqExpression.Block(
                    typeof(object),
                    [targetVar],
                    LinqExpression.Assign(targetVar, EnsureObjectExpression(rawTarget)),
                    LinqExpression.Condition(
                        LinqExpression.Equal(targetVar, LinqExpression.Constant(null, typeof(object))),
                        LinqExpression.Constant(null, typeof(object)),
                        LinqExpression.Convert(WrapIndexResult(targetAccess, resultType), typeof(object))));
            }

            var nonNullAccess = hasFastConstantIndex
                ? accessFactory(rawTarget, LinqExpression.Constant(fastConstantIndex))
                : BuildNormalizedIndexAccess(indexValue, countFactory, accessFactory, rawTarget);
            return LinqExpression.Convert(WrapIndexResult(nonNullAccess, resultType), typeof(object));

            LinqExpression BuildNormalizedIndexAccess(
                LinqExpression resolvedIndex,
                Func<LinqExpression, LinqExpression> countAccessor,
                Func<LinqExpression, LinqExpression, LinqExpression> elementAccessor,
                LinqExpression target)
            {
                var indexVar = LinqExpression.Variable(typeof(int), "index");
                var normalizedIndex = LinqExpression.Call(
                    NormalizeIndexMethod,
                    resolvedIndex,
                    countAccessor(target),
                    LinqExpression.Constant(_ctx.Options.LanguageMode));
                return LinqExpression.Block(
                    resultType,
                    [indexVar],
                    LinqExpression.Assign(indexVar, normalizedIndex),
                    elementAccessor(target, indexVar));
            }
        }
    }

    private static Type? GetGenericIListInterface(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
            return type;

        return type
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));
    }

    private static bool TryGetListIndexProperties(
        Type listInterface,
        Type targetType,
        out PropertyInfo itemProperty,
        out PropertyInfo countProperty)
    {
        itemProperty = null!;
        countProperty = null!;

        var itemType = listInterface.GetGenericArguments()[0];
        var resolvedItemProperty = listInterface.GetProperty("Item", [typeof(int)])
            ?? targetType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance, null, itemType, [typeof(int)], null);
        if (resolvedItemProperty == null)
            return false;
        itemProperty = resolvedItemProperty;

        var genericCollection = typeof(ICollection<>).MakeGenericType(itemType);
        var resolvedCountProperty = genericCollection.GetProperty("Count")
            ?? listInterface.GetProperty("Count")
            ?? targetType.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
        if (resolvedCountProperty == null)
            return false;
        countProperty = resolvedCountProperty;
        return true;
    }

    private static LinqExpression WrapIndexResult(LinqExpression value, Type valueType)
        => WrapGuardedValue(value, valueType, DirectIndexAccessGuardContext);

    private LinqExpression? TryEmitDirectChain(Expr terminal)
    {
        if (!TryFlattenMemberCallChain(terminal, out var root, out var segments))
            return null;
        if (segments.Count == 0)
            return null;

        var (currentExpr, currentType) = CompileTyped(root);
        if (currentType == typeof(object))
            return null;
        if (root is TypeReferenceExpr or IdentifierExpr && currentType == typeof(Type))
            return null;

        var current = EnsureTypedExpression(currentExpr, currentType);

        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            if (segment is ChainSegment.Index index)
            {
                if (index.NullSafe)
                    return null;
                if (!_ctx.Options.Sandbox.AllowPropertyRead)
                    return null;

                if (!TryEmitDirectIndexCore(current, currentType, index.IndexExpr, out var indexed, out var indexedType))
                    return null;

                currentType = indexedType;
                current = indexed;
                continue;
            }

            if (segment is ChainSegment.Member member)
            {
                if (member.NullSafe)
                    return null;
                if (!_ctx.Options.Sandbox.AllowPropertyRead)
                    return null;

                var flags = BindingFlags.Public | BindingFlags.Instance;
                if (!_ctx.Options.IsCaseSensitive)
                    flags |= BindingFlags.IgnoreCase;

                var prop = _ctx.Context.TypeCache.GetProperty(currentType, member.Name, flags);
                if (prop != null)
                {
                    var next = LinqExpression.Property(current, prop);
                    currentType = prop.PropertyType;
                    current = WrapGuardedValue(next, currentType, CreateMemberGuardContext(member.Name));
                    continue;
                }

                var field = _ctx.Context.TypeCache.GetField(currentType, member.Name, flags);
                if (field != null)
                {
                    var next = LinqExpression.Field(current, field);
                    currentType = field.FieldType;
                    current = WrapGuardedValue(next, currentType, CreateMemberGuardContext(member.Name));
                    continue;
                }

                return null;
            }

            if (segment is ChainSegment.Call call)
            {
                if (call.NullSafe)
                    return null;
                if (!_ctx.Options.Sandbox.AllowMethodCalls)
                    return null;
                if (call.TypeArguments.Count > 0 || call.Arguments.Any(a => a is NamedArgumentExpr or OutArgExpr))
                    return null;

                var argTypes = new Type[call.Arguments.Count];
                for (var a = 0; a < call.Arguments.Count; a++)
                {
                    argTypes[a] = _ctx.TypeInferrer.Infer(call.Arguments[a]);
                    if (argTypes[a] == typeof(object) || argTypes[a].IsArray)
                        return null;
                }

                var flags = BindingFlags.Public | BindingFlags.Instance;
                if (!_ctx.Options.IsCaseSensitive)
                    flags |= BindingFlags.IgnoreCase;

                var method = MethodResolver.TryResolveMethod(currentType, call.MethodName, argTypes, flags);
                if (method == null)
                    return null;

                var parameters = method.GetParameters();
                var typedArgs = new LinqExpression[call.Arguments.Count];
                for (var a = 0; a < call.Arguments.Count; a++)
                {
                    var (compiledArg, _) = CompileTyped(call.Arguments[a]);
                    if (compiledArg.Type == parameters[a].ParameterType)
                    {
                        typedArgs[a] = compiledArg;
                    }
                    else if (compiledArg.Type == typeof(object))
                    {
                        var coerced = LinqExpression.Call(
                            CompilerReflectionCache.CoerceNumericMethod,
                            compiledArg,
                            LinqExpression.Constant(parameters[a].ParameterType, typeof(Type)));
                        typedArgs[a] = LinqExpression.Convert(coerced, parameters[a].ParameterType);
                    }
                    else
                    {
                        typedArgs[a] = LinqExpression.Convert(compiledArg, parameters[a].ParameterType);
                    }
                }

                if (method.ReturnType == typeof(void))
                    return i == segments.Count - 1
                        ? LinqExpression.Block(LinqExpression.Call(current, method, typedArgs), LinqExpression.Constant(null, typeof(object)))
                        : null;

                var callExpr = LinqExpression.Call(current, method, typedArgs);
                currentType = method.ReturnType;
                current = WrapGuardedValue(callExpr, currentType, CreateMethodGuardContext(method.Name));
                continue;
            }

            return null;
        }

        return LinqExpression.Convert(current, typeof(object));
    }

    private static bool TryFlattenMemberCallChain(
        Expr terminal,
        out Expr root,
        out List<ChainSegment> segments)
    {
        segments = [];
        var current = terminal;

        while (true)
        {
            switch (current)
            {
                case MemberAccessExpr member:
                    segments.Add(new ChainSegment.Member(member.Name.Lexeme, member.NullSafe));
                    current = member.Object;
                    continue;
                case CallExpr call when call.Callee is MemberAccessExpr calleeMember:
                    segments.Add(new ChainSegment.Call(
                        calleeMember.Name.Lexeme,
                        call.Arguments,
                        call.TypeArguments ?? [],
                        calleeMember.NullSafe));
                    current = calleeMember.Object;
                    continue;
                case IndexAccessExpr index:
                    segments.Add(new ChainSegment.Index(index.Index, index.NullSafe));
                    current = index.Object;
                    continue;
                default:
                    root = current;
                    segments.Reverse();
                    return true;
            }
        }
    }

    private abstract record ChainSegment
    {
        internal sealed record Member(string Name, bool NullSafe) : ChainSegment;
        internal sealed record Call(string MethodName, List<Expr> Arguments, IReadOnlyList<string> TypeArguments, bool NullSafe) : ChainSegment;
        internal sealed record Index(Expr IndexExpr, bool NullSafe) : ChainSegment;
    }

    private bool TryEmitDirectIndexCore(
        LinqExpression targetExpr,
        Type targetType,
        Expr indexAst,
        out LinqExpression indexedExpression,
        out Type indexedType)
    {
        indexedExpression = null!;
        indexedType = typeof(object);

        if (targetType == typeof(object))
            return false;

        var (indexExpr, _) = CompileTyped(indexAst);
        var indexValue = indexExpr.Type == typeof(int)
            ? indexExpr
            : LinqExpression.Call(
                ConvertToInt32ObjectMethod,
                EnsureObjectExpression(indexExpr));

        if (targetType == typeof(string))
        {
            var stringTarget = EnsureTypedExpression(targetExpr, typeof(string));
            indexedType = typeof(char);
            indexedExpression = BuildIndexAccessExpression(
                stringTarget,
                t => LinqExpression.Property(t, nameof(string.Length)),
                (t, i) => LinqExpression.Property(t, "Chars", i),
                indexedType,
                indexValue,
                indexAst,
                allowFastConstantIndex: false);
            return true;
        }

        if (targetType.IsArray && targetType.GetArrayRank() == 1)
        {
            var elementType = targetType.GetElementType();
            if (elementType == null)
                return false;

            var arrayTarget = EnsureTypedExpression(targetExpr, targetType);
            indexedType = elementType;
            indexedExpression = BuildIndexAccessExpression(
                arrayTarget,
                LinqExpression.ArrayLength,
                LinqExpression.ArrayIndex,
                indexedType,
                indexValue,
                indexAst,
                allowFastConstantIndex: false);
            return true;
        }

        var listInterface = GetGenericIListInterface(targetType);
        if (listInterface == null)
            return false;

        var itemType = listInterface.GetGenericArguments()[0];
        if (!TryGetListIndexProperties(listInterface, targetType, out var itemProperty, out var countProperty))
            return false;

        var typedTarget = EnsureTypedExpression(targetExpr, listInterface);
        indexedType = itemType;
        indexedExpression = BuildIndexAccessExpression(
            typedTarget,
            t => LinqExpression.Property(t, countProperty),
            (t, i) => LinqExpression.Property(t, itemProperty, i),
            indexedType,
            indexValue,
            indexAst,
            allowFastConstantIndex: true);
        return true;
    }

    private LinqExpression BuildIndexAccessExpression(
        LinqExpression targetExpr,
        Func<LinqExpression, LinqExpression> countFactory,
        Func<LinqExpression, LinqExpression, LinqExpression> accessFactory,
        Type resultType,
        LinqExpression indexValue,
        Expr indexAst,
        bool allowFastConstantIndex)
    {
        if (allowFastConstantIndex &&
            TryGetDirectFastConstantIndex(indexAst, out var fastConstantIndex))
        {
            return WrapIndexResult(
                accessFactory(targetExpr, LinqExpression.Constant(fastConstantIndex)),
                resultType);
        }

        var indexVar = LinqExpression.Variable(typeof(int), "index");
        var normalizedIndex = LinqExpression.Call(
            NormalizeIndexMethod,
            indexValue,
            countFactory(targetExpr),
            LinqExpression.Constant(_ctx.Options.LanguageMode));

        return LinqExpression.Block(
            resultType,
            [indexVar],
            LinqExpression.Assign(indexVar, normalizedIndex),
            WrapIndexResult(accessFactory(targetExpr, indexVar), resultType));
    }

    private bool TryGetDirectFastConstantIndex(Expr indexAst, out int constantIndex)
    {
        constantIndex = default;
        if (_ctx.Options.LanguageMode != LanguageMode.Standard)
            return false;

        return indexAst is LiteralExpr { Value: int i } && i >= 0 &&
            (constantIndex = i) >= 0;
    }
}
