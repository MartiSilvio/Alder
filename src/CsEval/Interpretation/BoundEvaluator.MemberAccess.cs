using CsEval.Binding;
using CsEval.Binding.BoundNodes;
using CsEval.Diagnostics;
using CsEval.Runtime;
using CsEval.Runtime.Extensions;
using CsEval.Runtime.Semantics;
using System.Collections.Immutable;
using System.Reflection;

namespace CsEval.Interpretation;

internal sealed partial class BoundEvaluator
{
    private object? EvaluateMemberAccess(BoundMemberAccessExpr memberAccess)
    {
        var target = Evaluate(memberAccess.Target);
        if (memberAccess.NullSafe && target == null)
            return null;
        return MemberAccess.GetMember(
            target,
            memberAccess.MemberName,
            _options,
            nullSafe: memberAccess.NullSafe,
            _context);
    }

    private object? EvaluateIndexAccess(BoundIndexAccessExpr indexAccess)
    {
        var target = Evaluate(indexAccess.Target);
        if (indexAccess.NullSafe && target == null)
            return null;

        if (target == null)
            throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        var index = Evaluate(indexAccess.Index);
        return MemberAccess.GetIndex(target, index, _options, _context);
    }

    private object? EvaluateMultiDimIndexAccess(BoundMultiDimIndexAccessExpr multiDimIndexAccess)
    {
        var target = Evaluate(multiDimIndexAccess.Target);
        if (multiDimIndexAccess.NullSafe && target == null)
            return null;

        var indices = new int[multiDimIndexAccess.Indices.Length];
        for (var i = 0; i < multiDimIndexAccess.Indices.Length; i++)
            indices[i] = Convert.ToInt32(Evaluate(multiDimIndexAccess.Indices[i]));

        if (target is Array array)
            return array.GetValue(indices);

        if (target != null)
        {
            var indexer = _context.TypeMetadata
                .GetProperties(target.GetType(), BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.GetIndexParameters().Length == multiDimIndexAccess.Indices.Length);
            if (indexer != null)
            {
                var indexParams = indexer.GetIndexParameters();
                var convertedIndices = new object[indices.Length];
                for (var i = 0; i < indices.Length; i++)
                    convertedIndices[i] = Convert.ChangeType(indices[i], indexParams[i].ParameterType);
                return indexer.GetValue(target, convertedIndices);
            }
        }

        throw new CsEvalException(
            DiagnosticDescriptors.BadIndexerAccess,
            TypeNameFormatter.Of(target));
    }

    private object? EvaluateMultiDimIndexAssign(BoundMultiDimIndexAssignExpr multiDimIndexAssign)
    {
        var target = Evaluate(multiDimIndexAssign.Target);
        var indices = new int[multiDimIndexAssign.Indices.Length];
        for (var i = 0; i < multiDimIndexAssign.Indices.Length; i++)
            indices[i] = Convert.ToInt32(Evaluate(multiDimIndexAssign.Indices[i]));
        var value = Evaluate(multiDimIndexAssign.Value);

        if (target is Array array)
        {
            array.SetValue(value, indices);
            return value;
        }

        if (target != null)
        {
            var indexer = _context.TypeMetadata
                .GetProperties(target.GetType(), BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.GetIndexParameters().Length == multiDimIndexAssign.Indices.Length && p.CanWrite);
            if (indexer != null)
            {
                var indexParams = indexer.GetIndexParameters();
                var convertedIndices = new object[indices.Length];
                for (var i = 0; i < indices.Length; i++)
                    convertedIndices[i] = Convert.ChangeType(indices[i], indexParams[i].ParameterType);
                indexer.SetValue(target, value, convertedIndices);
                return value;
            }
        }

        throw new CsEvalException(
            DiagnosticDescriptors.BadIndexerAccess,
            TypeNameFormatter.Of(target));
    }

    private object? EvaluateNamedArgument(BoundNamedArgumentExpr namedArgument)
    {
        return new NamedArg(namedArgument.Name, Evaluate(namedArgument.Value));
    }

    private static object? EvaluateOutArg(BoundOutArgExpr outArg)
    {
        return new OutArgMarker(outArg.VariableName, outArg.TypeName, outArg.IsDiscard);
    }

    private object? EvaluateCall(BoundCallExpr call)
    {
        if (call.Callee is BoundMemberAccessExpr { Plan: not null } memberAccess)
        {
            var target = memberAccess.Plan.IsStatic ? null : Evaluate(memberAccess.Target);
            if (memberAccess.NullSafe && target == null)
                return null;

            if (!call.Plan.IsModuleCall)
            {
                ExecutionRuntime.EnsureMethodCallsAllowed(
                    _options,
                    call.Plan.SelectedMethod.Name,
                    call.Plan.IsStaticCall ? call.Plan.SelectedMethod.DeclaringType : null);
            }

            var plannedArgs = EvaluateArguments(call.Arguments);

            var plannedResult = CsEval.Runtime.MethodInvoker.InvokePlannedMethod(
                call.Plan,
                target,
                plannedArgs,
                _cancellationToken);
            if (plannedResult.Success)
                return plannedResult.Value;

            var result = CsEval.Runtime.MethodInvoker.InvokeMethodWithArgs(
                call.Plan.SelectedMethod,
                target,
                plannedArgs,
                _cancellationToken);
            if (result.Success)
                return result.Value;
        }

        var (args, outBindings) = EvaluateArgumentsWithOutBindings(call.Arguments);

        var callee = Evaluate(call.Callee);
        var invokeResult = CsEval.Runtime.MethodInvoker.InvokeCall(callee, args, _context, _options, _cancellationToken);
        DefineOutVariablesIfAny(args, outBindings);
        return invokeResult;
    }

    private object? EvaluateInvoke(BoundInvokeExpr invoke)
    {

        var (args, outBindings) = EvaluateArgumentsWithOutBindings(invoke.Arguments);

        IReadOnlyList<string>? typeArguments = invoke.TypeArguments.IsDefaultOrEmpty
            ? null
            : invoke.TypeArguments;

        if (invoke.Callee is BoundIdentifierExpr identifier)
        {
            var result = IdentifierRuntime.InvokeIdentifierCall(
                identifier.Name,
                args,
                _context,
                _options,
                _cancellationToken,
                typeArguments);
            DefineOutVariablesIfAny(args, outBindings);
            return result;
        }

        if (invoke.Callee is BoundMemberAccessExpr memberAccess)
        {
            var target = Evaluate(memberAccess.Target);
            var result = CsEval.Runtime.MethodInvoker.InvokeMemberCall(
                target,
                memberAccess.MemberName,
                args,
                memberAccess.NullSafe,
                _context,
                _options,
                _cancellationToken,
                typeArguments);
            DefineOutVariablesIfAny(args, outBindings);
            return result;
        }

        var callee = Evaluate(invoke.Callee);
        var invokeCallResult = CsEval.Runtime.MethodInvoker.InvokeCall(
            callee,
            args,
            _context,
            _options,
            _cancellationToken,
            typeArguments);
        DefineOutVariablesIfAny(args, outBindings);
        return invokeCallResult;
    }

    private object?[] EvaluateArguments(ImmutableArray<BoundExpr> arguments)
    {
        var argumentCount = arguments.Length;
        var values = new object?[argumentCount];

        for (var i = 0; i < argumentCount; i++)
            values[i] = Evaluate(arguments[i]);

        return values;
    }

    private (object?[] Values, OutVariableBinding[] OutBindings) EvaluateArgumentsWithOutBindings(ImmutableArray<BoundExpr> arguments)
    {
        var argumentCount = arguments.Length;
        var values = new object?[argumentCount];
        List<OutVariableBinding>? bindings = null;

        for (var i = 0; i < argumentCount; i++)
        {
            var argument = arguments[i];
            values[i] = Evaluate(argument);
            if (argument is BoundOutArgExpr { IsDiscard: false } outArg)
            {
                bindings ??= [];
                bindings.Add(new OutVariableBinding(i, outArg.VariableName, outArg.TypeName));
            }
        }

        return (values, bindings?.ToArray() ?? []);
    }

    private void DefineOutVariablesIfAny(object?[] args, OutVariableBinding[] outBindings)
    {
        if (outBindings.Length > 0)
            IdentifierRuntime.DefineOutVariables(args, outBindings, _context);
    }

    private object? EvaluateLambda(BoundLambdaExpr lambda)
    {
        return new LambdaValue(lambda.Parameters.ToList(), lambda.Body, _context, _options);
    }

    private object? EvaluatePipeline(BoundPipelineExpr pipeline)
    {
        var left = Evaluate(pipeline.Left);

        if (pipeline.Right is BoundIdentifierExpr rightIdentifier)
        {
            return IdentifierRuntime.InvokePipelineIdentifier(
                left,
                rightIdentifier.Name,
                _context,
                _options,
                _cancellationToken);
        }

        var right = Evaluate(pipeline.Right);
        return PipelineOperator.InvokePipeline(left, right, _context, _options, _cancellationToken);
    }
}
