using Alder.Runtime.Collections;

namespace Alder.Runtime.Extensions;

/// <summary>
/// Resolves bare math function and constant names in Extended mode.
/// sin(x), cos(x), pi, e, tau, etc. are available without Math. prefix.
/// User variables always shadow these built-in names.
/// </summary>
internal static class BareMathNames
{
    private static readonly FixedDictionary<string, object> Constants = FixedDictionary<string, object>.Create(new Dictionary<string, object>(StringComparer.Ordinal)
    {
        ["pi"] = Math.PI,
        ["e"] = Math.E,
        ["tau"] = Math.PI * 2.0,
        ["infinity"] = double.PositiveInfinity,
        ["nan"] = double.NaN
    }, StringComparer.Ordinal);

    /// <summary>
    /// Tries to resolve a bare name as a math constant.
    /// Returns false if the name is not a recognized constant.
    /// </summary>
    internal static bool TryGetConstant(string name, out object? value)
    {
        if (Constants.TryGetValue(name, out var constant))
        {
            value = constant;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Tries to resolve a bare name as a math function with the given argument count.
    /// Returns false if the name/argCount combination is not recognized.
    /// </summary>
    internal static bool TryGetFunction(string name, int argCount, out Func<object?[], object?> func)
    {
        func = null!;

        switch (argCount)
        {
            case 1:
                if (SingleArgFunctions.TryGetValue(name, out var singleFunc))
                {
                    func = singleFunc;
                    return true;
                }
                return false;

            case 2:
                if (TwoArgFunctions.TryGetValue(name, out var twoFunc))
                {
                    func = twoFunc;
                    return true;
                }
                return false;

            case 3:
                if (ThreeArgFunctions.TryGetValue(name, out var threeFunc))
                {
                    func = threeFunc;
                    return true;
                }
                return false;

            default:
                return false;
        }
    }

    private static double ToDouble(object? arg) => Convert.ToDouble(arg);

    private static readonly FixedDictionary<string, Func<object?[], object?>> SingleArgFunctions = FixedDictionary<string, Func<object?[], object?>>.Create(new Dictionary<string, Func<object?[], object?>>(StringComparer.Ordinal)
    {
        // Trigonometric
        ["sin"] = args => Math.Sin(ToDouble(args[0])),
        ["cos"] = args => Math.Cos(ToDouble(args[0])),
        ["tan"] = args => Math.Tan(ToDouble(args[0])),
        ["asin"] = args => Math.Asin(ToDouble(args[0])),
        ["acos"] = args => Math.Acos(ToDouble(args[0])),
        ["atan"] = args => Math.Atan(ToDouble(args[0])),

        // Hyperbolic
        ["sinh"] = args => Math.Sinh(ToDouble(args[0])),
        ["cosh"] = args => Math.Cosh(ToDouble(args[0])),
        ["tanh"] = args => Math.Tanh(ToDouble(args[0])),

        ["abs"] = args => Abs(args[0]),

        // Roots
        ["sqrt"] = args => Math.Sqrt(ToDouble(args[0])),
#if NET5_0_OR_GREATER
        ["cbrt"] = args => Math.Cbrt(ToDouble(args[0])),
#else
        ["cbrt"] = args => { var x = ToDouble(args[0]); return x < 0 ? -Math.Pow(-x, 1.0 / 3.0) : Math.Pow(x, 1.0 / 3.0); },
#endif

        // Logarithms
        ["log"] = args => Math.Log(ToDouble(args[0])),
        ["log2"] = args => Math.Log(ToDouble(args[0]), 2.0),
        ["log10"] = args => Math.Log10(ToDouble(args[0])),
        ["ln"] = args => Math.Log(ToDouble(args[0])),

        // Exponential
        ["exp"] = args => Math.Exp(ToDouble(args[0])),

        // Rounding: preserves decimal type per Math.Floor(decimal)/Math.Ceiling(decimal) overloads
        ["floor"] = args => args[0] is decimal m ? Math.Floor(m) : Math.Floor(ToDouble(args[0])),
        ["ceil"] = args => args[0] is decimal m ? Math.Ceiling(m) : Math.Ceiling(ToDouble(args[0])),
        ["round"] = args => args[0] is decimal m ? Math.Round(m) : Math.Round(ToDouble(args[0])),
        ["truncate"] = args => args[0] is decimal m ? Math.Truncate(m) : Math.Truncate(ToDouble(args[0])),

        // Sign: all overloads return int
        ["sign"] = args => Math.Sign(ToDouble(args[0]))
    }, StringComparer.Ordinal);

    private static readonly FixedDictionary<string, Func<object?[], object?>> TwoArgFunctions = FixedDictionary<string, Func<object?[], object?>>.Create(new Dictionary<string, Func<object?[], object?>>(StringComparer.Ordinal)
    {
        ["round"] = args => args[0] is decimal m ? Math.Round(m, Convert.ToInt32(args[1])) : Math.Round(ToDouble(args[0]), Convert.ToInt32(args[1])),
        ["log"] = args => Math.Log(ToDouble(args[0]), ToDouble(args[1])),
        ["atan2"] = args => Math.Atan2(ToDouble(args[0]), ToDouble(args[1])),
        ["min"] = args => Min(args[0], args[1]),
        ["max"] = args => Max(args[0], args[1]),
        ["pow"] = args => Math.Pow(ToDouble(args[0]), ToDouble(args[1]))
    }, StringComparer.Ordinal);

    private static readonly FixedDictionary<string, Func<object?[], object?>> ThreeArgFunctions = FixedDictionary<string, Func<object?[], object?>>.Create(new Dictionary<string, Func<object?[], object?>>(StringComparer.Ordinal)
    {
        ["clamp"] = args => Clamp(args[0], args[1], args[2])
    }, StringComparer.Ordinal);

    /// <summary>
    /// Preserves int/long/float/double/decimal types instead of converting to double.
    /// </summary>
    private static object? Abs(object? arg)
    {
        return arg switch
        {
            int i => Math.Abs(i),
            long l => Math.Abs(l),
            float f => Math.Abs(f),
            double d => Math.Abs(d),
            decimal m => Math.Abs(m),
            _ => Math.Abs(ToDouble(arg))
        };
    }

    /// <summary>
    /// Preserves int/long/float/double/decimal types instead of converting to double.
    /// </summary>
    private static object? Min(object? a, object? b)
    {
        return (a, b) switch
        {
            (int ai, int bi) => Math.Min(ai, bi),
            (long al, long bl) => Math.Min(al, bl),
            (float af, float bf) => Math.Min(af, bf),
            (double ad, double bd) => Math.Min(ad, bd),
            (decimal am, decimal bm) => Math.Min(am, bm),
            _ => Math.Min(ToDouble(a), ToDouble(b))
        };
    }

    /// <summary>
    /// Preserves int/long/float/double/decimal types instead of converting to double.
    /// </summary>
    private static object? Max(object? a, object? b)
    {
        return (a, b) switch
        {
            (int ai, int bi) => Math.Max(ai, bi),
            (long al, long bl) => Math.Max(al, bl),
            (float af, float bf) => Math.Max(af, bf),
            (double ad, double bd) => Math.Max(ad, bd),
            (decimal am, decimal bm) => Math.Max(am, bm),
            _ => Math.Max(ToDouble(a), ToDouble(b))
        };
    }

    /// <summary>
    /// Preserves int/long/float/double/decimal types instead of converting to double.
    /// </summary>
    private static object? Clamp(object? value, object? min, object? max)
    {
        return (value, min, max) switch
        {
            (int vi, int mi, int xi) => Math.Min(Math.Max(vi, mi), xi),
            (long vl, long ml, long xl) => Math.Min(Math.Max(vl, ml), xl),
            (float vf, float mf, float xf) => Math.Min(Math.Max(vf, mf), xf),
            (double vd, double md, double xd) => Math.Min(Math.Max(vd, md), xd),
            (decimal vm, decimal mm, decimal xm) => Math.Min(Math.Max(vm, mm), xm),
            _ => Math.Min(Math.Max(ToDouble(value), ToDouble(min)), ToDouble(max))
        };
    }
}
