using Alder.Security;

namespace Alder.Pipeline;

internal sealed class PipelineContext
{
    public SecurityPolicy Policy { get; }
    public AlderOptions Options { get; }
    public CancellationToken CancellationToken { get; }

    internal PipelineContext(SecurityPolicy policy, AlderOptions options, CancellationToken ct = default)
    {
        Policy = policy;
        Options = options;
        CancellationToken = ct;
    }
}
