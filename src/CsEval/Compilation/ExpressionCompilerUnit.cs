using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compilation;

/// <summary>
/// Compiles expression nodes (literals, binary, unary, member access, calls, lambdas, etc.)
/// to Expression Trees. Receives shared state via CompilerContext.
/// </summary>
internal sealed class ExpressionCompilerUnit
{
    private readonly CompilerContext _ctx;
    private readonly PatternCompilerUnit _patternUnit;

    // Lazily set references for cross-unit dispatch
    private ControlFlowCompilerUnit? _controlUnit;

    internal ExpressionCompilerUnit(CompilerContext ctx, PatternCompilerUnit patternUnit)
    {
        _ctx = ctx;
        _patternUnit = patternUnit;
    }

    internal void SetControlFlowUnit(ControlFlowCompilerUnit controlUnit)
    {
        _controlUnit = controlUnit;
    }

    /// <summary>
    /// Compile a sub-expression by dispatching through the central Compile method.
    /// </summary>
    internal LinqExpression Compile(Expr expr) =>
        CompilerContext.Compile(_ctx, expr, this, _controlUnit!, _patternUnit);

    internal LinqExpression CompileLiteral(LiteralExpr lit)
    {
        if (lit.Value == null)
            return LinqExpression.Constant(null, typeof(object));

        // Box value types to object
        return LinqExpression.Convert(
            LinqExpression.Constant(lit.Value, lit.Value.GetType()),
            typeof(object));
    }

    internal LinqExpression CompileIdentifier(IdentifierExpr id)
    {
        return LinqExpression.Call(
            CompilerContext.ResolveIdentifierMethod,
            LinqExpression.Constant(id.Name.Lexeme),
            _ctx.CurrentContext);
    }

    internal LinqExpression CompileTypeReference(TypeReferenceExpr typeRef)
    {
        // Return the Type object for static member access via context's TypeResolver
        return LinqExpression.Convert(
            LinqExpression.Call(
                _ctx.TypeResolverExpr,
                CompilerContext.ResolveTypeMethod,
                LinqExpression.Constant(typeRef.TypeToken.Lexeme)),
            typeof(object));
    }

    internal LinqExpression CompileTypeof(TypeofExpr expr)
    {
        // Resolve the type at compile time using the context's TypeResolver and embed as constant
        var resolvedType = _ctx.Context.TypeResolver.ResolveType(expr.TypeToken.Lexeme);
        return LinqExpression.Constant(resolvedType, typeof(object));
    }

    internal LinqExpression CompileSizeof(SizeofExpr expr)
    {
        int size = expr.TypeName switch
        {
            "bool" or "Boolean" or "System.Boolean" => 1,
            "byte" or "Byte" or "System.Byte" => 1,
            "sbyte" or "SByte" or "System.SByte" => 1,
            "char" or "Char" or "System.Char" => 2,
            "short" or "Int16" or "System.Int16" => 2,
            "ushort" or "UInt16" or "System.UInt16" => 2,
            "int" or "Int32" or "System.Int32" => 4,
            "uint" or "UInt32" or "System.UInt32" => 4,
            "float" or "Single" or "System.Single" => 4,
            "long" or "Int64" or "System.Int64" => 8,
            "ulong" or "UInt64" or "System.UInt64" => 8,
            "double" or "Double" or "System.Double" => 8,
            "decimal" or "Decimal" or "System.Decimal" => 16,
            _ => throw new CsEvalException($"Cannot take the sizeof of type '{expr.TypeName}'")
        };
        return LinqExpression.Constant((object)size, typeof(object));
    }

    internal LinqExpression CompileThrow(ThrowExpr expr)
    {
        var exceptionExpr = Compile(expr.Expression);
        // LinqExpression.Throw returns void, but we need object return type.
        // Wrap in block with unreachable default value to satisfy the type system.
        return LinqExpression.Block(
            typeof(object),
            LinqExpression.Throw(LinqExpression.Convert(exceptionExpr, typeof(Exception))),
            LinqExpression.Default(typeof(object)));
    }

    /// <summary>
    /// Compiles parameterless throw; (rethrow) using the Expression Trees rethrow instruction.
    /// ECMA-334 §13.10.6 -- only valid inside a catch block body.
    /// </summary>
    internal static LinqExpression CompileThrowStatement()
    {
        // Expression.Rethrow generates the IL rethrow instruction.
        // Must be typed to match the try/catch return type (typeof(object)).
        return LinqExpression.Rethrow(typeof(object));
    }

    internal LinqExpression CompileObjectCreation(ObjectCreationExpr expr)
    {
        // Compile arguments into an object[] array
        var argsInit = LinqExpression.NewArrayInit(
            typeof(object),
            expr.Arguments.Select(Compile));

        // Resolve type via context's TypeResolver then call RuntimeHelpers.InvokeConstructor(type, args)
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerContext.ResolveTypeMethod,
            LinqExpression.Constant(expr.TypeName));

        LinqExpression result = LinqExpression.Call(
            CompilerContext.InvokeConstructorMethod,
            resolvedType,
            argsInit);

        if (expr.Initializer != null)
        {
            // Apply each initializer entry sequentially
            var objVar = LinqExpression.Variable(typeof(object), "initObj");
            var statements = new List<LinqExpression> { LinqExpression.Assign(objVar, result) };

            foreach (var entry in expr.Initializer.Entries)
            {
                var value = Compile(entry.Value);
                if (entry.PropertyName != null)
                {
                    statements.Add(LinqExpression.Call(
                        CompilerContext.ApplyPropertyInitializerMethod,
                        objVar,
                        LinqExpression.Constant(entry.PropertyName),
                        value,
                        _ctx.OptionsParam,
                        _ctx.CurrentContext));
                }
                else
                {
                    statements.Add(LinqExpression.Call(
                        CompilerContext.ApplyCollectionInitializerMethod,
                        objVar,
                        value));
                }
            }

            statements.Add(objVar);
            result = LinqExpression.Block(typeof(object), [objVar], statements);
        }

        return result;
    }

    internal LinqExpression CompileTypedArrayCreation(TypedArrayCreationExpr expr)
    {
        var size = Compile(expr.Size);

        // Resolve element type via context's TypeResolver then call RuntimeHelpers.CreateTypedArray(elementType, sizeValue)
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerContext.ResolveTypeMethod,
            LinqExpression.Constant(expr.ElementTypeName));

        return LinqExpression.Call(
            CompilerContext.CreateTypedArrayFromTypeNameMethod,
            resolvedType,
            size);
    }

    internal LinqExpression CompileMultiDimTypedArrayCreation(MultiDimTypedArrayCreationExpr expr)
    {
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerContext.ResolveTypeMethod,
            LinqExpression.Constant(expr.ElementTypeName));

        var sizesArray = LinqExpression.NewArrayInit(
            typeof(object),
            expr.Sizes.Select(s => Compile(s)));

        return LinqExpression.Call(
            CompilerContext.CreateMultiDimArrayMethod,
            resolvedType,
            sizesArray);
    }

    internal LinqExpression CompileMultiDimIndexAccess(MultiDimIndexAccessExpr expr)
    {
        var obj = Compile(expr.Object);
        var indicesArray = LinqExpression.NewArrayInit(
            typeof(object),
            expr.Indices.Select(i => Compile(i)));

        if (expr.NullSafe)
        {
            var targetVar = LinqExpression.Variable(typeof(object), "target");
            return LinqExpression.Block(
                typeof(object),
                [targetVar],
                LinqExpression.Assign(targetVar, obj),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    LinqExpression.Call(CompilerContext.MultiDimArrayGetMethod, targetVar, indicesArray)));
        }

        return LinqExpression.Call(CompilerContext.MultiDimArrayGetMethod, obj, indicesArray);
    }

    internal LinqExpression CompileMultiDimIndexAssign(MultiDimIndexAssignExpr expr)
    {
        var obj = Compile(expr.Object);
        var indicesArray = LinqExpression.NewArrayInit(
            typeof(object),
            expr.Indices.Select(i => Compile(i)));
        var value = Compile(expr.Value);

        return LinqExpression.Call(CompilerContext.MultiDimArraySetMethod, obj, indicesArray, value);
    }

    internal LinqExpression CompileTypedArrayLiteral(TypedArrayLiteralExpr expr)
    {
        // Compile the array literal elements (returns a typed array from CreateTypedArray)
        var arrayLiteral = CompileArrayLiteral(expr.Elements);

        // Resolve the target element type
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerContext.ResolveTypeMethod,
            LinqExpression.Constant(expr.ElementTypeName));

        // Convert the source array to the typed array T[]
        // ConvertArrayToTyped accepts object (any Array) so no cast to object?[] needed
        return LinqExpression.Call(
            CompilerContext.ConvertArrayToTypedMethod,
            arrayLiteral,
            resolvedType);
    }

    internal LinqExpression CompileTuple(TupleExpr expr)
    {
        // Compile each element expression into an object[] array
        var elementsInit = LinqExpression.NewArrayInit(
            typeof(object),
            expr.Elements.Select(e => Compile(e.Expression)));

        // Call RuntimeHelpers.CreateTuple(elements)
        return LinqExpression.Call(
            CompilerContext.CreateTupleMethod,
            elementsInit);
    }

    internal LinqExpression CompileDeconstruction(DeconstructionExpr expr)
    {
        // Compile the value expression
        var value = Compile(expr.ValueExpression);

        // Create string[] of variable names
        var variableNamesArray = LinqExpression.NewArrayInit(
            typeof(string),
            expr.VariableNames.Select(n => LinqExpression.Constant(n)));

        // Call RuntimeHelpers.DeconstructTuple(value, variableNames, context)
        return LinqExpression.Call(
            CompilerContext.DeconstructTupleMethod,
            value,
            variableNamesArray,
            _ctx.CurrentContext);
    }

    internal LinqExpression CompileDefault(DefaultExpr def)
    {
        if (def.TypeToken == null)
            return LinqExpression.Constant(null, typeof(object));

        // Resolve type via context's TypeResolver then call TypeHelpers.GetDefaultValue(Type)
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerContext.ResolveTypeMethod,
            LinqExpression.Constant(def.TypeToken.Value.Lexeme));

        return LinqExpression.Call(
            CompilerContext.GetDefaultValueMethod,
            resolvedType);
    }

    internal LinqExpression CompileUnary(UnaryExpr u)
    {
        var operand = Compile(u.Right);

        var method = OperatorRegistry.GetUnaryMethod(u.Op.Type);
        if (method == null)
            throw new NotSupportedException($"Unary operator {u.Op.Type}");

        return LinqExpression.Call(method, operand);
    }

    internal LinqExpression CompileCast(CastExpr cast)
    {
        var value = Compile(cast.Expression);
        var sourceStaticType = _ctx.TypeInferrer.Infer(cast.Expression);

        // Only enforce unboxing semantics when the source expression is a simple identifier
        // with a known explicit type (e.g., object x = 42). For complex expressions (binary,
        // grouping, index access, etc.), the TypeInferrer defaults to typeof(object) which would
        // incorrectly block valid numeric conversions like (int)dynamicDouble.
        var effectiveSourceType = cast.Expression is IdentifierExpr ? sourceStaticType : null;

        // Resolve target type via context's TypeResolver
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerContext.ResolveTypeMethod,
            LinqExpression.Constant(cast.TargetType.Lexeme));

        return LinqExpression.Call(
            CompilerContext.ExplicitCastMethod,
            value,
            resolvedType,
            LinqExpression.Constant(effectiveSourceType, typeof(Type)));
    }

    internal LinqExpression CompileAs(AsExpr asExpr)
    {
        var value = Compile(asExpr.Expression);

        // Resolve target type via context's TypeResolver
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerContext.ResolveTypeMethod,
            LinqExpression.Constant(asExpr.TargetType.Lexeme));

        return LinqExpression.Call(
            CompilerContext.TryAsMethod,
            value,
            resolvedType);
    }

    internal LinqExpression CompileBinary(BinaryExpr b)
    {
        var left = Compile(b.Left);
        var right = Compile(b.Right);

        // ECMA-334 §10.2.11: Implicit constant expression conversions.
        ApplyConstantPromotion(b, ref left, ref right);

        var opInfo = OperatorRegistry.GetBinaryOperator(b.Op.Type);
        if (opInfo == null)
            throw new NotSupportedException($"Binary operator {b.Op.Type}");

        var info = opInfo.Value;
        return info.Signature switch
        {
            OperatorRegistry.BinaryOpSignature.TwoArgs =>
                LinqExpression.Call(info.Method, left, right),
            OperatorRegistry.BinaryOpSignature.WithOptions =>
                LinqExpression.Call(info.Method, left, right, _ctx.OptionsParam),
            OperatorRegistry.BinaryOpSignature.WithOptionsAndContext =>
                LinqExpression.Call(info.Method, left, right, _ctx.OptionsParam, _ctx.CurrentContext),
            _ => throw new NotSupportedException($"Unknown binary op signature {info.Signature}")
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

    internal LinqExpression CompileLogical(LogicalExpr l)
    {
        var left = Compile(l.Left);
        var right = Compile(l.Right);

        var leftTruthy = LinqExpression.Call(CompilerContext.RequireBooleanMethod, left);
        var rightTruthy = LinqExpression.Call(CompilerContext.RequireBooleanMethod, right);

        // Short-circuit evaluation
        LinqExpression result = l.Op.Type switch
        {
            TokenType.PipePipe or TokenType.Or => LinqExpression.OrElse(leftTruthy, rightTruthy),
            TokenType.AmpAmp or TokenType.And => LinqExpression.AndAlso(leftTruthy, rightTruthy),
            _ => throw new NotSupportedException($"Logical operator {l.Op.Type}")
        };

        return LinqExpression.Convert(result, typeof(object));
    }

    internal LinqExpression CompileConditional(ConditionalExpr c)
    {
        var condition = LinqExpression.Call(CompilerContext.RequireBooleanMethod, Compile(c.Condition));
        var thenBranch = Compile(c.ThenBranch);
        var elseBranch = Compile(c.ElseBranch);

        // Get static types for promotion check (ECMA-334 §12.18)
        var thenType = _ctx.TypeInferrer.Infer(c.ThenBranch);
        var elseType = _ctx.TypeInferrer.Infer(c.ElseBranch);

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

    internal LinqExpression CompileNullCoalesce(NullCoalesceExpr n)
    {
        var left = Compile(n.Left);
        var right = Compile(n.Right);

        return LinqExpression.Coalesce(left, right);
    }

    internal LinqExpression CompileMemberAccess(MemberAccessExpr m)
    {
        var obj = Compile(m.Object);

        return LinqExpression.Call(
            CompilerContext.GetMemberMethod,
            obj,
            LinqExpression.Constant(m.Name.Lexeme),
            _ctx.OptionsParam,
            LinqExpression.Constant(m.NullSafe),
            _ctx.CurrentContext);
    }

    internal LinqExpression CompileIndexAccess(IndexAccessExpr expr)
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
                    LinqExpression.Call(CompilerContext.GetIndexMethod, targetVar, index, _ctx.OptionsParam)));
        }

        var indexValue = Compile(expr.Index);
        return LinqExpression.Call(CompilerContext.GetIndexMethod, target, indexValue, _ctx.OptionsParam);
    }

    internal LinqExpression CompileSlice(SliceExpr expr)
    {
        var target = Compile(expr.Target);
        var start = expr.Start != null
            ? Compile(expr.Start)
            : LinqExpression.Constant(null, typeof(object));
        var end = expr.End != null
            ? Compile(expr.End)
            : LinqExpression.Constant(null, typeof(object));
        return LinqExpression.Call(CompilerContext.GetSliceMethod, target, start, end, _ctx.OptionsParam);
    }

    internal LinqExpression CompileVariableDecl(VariableDeclExpr v)
    {
        var value = Compile(v.Initializer);
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var inferredType = LinqExpression.Variable(typeof(Type), "inferredType");

        if (v.DeclaredType != null)
        {
            // Resolve type via context's TypeResolver
            var resolvedDeclType = LinqExpression.Call(
                _ctx.TypeResolverExpr,
                CompilerContext.ResolveTypeMethod,
                LinqExpression.Constant(v.DeclaredType.Value.Lexeme));

            var declTypeVar = LinqExpression.Variable(typeof(Type), "declType");

            value = LinqExpression.Block(
                new[] { declTypeVar },
                LinqExpression.Assign(declTypeVar, resolvedDeclType),
                LinqExpression.Call(
                    CompilerContext.ValidateAndCoerceTypeMethod,
                    declTypeVar,
                    value,
                    LinqExpression.Constant(v.Name.Lexeme)));
        }

        LinqExpression getInferredType;
        if (v.DeclaredType != null)
        {
            // Resolve type via context's TypeResolver
            getInferredType = LinqExpression.Call(
                _ctx.TypeResolverExpr,
                CompilerContext.ResolveTypeMethod,
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
                    LinqExpression.Call(_ctx.CurrentContext, CompilerContext.DefineMethod,
                        LinqExpression.Constant(v.Name.Lexeme), temp),
                    temp),
                LinqExpression.Block(
                    LinqExpression.Call(_ctx.CurrentContext, CompilerContext.DefineNewMethod,
                        LinqExpression.Constant(v.Name.Lexeme), temp, inferredType),
                    temp)));
    }

    internal LinqExpression CompileAssign(AssignExpr a)
    {
        var name = a.Name.Lexeme;
        var value = Compile(a.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");

        return LinqExpression.Block(
            new[] { temp },
            // Check sandbox allows assignment
            LinqExpression.Call(CompilerContext.CheckAllowAssignmentMethod, _ctx.OptionsParam,
                LinqExpression.Constant($"{name} = ...")),
            LinqExpression.Assign(temp, value),
            LinqExpression.Call(_ctx.CurrentContext, CompilerContext.SetMethod,
                LinqExpression.Constant(name), temp),
            temp);
    }

    internal LinqExpression CompileCompoundAssign(CompoundAssignExpr ca)
    {
        var name = ca.Name.Lexeme;
        var currentValue = CompileIdentifier(new IdentifierExpr(ca.Name));
        var rightValueExpr = Compile(ca.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var rightTemp = LinqExpression.Variable(typeof(object), "rightTemp");

        // Map compound op to base binary op
        if (!OperatorRegistry.CompoundToBaseOperator.TryGetValue(ca.Op.Type, out var baseOp))
            throw new NotSupportedException($"Compound operator {ca.Op.Type}");

        var opInfo = OperatorRegistry.GetBinaryOperator(baseOp);
        if (opInfo == null)
            throw new NotSupportedException($"Binary operator for compound {ca.Op.Type}");

        var info = opInfo.Value;
        LinqExpression opCall = info.Signature switch
        {
            OperatorRegistry.BinaryOpSignature.TwoArgs =>
                LinqExpression.Call(info.Method, currentValue, rightTemp),
            OperatorRegistry.BinaryOpSignature.WithOptions =>
                LinqExpression.Call(info.Method, currentValue, rightTemp, _ctx.OptionsParam),
            OperatorRegistry.BinaryOpSignature.WithOptionsAndContext =>
                LinqExpression.Call(info.Method, currentValue, rightTemp, _ctx.OptionsParam, _ctx.CurrentContext),
            _ => throw new NotSupportedException($"Unknown binary op signature {info.Signature}")
        };

        var validateCall = LinqExpression.Call(CompilerContext.ValidateCompoundAssignmentMethod,
            LinqExpression.Constant(name), opCall, rightTemp, _ctx.CurrentContext);

        return LinqExpression.Block(
            new[] { temp, rightTemp },
            LinqExpression.Call(CompilerContext.CheckAllowAssignmentMethod, _ctx.OptionsParam,
                LinqExpression.Constant($"{name} {ca.Op.Lexeme} ...")),
            LinqExpression.Assign(rightTemp, rightValueExpr),
            LinqExpression.Assign(temp, validateCall),
            LinqExpression.Call(_ctx.CurrentContext, CompilerContext.SetMethod,
                LinqExpression.Constant(name), temp),
            temp);
    }

    internal LinqExpression CompileMemberCompoundAssign(MemberCompoundAssignExpr expr)
    {
        var objExpr = Compile(expr.Object);
        var rightValueExpr = Compile(expr.Value);
        var objTemp = LinqExpression.Variable(typeof(object), "obj");
        var rightTemp = LinqExpression.Variable(typeof(object), "rightTemp");
        var temp = LinqExpression.Variable(typeof(object), "temp");

        // Get current value via MemberAccess.GetMember
        var currentValue = LinqExpression.Call(
            CompilerContext.GetMemberMethod,
            objTemp,
            LinqExpression.Constant(expr.MemberName),
            _ctx.OptionsParam,
            LinqExpression.Constant(false),
            _ctx.CurrentContext);

        // Map compound op to base binary op
        if (!OperatorRegistry.CompoundToBaseOperator.TryGetValue(expr.Operator, out var baseOp))
            throw new NotSupportedException($"Compound operator {expr.Operator}");

        var opInfo = OperatorRegistry.GetBinaryOperator(baseOp);
        if (opInfo == null)
            throw new NotSupportedException($"Binary operator for compound {expr.Operator}");

        var info = opInfo.Value;
        LinqExpression opCall = info.Signature switch
        {
            OperatorRegistry.BinaryOpSignature.TwoArgs =>
                LinqExpression.Call(info.Method, currentValue, rightTemp),
            OperatorRegistry.BinaryOpSignature.WithOptions =>
                LinqExpression.Call(info.Method, currentValue, rightTemp, _ctx.OptionsParam),
            OperatorRegistry.BinaryOpSignature.WithOptionsAndContext =>
                LinqExpression.Call(info.Method, currentValue, rightTemp, _ctx.OptionsParam, _ctx.CurrentContext),
            _ => throw new NotSupportedException($"Unknown binary op signature {info.Signature}")
        };

        // Set via MemberAccess.SetMember
        var setCall = LinqExpression.Call(CompilerContext.SetMemberMethod,
            objTemp, LinqExpression.Constant(expr.MemberName), temp, _ctx.OptionsParam, _ctx.CurrentContext);

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
        var objExpr = Compile(expr.Object);
        var indexExpr = Compile(expr.Index);
        var rightValueExpr = Compile(expr.Value);
        var objTemp = LinqExpression.Variable(typeof(object), "obj");
        var indexTemp = LinqExpression.Variable(typeof(object), "idx");
        var rightTemp = LinqExpression.Variable(typeof(object), "rightTemp");
        var temp = LinqExpression.Variable(typeof(object), "temp");

        // Get current value via MemberAccess.GetIndex
        var currentValue = LinqExpression.Call(
            CompilerContext.GetIndexMethod,
            objTemp, indexTemp, _ctx.OptionsParam);

        // Map compound op to base binary op
        if (!OperatorRegistry.CompoundToBaseOperator.TryGetValue(expr.Operator, out var baseOp))
            throw new NotSupportedException($"Compound operator {expr.Operator}");

        var opInfo = OperatorRegistry.GetBinaryOperator(baseOp);
        if (opInfo == null)
            throw new NotSupportedException($"Binary operator for compound {expr.Operator}");

        var info = opInfo.Value;
        LinqExpression opCall = info.Signature switch
        {
            OperatorRegistry.BinaryOpSignature.TwoArgs =>
                LinqExpression.Call(info.Method, currentValue, rightTemp),
            OperatorRegistry.BinaryOpSignature.WithOptions =>
                LinqExpression.Call(info.Method, currentValue, rightTemp, _ctx.OptionsParam),
            OperatorRegistry.BinaryOpSignature.WithOptionsAndContext =>
                LinqExpression.Call(info.Method, currentValue, rightTemp, _ctx.OptionsParam, _ctx.CurrentContext),
            _ => throw new NotSupportedException($"Unknown binary op signature {info.Signature}")
        };

        // Set via MemberAccess.SetIndex
        var setCall = LinqExpression.Call(CompilerContext.SetIndexMethod,
            objTemp, indexTemp, temp, _ctx.OptionsParam);

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
        var target = Compile(expr.Object);
        var index = Compile(expr.Index);
        var value = Compile(expr.Value);

        // Use a temp for index since we need it for both the check and the set
        var indexTemp = LinqExpression.Variable(typeof(object), "idx");
        var check = LinqExpression.Call(CompilerContext.CheckAllowIndexSetMethod, _ctx.OptionsParam, indexTemp);
        var set = LinqExpression.Call(CompilerContext.SetIndexMethod, target, indexTemp, value, _ctx.OptionsParam);

        return LinqExpression.Block(
            new[] { indexTemp },
            LinqExpression.Assign(indexTemp, index),
            check,
            set,
            value);
    }

    internal LinqExpression CompileIncrementDecrement(IncrementDecrementExpr inc)
    {
        var name = inc.Name.Lexeme;
        var isIncrement = inc.Op.Type == TokenType.PlusPlus;
        var currentValue = CompileIdentifier(new IdentifierExpr(inc.Name));
        var one = LinqExpression.Convert(LinqExpression.Constant(1), typeof(object));
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var original = LinqExpression.Variable(typeof(object), "original");

        // Get Add/Subtract info from registry
        var addInfo = OperatorRegistry.GetBinaryOperator(TokenType.Plus)!.Value;
        var subInfo = OperatorRegistry.GetBinaryOperator(TokenType.Minus)!.Value;

        LinqExpression MakeOpCall(LinqExpression left) => isIncrement
            ? LinqExpression.Call(addInfo.Method, left, one, _ctx.OptionsParam, _ctx.CurrentContext)
            : LinqExpression.Call(subInfo.Method, left, one);

        var checkExpr = LinqExpression.Call(CompilerContext.CheckAllowAssignmentMethod, _ctx.OptionsParam,
            LinqExpression.Constant(isIncrement ? $"{name}++" : $"{name}--"));

        if (inc.IsPrefix)
        {
            return LinqExpression.Block(
                new[] { temp },
                checkExpr,
                LinqExpression.Assign(temp, MakeOpCall(currentValue)),
                LinqExpression.Call(_ctx.CurrentContext, CompilerContext.SetMethod,
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
                LinqExpression.Call(_ctx.CurrentContext, CompilerContext.SetMethod,
                    LinqExpression.Constant(name), temp),
                original);
        }
    }

    internal LinqExpression CompileMemberNullCoalesceAssign(MemberNullCoalesceAssignExpr expr)
    {
        var objExpr = Compile(expr.Object);
        var objTemp = LinqExpression.Variable(typeof(object), "obj");
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var result = LinqExpression.Variable(typeof(object), "result");

        var currentValue = LinqExpression.Call(
            CompilerContext.GetMemberMethod,
            objTemp,
            LinqExpression.Constant(expr.MemberName),
            _ctx.OptionsParam,
            LinqExpression.Constant(false),
            _ctx.CurrentContext);

        var newValue = Compile(expr.Value);

        var setCall = LinqExpression.Call(CompilerContext.SetMemberMethod,
            objTemp, LinqExpression.Constant(expr.MemberName), result, _ctx.OptionsParam, _ctx.CurrentContext);

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
        var objExpr = Compile(expr.Object);
        var indexExpr = Compile(expr.Index);
        var objTemp = LinqExpression.Variable(typeof(object), "obj");
        var indexTemp = LinqExpression.Variable(typeof(object), "idx");
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var result = LinqExpression.Variable(typeof(object), "result");

        var currentValue = LinqExpression.Call(
            CompilerContext.GetIndexMethod,
            objTemp, indexTemp, _ctx.OptionsParam);

        var newValue = Compile(expr.Value);

        var setCall = LinqExpression.Call(CompilerContext.SetIndexMethod,
            objTemp, indexTemp, result, _ctx.OptionsParam);

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
        var objExpr = Compile(expr.Object);
        var objTemp = LinqExpression.Variable(typeof(object), "obj");
        var original = LinqExpression.Variable(typeof(object), "original");
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var one = LinqExpression.Convert(LinqExpression.Constant(1), typeof(object));

        var currentValue = LinqExpression.Call(
            CompilerContext.GetMemberMethod,
            objTemp,
            LinqExpression.Constant(expr.MemberName),
            _ctx.OptionsParam,
            LinqExpression.Constant(false),
            _ctx.CurrentContext);

        var addInfo = OperatorRegistry.GetBinaryOperator(TokenType.Plus)!.Value;
        var subInfo = OperatorRegistry.GetBinaryOperator(TokenType.Minus)!.Value;

        LinqExpression MakeOpCall(LinqExpression left) => expr.IsIncrement
            ? LinqExpression.Call(addInfo.Method, left, one, _ctx.OptionsParam, _ctx.CurrentContext)
            : LinqExpression.Call(subInfo.Method, left, one);

        var setCall = LinqExpression.Call(CompilerContext.SetMemberMethod,
            objTemp, LinqExpression.Constant(expr.MemberName), temp, _ctx.OptionsParam, _ctx.CurrentContext);

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
        var objExpr = Compile(expr.Object);
        var indexExpr = Compile(expr.Index);
        var objTemp = LinqExpression.Variable(typeof(object), "obj");
        var indexTemp = LinqExpression.Variable(typeof(object), "idx");
        var original = LinqExpression.Variable(typeof(object), "original");
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var one = LinqExpression.Convert(LinqExpression.Constant(1), typeof(object));

        var currentValue = LinqExpression.Call(
            CompilerContext.GetIndexMethod,
            objTemp, indexTemp, _ctx.OptionsParam);

        var addInfo = OperatorRegistry.GetBinaryOperator(TokenType.Plus)!.Value;
        var subInfo = OperatorRegistry.GetBinaryOperator(TokenType.Minus)!.Value;

        LinqExpression MakeOpCall(LinqExpression left) => expr.IsIncrement
            ? LinqExpression.Call(addInfo.Method, left, one, _ctx.OptionsParam, _ctx.CurrentContext)
            : LinqExpression.Call(subInfo.Method, left, one);

        var setCall = LinqExpression.Call(CompilerContext.SetIndexMethod,
            objTemp, indexTemp, temp, _ctx.OptionsParam);

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

    internal LinqExpression CompileCall(CallExpr call)
    {
        // Compile arguments into an object[] array, wrapping named arguments in NamedArg
        var argsVar = LinqExpression.Variable(typeof(object?[]), "args");
        var argsInit = LinqExpression.NewArrayInit(
            typeof(object),
            call.Arguments.Select(CompileArgument));

        var typeArgsExpr = call.TypeArguments != null
            ? LinqExpression.Constant(call.TypeArguments, typeof(IReadOnlyList<string>))
            : LinqExpression.Constant(null, typeof(IReadOnlyList<string>));

        // Check if any arguments are out parameters (need post-call variable definition)
        var hasOutArgs = call.Arguments.Any(a => a is OutArgExpr);

        LinqExpression callExpr;

        // Check if this is a member access call (target.Method(args))
        if (call.Callee is MemberAccessExpr memberAccess)
        {
            var target = Compile(memberAccess.Object);
            var methodName = memberAccess.Name.Lexeme;

            callExpr = LinqExpression.Call(
                CompilerContext.InvokeMemberCallMethod,
                target,
                LinqExpression.Constant(methodName),
                argsVar,
                LinqExpression.Constant(memberAccess.NullSafe),
                _ctx.CurrentContext,
                _ctx.OptionsParam,
                _ctx.CtParam,
                _ctx.ArgumentTransformerParam,
                typeArgsExpr);
        }
        else
        {
            // General call: evaluate callee and invoke
            var callee = Compile(call.Callee);
            callExpr = LinqExpression.Call(
                CompilerContext.InvokeCallMethod,
                callee,
                argsVar,
                _ctx.CurrentContext,
                _ctx.OptionsParam,
                _ctx.CtParam,
                _ctx.ArgumentTransformerParam,
                typeArgsExpr);
        }

        if (!hasOutArgs)
        {
            // Simple case: no out args, just call and return
            return LinqExpression.Block(
                new[] { argsVar },
                LinqExpression.Assign(argsVar, argsInit),
                callExpr);
        }

        // Out args case: call, then define out variables from modified args array
        var resultVar = LinqExpression.Variable(typeof(object), "callResult");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(argsVar, argsInit),
            LinqExpression.Assign(resultVar, callExpr)
        };

        // After the call, MethodInvoker.CopyBackOutArgs has replaced OutArgMarker entries
        // in argsVar with the actual values. Define variables for non-discard out args.
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            if (call.Arguments[i] is OutArgExpr outArg && !outArg.IsDiscard)
            {
                // Read the out value from argsVar[i]
                var outValue = LinqExpression.ArrayIndex(argsVar, LinqExpression.Constant(i));

                // Resolve type: if explicit type specified, use it; otherwise use runtime type
                LinqExpression typeExpr;
                if (outArg.TypeName != null)
                {
                    typeExpr = LinqExpression.Call(
                        _ctx.TypeResolverExpr,
                        CompilerContext.ResolveTypeMethod,
                        LinqExpression.Constant(outArg.TypeName));
                }
                else
                {
                    // typeof(object) — the runtime type will be set by the value itself
                    // Use a conditional: value?.GetType() ?? typeof(object)
                    var getTypeMethod = typeof(object).GetMethod(nameof(GetType))!;
                    typeExpr = LinqExpression.Condition(
                        LinqExpression.NotEqual(outValue, LinqExpression.Constant(null, typeof(object))),
                        LinqExpression.Call(outValue, getTypeMethod),
                        LinqExpression.Constant(typeof(object), typeof(Type)));
                }

                statements.Add(LinqExpression.Call(
                    _ctx.CurrentContext,
                    CompilerContext.DefineNewMethod,
                    LinqExpression.Constant(outArg.VariableName),
                    outValue,
                    typeExpr));
            }
        }

        // Return the call result
        statements.Add(resultVar);

        return LinqExpression.Block(
            new[] { argsVar, resultVar },
            statements);
    }

    /// <summary>
    /// Compiles an OutArgExpr to create an OutArgMarker at runtime.
    /// The marker flows through MethodInvoker which recognizes it for ByRef parameter handling.
    /// </summary>
    internal LinqExpression CompileOutArg(OutArgExpr outArg)
    {
        return LinqExpression.Convert(
            LinqExpression.New(
                CompilerContext.OutArgMarkerCtor,
                LinqExpression.Constant(outArg.VariableName),
                LinqExpression.Constant(outArg.TypeName, typeof(string)),
                LinqExpression.Constant(outArg.IsDiscard)),
            typeof(object));
    }

    private LinqExpression CompileArgument(Expr arg)
    {
        if (arg is NamedArgumentExpr namedArg)
        {
            // Wrap named argument in NamedArg: new NamedArg(name, value)
            return LinqExpression.Convert(
                LinqExpression.New(
                    CompilerContext.NamedArgCtor,
                    LinqExpression.Constant(namedArg.Name.Lexeme),
                    Compile(namedArg.Value)),
                typeof(object));
        }
        return Compile(arg);
    }

    internal LinqExpression CompileLambda(LambdaExpr lambda)
    {
        var parameterNames = lambda.Parameters.Select(p => p.Name.Lexeme).ToList();

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
                LinqExpression.Call(closureParam, CompilerContext.CreateChildMethod))
        };

        // Define each parameter in the child context
        for (var i = 0; i < parameterNames.Count; i++)
        {
            statements.Add(LinqExpression.Call(childContextVar, CompilerContext.DefineMethod,
                LinqExpression.Constant(parameterNames[i]),
                LinqExpression.Call(CompilerContext.GetLambdaArgMethod, argsParam, LinqExpression.Constant(i))));
        }

        // Save the current context and create new return context for the lambda
        var savedContext = _ctx.CurrentContext;
        _ctx.CurrentContext = childContextVar;

        // Lambda needs its own return label for return statements in block bodies
        var lambdaReturnLabel = LinqExpression.Label(typeof(object), "lambdaReturn");
        var lambdaReturnValue = LinqExpression.Variable(typeof(object), "lambdaReturnValue");

        // Push new return context for the lambda body
        _ctx.PushReturnContext(lambdaReturnLabel, lambdaReturnValue);

        try
        {
            // Compile the lambda body
            var compiledBody = Compile(lambda.Body);

            // Assign the body result to the return value variable
            statements.Add(LinqExpression.Assign(lambdaReturnValue, compiledBody));
        }
        finally
        {
            _ctx.CurrentContext = savedContext;
            _ctx.PopReturnContext();
        }

        // Add the return label at the end - early returns jump here
        statements.Add(LinqExpression.Label(lambdaReturnLabel, lambdaReturnValue));

        var lambdaBody = LinqExpression.Block(
            typeof(object),
            [childContextVar, lambdaReturnValue],
            statements);

        // Create the delegate: Func<object?[], CsEvalContext, object?>
        var compiledDelegate = LinqExpression.Lambda<Func<object?[], CsEvalContext, object?>>(
            lambdaBody,
            argsParam,
            closureParam);

        // Create CompiledLambdaValue(parameters, compiledBody, closure)
        return LinqExpression.New(
            CompilerContext.CompiledLambdaValueCtor,
            listInit,
            compiledDelegate,
            _ctx.CurrentContext);
    }

    internal LinqExpression CompileArrayLiteral(ArrayLiteralExpr expr)
    {
        var listVar = LinqExpression.Variable(typeof(List<object?>), "list");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(listVar, LinqExpression.New(CompilerContext.ListCtor))
        };

        foreach (var element in expr.Elements)
        {
            if (element is SpreadExpr spread)
            {
                var spreadValue = Compile(spread.Expression);
                statements.Add(LinqExpression.Call(CompilerContext.SpreadIntoListMethod, listVar, spreadValue));
            }
            else
            {
                statements.Add(LinqExpression.Call(listVar, CompilerContext.ListAddMethod, Compile(element)));
            }
        }

        statements.Add(LinqExpression.Call(CompilerContext.CreateTypedArrayMethod, listVar));
        return LinqExpression.Block(new[] { listVar }, statements);
    }

    internal LinqExpression CompileObjectLiteral(ObjectLiteralExpr expr) =>
        Extensions.ObjectLiteralCompiler.CompileObjectLiteral(expr, _ctx, Compile);

    internal LinqExpression CompileInterpolatedString(InterpolatedStringExpr expr)
    {
        var sbVar = LinqExpression.Variable(typeof(StringBuilder), "sb");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(sbVar, LinqExpression.New(CompilerContext.StringBuilderCtor))
        };

        foreach (var part in expr.Parts)
        {
            switch (part)
            {
                case TextPart text:
                    statements.Add(LinqExpression.Call(sbVar, CompilerContext.StringBuilderAppendMethod,
                        LinqExpression.Constant(text.Text)));
                    break;
                case ExpressionPart exprPart:
                    var value = Compile(exprPart.Expression);
                    if (exprPart.AlignmentSpecifier != null || exprPart.FormatSpecifier != null)
                    {
                        // Build format string like "{0,10:F2}" and call string.Format
                        var formatStr = "{0";
                        if (exprPart.AlignmentSpecifier != null) formatStr += "," + exprPart.AlignmentSpecifier;
                        if (exprPart.FormatSpecifier != null) formatStr += ":" + exprPart.FormatSpecifier;
                        formatStr += "}";
                        var formatted = LinqExpression.Call(
                            CompilerContext.StringFormatMethod,
                            LinqExpression.Constant(formatStr),
                            value);
                        statements.Add(LinqExpression.Call(sbVar, CompilerContext.StringBuilderAppendMethod, formatted));
                    }
                    else
                    {
                        var valueAsString = LinqExpression.Condition(
                            LinqExpression.Equal(value, LinqExpression.Constant(null, typeof(object))),
                            LinqExpression.Constant(""),
                            LinqExpression.Call(value, CompilerContext.ObjectToStringMethod));
                        statements.Add(LinqExpression.Call(sbVar, CompilerContext.StringBuilderAppendMethod, valueAsString));
                    }
                    break;
            }
        }

        statements.Add(LinqExpression.Convert(
            LinqExpression.Call(sbVar, CompilerContext.StringBuilderToStringMethod),
            typeof(object)));
        return LinqExpression.Block(new[] { sbVar }, statements);
    }

    internal LinqExpression CompileMemberAssign(MemberAssignExpr expr)
    {
        var target = Compile(expr.Object);
        var value = Compile(expr.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");

        return LinqExpression.Block(
            new[] { temp },
            LinqExpression.Call(CompilerContext.CheckAllowAssignmentMethod, _ctx.OptionsParam,
                LinqExpression.Constant($"{expr.Name.Lexeme} = ...")),
            LinqExpression.Assign(temp, value),
            LinqExpression.Call(CompilerContext.SetMemberMethod, target,
                LinqExpression.Constant(expr.Name.Lexeme), temp, _ctx.OptionsParam, _ctx.CurrentContext),
            temp);
    }

    internal LinqExpression CompileNullCoalesceAssign(NullCoalesceAssignExpr expr)
    {
        var name = expr.Name.Lexeme;
        var currentValue = CompileIdentifier(new IdentifierExpr(expr.Name));
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var result = LinqExpression.Variable(typeof(object), "result");

        var newValue = Compile(expr.Value);

        return LinqExpression.Block(
            new[] { temp, result },
            LinqExpression.Call(CompilerContext.CheckNullCoalesceAssignAllowedMethod,
                LinqExpression.Constant(name), _ctx.CurrentContext),
            LinqExpression.Assign(temp, currentValue),
            LinqExpression.IfThenElse(
                LinqExpression.NotEqual(temp, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Assign(result, temp),
                LinqExpression.Block(
                    LinqExpression.Call(CompilerContext.CheckAllowAssignmentMethod, _ctx.OptionsParam,
                        LinqExpression.Constant($"{name} ??= ...")),
                    LinqExpression.Assign(result, newValue),
                    LinqExpression.Call(_ctx.CurrentContext, CompilerContext.SetMethod,
                        LinqExpression.Constant(name), result))),
            result);
    }
}
