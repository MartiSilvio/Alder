using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.TypedLambda)]
internal static class TypedLambdaEvaluator
{
    public static object? Evaluate(BoundTypedLambdaExpr node, EvaluationContext ctx)
    {
        var parameters = node.Parameters;
        var body = node.Body;
        var config = ctx.Config;
        var closure = ctx.Context;

        return new CompiledLambdaValue(
            parameters.Select(static p => p.Name).ToList(),
            (args, closureCtx) =>
            {
                var childContext = closureCtx.CreateChild();
                for (var i = 0; i < parameters.Length && i < args.Length; i++)
                    childContext.Define(parameters[i].Name, args[i], parameters[i].Type);

                var evaluator = new BoundEvaluator(childContext, config);
                var result = evaluator.Evaluate(body);
                return result is ControlFlowSignal signal ? signal.Value : result;
            },
            closure,
            parameters.Length == 0 ? closureCtx =>
            {
                var evaluator = new BoundEvaluator(closureCtx, config);
                var result = evaluator.Evaluate(body);
                return result is ControlFlowSignal signal ? signal.Value : result;
            } : null,
            parameters.Length == 1 ? (arg0, closureCtx) =>
            {
                var childContext = closureCtx.CreateChild();
                childContext.Define(parameters[0].Name, arg0, parameters[0].Type);
                var evaluator = new BoundEvaluator(childContext, config);
                var result = evaluator.Evaluate(body);
                return result is ControlFlowSignal signal ? signal.Value : result;
            } : null,
            parameters.Length == 2 ? (arg0, arg1, closureCtx) =>
            {
                var childContext = closureCtx.CreateChild();
                childContext.Define(parameters[0].Name, arg0, parameters[0].Type);
                childContext.Define(parameters[1].Name, arg1, parameters[1].Type);
                var evaluator = new BoundEvaluator(childContext, config);
                var result = evaluator.Evaluate(body);
                return result is ControlFlowSignal signal ? signal.Value : result;
            } : null);
    }
}
