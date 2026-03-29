using System.Linq.Expressions;
using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation;

internal sealed partial class BoundExpressionEmitter
{
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
}
