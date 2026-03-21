namespace Alder.Aot;

public abstract class AlderTypeContext
{
    public abstract IReadOnlyList<IAotTypeMetadata> GetTypeMetadata();
}
