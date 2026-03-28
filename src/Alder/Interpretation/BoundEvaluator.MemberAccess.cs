using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;
using Alder.Runtime.Extensions;
using Alder.Runtime.Semantics;
using MethodInvoker = Alder.Runtime.MethodInvoker;

namespace Alder.Interpretation;

internal sealed partial class BoundEvaluator
{
    private object? EvaluatePropertyAccess(BoundPropertyAccessExpr node)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return EvaluatePostfixChain(chain.Value);

        if (node.IsStatic)
            return TypeHelpers.GuardReflectionLeak(node.Property.GetValue(null), $"static property {node.MemberName}");

        var target = Evaluate(node.Target);
        if (node.NullSafe && target == null) return null;
        return ResolvePropertyAccess(node, target);
    }

    private object? EvaluateFieldAccess(BoundFieldAccessExpr node)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return EvaluatePostfixChain(chain.Value);

        if (node.IsStatic)
            return TypeHelpers.GuardReflectionLeak(node.Field.GetValue(null), $"static field {node.MemberName}");

        var target = Evaluate(node.Target);
        if (node.NullSafe && target == null) return null;
        return ResolveFieldAccess(node, target);
    }

    private object? EvaluateMethodGroup(BoundMethodGroupExpr node)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return EvaluatePostfixChain(chain.Value);

        if (node.IsStatic) return new StaticMethodRef(node.DeclaringType, node.MethodName);
        var target = Evaluate(node.Target);
        if (node.NullSafe && target == null) return null;
        if (target == null)
            throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "method", node.MethodName);
        return new MethodRef(target, node.MethodName);
    }

    private object? EvaluateDynamicMemberAccess(BoundDynamicMemberAccessExpr node)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return EvaluatePostfixChain(chain.Value);

        var target = Evaluate(node.Target);
        if (node.NullSafe && target == null) return null;
        return MemberAccess.GetMember(target, node.MemberName, _config, node.NullSafe, _context);
    }

    private object? ResolveMemberAccessWithTarget(BoundMemberAccessBase ma, object? target)
    {
        return ma switch
        {
            BoundPropertyAccessExpr prop => ResolvePropertyAccess(prop, target),
            BoundFieldAccessExpr field => ResolveFieldAccess(field, target),
            BoundMethodGroupExpr mg => target == null
                ? throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "method", mg.MethodName)
                : new MethodRef(target, mg.MethodName),
            BoundDynamicMemberAccessExpr dyn => MemberAccess.GetMember(target, dyn.MemberName, _config, dyn.NullSafe, _context),
            _ => throw new BindingNotSupportedException($"Unexpected member access type '{ma.GetType().Name}'")
        };
    }

    private object? ResolvePropertyAccess(BoundPropertyAccessExpr node, object? target)
    {
        if (target == null)
            throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "property", node.MemberName);
        if (TypeHelpers.IsValueTupleType(node.Property.DeclaringType ?? node.Property.ReflectedType!))
            return MemberAccess.GetMember(target, node.MemberName, _config, node.NullSafe, _context);
        return TypeHelpers.GuardReflectionLeak(
            _context.TypeMetadata.GetPropertyValue(node.Property, target), $"property {node.MemberName}");
    }

    private object? ResolveFieldAccess(BoundFieldAccessExpr node, object? target)
    {
        if (target == null)
            throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "field", node.MemberName);
        if (TypeHelpers.IsValueTupleType(node.Field.DeclaringType ?? node.Field.ReflectedType!))
            return MemberAccess.GetMember(target, node.MemberName, _config, node.NullSafe, _context);
        return TypeHelpers.GuardReflectionLeak(node.Field.GetValue(target), $"field {node.MemberName}");
    }

    private object? EvaluateResolvedIndexAccess(BoundResolvedIndexAccessExpr indexAccess)
    {
        var target = Evaluate(indexAccess.Target);
        if (indexAccess.NullSafe && target == null)
            return null;

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        var index = Evaluate(indexAccess.Index);

        if (indexAccess.IsDirectCollectionAccess)
        {
            if (indexAccess.TargetType == typeof(string))
            {
                var s = (string)target;
                var i = MemberAccess.NormalizeIndex(Convert.ToInt32(index), s.Length, _config.LanguageMode);
                return (object)s[i];
            }

            if (typeof(IList).IsAssignableFrom(indexAccess.TargetType))
            {
                var list = (IList)target;
                var i = MemberAccess.NormalizeIndex(Convert.ToInt32(index), list.Count, _config.LanguageMode);
                return TypeHelpers.GuardReflectionLeak(list[i], $"index [{i}]");
            }

            if (typeof(IDictionary<string, object?>).IsAssignableFrom(indexAccess.TargetType))
            {
                var dict = (IDictionary<string, object?>)target;
                return index is string key && dict.TryGetValue(key, out var value)
                    ? TypeHelpers.GuardReflectionLeak(value, $"index [{key}]")
                    : null;
            }
        }

        return MemberAccess.GetIndex(target, index, _config, _context);
    }

    private object? EvaluateDynamicIndexAccess(BoundDynamicIndexAccessExpr indexAccess)
    {
        var target = Evaluate(indexAccess.Target);
        if (indexAccess.NullSafe && target == null)
            return null;

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        var index = Evaluate(indexAccess.Index);
        return MemberAccess.GetIndex(target, index, _config, _context);
    }

    private object? EvaluateResolvedMultiDimIndexAccess(BoundResolvedMultiDimIndexAccessExpr multiDimIndexAccess)
    {
        var target = Evaluate(multiDimIndexAccess.Target);
        if (multiDimIndexAccess.NullSafe && target == null)
            return null;

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        if (multiDimIndexAccess.IsArray)
        {
            var indices = EvaluateIntIndices(multiDimIndexAccess.Indices);
            return ((Array)target).GetValue(indices);
        }

        if (multiDimIndexAccess.Indexer is { } indexer)
        {
            var convertedIndices = EvaluateConvertedIndices(multiDimIndexAccess.Indices, indexer);
            return indexer.GetValue(target, convertedIndices);
        }

        throw new AlderException(
            DiagnosticDescriptors.BadIndexerAccess,
            TypeNameFormatter.Of(target));
    }

    private object? EvaluateDynamicMultiDimIndexAccess(BoundDynamicMultiDimIndexAccessExpr multiDimIndexAccess)
    {
        var target = Evaluate(multiDimIndexAccess.Target);
        if (multiDimIndexAccess.NullSafe && target == null)
            return null;

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        throw new AlderException(
            DiagnosticDescriptors.BadIndexerAccess,
            TypeNameFormatter.Of(target));
    }

    private object? EvaluateMultiDimIndexAssign(BoundMultiDimIndexAssignExpr multiDimIndexAssign)
    {
        var target = Evaluate(multiDimIndexAssign.Target);

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        var value = Evaluate(multiDimIndexAssign.Value);

        if (multiDimIndexAssign.IsArray)
        {
            var indices = EvaluateIntIndices(multiDimIndexAssign.Indices);
            ((Array)target).SetValue(value, indices);
            return value;
        }

        if (multiDimIndexAssign.Indexer is { CanWrite: true } indexer)
        {
            var convertedIndices = EvaluateConvertedIndices(multiDimIndexAssign.Indices, indexer);
            indexer.SetValue(target, value, convertedIndices);
            return value;
        }

        throw new AlderException(
            DiagnosticDescriptors.BadIndexerAccess,
            TypeNameFormatter.Of(target));
    }

    private int[] EvaluateIntIndices(ImmutableArray<BoundExpr> indexExprs)
    {
        var indices = new int[indexExprs.Length];
        for (var i = 0; i < indexExprs.Length; i++)
            indices[i] = Convert.ToInt32(Evaluate(indexExprs[i]));
        return indices;
    }

    private object[] EvaluateConvertedIndices(ImmutableArray<BoundExpr> indexExprs, PropertyInfo indexer)
    {
        var indexParams = indexer.GetIndexParameters();
        var converted = new object[indexExprs.Length];
        for (var i = 0; i < indexExprs.Length; i++)
        {
            var value = Evaluate(indexExprs[i]);
            converted[i] = Convert.ChangeType(value, indexParams[i].ParameterType);
        }
        return converted;
    }

    private object? EvaluateNamedArgument(BoundNamedArgumentExpr namedArgument)
    {
        return new NamedArg(namedArgument.Name, Evaluate(namedArgument.Value));
    }

    private static object? EvaluateOutArg(BoundOutArgExpr outArg)
    {
        return new OutArgMarker(outArg.VariableName, outArg.TypeName, outArg.IsDiscard);
    }

    private object? EvaluateResolvedCall(BoundResolvedCallExpr call)
    {
        var chain = PostfixChain.TryCollect(call);
        if (chain != null)
            return EvaluatePostfixChain(chain.Value);

        return EvaluateResolvedCallDirect(call, null);
    }

    private object? EvaluateResolvedCallDirect(BoundResolvedCallExpr call, object? evaluatedTarget)
    {
        if (call.Callee is BoundMethodGroupExpr methodGroup)
        {
            var target = evaluatedTarget ?? (methodGroup.IsStatic ? null : Evaluate(methodGroup.Target));
            if (methodGroup.NullSafe && target == null)
                return null;

            var plannedArgs = EvaluateArguments(call.Arguments);

            var resolved = call.Resolution;
            var parameters = Runtime.MethodDispatchCache.GetParameters(resolved.Method);
            var prepared = Runtime.ArgumentPreparer.Prepare(resolved, plannedArgs, parameters, _cancellationToken);
            var plannedResult = MethodInvoker.InvokeMethodCore(resolved.Method, target, prepared);
            Runtime.ArgumentPreparer.CopyBackOutArgs(plannedArgs, prepared, parameters);
            ExecutionRuntime.CheckCollectionSize(plannedResult, _config.Security);
            return plannedResult;
        }

        var (args, outBindings) = EvaluateArgumentsWithOutBindings(call.Arguments);

        var callee = Evaluate(call.Callee);
        var invokeResult = MethodInvoker.InvokeCall(callee, args, _context, _config, ct: _cancellationToken);
        DefineOutVariablesIfAny(args, outBindings);
        ExecutionRuntime.CheckCollectionSize(invokeResult, _config.Security);
        return invokeResult;
    }

    private object? EvaluateDynamicCall(BoundDynamicCallExpr invoke)
    {
        var chain = PostfixChain.TryCollect(invoke);
        if (chain != null)
            return EvaluatePostfixChain(chain.Value);

        return EvaluateDynamicCallDirect(invoke, null);
    }

    private object? EvaluateDynamicCallDirect(BoundDynamicCallExpr invoke, object? evaluatedTarget)
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
            ExecutionRuntime.CheckCollectionSize(result, _config.Security);
            return result;
        }

        if (invoke.Callee is BoundMemberAccessBase memberAccess)
        {
            var target = evaluatedTarget ?? Evaluate(memberAccess.Target);
            var result = MethodInvoker.InvokeMemberCall(
                target, memberAccess.MemberName, args, memberAccess.NullSafe,
                _context, _config, typeArguments, _cancellationToken);
            DefineOutVariablesIfAny(args, outBindings);
            ExecutionRuntime.CheckCollectionSize(result, _config.Security);
            return result;
        }

        var callee = Evaluate(invoke.Callee);
        var invokeCallResult = MethodInvoker.InvokeCall(
            callee, args, _context, _config, typeArguments, _cancellationToken);
        DefineOutVariablesIfAny(args, outBindings);
        ExecutionRuntime.CheckCollectionSize(invokeCallResult, _config.Security);
        return invokeCallResult;
    }

    private object? EvaluatePostfixChain(PostfixChain.Chain chain)
    {
        var result = Evaluate(chain.Root);

        for (var i = chain.Segments.Count - 1; i >= 0; i--)
        {
            var seg = chain.Segments[i];

            if (seg.CallOrInvoke is BoundResolvedCallExpr call)
                result = EvaluateResolvedCallDirect(call, result);
            else if (seg.CallOrInvoke is BoundDynamicCallExpr invoke)
                result = EvaluateDynamicCallDirect(invoke, result);
            else
            {
                var ma = seg.MemberAccess;
                if (ma.NullSafe && result == null) return null;
                result = ResolveMemberAccessWithTarget(ma, result);
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
