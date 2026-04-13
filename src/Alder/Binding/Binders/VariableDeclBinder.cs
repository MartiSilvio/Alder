using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(VariableDeclExpr))]
internal static class VariableDeclBinder
{
    public static BoundExpr Bind(VariableDeclExpr expr, BindingContext context, BinderContext binder)
    {
        var declaredType = expr.DeclaredType != null
            ? context.RuntimeContext.TypeResolver.ResolveType(expr.DeclaredType.Value.Lexeme)
            : null;

        BoundExpr initializer;

        switch (expr.Initializer)
        {
            case CollectionExpr collectionExpr when declaredType != null:
                initializer = CollectionExprBinder.BindCollectionWithTargetType(collectionExpr, context, binder, declaredType);
                break;
            case TupleExpr tupleExpr when declaredType != null && TypeHelpers.IsValueTupleType(declaredType):
                initializer = TupleBinder.BindWithTargetType(tupleExpr, context, binder, declaredType);
                break;
            case ObjectCreationExpr { TypeName: "" } targetTypedNew when expr.DeclaredType != null:
            {
                var typedNew = targetTypedNew with { TypeName = expr.DeclaredType.Value.Lexeme };
                initializer = binder.Bind(typedNew, context);
                break;
            }
            // §12.8.20: a bare `default` literal takes its value from the target type of the declaration.
            case DefaultExpr { TypeToken: null } when declaredType != null:
            {
                var defaultValue = TypeHelpers.GetDefaultValue(declaredType);
                initializer = new BoundLiteralExpr(defaultValue, new BoundType(declaredType));
                break;
            }
            // CS8716: `var x = default;` has no target type for the default literal.
            case DefaultExpr { TypeToken: null }:
                throw new AlderException(DiagnosticDescriptors.NoTargetTypeForDefault);
            // CS9176: `var x = [1, 2, 3];` — a bare collection expression has no target type.
            // Only rejected in Standard mode; Extended mode infers an array element type.
            case CollectionExpr when declaredType == null && context.LanguageMode == LanguageMode.Standard:
                throw new AlderException(DiagnosticDescriptors.NoTargetTypeForCollectionExpression);
            // §10.7.1: a lambda has no type without a delegate target. Roslyn splits the rejection
            // based on `IsValidFunctionTypeConversionTarget` (ConversionsBase.cs:2828): base classes
            // of `MulticastDelegate` produce CS8917, any other unrelated nominal target produces CS1660.
            case LambdaExpr when declaredType != null && !typeof(Delegate).IsAssignableFrom(declaredType):
                throw TypeHelpers.IsValidFunctionTypeConversionTarget(declaredType)
                    ? new AlderException(DiagnosticDescriptors.CannotInferDelegateType)
                    : new AlderException(DiagnosticDescriptors.LambdaToNonDelegate, declaredType.Name);
            default:
                initializer = binder.Bind(expr.Initializer, context);
                break;
        }

        // §13.6.3: const locals must be initialized with a compile-time constant expression.
        if (expr.IsConst && !IsCompileTimeConstant(initializer))
        {
            var ex = new AlderException(DiagnosticDescriptors.ConstInitializerMustBeConstant, expr.Name.Lexeme);
            ex.EnrichDiagnosticsWithPosition(expr.Span, null, null);
            throw ex;
        }

        // §10.2: the initializer's static type must be implicitly convertible to the declared type.
        // Lambdas, method groups, and ranges rely on their own target-typing flow; unknown source
        // types defer to runtime.
        if (declaredType != null && !initializer.HasErrors
            && initializer is not BoundLambdaExpr
            && initializer is not BoundMethodGroupExpr
            && initializer is not BoundRangeExpr
            && initializer.StaticType is not BoundUnknownType)
        {
            ValidateInitializerConversion(initializer, declaredType);
        }

        var staticType = declaredType != null
            ? CreateBoundType(declaredType, expr.TupleElementNames)
            : initializer.StaticType;
        var localId = context.DeclareLocal(expr.Name.Lexeme, staticType, expr.IsConst ? ReadOnlyReason.Const : ReadOnlyReason.None);
        return new BoundVariableDeclExpr(
            expr.Name.Lexeme,
            initializer,
            declaredType,
            staticType,
            IsConst: expr.IsConst,
            LocalId: localId);
    }

    private static void ValidateInitializerConversion(BoundExpr initializer, Type declaredType)
    {
        var sourceClr = initializer.StaticType.ClrType;
        var sourceEffective = Nullable.GetUnderlyingType(sourceClr) ?? sourceClr;
        // Target=object is an implicit boxing/reference conversion (§10.2.8). Enum sources/targets
        // are skipped because Alder's enum-arithmetic inference returns the enum rather than the
        // underlying integral type, which would false-trip the check.
        if (sourceClr == declaredType || declaredType == typeof(object)
            || sourceEffective.IsEnum || declaredType.IsEnum)
            return;

        // Source=object from a non-local (method return, cast, engine-supplied variable) defers
        // to runtime because the runtime type is unknown. Only a local identifier whose static
        // type is explicitly `object` carries enough information to enforce §10.3.7 at bind time.
        if (sourceClr == typeof(object)
            && initializer is not BoundIdentifierExpr { LocalId: not null })
            return;

        if (TypeHelpers.CanImplicitlyConvert(sourceClr, declaredType))
            return;

        // §10.2.11 constant-expression conversion: int constants implicitly convert to any narrower
        // integral target they fit, and long constants implicitly convert to ulong when non-negative.
        // §11.1.6: out-of-range constants within those same source/target pairs produce CS0031 rather
        // than CS0266. Outside those pairs (e.g. long→int) the rule does not apply and we fall through
        // to the regular explicit-conversion diagnostic.
        if (TryGetConstantIntegralValue(initializer, out var iVal, out var lVal, out var isLong))
        {
            var effectiveTarget = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
            if (IsConstantConversionTarget(isLong, effectiveTarget))
            {
                if (ConstantFitsTarget(iVal, lVal, isLong, effectiveTarget))
                    return;
                throw new AlderException(
                    DiagnosticDescriptors.ConstantValueCannotConvert,
                    isLong ? (object)lVal : iVal, effectiveTarget.Name);
            }
        }

        var descriptor = IsRelatedForCS0266(sourceClr, declaredType)
            ? DiagnosticDescriptors.ExplicitConversionExists
            : DiagnosticDescriptors.NoImplicitConversion;
        throw new AlderException(descriptor, sourceClr.Name, declaredType.Name);
    }

    // §10.2/§10.3: chooses between CS0266 ("explicit conversion exists, are you missing a cast?")
    // and CS0029 ("cannot implicitly convert"). Narrower than TypeHelpers.HasExplicitConversion,
    // which answers a different question (whether any cast operator is legal for CastBinder).
    // Here we only report CS0266 when an explicit cast would actually succeed at bind time —
    // numeric/char pairs, enum↔integral, nullable↔underlying, and reference pairs that stand in
    // an inheritance relationship. Unrelated reference pairs (string↔Exception) and value-type
    // array covariance (int[]↔object[]) fall through to CS0029.
    private static bool IsRelatedForCS0266(Type source, Type target)
    {
        var s = Nullable.GetUnderlyingType(source) ?? source;
        var t = Nullable.GetUnderlyingType(target) ?? target;

        if (s == typeof(object) || t == typeof(object))
            return true;
        if ((TypeHelpers.IsArithmetic(s) || s == typeof(char)) && (TypeHelpers.IsArithmetic(t) || t == typeof(char)))
            return true;
        if ((s.IsEnum && TypeHelpers.IsArithmetic(t)) || (t.IsEnum && TypeHelpers.IsArithmetic(s)))
            return true;
        if (!s.IsValueType && !t.IsValueType && (s.IsAssignableFrom(t) || t.IsAssignableFrom(s)))
            return true;
        if (Nullable.GetUnderlyingType(source) != null || Nullable.GetUnderlyingType(target) != null)
        {
            if (s == t) return true;
        }
        return false;
    }

    // §10.2.11: int constants can target sbyte/byte/short/ushort/uint/ulong (char excluded by
    // §10.2.3). Long constants can only target ulong. Outside this set the constant conversion
    // rule does not participate — long→int is not a constant conversion, it's CS0266.
    private static bool IsConstantConversionTarget(bool isLong, Type target) =>
        isLong
            ? target == typeof(ulong)
            : target == typeof(sbyte) || target == typeof(byte)
              || target == typeof(short) || target == typeof(ushort)
              || target == typeof(uint) || target == typeof(ulong);

    private static bool ConstantFitsTarget(int iVal, long lVal, bool isLong, Type target)
    {
        if (isLong)
            return target == typeof(ulong) && lVal >= 0;

        return target == typeof(sbyte) && iVal >= sbyte.MinValue && iVal <= sbyte.MaxValue
            || target == typeof(byte) && iVal >= byte.MinValue && iVal <= byte.MaxValue
            || target == typeof(short) && iVal >= short.MinValue && iVal <= short.MaxValue
            || target == typeof(ushort) && iVal >= ushort.MinValue && iVal <= ushort.MaxValue
            || target == typeof(uint) && iVal >= 0
            || target == typeof(ulong) && iVal >= 0;
    }

    private static bool TryGetConstantIntegralValue(BoundExpr expr, out int intValue, out long longValue, out bool isLong)
    {
        intValue = 0;
        longValue = 0;
        isLong = false;

        if (expr is BoundUnaryExpr { Operator: TokenType.Minus, Operand: BoundLiteralExpr inner })
        {
            if (inner.Value is int i) { intValue = -i; return true; }
            if (inner.Value is long l) { longValue = -l; isLong = true; return true; }
            return false;
        }

        if (expr is BoundLiteralExpr literal)
        {
            if (literal.Value is int i) { intValue = i; return true; }
            if (literal.Value is long l) { longValue = l; isLong = true; return true; }
        }

        return false;
    }

    // §12.23: a constant expression is a literal, a sequence of unary/binary operators over
    // constants, or a static field access on an enum/const field (e.g. DayOfWeek.Monday).
    private static bool IsCompileTimeConstant(BoundExpr expr) => expr switch
    {
        BoundLiteralExpr => true,
        BoundUnaryExpr u => IsCompileTimeConstant(u.Operand),
        BoundBinaryExpr b => IsCompileTimeConstant(b.Left) && IsCompileTimeConstant(b.Right),
        BoundFieldAccessExpr f => f.Field.IsLiteral || (f.Field.IsStatic && f.Field.DeclaringType?.IsEnum == true),
        _ => false
    };

    private static BoundType CreateBoundType(Type clrType, IReadOnlyList<string?>? tupleElementNames)
    {
        if (tupleElementNames == null || !TypeHelpers.IsValueTupleType(clrType))
            return new BoundType(clrType);

        var genericArgs = clrType.GetGenericArguments();
        var members = ImmutableDictionary.CreateBuilder<string, Type>();
        for (var i = 0; i < tupleElementNames.Count && i < genericArgs.Length; i++)
        {
            if (tupleElementNames[i] is { } name)
                members[name] = genericArgs[i];
        }

        return members.Count > 0
            ? new BoundStructuralType(clrType, members.ToImmutable(), [..tupleElementNames])
            : new BoundType(clrType);
    }
}
