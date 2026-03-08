using CsEval;

namespace CsEval.Compiled;

public static class CsEvalCompiledExtensions
{
    public static CsEvalOptions UseCompiled(this CsEvalOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        RegisterCompiledProvider();
        return options;
    }

    public static void RegisterCompiledProvider()
    {
        CompiledProviderRegistration.Register();
    }
}
