using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation;

internal sealed partial class BoundExpressionEmitter
{
    private LinqExpression EmitObjectCreation(BoundObjectCreationExpr objectCreation)
    {
        if (objectCreation.StaticType.ClrType != typeof(object) && !objectCreation.StaticType.ClrType.IsAbstract &&
            !objectCreation.StaticType.ClrType.IsInterface && objectCreation.InitializerEntries.IsDefaultOrEmpty)
        {
            var pure = TryEmitPureObjectCreation(objectCreation);
            if (pure != null) return pure;
        }

        var argsArray = LinqExpression.NewArrayInit(
            typeof(object),
            objectCreation.Arguments.Select(arg => EmitHelpers.AsObject(Emit(arg))));
        var result = LinqExpression.Call(
            InvokeConstructorMethod,
            objectCreation.StaticType is BoundUnknownType
                ? ResolveTypeByName(objectCreation.TypeName)
                : LinqExpression.Constant(objectCreation.StaticType.ClrType, typeof(Type)),
            argsArray,
            _configParam);

        if (objectCreation.InitializerEntries.IsDefaultOrEmpty)
            return result;

        var objVar = LinqExpression.Variable(typeof(object), "initObj");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(objVar, result)
        };

        for (var i = 0; i < objectCreation.InitializerEntries.Length; i++)
        {
            var entry = objectCreation.InitializerEntries[i];
            var value = EmitHelpers.AsObject(Emit(entry.Value));
            if (entry.PropertyName != null)
            {
                statements.Add(LinqExpression.Call(
                    ApplyPropertyInitializerMethod,
                    objVar,
                    LinqExpression.Constant(entry.PropertyName),
                    value,
                    _configParam,
                    _contextParam));
            }
            else if (entry.IndexerKey != null)
            {
                var key = EmitHelpers.AsObject(Emit(entry.IndexerKey));
                statements.Add(LinqExpression.Call(
                    ApplyIndexerInitializerMethod,
                    objVar,
                    key,
                    value,
                    _configParam,
                    _contextParam));
            }
            else
            {
                statements.Add(LinqExpression.Call(
                    ApplyCollectionInitializerMethod,
                    objVar,
                    value,
                    _configParam,
                    _contextParam));
            }
        }

        statements.Add(objVar);
        return LinqExpression.Block(typeof(object), [objVar], statements);
    }

    private LinqExpression? TryEmitPureObjectCreation(BoundObjectCreationExpr objectCreation)
    {
        var type = objectCreation.StaticType.ClrType;

        if (objectCreation.Arguments.Length == 0)
        {
            var defaultCtor = type.GetConstructor(Type.EmptyTypes);
            if (defaultCtor != null)
                return LinqExpression.Convert(LinqExpression.New(defaultCtor), typeof(object));
            if (type.IsValueType)
                return LinqExpression.Convert(LinqExpression.New(type), typeof(object));
        }

        var argTypes = new Type[objectCreation.Arguments.Length];
        for (var i = 0; i < objectCreation.Arguments.Length; i++)
        {
            var argType = objectCreation.Arguments[i].StaticType.ClrType;
            if (argType == typeof(object))
                return null;
            argTypes[i] = argType;
        }

        var ctor = type.GetConstructor(argTypes);
        if (ctor == null)
            return null;

        var ctorParams = ctor.GetParameters();
        var args = new LinqExpression[objectCreation.Arguments.Length];
        for (var i = 0; i < args.Length; i++)
            args[i] = EmitHelpers.EnsureTypedExpression(Emit(objectCreation.Arguments[i]), ctorParams[i].ParameterType);

        return LinqExpression.Convert(LinqExpression.New(ctor, args), typeof(object));
    }

    private LinqExpression EmitArrayAllocation(BoundArrayAllocationExpr allocation)
    {
        if (allocation.Sizes.Length == 1)
        {
            var sizeExpr = EmitHelpers.EnsureTypedExpression(Emit(allocation.Sizes[0]), typeof(int));
            var maxLengthExpr = LinqExpression.Property(
                LinqExpression.Property(_configParam, nameof(AlderConfig.Security)),
                nameof(Security.SecurityPolicy.MaxCollectionSize));
            return LinqExpression.Call(
                typeof(RuntimeArrayFactory).GetMethod(nameof(RuntimeArrayFactory.Create),
                    [typeof(Type), typeof(int), typeof(int)])!,
                LinqExpression.Constant(allocation.ElementType, typeof(Type)),
                sizeExpr,
                maxLengthExpr);
        }

        var sizeExprs = allocation.Sizes.Select(
            size => EmitHelpers.EnsureTypedExpression(Emit(size), typeof(int)));
        return EmitHelpers.AsObject(LinqExpression.NewArrayBounds(allocation.ElementType, sizeExprs));
    }

    private LinqExpression EmitTuple(BoundTupleExpr tuple)
    {
        var hasNames = tuple.ElementNames.Any(static n => n != null);
        if (!hasNames && TypeHelpers.IsValueTupleType(tuple.StaticType.ClrType) && tuple.Elements.Length <= 7)
        {
            var pure = TryEmitPureTuple(tuple);
            if (pure != null) return pure;
        }

        var elements = LinqExpression.NewArrayInit(
            typeof(object),
            tuple.Elements.Select(element => EmitHelpers.AsObject(Emit(element))));

        if (hasNames)
        {
            var names = LinqExpression.NewArrayInit(
                typeof(string),
                tuple.ElementNames.Select(static n =>
                    n != null
                        ? LinqExpression.Constant(n, typeof(string))
                        : LinqExpression.Constant(null, typeof(string))));
            return LinqExpression.Call(CreateNamedTupleMethod, elements, names);
        }

        return LinqExpression.Call(CreateTupleMethod, elements);
    }

    private LinqExpression? TryEmitPureTuple(BoundTupleExpr tuple)
    {
        var tupleType = tuple.StaticType.ClrType;
        var elementTypes = tupleType.GetGenericArguments();
        var ctor = tupleType.GetConstructor(elementTypes);
        if (ctor == null)
            return null;

        var args = new LinqExpression[tuple.Elements.Length];
        for (var i = 0; i < args.Length; i++)
            args[i] = EmitHelpers.EnsureTypedExpression(Emit(tuple.Elements[i]), elementTypes[i]);

        return LinqExpression.Convert(LinqExpression.New(ctor, args), typeof(object));
    }

    private LinqExpression EmitDeconstruction(BoundDeconstructionExpr deconstruction)
    {
        var variableNames = LinqExpression.NewArrayInit(
            typeof(string),
            deconstruction.VariableNames.Select(static name => LinqExpression.Constant(name)));
        return LinqExpression.Call(
            DeconstructTupleMethod,
            EmitHelpers.AsObject(Emit(deconstruction.ValueExpression)),
            variableNames,
            _contextParam);
    }

    private LinqExpression EmitMultiDimArrayInit(BoundMultiDimArrayInitExpr init)
    {
        var dimensions = LinqExpression.NewArrayInit(
            typeof(int),
            init.InferredDimensions.Select(d => LinqExpression.Constant(d)));
        var flatValues = LinqExpression.NewArrayInit(
            typeof(object),
            init.FlatValues.Select(v => EmitHelpers.AsObject(Emit(v))));

        return EmitHelpers.AsObject(LinqExpression.Call(
            typeof(RuntimeArrayFactory).GetMethod(nameof(RuntimeArrayFactory.CreateAndFill))!,
            LinqExpression.Constant(init.ElementType, typeof(Type)),
            dimensions,
            flatValues));
    }

    private LinqExpression EmitDynamicMultiDimIndexAccess(BoundDynamicMultiDimIndexAccessExpr multiDimIndexAccess)
    {
        var target = EmitHelpers.AsObject(Emit(multiDimIndexAccess.Target));
        var indices = LinqExpression.NewArrayInit(
            typeof(object),
            multiDimIndexAccess.Indices.Select(index => EmitHelpers.AsObject(Emit(index))));

        if (!multiDimIndexAccess.NullSafe)
            return LinqExpression.Call(MultiDimArrayGetMethod, target, indices);

        var mdTargetVar = LinqExpression.Variable(typeof(object), "mdTarget");
        return LinqExpression.Block(
            typeof(object),
            [mdTargetVar],
            LinqExpression.Assign(mdTargetVar, target),
            LinqExpression.Condition(
                LinqExpression.Equal(mdTargetVar, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Constant(null, typeof(object)),
                LinqExpression.Call(MultiDimArrayGetMethod, mdTargetVar, indices)));
    }

    private LinqExpression EmitMultiDimIndexAccess(BoundResolvedMultiDimIndexAccessExpr multiDimIndexAccess)
    {
        if (multiDimIndexAccess.IsArray)
        {
            var targetType = multiDimIndexAccess.TargetType;
            var getMethod = targetType.GetMethod("Get");
            if (getMethod != null)
            {
                var intIndices = multiDimIndexAccess.Indices.Select(
                    index => EmitHelpers.EnsureTypedExpression(Emit(index), typeof(int))).ToArray();

                if (!multiDimIndexAccess.NullSafe)
                {
                    var typedTarget = EmitHelpers.EnsureTypedExpression(
                        Emit(multiDimIndexAccess.Target), targetType);
                    return EmitHelpers.AsObject(LinqExpression.Call(typedTarget, getMethod, intIndices));
                }

                var targetVar = LinqExpression.Variable(typeof(object), "mdTarget");
                return LinqExpression.Block(
                    typeof(object),
                    [targetVar],
                    LinqExpression.Assign(targetVar, EmitHelpers.AsObject(Emit(multiDimIndexAccess.Target))),
                    LinqExpression.Condition(
                        LinqExpression.Equal(targetVar, LinqExpression.Constant(null, typeof(object))),
                        LinqExpression.Constant(null, typeof(object)),
                        EmitHelpers.AsObject(
                            LinqExpression.Call(
                                EmitHelpers.EnsureTypedExpression(targetVar, targetType),
                                getMethod,
                                intIndices))));
            }
        }

        if (multiDimIndexAccess.Indexer is { } indexer)
        {
            var indexParams = indexer.GetIndexParameters();
            var emittedIndices = new LinqExpression[multiDimIndexAccess.Indices.Length];
            for (var i = 0; i < multiDimIndexAccess.Indices.Length; i++)
            {
                var paramType = indexParams[i].ParameterType;
                emittedIndices[i] = EmitHelpers.EnsureTypedExpression(Emit(multiDimIndexAccess.Indices[i]), paramType);
            }

            var typedTarget = EmitHelpers.EnsureTypedExpression(Emit(multiDimIndexAccess.Target), multiDimIndexAccess.TargetType);
            return EmitHelpers.AsObject(LinqExpression.Property(typedTarget, indexer, emittedIndices));
        }

        var target = EmitHelpers.AsObject(Emit(multiDimIndexAccess.Target));
        var indices = LinqExpression.NewArrayInit(
            typeof(object),
            multiDimIndexAccess.Indices.Select(index => EmitHelpers.AsObject(Emit(index))));

        if (!multiDimIndexAccess.NullSafe)
            return LinqExpression.Call(MultiDimArrayGetMethod, target, indices);

        var mdTargetVar = LinqExpression.Variable(typeof(object), "mdTarget");
        return LinqExpression.Block(
            typeof(object),
            [mdTargetVar],
            LinqExpression.Assign(mdTargetVar, target),
            LinqExpression.Condition(
                LinqExpression.Equal(mdTargetVar, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Constant(null, typeof(object)),
                LinqExpression.Call(MultiDimArrayGetMethod, mdTargetVar, indices)));
    }

    private LinqExpression EmitMultiDimIndexAssign(BoundMultiDimIndexAssignExpr multiDimIndexAssign)
    {
        if (multiDimIndexAssign.IsArray && multiDimIndexAssign.TargetType != null)
        {
            var targetType = multiDimIndexAssign.TargetType;
            var setMethod = targetType.GetMethod("Set");
            if (setMethod != null)
            {
                var intIndices = multiDimIndexAssign.Indices.Select(
                    index => EmitHelpers.EnsureTypedExpression(Emit(index), typeof(int))).ToArray();
                var valueExpr = Emit(multiDimIndexAssign.Value);
                var elementType = targetType.GetElementType()!;
                var typedValue = EmitHelpers.EnsureTypedExpression(valueExpr, elementType);
                var typedTarget = EmitHelpers.EnsureTypedExpression(Emit(multiDimIndexAssign.Target), targetType);
                var args = intIndices.Append(typedValue).ToArray();
                return LinqExpression.Block(
                    typeof(object),
                    LinqExpression.Call(typedTarget, setMethod, args),
                    EmitHelpers.AsObject(typedValue));
            }
        }

        if (multiDimIndexAssign.Indexer is { CanWrite: true } indexer)
        {
            var indexParams = indexer.GetIndexParameters();
            var emittedIndices = new LinqExpression[multiDimIndexAssign.Indices.Length];
            for (var i = 0; i < multiDimIndexAssign.Indices.Length; i++)
            {
                var paramType = indexParams[i].ParameterType;
                emittedIndices[i] = EmitHelpers.EnsureTypedExpression(Emit(multiDimIndexAssign.Indices[i]), paramType);
            }

            var typedTarget = EmitHelpers.EnsureTypedExpression(Emit(multiDimIndexAssign.Target), multiDimIndexAssign.TargetType!);
            var valueExpr = Emit(multiDimIndexAssign.Value);
            var typedValue = EmitHelpers.EnsureTypedExpression(valueExpr, indexer.PropertyType);
            return LinqExpression.Block(
                typeof(object),
                LinqExpression.Assign(
                    LinqExpression.Property(typedTarget, indexer, emittedIndices),
                    typedValue),
                EmitHelpers.AsObject(typedValue));
        }

        var indices = LinqExpression.NewArrayInit(
            typeof(object),
            multiDimIndexAssign.Indices.Select(index => EmitHelpers.AsObject(Emit(index))));
        return LinqExpression.Call(
            MultiDimArraySetMethod,
            EmitHelpers.AsObject(Emit(multiDimIndexAssign.Target)),
            indices,
            EmitHelpers.AsObject(Emit(multiDimIndexAssign.Value)));
    }

    private LinqExpression EmitThrow(BoundThrowExpr throwExpr)
    {
        var resultType = throwExpr.StaticType.ClrType != typeof(object) ? throwExpr.StaticType.ClrType : typeof(object);

        if (throwExpr.Expression != null)
        {
            var exceptionExpr = Emit(throwExpr.Expression);
            var validated = LinqExpression.Call(ValidateThrowOperandMethod, EmitHelpers.AsObject(exceptionExpr));
            return LinqExpression.Block(
                resultType,
                LinqExpression.Throw(validated, resultType),
                LinqExpression.Default(resultType));
        }

        if (_catchDepth == 0)
        {
            return LinqExpression.Throw(
                LinqExpression.Constant(new AlderException(Alder.Diagnostics.DiagnosticDescriptors.ThrowOutsideCatch)),
                resultType);
        }

        return LinqExpression.Rethrow(resultType);
    }
}