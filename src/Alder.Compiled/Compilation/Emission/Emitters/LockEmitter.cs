using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.LockStatement)]
internal static class LockEmitter
{
    public static LinqExpression Emit(BoundLockStatementExpr node, EmissionContext ctx)
    {
        var lockObjVar = LinqExpression.Variable(typeof(object), "lockObj");
        var resultVar = LinqExpression.Variable(typeof(object), "lockResult");

        return LinqExpression.Block(
            typeof(object),
            [lockObjVar, resultVar],
            LinqExpression.Assign(
                lockObjVar,
                LinqExpression.Call(ValidateLockObjectMethod, ctx.EmitBoxed(node.LockObject))),
            LinqExpression.Call(MonitorEnterMethod, lockObjVar),
            LinqExpression.TryFinally(
                LinqExpression.Assign(resultVar, ctx.EmitBoxed(node.Body)),
                LinqExpression.Call(MonitorExitMethod, lockObjVar)),
            resultVar);
    }
}
