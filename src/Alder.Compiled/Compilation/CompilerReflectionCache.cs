using System.Collections.Concurrent;
using Alder.Parsing;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Compiled.Compilation;

internal static class CompilerReflectionCache
{

internal static readonly MethodInfo GetMethod = typeof(AlderContext).GetMethod("Get", [typeof(string)])!;
internal static readonly MethodInfo SetMethod = typeof(AlderContext).GetMethod("Set", [typeof(string), typeof(object)])!;
internal static readonly MethodInfo DefineMethod = typeof(AlderContext).GetMethod("Define", [typeof(string), typeof(object)])!;
internal static readonly MethodInfo DefineWithTypeMethod = typeof(AlderContext).GetMethod("Define", [typeof(string), typeof(object), typeof(Type)])!;
internal static readonly MethodInfo DefineNewMethod = typeof(AlderContext).GetMethod("DefineNew", [typeof(string), typeof(object), typeof(Type)])!;
internal static readonly MethodInfo TryGetVariableTypeMethod = typeof(AlderContext).GetMethod("TryGetVariableType")!;
internal static readonly MethodInfo CreateChildMethod = typeof(AlderContext).GetMethod("CreateChild")!;
internal static readonly MethodInfo RequireBooleanMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.RequireBoolean))!;
internal static readonly MethodInfo RequireBooleanForLogicalOperatorMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.RequireBooleanForLogicalOperator))!;
internal static readonly MethodInfo GetTypeResolverProperty = typeof(AlderContext).GetProperty(nameof(AlderContext.TypeResolver), BindingFlags.NonPublic | BindingFlags.Instance)!.GetGetMethod(true)!;
internal static readonly MethodInfo ResolveTypeMethod = typeof(TypeResolver).GetMethod(nameof(TypeResolver.ResolveType))!;
internal static readonly MethodInfo InvokeConstructorMethod = typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.InvokeConstructor), [typeof(Type), typeof(object?[]), typeof(AlderConfig)])!;
internal static readonly MethodInfo CreateTypedArrayFromTypeNameMethod = typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.CreateTypedArray))!;
internal static readonly MethodInfo ConvertArrayToTypedMethod = typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.ConvertArrayToTyped))!;
internal static readonly MethodInfo CreateTupleMethod = typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.CreateTuple))!;
internal static readonly MethodInfo CreateNamedTupleMethod = typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.CreateNamedTuple))!;
internal static readonly MethodInfo DeconstructTupleMethod = typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.DeconstructTuple))!;
internal static readonly MethodInfo GetDefaultValueMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.GetDefaultValue), [typeof(Type)])!;
internal static readonly MethodInfo IsNullableTypeMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.IsNullableType))!;
internal static readonly MethodInfo GetMemberMethod = typeof(MemberAccess).GetMethod(nameof(MemberAccess.GetMember))!;
internal static readonly MethodInfo GetIndexMethod = typeof(MemberAccess).GetMethod(
    nameof(MemberAccess.GetIndex),
    [typeof(object), typeof(object), typeof(AlderConfig), typeof(AlderContext)])!;
internal static readonly MethodInfo GetSliceMethod = typeof(MemberAccess).GetMethod(nameof(MemberAccess.GetSlice), [typeof(object), typeof(object), typeof(object)])!;
internal static readonly MethodInfo GetSliceStepMethod = typeof(MemberAccess).GetMethod(nameof(MemberAccess.GetSlice), [typeof(object), typeof(object), typeof(object), typeof(object)])!;
internal static readonly MethodInfo SetIndexMethod = typeof(MemberAccess).GetMethod(
    nameof(MemberAccess.SetIndex),
    [typeof(object), typeof(object), typeof(object), typeof(AlderConfig), typeof(AlderContext)])!;
internal static readonly MethodInfo SetMemberMethod = typeof(MemberAccess).GetMethod(nameof(MemberAccess.SetMember))!;
internal static readonly MethodInfo ListAddMethod = typeof(List<object?>).GetMethod(nameof(List<object?>.Add))!;
internal static readonly MethodInfo ListAddRangeMethod = typeof(List<object?>).GetMethod(nameof(List<object?>.AddRange))!;
internal static readonly ConstructorInfo ListCtor = typeof(List<object?>).GetConstructor(Type.EmptyTypes)!;
internal static readonly ConstructorInfo ExpandoObjectCtor = typeof(System.Dynamic.ExpandoObject).GetConstructor(Type.EmptyTypes)!;
internal static readonly ConstructorInfo StringBuilderCtor = typeof(StringBuilder).GetConstructor(Type.EmptyTypes)!;
internal static readonly MethodInfo StringBuilderAppendMethod = typeof(StringBuilder).GetMethod(nameof(StringBuilder.Append), [typeof(string)])!;
internal static readonly MethodInfo StringBuilderToStringMethod = typeof(StringBuilder).GetMethod(nameof(StringBuilder.ToString), Type.EmptyTypes)!;
internal static readonly MethodInfo ObjectToStringMethod = typeof(object).GetMethod(nameof(ToString))!;
// Spread and collection literal helpers
internal static readonly MethodInfo SpreadIntoDictMethod = typeof(SpreadHelpers).GetMethod(nameof(SpreadHelpers.SpreadIntoDict))!;
internal static readonly MethodInfo SpreadIntoListMethod = typeof(SpreadHelpers).GetMethod(nameof(SpreadHelpers.SpreadIntoList))!;
internal static readonly MethodInfo CreateTypedArrayMethod = typeof(SpreadHelpers).GetMethod(nameof(SpreadHelpers.CreateTypedArray))!;
internal static readonly MethodInfo ThrowIfCancellationRequestedMethod = typeof(CancellationToken).GetMethod(nameof(CancellationToken.ThrowIfCancellationRequested))!;
internal static readonly MethodInfo ApplyPropertyInitializerMethod = typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.ApplyPropertyInitializer))!;
internal static readonly MethodInfo ApplyCollectionInitializerMethod = typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.ApplyCollectionInitializer))!;
internal static readonly MethodInfo ApplyIndexerInitializerMethod = typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.ApplyIndexerInitializer))!;
internal static readonly MethodInfo CreateMultiDimArrayMethod = typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.CreateMultiDimArray))!;
internal static readonly MethodInfo MultiDimArrayGetMethod = typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.MultiDimArrayGet))!;
internal static readonly MethodInfo MultiDimArraySetMethod = typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.MultiDimArraySet))!;
internal static readonly MethodInfo CheckExecutionConstraintsMethod = typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.CheckExecutionConstraints))!;
internal static readonly MethodInfo GetConstraintsProperty = typeof(AlderConfig).GetProperty(nameof(AlderConfig.Constraints))!.GetGetMethod()!;
internal static readonly MethodInfo GetEnumeratorMethod = typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.GetEnumerator))!;
internal static readonly MethodInfo MoveNextMethod = typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext))!;
internal static readonly MethodInfo GetCurrentProperty = typeof(IEnumerator).GetProperty(nameof(IEnumerator.Current))!.GetGetMethod()!;
internal static readonly MethodInfo DisposeMethod = typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose))!;
internal static readonly MethodInfo CheckNullCoalesceAssignAllowedMethod = typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.CheckNullCoalesceAssignAllowed))!;
internal static readonly MethodInfo DisposeResourceMethod = typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.DisposeResource))!;
internal static readonly MethodInfo ValidateLockObjectMethod = typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.ValidateLockObject))!;
internal static readonly MethodInfo ValidateThrowOperandMethod = typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.ValidateThrowOperand))!;
internal static readonly MethodInfo ValidateCompoundAssignmentMethod = typeof(AssignmentRuntime).GetMethod(nameof(AssignmentRuntime.ValidateCompoundAssignment))!;
internal static readonly MethodInfo EvaluateCatchWhenGuardMethod = typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.EvaluateCatchWhenGuard))!;
internal static readonly MethodInfo ValidateAndCoerceTypeMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.ValidateAndCoerceType), [typeof(Type), typeof(object), typeof(string)])!;
internal static readonly MethodInfo ExplicitCastMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.ExplicitCast), [typeof(object), typeof(Type), typeof(Type), typeof(bool)])!;
internal static readonly MethodInfo IsTypeMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.IsType), [typeof(object), typeof(Type)])!;
internal static readonly MethodInfo TryAsMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.TryAs), [typeof(object), typeof(Type)])!;
internal static readonly MethodInfo GuardReflectionLeakMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.GuardReflectionLeak), [typeof(object), typeof(string)])!;
internal static readonly MethodInfo GuardReflectionLeakTypedMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.GuardReflectionLeakTyped))!;
internal static readonly MethodInfo CoerceNumericMethod = typeof(TypeHelpers).GetMethod("CoerceNumeric", BindingFlags.NonPublic | BindingFlags.Static)!;
internal static readonly MethodInfo InvokeCallMethod = typeof(Runtime.MethodInvoker).GetMethod(nameof(Runtime.MethodInvoker.InvokeCall))!;
internal static readonly MethodInfo InvokeMemberCallMethod = typeof(Runtime.MethodInvoker).GetMethod(nameof(Runtime.MethodInvoker.InvokeMemberCall))!;
internal static readonly MethodInfo GetVariableTypedMethod = typeof(IdentifierRuntime).GetMethod(nameof(IdentifierRuntime.GetVariableTyped))!;
internal static readonly MethodInfo ResolveIdentifierMethod = typeof(IdentifierRuntime).GetMethod(nameof(IdentifierRuntime.ResolveIdentifier))!;
internal static readonly MethodInfo ResolveIdentifierTypedMethod = typeof(IdentifierRuntime).GetMethod(nameof(IdentifierRuntime.ResolveIdentifierTyped))!;
internal static readonly MethodInfo InvokeIdentifierCallMethod = typeof(IdentifierRuntime).GetMethod(nameof(IdentifierRuntime.InvokeIdentifierCall))!;
internal static readonly MethodInfo InvokePipelineIdentifierMethod = typeof(IdentifierRuntime).GetMethod(nameof(IdentifierRuntime.InvokePipelineIdentifier))!;
internal static readonly MethodInfo DefineOutVariablesMethod = typeof(IdentifierRuntime).GetMethod(nameof(IdentifierRuntime.DefineOutVariables))!;
internal static readonly MethodInfo ConditionalTypePromotionMethod = typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.ConditionalTypePromotion))!;
internal static readonly ConstructorInfo NamedArgCtor = typeof(NamedArg).GetConstructor([typeof(string), typeof(object)])!;
internal static readonly ConstructorInfo CompiledLambdaValueCtor =
    typeof(CompiledLambdaValue).GetConstructor([
        typeof(List<string>),
        typeof(Func<object?[], AlderContext, object?>),
        typeof(AlderContext),
        typeof(Func<AlderContext, object?>),
        typeof(Func<object?, AlderContext, object?>),
        typeof(Func<object?, object?, AlderContext, object?>),
        typeof(LambdaExpr)
    ])!;
internal static readonly MethodInfo GetLambdaArgMethod =
    typeof(IdentifierRuntime).GetMethod(nameof(IdentifierRuntime.GetLambdaArg))!;
internal static readonly MethodInfo StringFormatMethod =
    typeof(string).GetMethod(nameof(string.Format), [typeof(string), typeof(object)])!;

internal static readonly ConstructorInfo OutArgMarkerCtor =
    typeof(OutArgMarker).GetConstructor([typeof(string), typeof(string), typeof(bool)])!;

private static readonly ConcurrentDictionary<Type, MethodInfo> ResolveIdentifierTypedMethodCache = new();
private static readonly ConcurrentDictionary<Type, MethodInfo> GetVariableTypedMethodCache = new();
private static readonly ConcurrentDictionary<Type, MethodInfo> GuardReflectionLeakTypedMethodCache = new();


internal static MethodInfo GetResolveIdentifierTypedMethod(Type valueType) =>
    ResolveIdentifierTypedMethodCache.GetOrAdd(
        valueType,
        static t => ResolveIdentifierTypedMethod.MakeGenericMethod(t));

internal static MethodInfo GetVariableTypedMethodFor(Type valueType) =>
    GetVariableTypedMethodCache.GetOrAdd(
        valueType,
        static t => GetVariableTypedMethod.MakeGenericMethod(t));

internal static MethodInfo GetGuardReflectionLeakTypedMethod(Type valueType) =>
    GuardReflectionLeakTypedMethodCache.GetOrAdd(
        valueType,
        static t => GuardReflectionLeakTypedMethod.MakeGenericMethod(t));

}
