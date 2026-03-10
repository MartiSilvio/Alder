using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compiled.Compilation.CompilerUnits;

internal sealed class ExpressionAssignmentCompiler
{
    private readonly ExpressionCompilerUnit _owner;
    private readonly ExpressionBinaryCompiler _binary;

    internal ExpressionAssignmentCompiler(
        ExpressionCompilerUnit owner,
        ExpressionBinaryCompiler binary)
    {
        _owner = owner;
        _binary = binary;
    }

    internal LinqExpression CompileVariableDecl(VariableDeclExpr v)
    {
        var value = _owner.Compile(v.Initializer);
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var inferredType = LinqExpression.Variable(typeof(Type), "inferredType");

        if (v.DeclaredType != null)
        {
            // Resolve type via context's TypeResolver
            var resolvedDeclType = LinqExpression.Call(
                _owner.Context.TypeResolverExpr,
                CompilerReflectionCache.ResolveTypeMethod,
                LinqExpression.Constant(v.DeclaredType.Value.Lexeme));

            var declTypeVar = LinqExpression.Variable(typeof(Type), "declType");

            value = LinqExpression.Block(
                new[] { declTypeVar },
                LinqExpression.Assign(declTypeVar, resolvedDeclType),
                LinqExpression.Call(
                    CompilerReflectionCache.ValidateAndCoerceTypeMethod,
                    declTypeVar,
                    value,
                    LinqExpression.Constant(v.Name.Lexeme)));
        }

        LinqExpression getInferredType;
        if (v.DeclaredType != null)
        {
            // Resolve type via context's TypeResolver
            getInferredType = LinqExpression.Call(
                _owner.Context.TypeResolverExpr,
                CompilerReflectionCache.ResolveTypeMethod,
                LinqExpression.Constant(v.DeclaredType.Value.Lexeme));
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
            LinqExpression.Condition(
                LinqExpression.Equal(
                    LinqExpression.Constant(v.Name.Lexeme),
                    LinqExpression.Constant("_")),
                LinqExpression.Block(
                    LinqExpression.Call(_owner.Context.CurrentContext, CompilerReflectionCache.DefineMethod,
                        LinqExpression.Constant(v.Name.Lexeme), temp),
                    temp),
                LinqExpression.Block(
                    LinqExpression.Call(_owner.Context.CurrentContext, CompilerReflectionCache.DefineNewMethod,
                        LinqExpression.Constant(v.Name.Lexeme), temp, inferredType),
                    temp)));
    }

    internal LinqExpression CompileAssign(AssignExpr a)
    {
        var name = a.Name.Lexeme;
        var value = _owner.Compile(a.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");

        return LinqExpression.Block(
            new[] { temp },
            // Check sandbox allows assignment
            LinqExpression.Call(CompilerReflectionCache.CheckAllowAssignmentMethod, _owner.Context.OptionsParam,
                LinqExpression.Constant($"{name} = ...")),
            LinqExpression.Assign(temp, value),
            LinqExpression.Call(_owner.Context.CurrentContext, CompilerReflectionCache.SetMethod,
                LinqExpression.Constant(name), temp),
            temp);
    }

    internal LinqExpression CompileCompoundAssign(CompoundAssignExpr ca)
    {
        var name = ca.Name.Lexeme;
        var currentValue = _owner.CompileIdentifier(new IdentifierExpr(ca.Name));
        var rightValueExpr = _owner.Compile(ca.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var rightTemp = LinqExpression.Variable(typeof(object), "rightTemp");

        // Map compound op to base binary op
        if (!OperatorRegistry.CompoundToBaseOperator.TryGetValue(ca.Op.Type, out var baseOp))
            throw new NotSupportedException($"Compound operator {ca.Op.Type}");

        var opInfo = OperatorRegistry.GetBinaryOperator(baseOp);
        if (opInfo == null)
            throw new NotSupportedException($"Binary operator for compound {ca.Op.Type}");

        LinqExpression opCall = _binary.EmitBinaryOpCall(opInfo.Value, currentValue, rightTemp);

        var validateCall = LinqExpression.Call(CompilerReflectionCache.ValidateCompoundAssignmentMethod,
            LinqExpression.Constant(name), opCall, rightTemp, _owner.Context.CurrentContext);

        return LinqExpression.Block(
            new[] { temp, rightTemp },
            LinqExpression.Call(CompilerReflectionCache.CheckAllowAssignmentMethod, _owner.Context.OptionsParam,
                LinqExpression.Constant($"{name} {ca.Op.Lexeme} ...")),
            LinqExpression.Assign(rightTemp, rightValueExpr),
            LinqExpression.Assign(temp, validateCall),
            LinqExpression.Call(_owner.Context.CurrentContext, CompilerReflectionCache.SetMethod,
                LinqExpression.Constant(name), temp),
            temp);
    }

    internal LinqExpression CompileMemberCompoundAssign(MemberCompoundAssignExpr expr)
    {
        var objExpr = _owner.Compile(expr.Object);
        var rightValueExpr = _owner.Compile(expr.Value);
        var objTemp = LinqExpression.Variable(typeof(object), "obj");
        var rightTemp = LinqExpression.Variable(typeof(object), "rightTemp");
        var temp = LinqExpression.Variable(typeof(object), "temp");

        // Get current value via MemberAccess.GetMember
        var currentValue = LinqExpression.Call(
            CompilerReflectionCache.GetMemberMethod,
            objTemp,
            LinqExpression.Constant(expr.MemberName),
            _owner.Context.OptionsParam,
            LinqExpression.Constant(false),
            _owner.Context.CurrentContext);

        // Map compound op to base binary op
        if (!OperatorRegistry.CompoundToBaseOperator.TryGetValue(expr.Operator, out var baseOp))
            throw new NotSupportedException($"Compound operator {expr.Operator}");

        var opInfo = OperatorRegistry.GetBinaryOperator(baseOp);
        if (opInfo == null)
            throw new NotSupportedException($"Binary operator for compound {expr.Operator}");

        LinqExpression opCall = _binary.EmitBinaryOpCall(opInfo.Value, currentValue, rightTemp);

        // Set via MemberAccess.SetMember
        var setCall = LinqExpression.Call(CompilerReflectionCache.SetMemberMethod,
            objTemp, LinqExpression.Constant(expr.MemberName), temp, _owner.Context.OptionsParam, _owner.Context.CurrentContext);

        return LinqExpression.Block(
            new[] { objTemp, rightTemp, temp },
            LinqExpression.Assign(objTemp, objExpr),
            LinqExpression.Assign(rightTemp, rightValueExpr),
            LinqExpression.Assign(temp, opCall),
            setCall,
            temp);
    }

    internal LinqExpression CompileIndexCompoundAssign(IndexCompoundAssignExpr expr)
    {
        var objExpr = _owner.Compile(expr.Object);
        var indexExpr = _owner.Compile(expr.Index);
        var rightValueExpr = _owner.Compile(expr.Value);
        var objTemp = LinqExpression.Variable(typeof(object), "obj");
        var indexTemp = LinqExpression.Variable(typeof(object), "idx");
        var rightTemp = LinqExpression.Variable(typeof(object), "rightTemp");
        var temp = LinqExpression.Variable(typeof(object), "temp");

        // Get current value via MemberAccess.GetIndex
        var currentValue = LinqExpression.Call(
            CompilerReflectionCache.GetIndexMethod,
            objTemp, indexTemp, _owner.Context.OptionsParam);

        // Map compound op to base binary op
        if (!OperatorRegistry.CompoundToBaseOperator.TryGetValue(expr.Operator, out var baseOp))
            throw new NotSupportedException($"Compound operator {expr.Operator}");

        var opInfo = OperatorRegistry.GetBinaryOperator(baseOp);
        if (opInfo == null)
            throw new NotSupportedException($"Binary operator for compound {expr.Operator}");

        LinqExpression opCall = _binary.EmitBinaryOpCall(opInfo.Value, currentValue, rightTemp);

        // Set via MemberAccess.SetIndex
        var setCall = LinqExpression.Call(CompilerReflectionCache.SetIndexMethod,
            objTemp, indexTemp, temp, _owner.Context.OptionsParam);

        return LinqExpression.Block(
            new[] { objTemp, indexTemp, rightTemp, temp },
            LinqExpression.Assign(objTemp, objExpr),
            LinqExpression.Assign(indexTemp, indexExpr),
            LinqExpression.Assign(rightTemp, rightValueExpr),
            LinqExpression.Assign(temp, opCall),
            setCall,
            temp);
    }

    internal LinqExpression CompileIndexAssign(IndexAssignExpr expr)
    {
        var target = _owner.Compile(expr.Object);
        var index = _owner.Compile(expr.Index);
        var value = _owner.Compile(expr.Value);

        var indexTemp = LinqExpression.Variable(typeof(object), "idx");
        var valueTemp = LinqExpression.Variable(typeof(object), "val");
        var check = LinqExpression.Call(CompilerReflectionCache.CheckAllowIndexSetMethod, _owner.Context.OptionsParam, indexTemp);
        var set = LinqExpression.Call(CompilerReflectionCache.SetIndexMethod, target, indexTemp, valueTemp, _owner.Context.OptionsParam);

        return LinqExpression.Block(
            new[] { indexTemp, valueTemp },
            LinqExpression.Assign(indexTemp, index),
            LinqExpression.Assign(valueTemp, value),
            check,
            set,
            valueTemp);
    }

    internal LinqExpression CompileIncrementDecrement(IncrementDecrementExpr inc)
    {
        var name = inc.Name.Lexeme;
        var isIncrement = inc.Op.Type == TokenType.PlusPlus;
        var currentValue = _owner.CompileIdentifier(new IdentifierExpr(inc.Name));
        var one = LinqExpression.Convert(LinqExpression.Constant(1), typeof(object));
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var original = LinqExpression.Variable(typeof(object), "original");

        // Get Add/Subtract info from registry
        var addInfo = OperatorRegistry.GetBinaryOperator(TokenType.Plus)!.Value;
        var subInfo = OperatorRegistry.GetBinaryOperator(TokenType.Minus)!.Value;

        LinqExpression MakeOpCall(LinqExpression left) => isIncrement
            ? _binary.EmitBinaryOpCall(addInfo, left, one)
            : _binary.EmitBinaryOpCall(subInfo, left, one);

        var checkExpr = LinqExpression.Call(CompilerReflectionCache.CheckAllowAssignmentMethod, _owner.Context.OptionsParam,
            LinqExpression.Constant(isIncrement ? $"{name}++" : $"{name}--"));

        if (inc.IsPrefix)
        {
            return LinqExpression.Block(
                new[] { temp },
                checkExpr,
                LinqExpression.Assign(temp, MakeOpCall(currentValue)),
                LinqExpression.Call(_owner.Context.CurrentContext, CompilerReflectionCache.SetMethod,
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
                LinqExpression.Call(_owner.Context.CurrentContext, CompilerReflectionCache.SetMethod,
                    LinqExpression.Constant(name), temp),
                original);
        }
    }

    internal LinqExpression CompileMemberNullCoalesceAssign(MemberNullCoalesceAssignExpr expr)
    {
        var objExpr = _owner.Compile(expr.Object);
        var objTemp = LinqExpression.Variable(typeof(object), "obj");
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var result = LinqExpression.Variable(typeof(object), "result");

        var currentValue = LinqExpression.Call(
            CompilerReflectionCache.GetMemberMethod,
            objTemp,
            LinqExpression.Constant(expr.MemberName),
            _owner.Context.OptionsParam,
            LinqExpression.Constant(false),
            _owner.Context.CurrentContext);

        var newValue = _owner.Compile(expr.Value);

        var setCall = LinqExpression.Call(CompilerReflectionCache.SetMemberMethod,
            objTemp, LinqExpression.Constant(expr.MemberName), result, _owner.Context.OptionsParam, _owner.Context.CurrentContext);

        return LinqExpression.Block(
            new[] { objTemp, temp, result },
            LinqExpression.Assign(objTemp, objExpr),
            LinqExpression.Assign(temp, currentValue),
            LinqExpression.IfThenElse(
                LinqExpression.NotEqual(temp, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Assign(result, temp),
                LinqExpression.Block(
                    LinqExpression.Assign(result, newValue),
                    setCall)),
            result);
    }

    internal LinqExpression CompileIndexNullCoalesceAssign(IndexNullCoalesceAssignExpr expr)
    {
        var objExpr = _owner.Compile(expr.Object);
        var indexExpr = _owner.Compile(expr.Index);
        var objTemp = LinqExpression.Variable(typeof(object), "obj");
        var indexTemp = LinqExpression.Variable(typeof(object), "idx");
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var result = LinqExpression.Variable(typeof(object), "result");

        var currentValue = LinqExpression.Call(
            CompilerReflectionCache.GetIndexMethod,
            objTemp, indexTemp, _owner.Context.OptionsParam);

        var newValue = _owner.Compile(expr.Value);

        var setCall = LinqExpression.Call(CompilerReflectionCache.SetIndexMethod,
            objTemp, indexTemp, result, _owner.Context.OptionsParam);

        return LinqExpression.Block(
            new[] { objTemp, indexTemp, temp, result },
            LinqExpression.Assign(objTemp, objExpr),
            LinqExpression.Assign(indexTemp, indexExpr),
            LinqExpression.Assign(temp, currentValue),
            LinqExpression.IfThenElse(
                LinqExpression.NotEqual(temp, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Assign(result, temp),
                LinqExpression.Block(
                    LinqExpression.Assign(result, newValue),
                    setCall)),
            result);
    }

    internal LinqExpression CompileMemberIncrement(MemberIncrementExpr expr)
    {
        var objExpr = _owner.Compile(expr.Object);
        var objTemp = LinqExpression.Variable(typeof(object), "obj");
        var original = LinqExpression.Variable(typeof(object), "original");
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var one = LinqExpression.Convert(LinqExpression.Constant(1), typeof(object));

        var currentValue = LinqExpression.Call(
            CompilerReflectionCache.GetMemberMethod,
            objTemp,
            LinqExpression.Constant(expr.MemberName),
            _owner.Context.OptionsParam,
            LinqExpression.Constant(false),
            _owner.Context.CurrentContext);

        var addInfo = OperatorRegistry.GetBinaryOperator(TokenType.Plus)!.Value;
        var subInfo = OperatorRegistry.GetBinaryOperator(TokenType.Minus)!.Value;

        LinqExpression MakeOpCall(LinqExpression left) => expr.IsIncrement
            ? _binary.EmitBinaryOpCall(addInfo, left, one)
            : _binary.EmitBinaryOpCall(subInfo, left, one);

        var setCall = LinqExpression.Call(CompilerReflectionCache.SetMemberMethod,
            objTemp, LinqExpression.Constant(expr.MemberName), temp, _owner.Context.OptionsParam, _owner.Context.CurrentContext);

        if (expr.IsPrefix)
        {
            return LinqExpression.Block(
                new[] { objTemp, temp },
                LinqExpression.Assign(objTemp, objExpr),
                LinqExpression.Assign(temp, MakeOpCall(currentValue)),
                setCall,
                temp);
        }
        else
        {
            return LinqExpression.Block(
                new[] { objTemp, original, temp },
                LinqExpression.Assign(objTemp, objExpr),
                LinqExpression.Assign(original, currentValue),
                LinqExpression.Assign(temp, MakeOpCall(original)),
                setCall,
                original);
        }
    }

    internal LinqExpression CompileIndexIncrement(IndexIncrementExpr expr)
    {
        var objExpr = _owner.Compile(expr.Object);
        var indexExpr = _owner.Compile(expr.Index);
        var objTemp = LinqExpression.Variable(typeof(object), "obj");
        var indexTemp = LinqExpression.Variable(typeof(object), "idx");
        var original = LinqExpression.Variable(typeof(object), "original");
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var one = LinqExpression.Convert(LinqExpression.Constant(1), typeof(object));

        var currentValue = LinqExpression.Call(
            CompilerReflectionCache.GetIndexMethod,
            objTemp, indexTemp, _owner.Context.OptionsParam);

        var addInfo = OperatorRegistry.GetBinaryOperator(TokenType.Plus)!.Value;
        var subInfo = OperatorRegistry.GetBinaryOperator(TokenType.Minus)!.Value;

        LinqExpression MakeOpCall(LinqExpression left) => expr.IsIncrement
            ? _binary.EmitBinaryOpCall(addInfo, left, one)
            : _binary.EmitBinaryOpCall(subInfo, left, one);

        var setCall = LinqExpression.Call(CompilerReflectionCache.SetIndexMethod,
            objTemp, indexTemp, temp, _owner.Context.OptionsParam);

        if (expr.IsPrefix)
        {
            return LinqExpression.Block(
                new[] { objTemp, indexTemp, temp },
                LinqExpression.Assign(objTemp, objExpr),
                LinqExpression.Assign(indexTemp, indexExpr),
                LinqExpression.Assign(temp, MakeOpCall(currentValue)),
                setCall,
                temp);
        }
        else
        {
            return LinqExpression.Block(
                new[] { objTemp, indexTemp, original, temp },
                LinqExpression.Assign(objTemp, objExpr),
                LinqExpression.Assign(indexTemp, indexExpr),
                LinqExpression.Assign(original, currentValue),
                LinqExpression.Assign(temp, MakeOpCall(original)),
                setCall,
                original);
        }
    }

}
