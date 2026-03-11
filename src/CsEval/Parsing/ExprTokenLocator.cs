namespace CsEval.Parsing;

internal static class ExprTokenLocator
{
    internal static bool TryGetPrimaryToken(Expr expr, out Token token)
    {
        switch (expr)
        {
            case IdentifierExpr e:
                token = e.Name;
                return true;
            case MemberAccessExpr e:
                token = e.Name;
                return true;
            case TypeReferenceExpr e:
                token = e.TypeToken;
                return true;
            case UnaryExpr e:
                token = e.Op;
                return true;
            case BinaryExpr e:
                token = e.Op;
                return true;
            case LogicalExpr e:
                token = e.Op;
                return true;
            case CastExpr e:
                token = e.TargetType;
                return true;
            case AsExpr e:
                token = e.TargetType;
                return true;
            case AssignExpr e:
                token = e.Name;
                return true;
            case NullCoalesceAssignExpr e:
                token = e.Name;
                return true;
            case CompoundAssignExpr e:
                token = e.Op;
                return true;
            case IncrementDecrementExpr e:
                token = e.Op;
                return true;
            case MemberAssignExpr e:
                token = e.Name;
                return true;
            case VariableDeclExpr e:
                token = e.Name;
                return true;
            case ForEachStatementExpr e:
                token = e.VariableName;
                return true;
            case NamedArgumentExpr e:
                token = e.Name;
                return true;
            case TypeofExpr e:
                token = e.TypeToken;
                return true;
            case DefaultExpr e when e.TypeToken.HasValue:
                token = e.TypeToken.Value;
                return true;
            default:
                token = default;
                return false;
        }
    }
}
