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

        if (b.Op.Type == TokenType.Plus)
            return LinqExpression.Call(AddMethod, left, right, _optionsParam, _currentContext);

        var method = b.Op.Type switch
        {
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

        if (v.DeclaredType != null)
        {
            value = LinqExpression.Call(
                ValidateAndCoerceTypeMethod,
                LinqExpression.Constant(v.DeclaredType.Value.Lexeme),
                value,
                LinqExpression.Constant(v.Name.Lexeme));
        }

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

        var opCall = ca.Op.Type switch
        {
            TokenType.PlusEqual => LinqExpression.Call(AddMethod, currentValue, rightValue, _optionsParam, _currentContext),
            TokenType.MinusEqual => LinqExpression.Call(SubtractMethod, currentValue, rightValue, _optionsParam),
            TokenType.StarEqual => LinqExpression.Call(MultiplyMethod, currentValue, rightValue, _optionsParam),
            TokenType.SlashEqual => LinqExpression.Call(DivideMethod, currentValue, rightValue, _optionsParam),
            TokenType.PercentEqual => LinqExpression.Call(ModuloMethod, currentValue, rightValue, _optionsParam),
            _ => throw new NotSupportedException($"Compound operator {ca.Op.Type}")
        };

        return LinqExpression.Block(
            new[] { temp },
            LinqExpression.Call(CheckAllowAssignmentMethod, _optionsParam,
                LinqExpression.Constant($"{name} {ca.Op.Lexeme} ...")),
            LinqExpression.Assign(temp, opCall),
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

        LinqExpression MakeOpCall(LinqExpression left) => isIncrement
            ? LinqExpression.Call(AddMethod, left, one, _optionsParam, _currentContext)
            : LinqExpression.Call(SubtractMethod, left, one, _optionsParam);

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

    #endregion
}
