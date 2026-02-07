// ============================================================
// HOW TO ADD A NEW SYNTAX EXTENSION
//
// Each extension feature gets its own file in the relevant Extensions/ directory.
// File naming convention: <FeatureName>Parser.cs, <FeatureName>Evaluator.cs,
// <FeatureName>Compiler.cs, <FeatureName>Helpers.cs
//
// New token/keyword only (e.g., adding a `let` alias):
//   1. Token.cs -- add TokenType entry
//   2. Lexer.cs -- add keyword mapping or token scanning
//   3. Tests in tests/CsEval.Test/Extensions/
//
// New expression type (needs new AST node):
//   1. Token.cs -- add TokenType (if new token needed)
//   2. Lexer.cs -- add keyword/token scanning (if new token needed)
//   3. Ast.cs -- add Expr record in the Collection Literals and Spread region
//   4. Ast.cs -- add visitor method to IExprVisitor<T>
//   5. AstWalker.cs -- add traversal method
//   6. Parsing/Extensions/<Feature>Parser.cs -- add parsing, called from PrimaryParser
//   7. Interpretation/Extensions/<Feature>Evaluator.cs -- add evaluation
//   8. Evaluator.cs -- add Visit* method delegating to <Feature>Evaluator
//   9. Compilation/Extensions/<Feature>Compiler.cs -- add compilation
//  10. ExpressionCompilerUnit.cs -- add Compile* delegating to <Feature>Compiler
//  11. CompilerContext.cs -- add to Compile() dispatch switch
//  12. CompilerContext.cs -- add to CanCompile() check
//  13. Runtime/Extensions/<Feature>Helpers.cs -- add runtime helpers (if needed)
//  14. Tests in tests/CsEval.Test/Extensions/<Feature>Tests.cs
//
// New operator (reuses BinaryExpr/UnaryExpr):
//   1. Token.cs -- add TokenType
//   2. Lexer.cs -- add token scanning
//   3. Runtime/Extensions/<Feature>Operator.cs -- add operator implementation
//   4. OperatorRegistry.cs -- add registry entry
//   5. Evaluator.cs -- add entry to BinaryOperators/UnaryOperators dictionary
//   6. CompilerContext.cs -- add to IsCompilableBinaryOp/IsCompilableCompoundOp
//   7. Tests in tests/CsEval.Test/Extensions/<Feature>Tests.cs
// ============================================================

namespace CsEval.Parsing.Extensions;

/// <summary>
/// Parser for CsEval array literal syntax: [1, 2, 3] and new[] { 1, 2, 3 }.
/// Handles spread operator (...) within array literals.
/// Called explicitly from PrimaryParser.
/// </summary>
internal static class ArrayLiteralParser
{
    internal static Expr ParseArrayLiteral(ParserBase parser, Func<Expr> parseExpression)
    {
        var elements = new List<Expr>();

        if (!parser.Check(TokenType.RightBracket))
        {
            do
            {
                if (parser.Match(TokenType.DotDotDot))
                {
                    // Spread element: ...expr
                    var spreadExpr = parseExpression();
                    elements.Add(new SpreadExpr(spreadExpr));
                }
                else
                {
                    elements.Add(parseExpression());
                }
            } while (parser.Match(TokenType.Comma));
        }

        parser.Consume(TokenType.RightBracket, "Expected ']' after array elements");
        return new ArrayLiteralExpr(elements);
    }

    internal static Expr ParseArrayLiteralBody(ParserBase parser, Func<Expr> parseExpression)
    {
        var elements = new List<Expr>();

        if (!parser.Check(TokenType.RightBrace))
        {
            do
            {
                if (parser.Match(TokenType.DotDotDot))
                {
                    var spreadExpr = parseExpression();
                    elements.Add(new SpreadExpr(spreadExpr));
                }
                else
                {
                    elements.Add(parseExpression());
                }
            } while (parser.Match(TokenType.Comma));
        }

        parser.Consume(TokenType.RightBrace, "Expected '}' after array elements");
        return new ArrayLiteralExpr(elements);
    }
}
