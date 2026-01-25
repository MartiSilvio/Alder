using CsEval.Parsing;

namespace CsEval.Evaluation.Compiler;

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
            _currentContext,
            GetMethod,
            LinqExpression.Constant(id.Name.Lexeme));
    }

    private LinqExpression CompileUnary(UnaryExpr u)
    {
        var operand = Compile(u.Right);

        return u.Op.Type switch
        {
            TokenType.Minus => LinqExpression.Call(NegateMethod, operand),
            TokenType.Bang => LinqExpression.Convert(
                LinqExpression.Not(LinqExpression.Call(IsTruthyMethod, operand)),
                typeof(object)),
            _ => throw new NotSupportedException($"Unary operator {u.Op.Type}")
        };
    }

    private LinqExpression CompileBinary(BinaryExpr b)
    {
        var left = Compile(b.Left);
        var right = Compile(b.Right);

        var method = b.Op.Type switch
        {
            TokenType.Plus => AddMethod,
            TokenType.Minus => SubtractMethod,
            TokenType.Star => MultiplyMethod,
            TokenType.Slash => DivideMethod,
            TokenType.Percent => ModuloMethod,
            TokenType.EqualEqual or TokenType.EqualEqualEqual => EqualsMethod,
            TokenType.BangEqual or TokenType.BangEqualEqual => NotEqualsMethod,
            TokenType.Less => LessThanMethod,
            TokenType.LessEqual => LessThanOrEqualMethod,
            TokenType.Greater => GreaterThanMethod,
            TokenType.GreaterEqual => GreaterThanOrEqualMethod,
            TokenType.Amp => BitwiseAndMethod,
            TokenType.Pipe => BitwiseOrMethod,
            TokenType.Caret => BitwiseXorMethod,
            _ => throw new NotSupportedException($"Binary operator {b.Op.Type}")
        };

        return LinqExpression.Call(method, left, right, _optionsParam);
    }

    private LinqExpression CompileLogical(LogicalExpr l)
    {
        var left = Compile(l.Left);
        var right = Compile(l.Right);

        var leftTruthy = LinqExpression.Call(IsTruthyMethod, left);
        var rightTruthy = LinqExpression.Call(IsTruthyMethod, right);

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
        var condition = LinqExpression.Call(IsTruthyMethod, Compile(c.Condition));
        var thenBranch = Compile(c.ThenBranch);
        var elseBranch = Compile(c.ElseBranch);

        return LinqExpression.Condition(condition, thenBranch, elseBranch);
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
        var index = Compile(expr.Index);
        return LinqExpression.Call(GetIndexMethod, target, index, _optionsParam);
    }

    private LinqExpression CompileVariableDecl(VariableDeclExpr v)
    {
        var value = Compile(v.Initializer);
        var temp = LinqExpression.Variable(typeof(object), "temp");

        return LinqExpression.Block(
            new[] { temp },
            LinqExpression.Assign(temp, value),
            LinqExpression.Call(_currentContext, DefineMethod,
                LinqExpression.Constant(v.Name.Lexeme), temp),
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
        var rightValue = Compile(ca.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");

        var method = ca.Op.Type switch
        {
            TokenType.PlusEqual => AddMethod,
            TokenType.MinusEqual => SubtractMethod,
            TokenType.StarEqual => MultiplyMethod,
            TokenType.SlashEqual => DivideMethod,
            TokenType.PercentEqual => ModuloMethod,
            _ => throw new NotSupportedException($"Compound operator {ca.Op.Type}")
        };

        return LinqExpression.Block(
            new[] { temp },
            LinqExpression.Call(CheckAllowAssignmentMethod, _optionsParam,
                LinqExpression.Constant($"{name} {ca.Op.Lexeme} ...")),
            LinqExpression.Assign(temp, LinqExpression.Call(method, currentValue, rightValue, _optionsParam)),
            LinqExpression.Call(_currentContext, SetMethod,
                LinqExpression.Constant(name), temp),
            temp);
    }

    private LinqExpression CompileIndexAssign(IndexAssignExpr expr)
    {
        var target = Compile(expr.Object);
        var index = Compile(expr.Index);
        var value = Compile(expr.Value);
        
        var checkStr = LinqExpression.Constant("Index assignment");
        var check = LinqExpression.Call(CheckAllowAssignmentMethod, _optionsParam, checkStr);

        var set = LinqExpression.Call(SetIndexMethod, target, index, value);
        
        return LinqExpression.Block(check, set, value);
    }

    private LinqExpression CompileIncrementDecrement(IncrementDecrementExpr inc)
    {
        var name = inc.Name.Lexeme;
        var isIncrement = inc.Op.Type == TokenType.PlusPlus;
        var currentValue = CompileIdentifier(new IdentifierExpr(inc.Name));
        var one = LinqExpression.Convert(LinqExpression.Constant(1), typeof(object));
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var original = LinqExpression.Variable(typeof(object), "original");

        var method = isIncrement ? AddMethod : SubtractMethod;

        // Check sandbox
        var checkExpr = LinqExpression.Call(CheckAllowAssignmentMethod, _optionsParam,
            LinqExpression.Constant(isIncrement ? $"{name}++" : $"{name}--"));

        if (inc.IsPrefix)
        {
            // Prefix: return new value
            return LinqExpression.Block(
                new[] { temp },
                checkExpr,
                LinqExpression.Assign(temp, LinqExpression.Call(method, currentValue, one, _optionsParam)),
                LinqExpression.Call(_currentContext, SetMethod,
                    LinqExpression.Constant(name), temp),
                temp);
        }
        else
        {
            // Postfix: return original value
            return LinqExpression.Block(
                new[] { temp, original },
                checkExpr,
                LinqExpression.Assign(original, currentValue),
                LinqExpression.Assign(temp, LinqExpression.Call(method, original, one, _optionsParam)),
                LinqExpression.Call(_currentContext, SetMethod,
                    LinqExpression.Constant(name), temp),
                original);
        }
    }

    #endregion
}
