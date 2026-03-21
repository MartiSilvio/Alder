namespace Alder.Runtime;

internal static class OverloadScoring
{
    public const int NormalFormBase = 1000;
    public const int ExpandedFormBase = 500;
    public const int ExactMatch = 100;
    public const int AssignableMatch = 10;
    public const int ImplicitConversion = 1;
    public const int DefaultArgPenalty = 10;
}
