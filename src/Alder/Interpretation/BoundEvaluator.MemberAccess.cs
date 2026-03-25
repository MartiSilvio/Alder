using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Binding.Plans;
using Alder.Diagnostics;
using Alder.Runtime;
using Alder.Runtime.Extensions;
using Alder.Runtime.Semantics;
using MethodInvoker = Alder.Runtime.MethodInvoker;

namespace Alder.Interpretation;

internal sealed partial class BoundEvaluator
{
    private object? EvaluateMemberAccess(BoundMemberAccessExpr memberAccess)
    {
        var chain = PostfixChain.TryCollect(memberAccess);
        if (chain != null)
            return EvaluatePostfixChain(chain.Value);

        var target = Evaluate(memberAccess.Target);
        if (memberAccess.NullSafe && target == null)
            return null;
        return ResolveMemberWithPlan(target, memberAccess.Plan, memberAccess.MemberName, memberAccess.NullSafe);
    }

    private object? ResolveMemberWithPlan(object? target, BoundMemberPlan? plan, string memberName, bool nullSafe)
    {
        if (plan == null)
            return MemberAccess.GetMember(target, memberName, _config, nullSafe, _context);

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "property", memberName);

        if (plan.IsMethodGroup)
        {
            return plan.IsStatic
                ? new StaticMethodRef(plan.DeclaringType, memberName)
                : new MethodRef(target, memberName);
        }

        if (plan.Member is PropertyInfo property)
        {
            if (plan.IsStatic)
                return TypeHelpers.GuardReflectionLeak(property.GetValue(null), $"static property {memberName}");

            if (plan.DeclaringType.IsAssignableFrom(target.GetType()))
                return TypeHelpers.GuardReflectionLeak(
                    _context.TypeMetadata.GetPropertyValue(property, target), $"property {memberName}");

            return MemberAccess.GetMember(target, memberName, _config, nullSafe, _context);
        }

        if (plan.Member is FieldInfo field)
        {
            if (plan.IsStatic)
                return TypeHelpers.GuardReflectionLeak(field.GetValue(null), $"static field {memberName}");

            if (plan.DeclaringType.IsAssignableFrom(target.GetType()))
                return TypeHelpers.GuardReflectionLeak(field.GetValue(target), $"field {memberName}");

            return MemberAccess.GetMember(target, memberName, _config, nullSafe, _context);
        }

        return MemberAccess.GetMember(target, memberName, _config, nullSafe, _context);
    }

    private object? EvaluateIndexAccess(BoundIndexAccessExpr indexAccess)
    {
        var target = Evaluate(indexAccess.Target);
        if (indexAccess.NullSafe && target == null)
            return null;

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        var index = Evaluate(indexAccess.Index);

        if (indexAccess.Plan is { IsDirectCollectionAccess: true })
        {
            switch (target)
            {
                case string s when index != null:
                {
                    var i = MemberAccess.NormalizeIndex(Convert.ToInt32(index), s.Length, _config.LanguageMode);
                    return (object)s[i];
                }
                case IList list when index != null:
                {
                    var i = MemberAccess.NormalizeIndex(Convert.ToInt32(index), list.Count, _config.LanguageMode);
                    return TypeHelpers.GuardReflectionLeak(list[i], $"index [{i}]");
                }
                case IDictionary<string, object?> dict when index is string key:
                    return dict.TryGetValue(key, out var value)
                        ? TypeHelpers.GuardReflectionLeak(value, $"index [{key}]")
                        : null;
            }
        }

        return MemberAccess.GetIndex(target, index, _config, _context);
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

        throw new AlderException(
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

        throw new AlderException(
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
        var chain = PostfixChain.TryCollect(call);
        if (chain != null)
            return EvaluatePostfixChain(chain.Value);

        return EvaluateCallDirect(call, null);
    }

    private object? EvaluateCallDirect(BoundCallExpr call, object? evaluatedTarget)
    {
        if (call.Callee is BoundMemberAccessExpr { Plan: not null } memberAccess)
        {
            var target = evaluatedTarget ?? (memberAccess.Plan.IsStatic ? null : Evaluate(memberAccess.Target));
            if (memberAccess.NullSafe && target == null)
                return null;

            var plannedArgs = EvaluateArguments(call.Arguments);

            var resolved = call.Plan.Resolution;
            var parameters = Runtime.MethodDispatchCache.GetParameters(resolved.Method);
            var prepared = Runtime.ArgumentPreparer.Prepare(resolved, plannedArgs, parameters, _cancellationToken);
            var plannedResult = MethodInvoker.InvokeMethodCore(resolved.Method, target, prepared);
            Runtime.ArgumentPreparer.CopyBackOutArgs(plannedArgs, prepared, parameters);
            return plannedResult;
        }

        var (args, outBindings) = EvaluateArgumentsWithOutBindings(call.Arguments);

        var callee = Evaluate(call.Callee);
        var invokeResult = MethodInvoker.InvokeCall(callee, args, _context, _config, ct: _cancellationToken);
        DefineOutVariablesIfAny(args, outBindings);
        return invokeResult;
    }

    private object? EvaluateInvoke(BoundInvokeExpr invoke)
    {
        var chain = PostfixChain.TryCollect(invoke);
        if (chain != null)
            return EvaluatePostfixChain(chain.Value);

        return EvaluateInvokeDirect(invoke, null);
    }

    private object? EvaluateInvokeDirect(BoundInvokeExpr invoke, object? evaluatedTarget)
    {
        var (args, outBindings) = EvaluateArgumentsWithOutBindings(invoke.Arguments);

        IReadOnlyList<string>? typeArguments = invoke.TypeArguments.IsDefaultOrEmpty
            ? null
            : invoke.TypeArguments;

        if (invoke.Callee is BoundIdentifierExpr identifier)
        {
            var result = IdentifierRuntime.InvokeIdentifierCall(
                identifier.Name, args, _context, _config, typeArguments, _cancellationToken);
            DefineOutVariablesIfAny(args, outBindings);
            return result;
        }

        if (invoke.Callee is BoundMemberAccessExpr memberAccess)
        {
            var target = evaluatedTarget ?? Evaluate(memberAccess.Target);
            var result = MethodInvoker.InvokeMemberCall(
                target, memberAccess.MemberName, args, memberAccess.NullSafe,
                _context, _config, typeArguments, _cancellationToken);
            DefineOutVariablesIfAny(args, outBindings);
            return result;
        }

        var callee = Evaluate(invoke.Callee);
        var invokeCallResult = MethodInvoker.InvokeCall(
            callee, args, _context, _config, typeArguments, _cancellationToken);
        DefineOutVariablesIfAny(args, outBindings);
        return invokeCallResult;
    }

    private object? EvaluatePostfixChain(PostfixChain.Chain chain)
    {
        var result = Evaluate(chain.Root);

        for (var i = chain.Segments.Count - 1; i >= 0; i--)
        {
            var seg = chain.Segments[i];

            if (seg.CallOrInvoke is BoundCallExpr call)
                result = EvaluateCallDirect(call, result);
            else if (seg.CallOrInvoke is BoundInvokeExpr invoke)
                result = EvaluateInvokeDirect(invoke, result);
            else
            {
                var ma = seg.MemberAccess;
                if (ma.NullSafe && result == null) return null;
                result = ResolveMemberWithPlan(result, ma.Plan, ma.MemberName, ma.NullSafe);
            }
        }

        return result;
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
        return new LambdaValue(lambda.Parameters.ToList(), lambda.Body, _context, _config);
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
                _config,
                _cancellationToken);
        }

        var right = Evaluate(pipeline.Right);
        return PipelineOperator.InvokePipeline(left, right, _context, _config, _cancellationToken);
    }
}
