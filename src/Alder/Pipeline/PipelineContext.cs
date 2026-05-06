using Alder.Security;

namespace Alder.Pipeline;

internal sealed class PipelineContext
{
    public SecurityPolicy Policy { get; }
    public CancellationToken CancellationToken { get; }

    internal PipelineContext(SecurityPolicy policy, CancellationToken ct = default)
    {
        Policy = policy;
        CancellationToken = ct;
    }
}
