using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compilation;

/// <summary>
/// Compiles pattern matching nodes (is-pattern, switch expression, all pattern types)
/// to Expression Trees. Receives shared state via CompilerContext.
/// </summary>
internal sealed class PatternCompilerUnit
{
    private readonly CompilerContext _ctx;

    // Lazily set reference for cross-unit dispatch
    private ExpressionCompilerUnit? _exprUnit;

    internal PatternCompilerUnit(CompilerContext ctx)
    {
        _ctx = ctx;
    }

    internal void SetExpressionUnit(ExpressionCompilerUnit exprUnit)
    {
        _exprUnit = exprUnit;
    }

    /// <summary>
    /// Compile a sub-expression by dispatching through the expression unit.
    /// </summary>
    private LinqExpression Compile(Expr expr) => _exprUnit!.Compile(expr);

    internal LinqExpression CompileIsPattern(IsPatternExpr isExpr)
    {
        var value = Compile(isExpr.Expression);
        return CompilePatternMatch(value, isExpr.Pattern);
    }

    /// <summary>
    /// Compiles a switch expression to an if-goto chain with per-arm scoping and SwitchExpressionException fallback.
    /// ECMA-334 §12.8.21: throws SwitchExpressionException when no arm matches.
    /// Each arm gets its own child context so pattern variables don't leak between arms.
    /// </summary>
    internal LinqExpression CompileSwitchExpression(SwitchExpressionExpr expr)
    {
        // Compile subject expression and cache in a variable
        var subjectValue = Compile(expr.Expression);
        var subjectVar = LinqExpression.Variable(typeof(object), "switchSubject");
        var resultVar = LinqExpression.Variable(typeof(object), "switchResult");
        var parentContextVar = LinqExpression.Variable(typeof(CsEvalContext), "switchParent");
        var endLabel = LinqExpression.Label(typeof(object), "switchEnd");

        var statements = new List<LinqExpression>();
        statements.Add(LinqExpression.Assign(subjectVar, subjectValue));

        // For each arm: enter scope, check pattern+guard, store result+goto end, exit scope
        foreach (var arm in expr.Arms)
        {
            // Save parent context and create child context for this arm
            statements.Add(LinqExpression.Assign(parentContextVar, _ctx.CurrentContext));
            statements.Add(LinqExpression.Assign(_ctx.CurrentContext,
                LinqExpression.Call(_ctx.CurrentContext, CompilerContext.CreateChildMethod)));

            // Compile pattern match against the cached subject
            var patternMatch = CompilePatternMatch(subjectVar, arm.Pattern);
            var matchBool = LinqExpression.Call(CompilerContext.RequireBooleanMethod, patternMatch);

            // Build the condition: pattern matches AND when guard (if present)
            LinqExpression condition = matchBool;
            if (arm.WhenGuard != null)
            {
                var guardResult = Compile(arm.WhenGuard);
                var guardBool = LinqExpression.Call(CompilerContext.RequireBooleanMethod, guardResult);
                condition = LinqExpression.AndAlso(matchBool, guardBool);
            }

            // Compile the arm's result expression
            var armResult = Compile(arm.Value);

            // If condition: store result, restore context, goto end
            statements.Add(LinqExpression.IfThen(condition,
                LinqExpression.Block(
                    LinqExpression.Assign(resultVar, armResult),
                    LinqExpression.Assign(_ctx.CurrentContext, parentContextVar),
                    LinqExpression.Goto(endLabel, resultVar, typeof(object)))));

            // Restore parent context (arm didn't match)
            statements.Add(LinqExpression.Assign(_ctx.CurrentContext, parentContextVar));
        }

        // ECMA-334 §12.8.21: throw SwitchExpressionException when no arm matches
        statements.Add(LinqExpression.Throw(LinqExpression.New(
            typeof(System.Runtime.CompilerServices.SwitchExpressionException).GetConstructor([typeof(object)])!,
            subjectVar)));

        // End label returns the result
        statements.Add(LinqExpression.Label(endLabel, LinqExpression.Default(typeof(object))));

        return LinqExpression.Block(
            typeof(object),
            [subjectVar, resultVar, parentContextVar],
            statements);
    }

    /// <summary>
    /// Compiles a pattern match for the given value expression against the given pattern.
    /// Returns a LinqExpression of type object (boxed bool).
    /// Dispatches to per-pattern-type methods for focused, maintainable compilation.
    /// </summary>
    internal LinqExpression CompilePatternMatch(LinqExpression value, Pattern pattern)
    {
        return pattern switch
        {
            ConstantPattern cp => CompileConstantPattern(value, cp),
            TypePattern tp => CompileTypePattern(value, tp),
            VarPattern vp => CompileVarPattern(value, vp),
            DiscardPattern => CompileDiscardPattern(),
            NotPattern np => CompileNotPattern(value, np),
            AndPattern ap => CompileAndPattern(value, ap),
            OrPattern op => CompileOrPattern(value, op),
            ParenthesizedPattern pp => CompilePatternMatch(value, pp.Inner),
            RelationalPattern rp => CompileRelationalPattern(value, rp),
            PropertyPattern pp => CompilePropertyPattern(value, pp),
            _ => throw new NotSupportedException($"Pattern type '{pattern.GetType().Name}' not yet compiled")
        };
    }

    private LinqExpression CompileConstantPattern(LinqExpression value, ConstantPattern cp)
    {
        // null check: value == null
        if (cp.Value is LiteralExpr { Value: null })
        {
            var isNull = LinqExpression.Equal(value, LinqExpression.Constant(null, typeof(object)));
            return LinqExpression.Convert(isNull, typeof(object));
        }
        // General constant: Operators.Equals(value, constant)
        var constValue = Compile(cp.Value);
        var equalsInfo = OperatorRegistry.GetBinaryOperator(TokenType.EqualEqual)!.Value;
        return LinqExpression.Call(equalsInfo.Method, value, constValue);
    }

    private LinqExpression CompileTypePattern(LinqExpression value, TypePattern tp)
    {
        // Resolve type via context's TypeResolver
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerContext.ResolveTypeMethod,
            LinqExpression.Constant(tp.TypeToken.Lexeme));

        var typeCheck = LinqExpression.Call(
            CompilerContext.IsTypeMethod,
            value,
            resolvedType);

        if (tp.VariableName == null)
        {
            return LinqExpression.Convert(typeCheck, typeof(object));
        }

        // x is type name - declare variable if match succeeds
        var typeValueVar = LinqExpression.Variable(typeof(object), "isValue");
        var matchVar = LinqExpression.Variable(typeof(bool), "isMatch");
        var resolvedTypeVar = LinqExpression.Variable(typeof(Type), "resolvedType");

        return LinqExpression.Block(
            typeof(object),
            [typeValueVar, matchVar, resolvedTypeVar],
            LinqExpression.Assign(typeValueVar, value),
            LinqExpression.Assign(resolvedTypeVar, LinqExpression.Call(
                _ctx.TypeResolverExpr,
                CompilerContext.ResolveTypeMethod,
                LinqExpression.Constant(tp.TypeToken.Lexeme))),
            LinqExpression.Assign(matchVar, LinqExpression.Call(
                CompilerContext.IsTypeMethod,
                typeValueVar,
                resolvedTypeVar)),
            LinqExpression.IfThen(
                matchVar,
                LinqExpression.Call(_ctx.CurrentContext, CompilerContext.DefineNewMethod,
                    LinqExpression.Constant(tp.VariableName.Value.Lexeme),
                    typeValueVar,
                    resolvedTypeVar)),
            LinqExpression.Convert(matchVar, typeof(object)));
    }

    private LinqExpression CompileVarPattern(LinqExpression value, VarPattern vp)
    {
        var valueVar = LinqExpression.Variable(typeof(object), "varValue");
        var runtimeType = LinqExpression.Condition(
            LinqExpression.NotEqual(valueVar, LinqExpression.Constant(null, typeof(object))),
            LinqExpression.Call(valueVar, typeof(object).GetMethod("GetType")!),
            LinqExpression.Constant(typeof(object), typeof(Type)));

        return LinqExpression.Block(
            typeof(object),
            [valueVar],
            LinqExpression.Assign(valueVar, value),
            LinqExpression.Call(_ctx.CurrentContext, CompilerContext.DefineNewMethod,
                LinqExpression.Constant(vp.VariableName.Lexeme),
                valueVar,
                runtimeType),
            LinqExpression.Constant(true, typeof(object)));
    }

    private static LinqExpression CompileDiscardPattern()
    {
        return LinqExpression.Constant(true, typeof(object));
    }

    private LinqExpression CompileNotPattern(LinqExpression value, NotPattern np)
    {
        var inner = CompilePatternMatch(value, np.Operand);
        var innerBool = LinqExpression.Call(CompilerContext.RequireBooleanMethod, inner);
        return LinqExpression.Convert(LinqExpression.Not(innerBool), typeof(object));
    }

    private LinqExpression CompileAndPattern(LinqExpression value, AndPattern ap)
    {
        var leftResult = CompilePatternMatch(value, ap.Left);
        var rightResult = CompilePatternMatch(value, ap.Right);
        var leftBool = LinqExpression.Call(CompilerContext.RequireBooleanMethod, leftResult);
        var rightBool = LinqExpression.Call(CompilerContext.RequireBooleanMethod, rightResult);
        return LinqExpression.Convert(LinqExpression.AndAlso(leftBool, rightBool), typeof(object));
    }

    private LinqExpression CompileOrPattern(LinqExpression value, OrPattern op)
    {
        var leftResult = CompilePatternMatch(value, op.Left);
        var rightResult = CompilePatternMatch(value, op.Right);
        var leftBool = LinqExpression.Call(CompilerContext.RequireBooleanMethod, leftResult);
        var rightBool = LinqExpression.Call(CompilerContext.RequireBooleanMethod, rightResult);
        return LinqExpression.Convert(LinqExpression.OrElse(leftBool, rightBool), typeof(object));
    }

    private LinqExpression CompileRelationalPattern(LinqExpression value, RelationalPattern rp)
    {
        // Compile the constant operand expression
        var operand = Compile(rp.Operand);

        // Use OperatorRegistry for comparison methods
        var opInfo = OperatorRegistry.GetBinaryOperator(rp.Operator.Type);
        if (opInfo == null)
            throw new NotSupportedException($"Relational pattern operator '{rp.Operator.Lexeme}'");

        var info = opInfo.Value;
        // Relational operators always take (left, right, options)
        return LinqExpression.Call(info.Method, value, operand, _ctx.OptionsParam);
    }

    private LinqExpression CompilePropertyPattern(LinqExpression value, PropertyPattern pp)
    {
        // Property patterns never match null
        var valueVar = LinqExpression.Variable(typeof(object), "propPatternValue");
        var matchResult = LinqExpression.Variable(typeof(bool), "propMatch");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(valueVar, value),
            LinqExpression.Assign(matchResult, LinqExpression.Constant(true))
        };

        // Null check: property patterns never match null
        var nullCheck = LinqExpression.Equal(valueVar, LinqExpression.Constant(null, typeof(object)));

        // Build the matching logic inside a block
        var matchStatements = new List<LinqExpression>();

        // Type check if type specified
        if (pp.TypeToken != null)
        {
            var propResolvedType = LinqExpression.Call(
                _ctx.TypeResolverExpr,
                CompilerContext.ResolveTypeMethod,
                LinqExpression.Constant(pp.TypeToken.Value.Lexeme));

            matchStatements.Add(LinqExpression.IfThen(
                LinqExpression.Not(LinqExpression.Call(
                    CompilerContext.IsTypeMethod,
                    valueVar,
                    propResolvedType)),
                LinqExpression.Assign(matchResult, LinqExpression.Constant(false))));
        }

        // Check each property sub-pattern
        foreach (var (name, subPattern) in pp.Properties)
        {
            // Get member value: MemberAccess.GetMember(value, name, options, false, context)
            var propValue = LinqExpression.Call(
                CompilerContext.GetMemberMethod,
                valueVar,
                LinqExpression.Constant(name.Lexeme),
                _ctx.OptionsParam,
                LinqExpression.Constant(false),
                _ctx.CurrentContext);

            // Recursively compile sub-pattern match
            var subMatch = CompilePatternMatch(propValue, subPattern);
            var subMatchBool = LinqExpression.Call(CompilerContext.RequireBooleanMethod, subMatch);

            matchStatements.Add(LinqExpression.IfThen(
                matchResult, // Only check if still matching
                LinqExpression.IfThen(
                    LinqExpression.Not(subMatchBool),
                    LinqExpression.Assign(matchResult, LinqExpression.Constant(false)))));
        }

        // Bind variable if present and match succeeded
        if (pp.VariableName != null)
        {
            var runtimeType = LinqExpression.Call(valueVar, typeof(object).GetMethod("GetType")!);
            matchStatements.Add(LinqExpression.IfThen(
                matchResult,
                LinqExpression.Call(_ctx.CurrentContext, CompilerContext.DefineNewMethod,
                    LinqExpression.Constant(pp.VariableName.Value.Lexeme),
                    valueVar,
                    runtimeType)));
        }

        matchStatements.Add(matchResult);

        // If null, return false; otherwise execute property checks
        var matchBlock = LinqExpression.Block(matchStatements);
        statements.Add(LinqExpression.IfThenElse(
            nullCheck,
            LinqExpression.Assign(matchResult, LinqExpression.Constant(false)),
            matchBlock));

        statements.Add(LinqExpression.Convert(matchResult, typeof(object)));
        return LinqExpression.Block(
            typeof(object),
            [valueVar, matchResult],
            statements);
    }
}
