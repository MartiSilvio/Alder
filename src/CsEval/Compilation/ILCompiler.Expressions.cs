using CsEval.Interpretation;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compilation;

internal sealed partial class ILCompiler
{
    #region Expression Compilation

    private LinqExpression CompileLiteral(LiteralExpr lit)
    {
        if (lit.Value == null)
            return LinqExpression.Constant(null, typeof(object));

        // Box value types to object
        return LinqExpression.Convert(
            LinqExpression.Constant(lit.Value, lit.Value.GetType()),
            typeof(object));
    }

    private LinqExpression CompileIdentifier(IdentifierExpr id)
    {
        return LinqExpression.Call(
            ResolveIdentifierMethod,
            LinqExpression.Constant(id.Name.Lexeme),
            _currentContext);
    }

    private LinqExpression CompileTypeReference(TypeReferenceExpr typeRef)
    {
        // Return the Type object for static member access
        return LinqExpression.Call(
            ResolveTypeNameMethod,
            LinqExpression.Constant(typeRef.TypeToken.Lexeme));
    }

    private LinqExpression CompileTypeof(TypeofExpr expr)
    {
        // Resolve the type at compile time and embed as constant
        var resolvedType = TypeHelpers.ResolveTypeByName(expr.TypeToken.Lexeme);
        return LinqExpression.Constant(resolvedType, typeof(object));
    }

    private LinqExpression CompileThrow(ThrowExpr expr)
    {
        var exceptionExpr = Compile(expr.Expression);
        // LinqExpression.Throw returns void, but we need object return type.
        // Wrap in block with unreachable default value to satisfy the type system.
        return LinqExpression.Block(
            typeof(object),
            LinqExpression.Throw(LinqExpression.Convert(exceptionExpr, typeof(Exception))),
            LinqExpression.Default(typeof(object)));
    }

    private LinqExpression CompileObjectCreation(ObjectCreationExpr expr)
    {
        // Compile arguments into an object[] array
        var argsInit = LinqExpression.NewArrayInit(
            typeof(object),
            expr.Arguments.Select(Compile));

        // Call RuntimeHelpers.InvokeConstructor(typeName, args)
        return LinqExpression.Call(
            InvokeConstructorMethod,
            LinqExpression.Constant(expr.TypeName),
            argsInit);
    }

    private LinqExpression CompileTuple(TupleExpr expr)
    {
        // Compile each element expression into an object[] array
        var elementsInit = LinqExpression.NewArrayInit(
            typeof(object),
            expr.Elements.Select(e => Compile(e.Expression)));

        // Call RuntimeHelpers.CreateTuple(elements)
        return LinqExpression.Call(
            CreateTupleMethod,
            elementsInit);
    }

    private LinqExpression CompileDeconstruction(DeconstructionExpr expr)
    {
        // Compile the value expression
        var value = Compile(expr.ValueExpression);

        // Create string[] of variable names
        var variableNamesArray = LinqExpression.NewArrayInit(
            typeof(string),
            expr.VariableNames.Select(n => LinqExpression.Constant(n)));

        // Call RuntimeHelpers.DeconstructTuple(value, variableNames, context)
        return LinqExpression.Call(
            DeconstructTupleMethod,
            value,
            variableNamesArray,
            _currentContext);
    }

    private LinqExpression CompileDefault(DefaultExpr def)
    {
        if (def.TypeToken == null)
            return LinqExpression.Constant(null, typeof(object));

        return LinqExpression.Call(
            GetDefaultValueMethod,
            LinqExpression.Constant(def.TypeToken.Value.Lexeme));
    }

    private LinqExpression CompileUnary(UnaryExpr u)
    {
        var operand = Compile(u.Right);

        return u.Op.Type switch
        {
            TokenType.Minus => LinqExpression.Call(NegateMethod, operand),
            TokenType.Plus => LinqExpression.Call(UnaryPlusMethod, operand),
            TokenType.Bang => LinqExpression.Call(LogicalNotMethod, operand),
            TokenType.Tilde => LinqExpression.Call(BitwiseNotMethod, operand),
            _ => throw new NotSupportedException($"Unary operator {u.Op.Type}")
        };
    }

    private LinqExpression CompileCast(CastExpr cast)
    {
        var value = Compile(cast.Expression);
        var sourceStaticType = _typeInferrer.Infer(cast.Expression);
        return LinqExpression.Call(
            ExplicitCastMethod,
            value,
            LinqExpression.Constant(cast.TargetType.Lexeme),
            LinqExpression.Constant(sourceStaticType, typeof(Type)));
    }

    private LinqExpression CompileIsPattern(IsPatternExpr isExpr)
    {
        var value = Compile(isExpr.Expression);
        return CompilePatternMatch(value, isExpr.Pattern);
    }

    /// <summary>
    /// Compiles a switch expression to an if-goto chain with per-arm scoping and SwitchExpressionException fallback.
    /// ECMA-334 §12.8.21: throws SwitchExpressionException when no arm matches.
    /// Each arm gets its own child context so pattern variables don't leak between arms.
    /// </summary>
    private LinqExpression CompileSwitchExpression(SwitchExpressionExpr expr)
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
            statements.Add(LinqExpression.Assign(parentContextVar, _currentContext));
            statements.Add(LinqExpression.Assign(_currentContext,
                LinqExpression.Call(_currentContext, CreateChildMethod)));

            // Compile pattern match against the cached subject
            var patternMatch = CompilePatternMatch(subjectVar, arm.Pattern);
            var matchBool = LinqExpression.Call(RequireBooleanMethod, patternMatch);

            // Build the condition: pattern matches AND when guard (if present)
            LinqExpression condition = matchBool;
            if (arm.WhenGuard != null)
            {
                var guardResult = Compile(arm.WhenGuard);
                var guardBool = LinqExpression.Call(RequireBooleanMethod, guardResult);
                condition = LinqExpression.AndAlso(matchBool, guardBool);
            }

            // Compile the arm's result expression
            var armResult = Compile(arm.Value);

            // If condition: store result, restore context, goto end
            statements.Add(LinqExpression.IfThen(condition,
                LinqExpression.Block(
                    LinqExpression.Assign(resultVar, armResult),
                    LinqExpression.Assign(_currentContext, parentContextVar),
                    LinqExpression.Goto(endLabel, resultVar, typeof(object)))));

            // Restore parent context (arm didn't match)
            statements.Add(LinqExpression.Assign(_currentContext, parentContextVar));
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
    /// </summary>
    private LinqExpression CompilePatternMatch(LinqExpression value, Pattern pattern)
    {
        switch (pattern)
        {
            case ConstantPattern cp:
            {
                // null check: value == null
                if (cp.Value is LiteralExpr { Value: null })
                {
                    var isNull = LinqExpression.Equal(value, LinqExpression.Constant(null, typeof(object)));
                    return LinqExpression.Convert(isNull, typeof(object));
                }
                // General constant: Operators.Equals(value, constant)
                var constValue = Compile(cp.Value);
                return LinqExpression.Call(EqualsMethod, value, constValue);
            }

            case TypePattern tp:
            {
                var typeCheck = LinqExpression.Call(
                    IsTypeMethod,
                    value,
                    LinqExpression.Constant(tp.TypeToken.Lexeme));

                if (tp.VariableName == null)
                {
                    return LinqExpression.Convert(typeCheck, typeof(object));
                }

                // x is type name - declare variable if match succeeds
                var typeValueVar = LinqExpression.Variable(typeof(object), "isValue");
                var matchVar = LinqExpression.Variable(typeof(bool), "isMatch");

                return LinqExpression.Block(
                    typeof(object),
                    [typeValueVar, matchVar],
                    LinqExpression.Assign(typeValueVar, value),
                    LinqExpression.Assign(matchVar, LinqExpression.Call(
                        IsTypeMethod,
                        typeValueVar,
                        LinqExpression.Constant(tp.TypeToken.Lexeme))),
                    LinqExpression.IfThen(
                        matchVar,
                        LinqExpression.Call(_currentContext, DefineNewMethod,
                            LinqExpression.Constant(tp.VariableName.Value.Lexeme),
                            typeValueVar,
                            LinqExpression.Call(ResolveTypeNameMethod, LinqExpression.Constant(tp.TypeToken.Lexeme)))),
                    LinqExpression.Convert(matchVar, typeof(object)));
            }

            case VarPattern vp:
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
                    LinqExpression.Call(_currentContext, DefineNewMethod,
                        LinqExpression.Constant(vp.VariableName.Lexeme),
                        valueVar,
                        runtimeType),
                    LinqExpression.Constant(true, typeof(object)));
            }

            case DiscardPattern:
                return LinqExpression.Constant(true, typeof(object));

            case NotPattern np:
            {
                var inner = CompilePatternMatch(value, np.Operand);
                var innerBool = LinqExpression.Call(RequireBooleanMethod, inner);
                return LinqExpression.Convert(LinqExpression.Not(innerBool), typeof(object));
            }

            case AndPattern ap:
            {
                var leftResult = CompilePatternMatch(value, ap.Left);
                var rightResult = CompilePatternMatch(value, ap.Right);
                var leftBool = LinqExpression.Call(RequireBooleanMethod, leftResult);
                var rightBool = LinqExpression.Call(RequireBooleanMethod, rightResult);
                return LinqExpression.Convert(LinqExpression.AndAlso(leftBool, rightBool), typeof(object));
            }

            case OrPattern op:
            {
                var leftResult = CompilePatternMatch(value, op.Left);
                var rightResult = CompilePatternMatch(value, op.Right);
                var leftBool = LinqExpression.Call(RequireBooleanMethod, leftResult);
                var rightBool = LinqExpression.Call(RequireBooleanMethod, rightResult);
                return LinqExpression.Convert(LinqExpression.OrElse(leftBool, rightBool), typeof(object));
            }

            case ParenthesizedPattern pp:
                return CompilePatternMatch(value, pp.Inner);

            case RelationalPattern rp:
            {
                // Compile the constant operand expression
                var operand = Compile(rp.Operand);

                // Dispatch on operator type using Operators comparison methods
                var comparison = rp.Operator.Type switch
                {
                    TokenType.Less => LinqExpression.Call(LessThanMethod, value, operand, _optionsParam),
                    TokenType.LessEqual => LinqExpression.Call(LessThanOrEqualMethod, value, operand, _optionsParam),
                    TokenType.Greater => LinqExpression.Call(GreaterThanMethod, value, operand, _optionsParam),
                    TokenType.GreaterEqual => LinqExpression.Call(GreaterThanOrEqualMethod, value, operand, _optionsParam),
                    _ => throw new NotSupportedException($"Relational pattern operator '{rp.Operator.Lexeme}'")
                };

                return comparison;
            }

            case PropertyPattern pp:
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
                    matchStatements.Add(LinqExpression.IfThen(
                        LinqExpression.Not(LinqExpression.Call(
                            IsTypeMethod,
                            valueVar,
                            LinqExpression.Constant(pp.TypeToken.Value.Lexeme))),
                        LinqExpression.Assign(matchResult, LinqExpression.Constant(false))));
                }

                // Check each property sub-pattern
                foreach (var (name, subPattern) in pp.Properties)
                {
                    // Get member value: MemberAccess.GetMember(value, name, options, false, context)
                    var propValue = LinqExpression.Call(
                        GetMemberMethod,
                        valueVar,
                        LinqExpression.Constant(name.Lexeme),
                        _optionsParam,
                        LinqExpression.Constant(false),
                        _currentContext);

                    // Recursively compile sub-pattern match
                    var subMatch = CompilePatternMatch(propValue, subPattern);
                    var subMatchBool = LinqExpression.Call(RequireBooleanMethod, subMatch);

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
                        LinqExpression.Call(_currentContext, DefineNewMethod,
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

            default:
                throw new NotSupportedException($"Pattern type '{pattern.GetType().Name}' not yet compiled");
        }
    }

    private LinqExpression CompileAs(AsExpr asExpr)
    {
        var value = Compile(asExpr.Expression);
        return LinqExpression.Call(
            TryAsMethod,
            value,
            LinqExpression.Constant(asExpr.TargetType.Lexeme));
    }

    private LinqExpression CompileBinary(BinaryExpr b)
    {
        var left = Compile(b.Left);
        var right = Compile(b.Right);

        // ECMA-334 §10.2.11: Implicit constant expression conversions.
        // At IL-compile time, pre-promote constant operands so the runtime
        // NumericDispatch.PromoteOperands sees matching types (e.g., uint+uint
        // instead of uint+int, avoiding Rule 6 promotion to long).
        ApplyConstantPromotion(b, ref left, ref right);

        if (b.Op.Type == TokenType.Plus)
            return LinqExpression.Call(AddMethod, left, right, _optionsParam, _currentContext);

        if (b.Op.Type == TokenType.LessLess)
            return LinqExpression.Call(LeftShiftMethod, left, right);
        if (b.Op.Type == TokenType.GreaterGreater)
            return LinqExpression.Call(RightShiftMethod, left, right);

        return b.Op.Type switch
        {
            TokenType.Less => LinqExpression.Call(LessThanMethod, left, right, _optionsParam),
            TokenType.LessEqual => LinqExpression.Call(LessThanOrEqualMethod, left, right, _optionsParam),
            TokenType.Greater => LinqExpression.Call(GreaterThanMethod, left, right, _optionsParam),
            TokenType.GreaterEqual => LinqExpression.Call(GreaterThanOrEqualMethod, left, right, _optionsParam),
            TokenType.Minus => LinqExpression.Call(SubtractMethod, left, right),
            TokenType.Star => LinqExpression.Call(MultiplyMethod, left, right),
            TokenType.Slash => LinqExpression.Call(DivideMethod, left, right),
            TokenType.Percent => LinqExpression.Call(ModuloMethod, left, right),
            TokenType.EqualEqual or TokenType.EqualEqualEqual => LinqExpression.Call(EqualsMethod, left, right),
            TokenType.BangEqual or TokenType.BangEqualEqual => LinqExpression.Call(NotEqualsMethod, left, right),
            TokenType.Amp => LinqExpression.Call(BitwiseAndMethod, left, right),
            TokenType.Pipe => LinqExpression.Call(BitwiseOrMethod, left, right),
            TokenType.Caret => LinqExpression.Call(BitwiseXorMethod, left, right),
            _ => throw new NotSupportedException($"Binary operator {b.Op.Type}")
        };
    }

    /// <summary>
    /// ECMA-334 §10.2.11: At IL-compile time, pre-promote constant literal operands.
    /// Since literal values are known at compile time, we can replace the compiled
    /// LinqExpression.Constant with a promoted-type constant (e.g., int 3 -> uint 3).
    /// </summary>
    private static void ApplyConstantPromotion(BinaryExpr b, ref LinqExpression left, ref LinqExpression right)
    {
        var leftLiteral = b.Left as LiteralExpr;
        var rightLiteral = b.Right as LiteralExpr;

        bool leftIsConstant = leftLiteral is { IsConstant: true };
        bool rightIsConstant = rightLiteral is { IsConstant: true };

        if (!leftIsConstant && !rightIsConstant)
            return;

        // We need both operand values to call TryConstantPromotion.
        // Both sides must be non-null literals for this compile-time optimization.
        object? leftVal = leftLiteral?.Value;
        object? rightVal = rightLiteral?.Value;

        if (leftVal == null || rightVal == null) return;

        var promoted = NumericDispatch.TryConstantPromotion(
            leftVal, leftIsConstant, rightVal, rightIsConstant);

        if (promoted != null)
        {
            left = LinqExpression.Convert(
                LinqExpression.Constant(promoted.Value.Left, promoted.Value.Left.GetType()),
                typeof(object));
            right = LinqExpression.Convert(
                LinqExpression.Constant(promoted.Value.Right, promoted.Value.Right.GetType()),
                typeof(object));
        }
    }

    private LinqExpression CompileLogical(LogicalExpr l)
    {
        var left = Compile(l.Left);
        var right = Compile(l.Right);

        var leftTruthy = LinqExpression.Call(RequireBooleanMethod, left);
        var rightTruthy = LinqExpression.Call(RequireBooleanMethod, right);

        // Short-circuit evaluation
        LinqExpression result = l.Op.Type switch
        {
            TokenType.PipePipe or TokenType.Or => LinqExpression.OrElse(leftTruthy, rightTruthy),
            TokenType.AmpAmp or TokenType.And => LinqExpression.AndAlso(leftTruthy, rightTruthy),
            _ => throw new NotSupportedException($"Logical operator {l.Op.Type}")
        };

        return LinqExpression.Convert(result, typeof(object));
    }

    private LinqExpression CompileConditional(ConditionalExpr c)
    {
        var condition = LinqExpression.Call(RequireBooleanMethod, Compile(c.Condition));
        var thenBranch = Compile(c.ThenBranch);
        var elseBranch = Compile(c.ElseBranch);

        // Get static types for promotion check (ECMA-334 §12.18)
        var thenType = _typeInferrer.Infer(c.ThenBranch);
        var elseType = _typeInferrer.Infer(c.ElseBranch);

        var result = LinqExpression.Condition(condition, thenBranch, elseBranch);

        // Apply type promotion at compile time if both branches are numeric with different types
        if (thenType != typeof(object) && elseType != typeof(object) &&
            TypeHelpers.IsArithmetic(thenType) && TypeHelpers.IsArithmetic(elseType) &&
            thenType != elseType)
        {
            var promotionType = NumericDispatch.GetResultType(thenType, elseType);
            var promoteMethod = typeof(NumericDispatch).GetMethod(nameof(NumericDispatch.PromoteToType))!;
            return LinqExpression.Call(promoteMethod, result, LinqExpression.Constant(promotionType, typeof(Type)));
        }

        return result;
    }

    private LinqExpression CompileNullCoalesce(NullCoalesceExpr n)
    {
        var left = Compile(n.Left);
        var right = Compile(n.Right);

        return LinqExpression.Coalesce(left, right);
    }

    private LinqExpression CompileMemberAccess(MemberAccessExpr m)
    {
        var obj = Compile(m.Object);

        return LinqExpression.Call(
            GetMemberMethod,
            obj,
            LinqExpression.Constant(m.Name.Lexeme),
            _optionsParam,
            LinqExpression.Constant(m.NullSafe),
            _currentContext);
    }

    private LinqExpression CompileIndexAccess(IndexAccessExpr expr)
    {
        var target = Compile(expr.Object);

        if (expr.NullSafe)
        {
            // arr?[i] - null-safe index access
            var targetVar = LinqExpression.Variable(typeof(object), "target");
            var index = Compile(expr.Index);
            return LinqExpression.Block(
                typeof(object),
                [targetVar],
                LinqExpression.Assign(targetVar, target),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    LinqExpression.Call(GetIndexMethod, targetVar, index, _optionsParam)));
        }

        var indexValue = Compile(expr.Index);
        return LinqExpression.Call(GetIndexMethod, target, indexValue, _optionsParam);
    }

    private LinqExpression CompileVariableDecl(VariableDeclExpr v)
    {
        var value = Compile(v.Initializer);
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var inferredType = LinqExpression.Variable(typeof(Type), "inferredType");

        if (v.DeclaredType != null)
        {
            value = LinqExpression.Call(
                ValidateAndCoerceTypeMethod,
                LinqExpression.Constant(v.DeclaredType.Value.Lexeme),
                value,
                LinqExpression.Constant(v.Name.Lexeme));
        }

        LinqExpression getInferredType;
        if (v.DeclaredType != null)
        {
            getInferredType = LinqExpression.Call(ResolveTypeNameMethod, LinqExpression.Constant(v.DeclaredType.Value.Lexeme));
        }
        else
        {
            getInferredType = LinqExpression.Condition(
                LinqExpression.NotEqual(temp, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Call(temp, typeof(object).GetMethod("GetType")!),
                LinqExpression.Constant(typeof(object), typeof(Type)));
        }

        return LinqExpression.Block(
            new[] { temp, inferredType },
            LinqExpression.Assign(temp, value),
            LinqExpression.Assign(inferredType, getInferredType),
            LinqExpression.Call(_currentContext, DefineNewMethod,
                LinqExpression.Constant(v.Name.Lexeme), temp, inferredType),
            temp);
    }

    private LinqExpression CompileAssign(AssignExpr a)
    {
        var name = a.Name.Lexeme;
        var value = Compile(a.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");

        return LinqExpression.Block(
            new[] { temp },
            // Check sandbox allows assignment
            LinqExpression.Call(CheckAllowAssignmentMethod, _optionsParam,
                LinqExpression.Constant($"{name} = ...")),
            LinqExpression.Assign(temp, value),
            LinqExpression.Call(_currentContext, SetMethod,
                LinqExpression.Constant(name), temp),
            temp);
    }

    private LinqExpression CompileCompoundAssign(CompoundAssignExpr ca)
    {
        var name = ca.Name.Lexeme;
        var currentValue = CompileIdentifier(new IdentifierExpr(ca.Name));
        var rightValueExpr = Compile(ca.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var rightTemp = LinqExpression.Variable(typeof(object), "rightTemp");

        var opCall = ca.Op.Type switch
        {
            TokenType.PlusEqual => LinqExpression.Call(AddMethod, currentValue, rightTemp, _optionsParam, _currentContext),
            TokenType.MinusEqual => LinqExpression.Call(SubtractMethod, currentValue, rightTemp),
            TokenType.StarEqual => LinqExpression.Call(MultiplyMethod, currentValue, rightTemp),
            TokenType.SlashEqual => LinqExpression.Call(DivideMethod, currentValue, rightTemp),
            TokenType.PercentEqual => LinqExpression.Call(ModuloMethod, currentValue, rightTemp),
            TokenType.AmpEqual => LinqExpression.Call(BitwiseAndMethod, currentValue, rightTemp),
            TokenType.PipeEqual => LinqExpression.Call(BitwiseOrMethod, currentValue, rightTemp),
            TokenType.CaretEqual => LinqExpression.Call(BitwiseXorMethod, currentValue, rightTemp),
            TokenType.LessLessEqual => LinqExpression.Call(LeftShiftMethod, currentValue, rightTemp),
            TokenType.GreaterGreaterEqual => LinqExpression.Call(RightShiftMethod, currentValue, rightTemp),
            _ => throw new NotSupportedException($"Compound operator {ca.Op.Type}")
        };

        var validateCall = LinqExpression.Call(ValidateCompoundAssignmentMethod,
            LinqExpression.Constant(name), opCall, rightTemp, _currentContext);

        return LinqExpression.Block(
            new[] { temp, rightTemp },
            LinqExpression.Call(CheckAllowAssignmentMethod, _optionsParam,
                LinqExpression.Constant($"{name} {ca.Op.Lexeme} ...")),
            LinqExpression.Assign(rightTemp, rightValueExpr),
            LinqExpression.Assign(temp, validateCall),
            LinqExpression.Call(_currentContext, SetMethod,
                LinqExpression.Constant(name), temp),
            temp);
    }

    private LinqExpression CompileIndexAssign(IndexAssignExpr expr)
    {
        var target = Compile(expr.Object);
        var index = Compile(expr.Index);
        var value = Compile(expr.Value);

        // Use a temp for index since we need it for both the check and the set
        var indexTemp = LinqExpression.Variable(typeof(object), "idx");
        var check = LinqExpression.Call(CheckAllowIndexSetMethod, _optionsParam, indexTemp);
        var set = LinqExpression.Call(SetIndexMethod, target, indexTemp, value);

        return LinqExpression.Block(
            new[] { indexTemp },
            LinqExpression.Assign(indexTemp, index),
            check,
            set,
            value);
    }

    private LinqExpression CompileIncrementDecrement(IncrementDecrementExpr inc)
    {
        var name = inc.Name.Lexeme;
        var isIncrement = inc.Op.Type == TokenType.PlusPlus;
        var currentValue = CompileIdentifier(new IdentifierExpr(inc.Name));
        var one = LinqExpression.Convert(LinqExpression.Constant(1), typeof(object));
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var original = LinqExpression.Variable(typeof(object), "original");

        LinqExpression MakeOpCall(LinqExpression left) => isIncrement
            ? LinqExpression.Call(AddMethod, left, one, _optionsParam, _currentContext)
            : LinqExpression.Call(SubtractMethod, left, one);

        var checkExpr = LinqExpression.Call(CheckAllowAssignmentMethod, _optionsParam,
            LinqExpression.Constant(isIncrement ? $"{name}++" : $"{name}--"));

        if (inc.IsPrefix)
        {
            return LinqExpression.Block(
                new[] { temp },
                checkExpr,
                LinqExpression.Assign(temp, MakeOpCall(currentValue)),
                LinqExpression.Call(_currentContext, SetMethod,
                    LinqExpression.Constant(name), temp),
                temp);
        }
        else
        {
            return LinqExpression.Block(
                new[] { temp, original },
                checkExpr,
                LinqExpression.Assign(original, currentValue),
                LinqExpression.Assign(temp, MakeOpCall(original)),
                LinqExpression.Call(_currentContext, SetMethod,
                    LinqExpression.Constant(name), temp),
                original);
        }
    }

    private LinqExpression CompileCall(CallExpr call)
    {
        // Compile arguments into an object[] array, wrapping named arguments in NamedArg
        var argsVar = LinqExpression.Variable(typeof(object?[]), "args");
        var argsInit = LinqExpression.NewArrayInit(
            typeof(object),
            call.Arguments.Select(CompileArgument));

        var typeArgsExpr = call.TypeArguments != null
            ? LinqExpression.Constant(call.TypeArguments, typeof(IReadOnlyList<string>))
            : LinqExpression.Constant(null, typeof(IReadOnlyList<string>));

        // Check if this is a member access call (target.Method(args))
        if (call.Callee is MemberAccessExpr memberAccess)
        {
            var target = Compile(memberAccess.Object);
            var methodName = memberAccess.Name.Lexeme;

            return LinqExpression.Block(
                new[] { argsVar },
                LinqExpression.Assign(argsVar, argsInit),
                LinqExpression.Call(
                    InvokeMemberCallMethod,
                    target,
                    LinqExpression.Constant(methodName),
                    argsVar,
                    LinqExpression.Constant(memberAccess.NullSafe),
                    _currentContext,
                    _optionsParam,
                    _ctParam,
                    _argumentTransformerParam,
                    typeArgsExpr));
        }

        // General call: evaluate callee and invoke
        var callee = Compile(call.Callee);
        return LinqExpression.Block(
            new[] { argsVar },
            LinqExpression.Assign(argsVar, argsInit),
            LinqExpression.Call(
                InvokeCallMethod,
                callee,
                argsVar,
                _currentContext,
                _optionsParam,
                _ctParam,
                _argumentTransformerParam,
                typeArgsExpr));
    }

    private LinqExpression CompileArgument(Expr arg)
    {
        if (arg is NamedArgumentExpr namedArg)
        {
            // Wrap named argument in NamedArg: new NamedArg(name, value)
            return LinqExpression.Convert(
                LinqExpression.New(
                    NamedArgCtor,
                    LinqExpression.Constant(namedArg.Name.Lexeme),
                    Compile(namedArg.Value)),
                typeof(object));
        }
        return Compile(arg);
    }

    private static readonly ConstructorInfo CompiledLambdaValueCtor =
        typeof(CompiledLambdaValue).GetConstructor([
            typeof(List<string>),
            typeof(Func<object?[], CsEvalContext, object?>),
            typeof(CsEvalContext)
        ])!;

    private static readonly MethodInfo GetLambdaArgMethod =
        typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetLambdaArg))!;

    private LinqExpression CompileLambda(LambdaExpr lambda)
    {
        var parameterNames = lambda.Parameters.Select(p => p.Lexeme).ToList();

        // Create parameter list constant
        var listInit = LinqExpression.ListInit(
            LinqExpression.New(typeof(List<string>)),
            parameterNames.Select(p => LinqExpression.ElementInit(
                typeof(List<string>).GetMethod("Add")!,
                LinqExpression.Constant(p))));

        // Create the compiled lambda body
        var argsParam = LinqExpression.Parameter(typeof(object?[]), "args");
        var closureParam = LinqExpression.Parameter(typeof(CsEvalContext), "closure");

        // Create a child context for the lambda body
        var childContextVar = LinqExpression.Variable(typeof(CsEvalContext), "childContext");

        // Build statements to:
        // 1. Create child context from closure
        // 2. Define each parameter in the child context
        // 3. Execute the body
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(childContextVar,
                LinqExpression.Call(closureParam, CreateChildMethod))
        };

        // Define each parameter in the child context
        for (var i = 0; i < parameterNames.Count; i++)
        {
            statements.Add(LinqExpression.Call(childContextVar, DefineMethod,
                LinqExpression.Constant(parameterNames[i]),
                LinqExpression.Call(GetLambdaArgMethod, argsParam, LinqExpression.Constant(i))));
        }

        // Save the current context and swap to the child context for compiling the body
        var savedContext = _currentContext;
        _currentContext = childContextVar;

        try
        {
            // Compile the lambda body
            var compiledBody = Compile(lambda.Body);
            statements.Add(compiledBody);
        }
        finally
        {
            _currentContext = savedContext;
        }

        var lambdaBody = LinqExpression.Block(
            typeof(object),
            [childContextVar],
            statements);

        // Create the delegate: Func<object?[], CsEvalContext, object?>
        var compiledDelegate = LinqExpression.Lambda<Func<object?[], CsEvalContext, object?>>(
            lambdaBody,
            argsParam,
            closureParam);

        // Create CompiledLambdaValue(parameters, compiledBody, closure)
        return LinqExpression.New(
            CompiledLambdaValueCtor,
            listInit,
            compiledDelegate,
            _currentContext);
    }

    private LinqExpression CompileArrayLiteral(ArrayLiteralExpr expr)
    {
        var listVar = LinqExpression.Variable(typeof(List<object?>), "list");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(listVar, LinqExpression.New(ListCtor))
        };

        foreach (var element in expr.Elements)
        {
            if (element is SpreadExpr spread)
            {
                var spreadValue = Compile(spread.Expression);
                statements.Add(LinqExpression.Call(SpreadIntoListMethod, listVar, spreadValue));
            }
            else
            {
                statements.Add(LinqExpression.Call(listVar, ListAddMethod, Compile(element)));
            }
        }

        statements.Add(LinqExpression.Call(CreateTypedListMethod, listVar));
        return LinqExpression.Block(new[] { listVar }, statements);
    }

    private LinqExpression CompileObjectLiteral(ObjectLiteralExpr expr)
    {
        var dictVar = LinqExpression.Variable(typeof(IDictionary<string, object?>), "dict");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(dictVar, LinqExpression.New(ExpandoObjectCtor))
        };

        var dictItemProperty = typeof(IDictionary<string, object?>).GetProperty("Item")!;

        foreach (var (key, value) in expr.Properties)
        {
            if (key.Type == TokenType.DotDotDot && value is SpreadExpr spread)
            {
                var spreadValue = Compile(spread.Expression);
                statements.Add(LinqExpression.Call(SpreadIntoDictMethod, dictVar, spreadValue, _currentContext));
            }
            else
            {
                statements.Add(LinqExpression.Assign(
                    LinqExpression.Property(dictVar, dictItemProperty, LinqExpression.Constant(key.Lexeme)),
                    Compile(value)));
            }
        }

        statements.Add(LinqExpression.Convert(dictVar, typeof(object)));
        return LinqExpression.Block(new[] { dictVar }, statements);
    }

    private LinqExpression CompileInterpolatedString(InterpolatedStringExpr expr)
    {
        var sbVar = LinqExpression.Variable(typeof(StringBuilder), "sb");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(sbVar, LinqExpression.New(StringBuilderCtor))
        };

        foreach (var part in expr.Parts)
        {
            switch (part)
            {
                case TextPart text:
                    statements.Add(LinqExpression.Call(sbVar, StringBuilderAppendMethod,
                        LinqExpression.Constant(text.Text)));
                    break;
                case ExpressionPart exprPart:
                    var value = Compile(exprPart.Expression);
                    var valueAsString = LinqExpression.Condition(
                        LinqExpression.Equal(value, LinqExpression.Constant(null, typeof(object))),
                        LinqExpression.Constant(""),
                        LinqExpression.Call(value, ObjectToStringMethod));
                    statements.Add(LinqExpression.Call(sbVar, StringBuilderAppendMethod, valueAsString));
                    break;
            }
        }

        statements.Add(LinqExpression.Convert(
            LinqExpression.Call(sbVar, StringBuilderToStringMethod),
            typeof(object)));
        return LinqExpression.Block(new[] { sbVar }, statements);
    }

    private LinqExpression CompileMemberAssign(MemberAssignExpr expr)
    {
        var target = Compile(expr.Object);
        var value = Compile(expr.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");

        return LinqExpression.Block(
            new[] { temp },
            LinqExpression.Call(CheckAllowAssignmentMethod, _optionsParam,
                LinqExpression.Constant($"{expr.Name.Lexeme} = ...")),
            LinqExpression.Assign(temp, value),
            LinqExpression.Call(SetMemberMethod, target,
                LinqExpression.Constant(expr.Name.Lexeme), temp, _optionsParam, _currentContext),
            temp);
    }

    private LinqExpression CompileNullCoalesceAssign(NullCoalesceAssignExpr expr)
    {
        var name = expr.Name.Lexeme;
        var currentValue = CompileIdentifier(new IdentifierExpr(expr.Name));
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var result = LinqExpression.Variable(typeof(object), "result");

        var newValue = Compile(expr.Value);

        return LinqExpression.Block(
            new[] { temp, result },
            LinqExpression.Call(CheckNullCoalesceAssignAllowedMethod,
                LinqExpression.Constant(name), _currentContext),
            LinqExpression.Assign(temp, currentValue),
            LinqExpression.IfThenElse(
                LinqExpression.NotEqual(temp, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Assign(result, temp),
                LinqExpression.Block(
                    LinqExpression.Call(CheckAllowAssignmentMethod, _optionsParam,
                        LinqExpression.Constant($"{name} ??= ...")),
                    LinqExpression.Assign(result, newValue),
                    LinqExpression.Call(_currentContext, SetMethod,
                        LinqExpression.Constant(name), result))),
            result);
    }

    #endregion
}
