namespace CsEval.Test;

[SetUpFixture]
#pragma warning disable CA1050
// ReSharper disable once CheckNamespace
public sealed class CompiledProviderBootstrap
#pragma warning restore CA1050
{
    [OneTimeSetUp]
    public void RegisterCompiledProvider()
    {
        CsEvalCompiledExtensions.RegisterCompiledProvider();
    }
}