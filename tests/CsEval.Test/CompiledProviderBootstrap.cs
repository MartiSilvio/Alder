using CsEval.Compiled;

namespace CsEval.Test;

[SetUpFixture]
public sealed class CompiledProviderBootstrap
{
    [OneTimeSetUp]
    public void RegisterCompiledProvider()
    {
        CsEvalCompiledExtensions.RegisterCompiledProvider();
    }
}
