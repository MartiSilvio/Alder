namespace CsEval.Aot;

public abstract class CsEvalTypeContext
{
    public abstract IReadOnlyList<IAotTypeMetadata> GetTypeMetadata();
}
