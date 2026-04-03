using Alder.Binding;
using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class ObjectCreationEmitter : INodeEmitter<BoundObjectCreationExpr>
{
    public LinqExpression Emit(BoundObjectCreationExpr node, EmissionContext ctx)
    {
        if (node.StaticType.ClrType != typeof(object) && node.StaticType.ClrType is { IsAbstract: false, IsInterface: false } && node.InitializerEntries.IsDefaultOrEmpty)
        {
            var pure = TryEmitPure(node, ctx);
            if (pure != null) return pure;
        }

        var argsArray = LinqExpression.NewArrayInit(
            typeof(object),
            node.Arguments.Select(arg => ctx.EmitBoxed(arg)));
        var result = LinqExpression.Call(
            InvokeConstructorMethod,
            node.StaticType is BoundUnknownType
                ? LinqExpression.Call(
                    LinqExpression.Call(ctx.ContextParam, GetTypeResolverProperty),
                    ResolveTypeMethod,
                    LinqExpression.Constant(node.TypeName))
                : LinqExpression.Constant(node.StaticType.ClrType, typeof(Type)),
            argsArray,
            ctx.ContextParam);

        if (node.InitializerEntries.IsDefaultOrEmpty)
            return result;

        var objVar = LinqExpression.Variable(typeof(object), "initObj");
        var statements = new List<LinqExpression> { LinqExpression.Assign(objVar, result) };

        foreach (var entry in node.InitializerEntries)
        {
            if (entry.PropertyName != null)
            {
                var value = ctx.EmitBoxed(entry.Value!);
                statements.Add(LinqExpression.Call(ApplyPropertyInitializerMethod,
                    objVar, LinqExpression.Constant(entry.PropertyName), value, ctx.ContextParam));
            }
            else if (entry.IndexerKey != null)
            {
                var value = ctx.EmitBoxed(entry.Value!);
                var key = ctx.EmitBoxed(entry.IndexerKey);
                statements.Add(LinqExpression.Call(ApplyIndexerInitializerMethod,
                    objVar, key, value, ctx.ContextParam));
            }
            else if (!entry.Elements.IsDefaultOrEmpty)
            {
                var elemArray = LinqExpression.NewArrayInit(
                    typeof(object),
                    entry.Elements.Select(e => ctx.EmitBoxed(e)));
                statements.Add(LinqExpression.Call(ApplyGroupedCollectionInitializerMethod,
                    objVar, elemArray, ctx.ContextParam));
            }
            else
            {
                var value = ctx.EmitBoxed(entry.Value!);
                statements.Add(LinqExpression.Call(ApplyCollectionInitializerMethod,
                    objVar, value, ctx.ContextParam));
            }
        }

        statements.Add(objVar);
        return LinqExpression.Block(typeof(object), [objVar], statements);
    }

    private static LinqExpression? TryEmitPure(BoundObjectCreationExpr node, EmissionContext ctx)
    {
        var type = node.StaticType.ClrType;

        if (node.Arguments.Length == 0)
        {
            var defaultCtor = type.GetConstructor(Type.EmptyTypes);
            if (defaultCtor != null)
                return LinqExpression.New(defaultCtor);
            if (type.IsValueType)
                return LinqExpression.New(type);
        }

        var argTypes = new Type[node.Arguments.Length];
        for (var i = 0; i < node.Arguments.Length; i++)
        {
            var argType = node.Arguments[i].StaticType.ClrType;
            if (argType == typeof(object))
                return null;
            argTypes[i] = argType;
        }

        var ctor = type.GetConstructor(argTypes);
        if (ctor == null)
            return null;

        var ctorParams = ctor.GetParameters();
        var args = new LinqExpression[node.Arguments.Length];
        for (var i = 0; i < args.Length; i++)
            args[i] = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Arguments[i]), ctorParams[i].ParameterType);

        return LinqExpression.New(ctor, args);
    }
}
