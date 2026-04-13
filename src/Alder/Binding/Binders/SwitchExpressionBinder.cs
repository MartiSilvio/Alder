using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(SwitchExpressionExpr))]
internal static class SwitchExpressionBinder
{
    public static BoundExpr Bind(SwitchExpressionExpr expr, BindingContext context, BinderContext binder)
    {
        var expression = binder.Bind(expr.Expression, context);
        var arms = expr.Arms
            .Select(arm =>
            {
                PatternValidator.Validate(arm.Pattern, expression.StaticType, context);
                var armScope = context.CreateChildScope();
                var whenGuard = arm.WhenGuard != null ? binder.Bind(arm.WhenGuard, armScope) : null;
                var value = binder.Bind(arm.Value, armScope);
                return new BoundSwitchExpressionArm(arm.Pattern, whenGuard, value);
            })
            .ToImmutableArray();

        // §11.3 / CS8510: detect pattern arms that are subsumed by a prior arm. An arm without
        // a `when` guard that covers the full subject type (var/discard/type-pattern for the
        // subject's static type) makes every subsequent arm unreachable. Relational patterns
        // subsume later relationals with the same direction and a weaker bound.
        for (var i = 1; i < arms.Length; i++)
        {
            for (var j = 0; j < i; j++)
            {
                var prior = arms[j];
                if (prior.WhenGuard != null) continue;
                if (!Subsumes(prior.Pattern, arms[i].Pattern, expression.StaticType, context)) continue;
                throw new AlderException(
                    DiagnosticDescriptors.UnreachableSwitchArm,
                    arms[i].Value.Span, null, null);
            }
        }

        var staticType = typeof(object);
        if (arms.Length > 0)
        {
            staticType = arms[0].Value.StaticType.ClrType;
            for (var i = 1; i < arms.Length; i++)
                staticType = BinaryBinder.GetCommonType(staticType, arms[i].Value.StaticType.ClrType);
        }

        return new BoundSwitchExpressionExpr(expression, arms, new BoundType(staticType));
    }

    private static bool Subsumes(Pattern prior, Pattern later, BoundType subjectType, BindingContext context)
    {
        if (IsCatchAll(prior, subjectType, context)) return true;
        return RelationalSubsumes(Unwrap(prior), Unwrap(later));
    }

    private static Pattern Unwrap(Pattern p) => p is ParenthesizedPattern pp ? Unwrap(pp.Inner) : p;

    // A pattern is "catch-all" when it matches every value of the subject's static type.
    private static bool IsCatchAll(Pattern pattern, BoundType subjectType, BindingContext context) => Unwrap(pattern) switch
    {
        VarPattern => true,
        DiscardPattern => true,
        TypePattern tp => TypePatternCoversAll(tp, subjectType, context),
        OrPattern or => IsCatchAll(or.Left, subjectType, context) || IsCatchAll(or.Right, subjectType, context),
        _ => false
    };

    private static bool TypePatternCoversAll(TypePattern tp, BoundType subjectType, BindingContext context)
    {
        if (subjectType is BoundUnknownType or BoundVoidType) return false;
        // Only non-nullable value-type subjects can be fully covered by a type pattern.
        // Reference types and nullable value types can be null, and a type pattern never matches null
        // (equivalent to `x is T`, which is false for null). So the `_` arm remains reachable.
        if (!subjectType.ClrType.IsValueType) return false;
        if (Nullable.GetUnderlyingType(subjectType.ClrType) is not null) return false;
        var target = context.RuntimeContext.TypeResolver.TryResolveType(tp.TypeToken.Lexeme);
        if (target is null) return false;
        return target.IsAssignableFrom(subjectType.ClrType);
    }

    // `prior` subsumes `later` when every value matched by `later` is also matched by `prior`.
    // Only handles same-direction relational patterns over identical-typed literal bounds.
    private static bool RelationalSubsumes(Pattern prior, Pattern later)
    {
        if (prior is not RelationalPattern ra || later is not RelationalPattern rb) return false;
        if (ra.Operand is not LiteralExpr la || rb.Operand is not LiteralExpr lb) return false;
        if (la.Value is not IComparable || lb.Value is not IComparable) return false;
        if (la.Value.GetType() != lb.Value.GetType()) return false;

        var cmp = ((IComparable)la.Value).CompareTo(lb.Value);
        return (ra.Operator.Type, rb.Operator.Type) switch
        {
            // `> a` covers `> b` ⇔ a ≤ b; `> a` covers `>= b` ⇔ a < b.
            (TokenType.Greater, TokenType.Greater) => cmp <= 0,
            (TokenType.Greater, TokenType.GreaterEqual) => cmp < 0,
            (TokenType.GreaterEqual, TokenType.Greater) => cmp <= 0,
            (TokenType.GreaterEqual, TokenType.GreaterEqual) => cmp <= 0,
            // `< a` covers `< b` ⇔ a ≥ b; `< a` covers `<= b` ⇔ a > b.
            (TokenType.Less, TokenType.Less) => cmp >= 0,
            (TokenType.Less, TokenType.LessEqual) => cmp > 0,
            (TokenType.LessEqual, TokenType.Less) => cmp >= 0,
            (TokenType.LessEqual, TokenType.LessEqual) => cmp >= 0,
            _ => false
        };
    }
}
