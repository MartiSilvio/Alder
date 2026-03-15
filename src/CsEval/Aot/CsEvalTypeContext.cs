namespace CsEval;

public abstract class CsEvalTypeContext
{
    public abstract IReadOnlyList<IAotTypeMetadata> GetTypeMetadata();
}
