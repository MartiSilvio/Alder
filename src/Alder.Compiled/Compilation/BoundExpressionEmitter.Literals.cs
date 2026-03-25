using System.Linq.Expressions;
using Alder.Binding.BoundNodes;
using Alder.Binding.Plans;
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
            ResolveTypeByName(objectCreation.TypeName),
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
                    value));
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

    private LinqExpression EmitTypedArrayCreation(BoundTypedArrayCreationExpr typedArrayCreation)
    {
        if (typedArrayCreation.StaticType.ClrType.IsArray)
        {
            var elementType = typedArrayCreation.StaticType.ClrType.GetElementType()!;
            var sizeExpr = EmitHelpers.EnsureTypedExpression(Emit(typedArrayCreation.Size), typeof(int));
            return LinqExpression.Call(
                typeof(Alder.Runtime.RuntimeArrayFactory).GetMethod(nameof(Alder.Runtime.RuntimeArrayFactory.Create),
                    new[] { typeof(Type), typeof(int) })!,
                LinqExpression.Constant(elementType, typeof(Type)),
                sizeExpr);
        }

        return LinqExpression.Call(
            CreateTypedArrayFromTypeNameMethod,
            ResolveTypeByName(typedArrayCreation.ElementTypeName),
            EmitHelpers.AsObject(Emit(typedArrayCreation.Size)));
    }

    private LinqExpression EmitTypedArrayLiteral(BoundTypedArrayLiteralExpr typedArrayLiteral)
    {
        if (typedArrayLiteral.StaticType.ClrType.IsArray)
        {
            var elementType = typedArrayLiteral.StaticType.ClrType.GetElementType()!;
            var elements = typedArrayLiteral.Elements.Select(
                element => EmitHelpers.EnsureTypedExpression(Emit(element), elementType));
            return EmitHelpers.AsObject(LinqExpression.NewArrayInit(elementType, elements));
        }

        var sourceArray = LinqExpression.NewArrayInit(
            typeof(object),
            typedArrayLiteral.Elements.Select(element => EmitHelpers.AsObject(Emit(element))));
        return LinqExpression.Call(
            ConvertArrayToTypedMethod,
            sourceArray,
            ResolveTypeByName(typedArrayLiteral.ElementTypeName));
    }

    private LinqExpression EmitTuple(BoundTupleExpr tuple)
    {
        var hasNames = tuple.ElementNames.Any(static n => n != null);
        if (!hasNames && IsValueTupleType(tuple.StaticType.ClrType) && tuple.Elements.Length <= 7)
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

    private LinqExpression EmitMultiDimTypedArrayCreation(BoundMultiDimTypedArrayCreationExpr multiDimTypedArrayCreation)
    {
        if (multiDimTypedArrayCreation.StaticType.ClrType.IsArray)
        {
            var elementType = multiDimTypedArrayCreation.StaticType.ClrType.GetElementType()!;
            var sizeExprs = multiDimTypedArrayCreation.Sizes.Select(
                size => EmitHelpers.EnsureTypedExpression(Emit(size), typeof(int)));
            return EmitHelpers.AsObject(LinqExpression.NewArrayBounds(elementType, sizeExprs));
        }

        var sizes = LinqExpression.NewArrayInit(
            typeof(object),
            multiDimTypedArrayCreation.Sizes.Select(size => EmitHelpers.AsObject(Emit(size))));
        return LinqExpression.Call(
            CreateMultiDimArrayMethod,
            ResolveTypeByName(multiDimTypedArrayCreation.ElementTypeName),
            sizes);
    }

    private LinqExpression EmitMultiDimArrayInit(BoundMultiDimArrayInitExpr init)
    {
        var elementType = ResolveTypeByName(init.ElementTypeName);
        var dimensions = LinqExpression.NewArrayInit(
            typeof(int),
            init.InferredDimensions.Select(d => LinqExpression.Constant(d)));
        var flatValues = LinqExpression.NewArrayInit(
            typeof(object),
            init.FlatValues.Select(v => EmitHelpers.AsObject(Emit(v))));

        return EmitHelpers.AsObject(LinqExpression.Call(
            typeof(RuntimeArrayFactory).GetMethod(nameof(RuntimeArrayFactory.CreateAndFill))!,
            elementType,
            dimensions,
            flatValues));
    }

    private LinqExpression EmitMultiDimIndexAccess(BoundMultiDimIndexAccessExpr multiDimIndexAccess)
    {
        var targetType = multiDimIndexAccess.Target.StaticType.ClrType;
        if (targetType.IsArray && targetType.GetArrayRank() > 1)
        {
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
        var exceptionExpr = Emit(throwExpr.Expression);
        var validated = LinqExpression.Call(ValidateThrowOperandMethod, EmitHelpers.AsObject(exceptionExpr));
        var resultType = throwExpr.StaticType.ClrType != typeof(object) ? throwExpr.StaticType.ClrType : typeof(object);
        return LinqExpression.Block(
            resultType,
            LinqExpression.Throw(validated, resultType),
            LinqExpression.Default(resultType));
    }
}