using System.Dynamic;
using System.Collections.Concurrent;
using Alder.Interpretation;
using Alder.Runtime;
using Alder.Runtime.Extensions;
using Alder.Runtime.Semantics;

namespace Alder.Compiled.Compilation;

internal static class BoundRuntimeMethodCache
{
    internal static readonly MethodInfo ResolveIdentifierMethod =
        typeof(IdentifierRuntime).GetMethod(nameof(IdentifierRuntime.ResolveIdentifier))!;

    internal static readonly MethodInfo GetVariableTypedMethod =
        typeof(AlderContext).GetMethod(nameof(AlderContext.GetVariableTyped))!;

    internal static readonly MethodInfo ContextGetMethod =
        typeof(AlderContext).GetMethod(nameof(AlderContext.Get), [typeof(string)])!;

    internal static readonly MethodInfo ContextSetMethod =
        typeof(AlderContext).GetMethod(nameof(AlderContext.Set), [typeof(string), typeof(object)])!;

    internal static readonly MethodInfo ContextDefineNewMethod =
        typeof(AlderContext).GetMethod(nameof(AlderContext.DefineNew), [typeof(string), typeof(object), typeof(Type), typeof(bool)])!;

    internal static readonly MethodInfo ContextCreateChildMethod =
        typeof(AlderContext).GetMethod(nameof(AlderContext.CreateChild))!;

    internal static readonly MethodInfo GetTypeResolverProperty =
        typeof(AlderContext).GetProperty(nameof(AlderContext.TypeResolver), BindingFlags.NonPublic | BindingFlags.Instance)!.GetGetMethod(true)!;

    internal static readonly MethodInfo GetConfigProperty =
        typeof(AlderContext).GetProperty(nameof(AlderContext.Config))!.GetGetMethod()!;

    internal static readonly MethodInfo ResolveTypeMethod =
        typeof(TypeResolver).GetMethod(nameof(TypeResolver.ResolveType))!;

    internal static readonly MethodInfo GetMemberMethod =
        typeof(MemberAccess).GetMethod(nameof(MemberAccess.GetMember))!;

    internal static readonly MethodInfo GetIndexMethod =
        typeof(MemberAccess).GetMethod(
            nameof(MemberAccess.GetIndex),
            [typeof(object), typeof(object), typeof(AlderConfig), typeof(AlderContext)])!;

    internal static readonly MethodInfo SetMemberMethod =
        typeof(MemberAccess).GetMethod(nameof(MemberAccess.SetMember))!;

    internal static readonly MethodInfo SetIndexMethod =
        typeof(MemberAccess).GetMethod(
            nameof(MemberAccess.SetIndex),
            [typeof(object), typeof(object), typeof(object), typeof(AlderConfig), typeof(AlderContext)])!;

    internal static readonly MethodInfo NormalizeIndexMethod =
        typeof(MemberAccess).GetMethod(nameof(MemberAccess.NormalizeIndex), [typeof(int), typeof(int), typeof(LanguageMode)])!;

    internal static readonly MethodInfo ConvertToInt32ObjectMethod =
        typeof(Convert).GetMethod(nameof(Convert.ToInt32), [typeof(object)])!;

    internal static readonly MethodInfo StringFormatMethod =
        typeof(string).GetMethod(nameof(string.Format), [typeof(string), typeof(object)])!;

    internal static readonly MethodInfo GetEnumeratorMethod =
        typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.GetEnumerator))!;

    internal static readonly MethodInfo MoveNextMethod =
        typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext))!;

    internal static readonly MethodInfo GetCurrentMethod =
        typeof(IEnumerator).GetProperty(nameof(IEnumerator.Current))!.GetGetMethod()!;

    internal static readonly MethodInfo DisposeMethod =
        typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose))!;

    internal static readonly MethodInfo ControlFlowReturnMethod =
        typeof(ControlFlowSignal).GetMethod(nameof(ControlFlowSignal.Return))!;

    internal static readonly FieldInfo ControlFlowBreakField =
        typeof(ControlFlowSignal).GetField(nameof(ControlFlowSignal.Break), BindingFlags.Public | BindingFlags.Static)!;

    internal static readonly FieldInfo ControlFlowContinueField =
        typeof(ControlFlowSignal).GetField(nameof(ControlFlowSignal.Continue), BindingFlags.Public | BindingFlags.Static)!;

    internal static readonly FieldInfo ControlFlowGotoDefaultField =
        typeof(ControlFlowSignal).GetField(nameof(ControlFlowSignal.GotoDefaultSignal), BindingFlags.Public | BindingFlags.Static)!;

    internal static readonly MethodInfo ControlFlowGotoCaseMethod =
        typeof(ControlFlowSignal).GetMethod(nameof(ControlFlowSignal.GotoCaseSignal))!;

    internal static readonly MethodInfo ControlFlowGotoMethod =
        typeof(ControlFlowSignal).GetMethod(nameof(ControlFlowSignal.GotoSignal))!;

    internal static readonly PropertyInfo ControlFlowSignalKindProperty =
        typeof(ControlFlowSignal).GetProperty(nameof(ControlFlowSignal.SignalKind))!;

    internal static readonly PropertyInfo ControlFlowValueProperty =
        typeof(ControlFlowSignal).GetProperty(nameof(ControlFlowSignal.Value))!;

    internal static readonly MethodInfo InvokeCallMethod =
        typeof(Runtime.MethodInvoker).GetMethod(nameof(Runtime.MethodInvoker.InvokeCall))!;

    internal static readonly MethodInfo InvokeMemberCallMethod =
        typeof(Runtime.MethodInvoker).GetMethod(nameof(Runtime.MethodInvoker.InvokeMemberCall))!;

    internal static readonly MethodInfo InvokeIdentifierCallMethod =
        typeof(IdentifierRuntime).GetMethod(nameof(IdentifierRuntime.InvokeIdentifierCall))!;

    internal static readonly MethodInfo InvokePipelineIdentifierMethod =
        typeof(IdentifierRuntime).GetMethod(nameof(IdentifierRuntime.InvokePipelineIdentifier))!;

    internal static readonly MethodInfo InvokePipelineMethod =
        typeof(PipelineOperator).GetMethod(nameof(PipelineOperator.InvokePipeline))!;

    internal static readonly MethodInfo PerformComparisonMethod =
        typeof(ChainedComparisonHelper).GetMethod(nameof(ChainedComparisonHelper.PerformComparison))!;

    internal static readonly MethodInfo CheckExecutionConstraintsMethod =
        typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.CheckExecutionConstraints))!;

    internal static readonly MethodInfo CheckLoopIterationConstraintMethod =
        typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.CheckLoopIterationConstraint))!;

    internal static readonly MethodInfo DefineOutVariablesMethod =
        typeof(IdentifierRuntime).GetMethod(nameof(IdentifierRuntime.DefineOutVariables))!;

    internal static readonly MethodInfo ValidateVariableAssignmentMethod =
        typeof(AssignmentRuntime).GetMethod(nameof(AssignmentRuntime.ValidateVariableAssignment))!;

    internal static readonly MethodInfo DefineVariableMethod =
        typeof(AssignmentRuntime).GetMethod(nameof(AssignmentRuntime.DefineVariable))!;

    internal static readonly MethodInfo CheckNullCoalesceAssignAllowedMethod =
        typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.CheckNullCoalesceAssignAllowed))!;

    internal static readonly MethodInfo ApplyCompoundAssignMethod =
        typeof(AssignmentRuntime).GetMethod(nameof(AssignmentRuntime.ApplyCompoundAssign))!;

    internal static readonly MethodInfo ApplyIncrementDecrementMethod =
        typeof(AssignmentRuntime).GetMethod(nameof(AssignmentRuntime.ApplyIncrementDecrement))!;

    internal static readonly MethodInfo ApplyMemberAssignMethod =
        typeof(AssignmentRuntime).GetMethod(nameof(AssignmentRuntime.ApplyMemberAssign))!;

    internal static readonly MethodInfo ApplyIndexAssignMethod =
        typeof(AssignmentRuntime).GetMethod(nameof(AssignmentRuntime.ApplyIndexAssign))!;

    internal static readonly MethodInfo ApplyMemberCompoundAssignMethod =
        typeof(AssignmentRuntime).GetMethod(nameof(AssignmentRuntime.ApplyMemberCompoundAssign))!;

    internal static readonly MethodInfo ApplyIndexCompoundAssignMethod =
        typeof(AssignmentRuntime).GetMethod(nameof(AssignmentRuntime.ApplyIndexCompoundAssign))!;

    internal static readonly MethodInfo ApplyMemberIncrementMethod =
        typeof(AssignmentRuntime).GetMethod(nameof(AssignmentRuntime.ApplyMemberIncrement))!;

    internal static readonly MethodInfo ApplyIndexIncrementMethod =
        typeof(AssignmentRuntime).GetMethod(nameof(AssignmentRuntime.ApplyIndexIncrement))!;

    internal static readonly MethodInfo CreateLambdaValueMethod =
        typeof(IdentifierRuntime).GetMethod(nameof(IdentifierRuntime.CreateLambdaValue))!;

    internal static readonly MethodInfo InvokeConstructorMethod =
        typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.InvokeConstructor), [typeof(Type), typeof(object?[]), typeof(AlderConfig)])!;


    internal static readonly MethodInfo CreateSystemRangeMethod =
        typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.CreateSystemRange))!;

    internal static readonly MethodInfo EnsureEnumerableMethod =
        typeof(RangeHelpers).GetMethod(nameof(RangeHelpers.EnsureEnumerable))!;

    internal static readonly MethodInfo CreateTupleMethod =
        typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.CreateTuple))!;

    internal static readonly MethodInfo CreateNamedTupleMethod =
        typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.CreateNamedTuple))!;

    internal static readonly MethodInfo DeconstructTupleMethod =
        typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.DeconstructTuple))!;

    internal static readonly MethodInfo MultiDimArrayGetMethod =
        typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.MultiDimArrayGet))!;

    internal static readonly MethodInfo MultiDimArraySetMethod =
        typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.MultiDimArraySet))!;

    internal static readonly MethodInfo ValidateThrowOperandMethod =
        typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.ValidateThrowOperand))!;

    internal static readonly MethodInfo DisposeResourceMethod =
        typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.DisposeResource))!;

    internal static readonly MethodInfo ValidateLockObjectMethod =
        typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.ValidateLockObject))!;

    internal static readonly MethodInfo EvaluateCatchWhenGuardMethod =
        typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.EvaluateCatchWhenGuard))!;

    internal static readonly MethodInfo MonitorEnterMethod =
        typeof(Monitor).GetMethod(nameof(Monitor.Enter), [typeof(object)])!;

    internal static readonly MethodInfo MonitorExitMethod =
        typeof(Monitor).GetMethod(nameof(Monitor.Exit), [typeof(object)])!;

    internal static readonly MethodInfo NullableBoolAndMethod =
        typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.NullableBoolAnd))!;

    internal static readonly MethodInfo NullableBoolOrMethod =
        typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.NullableBoolOr))!;

    internal static readonly MethodInfo ApplyPropertyInitializerMethod =
        typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.ApplyPropertyInitializer))!;

    internal static readonly MethodInfo ApplyCollectionInitializerMethod =
        typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.ApplyCollectionInitializer))!;

    internal static readonly MethodInfo ApplyIndexerInitializerMethod =
        typeof(ConstructionRuntime).GetMethod(nameof(ConstructionRuntime.ApplyIndexerInitializer))!;

    internal static readonly MethodInfo EnsureMemberTargetNotNullMethod =
        typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.EnsureMemberTargetNotNull))!;

    internal static readonly MethodInfo EnsureCallTargetNotNullMethod =
        typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.EnsureCallTargetNotNull))!;

    internal static readonly MethodInfo EnsureIndexTargetNotNullMethod =
        typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.EnsureIndexTargetNotNull))!;

    internal static readonly MethodInfo CheckCollectionSizeMethod =
        typeof(ExecutionRuntime).GetMethod(nameof(ExecutionRuntime.CheckCollectionSize))!;

    internal static readonly PropertyInfo SecurityPolicyProperty =
        typeof(AlderConfig).GetProperty(nameof(AlderConfig.Security))!;

    internal static readonly MethodInfo RequireBooleanMethod =
        typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.RequireBoolean))!;

    internal static readonly MethodInfo RequireBooleanForLogicalOperatorMethod =
        typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.RequireBooleanForLogicalOperator))!;

    internal static readonly MethodInfo ExplicitCastMethod =
        typeof(TypeHelpers).GetMethod(
            nameof(TypeHelpers.ExplicitCast),
            [typeof(object), typeof(Type), typeof(Type), typeof(bool)])!;

    internal static readonly MethodInfo TryAsMethod =
        typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.TryAs), [typeof(object), typeof(Type)])!;

    internal static readonly MethodInfo MatchPatternMethod =
        typeof(PatternRuntime).GetMethod(nameof(PatternRuntime.MatchPattern))!;

    internal static readonly MethodInfo ApplyConstantNumericPromotionMethod =
        typeof(NumericPromotionRuntime).GetMethod(nameof(NumericPromotionRuntime.ApplyConstantNumericPromotion))!;

    internal static readonly ConstructorInfo AlderExceptionCtor =
        typeof(AlderException).GetConstructor([typeof(Diagnostics.DiagnosticDescriptor), typeof(object[])])!;

    internal static readonly FieldInfo SwitchExpressionNonExhaustiveDescriptor =
        typeof(Diagnostics.DiagnosticDescriptors).GetField(nameof(Diagnostics.DiagnosticDescriptors.SwitchExpressionNonExhaustive))!;

    internal static readonly MethodInfo GetSliceMethod =
        typeof(MemberAccess).GetMethod(nameof(MemberAccess.GetSlice), [typeof(object), typeof(object), typeof(object)])!;

    internal static readonly MethodInfo GetSliceStepMethod =
        typeof(MemberAccess).GetMethod(nameof(MemberAccess.GetSlice), [typeof(object), typeof(object), typeof(object), typeof(object)])!;

    internal static readonly PropertyInfo StringLengthProperty =
        typeof(string).GetProperty(nameof(string.Length))!;

    internal static readonly PropertyInfo StringCharsProperty =
        typeof(string).GetProperty("Chars")!;

    internal static readonly PropertyInfo IListIndexerProperty =
        typeof(IList).GetProperty("Item")!;

    internal static readonly PropertyInfo ICollectionCountProperty =
        typeof(ICollection).GetProperty(nameof(ICollection.Count))!;

    internal static readonly MethodInfo NegateMethod =
        typeof(Operators).GetMethod(nameof(Operators.Negate), [typeof(object), typeof(bool)])!;

    internal static readonly MethodInfo UnaryPlusMethod =
        typeof(Operators).GetMethod(nameof(Operators.UnaryPlus), [typeof(object)])!;

    internal static readonly MethodInfo LogicalNotMethod =
        typeof(Operators).GetMethod(nameof(Operators.LogicalNot), [typeof(object)])!;

    internal static readonly MethodInfo BitwiseNotMethod =
        typeof(Operators).GetMethod(nameof(Operators.BitwiseNot), [typeof(object)])!;

    internal static readonly MethodInfo AddMethod =
        typeof(Operators).GetMethod(nameof(Operators.Add), [typeof(object), typeof(object), typeof(AlderConfig), typeof(AlderContext), typeof(bool), typeof(bool)])!;

    internal static readonly MethodInfo SubtractMethod =
        typeof(Operators).GetMethod(nameof(Operators.Subtract), [typeof(object), typeof(object), typeof(bool)])!;

    internal static readonly MethodInfo MultiplyMethod =
        typeof(Operators).GetMethod(nameof(Operators.Multiply), [typeof(object), typeof(object), typeof(LanguageMode), typeof(bool)])!;

    internal static readonly MethodInfo DivideMethod =
        typeof(Operators).GetMethod(nameof(Operators.Divide), [typeof(object), typeof(object)])!;

    internal static readonly MethodInfo ModuloMethod =
        typeof(Operators).GetMethod(nameof(Operators.Modulo), [typeof(object), typeof(object)])!;

    internal static readonly MethodInfo EqualsMethod =
        typeof(Operators).GetMethod(nameof(Operators.Equals), [typeof(object), typeof(object)])!;

    internal static readonly MethodInfo NotEqualsMethod =
        typeof(Operators).GetMethod(nameof(Operators.NotEquals), [typeof(object), typeof(object)])!;

    internal static readonly MethodInfo StrictEqualsMethod =
        typeof(Operators).GetMethod(nameof(Operators.StrictEquals), [typeof(object), typeof(object)])!;

    internal static readonly MethodInfo StrictNotEqualsMethod =
        typeof(Operators).GetMethod(nameof(Operators.StrictNotEquals), [typeof(object), typeof(object)])!;

    internal static readonly MethodInfo LessThanMethod =
        typeof(Operators).GetMethod(nameof(Operators.LessThan), [typeof(object), typeof(object), typeof(StringComparison)])!;

    internal static readonly MethodInfo LessThanOrEqualMethod =
        typeof(Operators).GetMethod(nameof(Operators.LessThanOrEqual), [typeof(object), typeof(object), typeof(StringComparison)])!;

    internal static readonly MethodInfo GreaterThanMethod =
        typeof(Operators).GetMethod(nameof(Operators.GreaterThan), [typeof(object), typeof(object), typeof(StringComparison)])!;

    internal static readonly MethodInfo GreaterThanOrEqualMethod =
        typeof(Operators).GetMethod(nameof(Operators.GreaterThanOrEqual), [typeof(object), typeof(object), typeof(StringComparison)])!;

    internal static readonly MethodInfo BitwiseAndMethod =
        typeof(Operators).GetMethod(nameof(Operators.BitwiseAnd), [typeof(object), typeof(object)])!;

    internal static readonly MethodInfo BitwiseOrMethod =
        typeof(Operators).GetMethod(nameof(Operators.BitwiseOr), [typeof(object), typeof(object)])!;

    internal static readonly MethodInfo BitwiseXorMethod =
        typeof(Operators).GetMethod(nameof(Operators.BitwiseXor), [typeof(object), typeof(object)])!;

    internal static readonly MethodInfo LeftShiftMethod =
        typeof(Operators).GetMethod(nameof(Operators.LeftShift), [typeof(object), typeof(object)])!;

    internal static readonly MethodInfo RightShiftMethod =
        typeof(Operators).GetMethod(nameof(Operators.RightShift), [typeof(object), typeof(object)])!;

    internal static readonly MethodInfo UnsignedRightShiftMethod =
        typeof(Operators).GetMethod(nameof(Operators.UnsignedRightShift), [typeof(object), typeof(object)])!;

    internal static readonly MethodInfo PowerMethod =
        typeof(Operators).GetMethod(nameof(Operators.Power), [typeof(object), typeof(object)])!;

    internal static readonly MethodInfo InOperatorMethod =
        typeof(Operators).GetMethod(nameof(Operators.InOperator), [typeof(object), typeof(object)])!;

    internal static readonly MethodInfo LikeMethod =
        typeof(Operators).GetMethod(nameof(Operators.Like), [typeof(object), typeof(object), typeof(StringComparison)])!;

    internal static readonly MethodInfo RegexMatchMethod =
        typeof(Operators).GetMethod(nameof(Operators.RegexMatch), [typeof(object), typeof(object)])!;

    internal static readonly MethodInfo RegexNotMatchMethod =
        typeof(Operators).GetMethod(nameof(Operators.RegexNotMatch), [typeof(object), typeof(object)])!;

    internal static readonly MethodInfo SpaceshipMethod =
        typeof(Operators).GetMethod(nameof(Operators.Spaceship), [typeof(object), typeof(object)])!;

    internal static readonly ConstructorInfo NamedArgCtor =
        typeof(NamedArg).GetConstructor([typeof(string), typeof(object)])!;

    internal static readonly ConstructorInfo OutArgMarkerCtor =
        typeof(OutArgMarker).GetConstructor([typeof(string), typeof(string), typeof(bool)])!;

    internal static readonly ConstructorInfo ListCtor =
        typeof(List<object?>).GetConstructor(Type.EmptyTypes)!;

    internal static readonly MethodInfo ListAddMethod =
        typeof(List<object?>).GetMethod(nameof(List<object?>.Add))!;

    internal static readonly MethodInfo SpreadIntoListMethod =
        typeof(CollectionFactory).GetMethod(nameof(CollectionFactory.SpreadIntoList))!;

    internal static readonly MethodInfo InferAndCreateArrayMethod =
        typeof(RuntimeArrayFactory).GetMethod(nameof(RuntimeArrayFactory.InferAndCreateArray))!;

    internal static readonly MethodInfo CreateFromValuesMethod =
        typeof(RuntimeArrayFactory).GetMethod(nameof(RuntimeArrayFactory.CreateFromValues))!;

    internal static readonly MethodInfo CollectionFactoryCreateMethod =
        typeof(CollectionFactory).GetMethod(nameof(CollectionFactory.Create),
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;

    internal static readonly ConstructorInfo ExpandoObjectCtor =
        typeof(ExpandoObject).GetConstructor(Type.EmptyTypes)!;

    internal static readonly MethodInfo SpreadIntoDictMethod =
        typeof(CollectionFactory).GetMethod(nameof(CollectionFactory.SpreadIntoDict))!;

    internal static readonly ConstructorInfo StringBuilderCtor =
        typeof(StringBuilder).GetConstructor(Type.EmptyTypes)!;

    internal static readonly MethodInfo StringBuilderAppendMethod =
        typeof(StringBuilder).GetMethod(nameof(StringBuilder.Append), [typeof(string)])!;

    internal static readonly MethodInfo StringBuilderToStringMethod =
        typeof(StringBuilder).GetMethod(nameof(StringBuilder.ToString), Type.EmptyTypes)!;

    internal static readonly MethodInfo ObjectToStringMethod =
        typeof(object).GetMethod(nameof(ToString))!;

    internal static readonly MethodInfo ApplyCompoundAssignLocalMethod =
        typeof(AssignmentRuntime).GetMethod(nameof(AssignmentRuntime.ApplyCompoundAssignLocal))!;

    internal static readonly MethodInfo ApplyIncrementDecrementLocalMethod =
        typeof(AssignmentRuntime).GetMethod(nameof(AssignmentRuntime.ApplyIncrementDecrementLocal))!;

    internal static readonly MethodInfo ValidateVariableAssignmentLocalMethod =
        typeof(AssignmentRuntime).GetMethod(nameof(AssignmentRuntime.ValidateVariableAssignmentLocal))!;

    internal static readonly MethodInfo ValidateAndCoerceTypeMethod =
        typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.ValidateAndCoerceType))!;

    internal static readonly MethodInfo StringConcatTwoStringsMethod =
        typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string)])!;

    internal static readonly MethodInfo StringConcatObjectMethod =
        typeof(string).GetMethod(nameof(string.Concat), [typeof(object)])!;

    internal static readonly MethodInfo CoerceNumericMethod =
        typeof(TypeHelpers).GetMethod("CoerceNumeric", BindingFlags.NonPublic | BindingFlags.Static)!;

    internal static readonly MethodInfo GuardReflectionLeakTypedMethod =
        typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.GuardReflectionLeakTyped))!;

    private static readonly ConcurrentDictionary<Type, MethodInfo> GetVariableTypedMethodCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> GuardReflectionLeakTypedMethodCache = new();

    internal static MethodInfo GetVariableTypedMethodFor(Type valueType) =>
        GetVariableTypedMethodCache.GetOrAdd(
            valueType,
            static t => GetVariableTypedMethod.MakeGenericMethod(t));

    internal static MethodInfo GetGuardReflectionLeakTypedMethod(Type valueType) =>
        GuardReflectionLeakTypedMethodCache.GetOrAdd(
            valueType,
            static t => GuardReflectionLeakTypedMethod.MakeGenericMethod(t));
}
