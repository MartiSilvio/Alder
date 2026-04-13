using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

/// <summary>
/// Validates patterns against the static type of the match subject. Runs at bind time
/// so invalid patterns are rejected before evaluation, matching Roslyn's semantic phase.
/// </summary>
internal static class PatternValidator
{
    public static void Validate(Pattern pattern, BoundType subjectType, BindingContext context)
    {
        // An unknown subject type means the binder can't decide — defer to runtime.
        if (subjectType is BoundUnknownType or BoundVoidType) return;
        // A static type of `object` means the actual runtime type is undetermined (boxing site);
        // pattern applicability must be decided at evaluation time.
        if (subjectType.ClrType == typeof(object)) return;
        ValidateCore(pattern, subjectType.ClrType, context, negated: false);
    }

    private static void ValidateCore(Pattern pattern, Type subjectType, BindingContext context, bool negated)
    {
        switch (pattern)
        {
            case RelationalPattern:
                // §11.2.3: relational patterns require a type that has the corresponding predefined relational operators.
                // The built-in set is sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double, decimal
                // (plus nint/nuint via TypeHelpers.IsArithmetic).
                var effective = Nullable.GetUnderlyingType(subjectType) ?? subjectType;
                if (!TypeHelpers.IsArithmetic(effective) && effective != typeof(char))
                    throw new AlderException(DiagnosticDescriptors.RelationalPatternInvalidType, effective.Name);
                break;

            case ConstantPattern constant:
                ValidateConstantPattern(constant, subjectType, context, negated);
                break;

            case TypePattern typePattern:
                ValidateTypePattern(typePattern, subjectType, context);
                break;

            case ParenthesizedPattern parenthesized:
                ValidateCore(parenthesized.Inner, subjectType, context, negated);
                break;

            case AndPattern and:
                ValidateCore(and.Left, subjectType, context, negated);
                ValidateCore(and.Right, subjectType, context, negated);
                break;

            case OrPattern or:
                ValidateCore(or.Left, subjectType, context, negated);
                ValidateCore(or.Right, subjectType, context, negated);
                break;

            case NotPattern not:
                ValidateCore(not.Operand, subjectType, context, !negated);
                break;

            case ListPattern:
                ValidateListPattern(subjectType);
                break;
        }
    }

    // §11.2.1 / CS9135: the expression in a constant pattern must itself be a compile-time
    // constant. A literal, a unary/binary operator over constants, a `null`, or a static
    // member access on an enum/const field qualifies. A bare local identifier does not,
    // unless it names a `const` local (which Roslyn treats as a compile-time constant).
    private static bool IsCompileTimeConstantPatternExpression(Expr expr, BindingContext context) => expr switch
    {
        LiteralExpr => true,
        UnaryExpr u => IsCompileTimeConstantPatternExpression(u.Right, context),
        BinaryExpr b => IsCompileTimeConstantPatternExpression(b.Left, context)
                        && IsCompileTimeConstantPatternExpression(b.Right, context),
        // A dotted access such as `DayOfWeek.Monday` or `Math.PI` is constant when the target
        // is an enum member or a const field. Resolution happens at binding of the arm.
        MemberAccessExpr m => IsConstantMemberAccess(m, context),
        // A bare identifier is constant when it names a `const` local or a known enum type.
        IdentifierExpr id =>
            context.GetReadOnlyReason(id.Name.Lexeme) == ReadOnlyReason.Const
            || context.RuntimeContext.TypeResolver.TryResolveType(id.Name.Lexeme) is { IsEnum: true },
        _ => false,
    };

    private static bool IsConstantMemberAccess(MemberAccessExpr expr, BindingContext context)
    {
        // Walk the chain to the root identifier, collecting the type name.
        if (expr.Object is IdentifierExpr rootId)
        {
            var type = context.RuntimeContext.TypeResolver.TryResolveType(rootId.Name.Lexeme);
            if (type is null) return false;
            if (type.IsEnum) return true;
            var field = type.GetField(expr.Name.Lexeme,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.FlattenHierarchy);
            return field is { IsLiteral: true } || (field?.IsStatic == true && field.FieldType.IsEnum);
        }

        return expr.Object is MemberAccessExpr inner && IsConstantMemberAccess(inner, context);
    }

    private static void ValidateListPattern(Type subjectType)
    {
        // §11.2.5: list patterns require a countable + indexable subject. The runtime matcher
        // (PatternRuntime.GetCountableLength/GetIndexedElement) handles IList and string, so the
        // binder validates against the same contract — anything else is guaranteed to fail.
        var effective = Nullable.GetUnderlyingType(subjectType) ?? subjectType;
        if (effective == typeof(string)) return;
        if (typeof(System.Collections.IList).IsAssignableFrom(effective)) return;
        throw new AlderException(DiagnosticDescriptors.ListPatternRequiresListable, effective.Name);
    }

    private static void ValidateConstantPattern(ConstantPattern pattern, Type subjectType, BindingContext context, bool negated)
    {
        // §11.2.1: the case label / constant pattern expression must itself be a constant.
        // A bare identifier referring to a non-const local is CS9135, not CS0103.
        if (!IsCompileTimeConstantPatternExpression(pattern.Value, context))
            throw new AlderException(
                DiagnosticDescriptors.ConstantValueOfTypeExpected,
                (Nullable.GetUnderlyingType(subjectType) ?? subjectType).Name);

        // §11.2.1: constant pattern — the constant must be convertible to the input type.
        // Only check when the constant is a literal whose type is statically known.
        if (pattern.Value is not LiteralExpr literal) return;
        var effectiveSubject = Nullable.GetUnderlyingType(subjectType) ?? subjectType;

        if (literal.Value is null)
        {
            // §11.2.1: null constant matches reference types and Nullable<T>. A non-nullable
            // value type cannot be null, so the pattern is always false.
            // Roslyn allows `x is not null` (always true) for value types with a warning, so we
            // only error on the positive form.
            if (!negated && subjectType.IsValueType && Nullable.GetUnderlyingType(subjectType) == null)
                throw new AlderException(DiagnosticDescriptors.NullToNonNullable, subjectType.Name);
            return;
        }

        var constantType = literal.Value.GetType();
        if (constantType == effectiveSubject) return;
        // §11.2.1: the constant value must be implicitly convertible to the subject type.
        if (TypeHelpers.CanImplicitlyConvert(constantType, effectiveSubject)) return;
        // Reference conversions: the pattern succeeds if the subject's runtime type may be the constant's type.
        if (effectiveSubject.IsAssignableFrom(constantType)) return;

        // CS0266 when an explicit conversion exists (numeric → numeric); CS0029 otherwise.
        if (TypeHelpers.IsArithmetic(constantType) && TypeHelpers.IsArithmetic(effectiveSubject))
            throw new AlderException(DiagnosticDescriptors.ExplicitConversionExists, constantType.Name, subjectType.Name);
        throw new AlderException(DiagnosticDescriptors.NoImplicitConversion, constantType.Name, subjectType.Name);
    }

    private static void ValidateTypePattern(TypePattern pattern, Type subjectType, BindingContext context)
    {
        var targetType = context.RuntimeContext.TypeResolver.TryResolveType(pattern.TypeToken.Lexeme);
        if (targetType is null) return; // unresolved (may be an enum member reference) — defer to runtime

        // §11.2.2: a declaration pattern may not bind a nullable value type — Roslyn reports CS8116.
        if (pattern.VariableName is not null
            && targetType.IsGenericType
            && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlying = Nullable.GetUnderlyingType(targetType)!;
            throw new AlderException(DiagnosticDescriptors.NullableTypeInPattern, underlying.Name);
        }

        // §11.2.2: a declaration pattern with a variable binding must have a possible reference,
        // boxing, unboxing, or identity conversion between the subject type and the pattern type.
        // Without a binding, the `is` expression is always false at runtime — Roslyn warns but
        // doesn't error, so we only enforce CS8121 on the binding form.
        if (pattern.VariableName is null) return;

        var effectiveSubject = Nullable.GetUnderlyingType(subjectType) ?? subjectType;
        var effectiveTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (effectiveSubject == effectiveTarget) return;
        if (effectiveTarget.IsAssignableFrom(effectiveSubject)) return;
        if (effectiveSubject.IsAssignableFrom(effectiveTarget)) return;
        if (effectiveSubject.IsInterface || effectiveTarget.IsInterface) return;
        if (effectiveSubject == typeof(object) || effectiveTarget == typeof(object)) return;

        throw new AlderException(DiagnosticDescriptors.PatternTypeIncompatible, subjectType.Name, targetType.Name);
    }
}
