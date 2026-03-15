using System.Collections.Frozen;

namespace CsEval.Runtime.Extensions;

internal static class DateArithmeticSugar
{
    private static Dictionary<string, Func<double, TimeSpan>> BuildTimeSpanUnits() => new()
    {
        [ExtendedBuiltInNames.Day] = TimeSpan.FromDays,
        [ExtendedBuiltInNames.Days] = TimeSpan.FromDays,
        [ExtendedBuiltInNames.Hour] = TimeSpan.FromHours,
        [ExtendedBuiltInNames.Hours] = TimeSpan.FromHours,
        [ExtendedBuiltInNames.Minute] = TimeSpan.FromMinutes,
        [ExtendedBuiltInNames.Minutes] = TimeSpan.FromMinutes,
        [ExtendedBuiltInNames.Second] = TimeSpan.FromSeconds,
        [ExtendedBuiltInNames.Seconds] = TimeSpan.FromSeconds,
        [ExtendedBuiltInNames.Millisecond] = TimeSpan.FromMilliseconds,
        [ExtendedBuiltInNames.Milliseconds] = TimeSpan.FromMilliseconds,
        [ExtendedBuiltInNames.Week] = amount => TimeSpan.FromDays(amount * 7d),
        [ExtendedBuiltInNames.Weeks] = amount => TimeSpan.FromDays(amount * 7d),
    };

    private static readonly FrozenDictionary<string, Func<double, TimeSpan>> TimeSpanUnits =
        BuildTimeSpanUnits().ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, Func<double, TimeSpan>> TimeSpanUnitsOrdinalIgnoreCase =
        BuildTimeSpanUnits().ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    internal static bool TryResolveTimeSpanUnit(
        object target,
        string memberName,
        bool isCaseSensitive,
        out object? value)
    {
        value = null;
        if (!TypeHelpers.IsArithmetic(target))
            return false;

        var units = isCaseSensitive ? TimeSpanUnits : TimeSpanUnitsOrdinalIgnoreCase;
        if (!units.TryGetValue(memberName, out var factory))
            return false;

        value = factory(Convert.ToDouble(target));
        return true;
    }

    private static Dictionary<string, Func<object>> BuildClockFunctions() => new()
    {
        [ExtendedBuiltInNames.Now] = () => DateTime.Now,
        [ExtendedBuiltInNames.Today] = () => DateTime.Today,
    };

    private static readonly FrozenDictionary<string, Func<object>> ClockFunctions =
        BuildClockFunctions().ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, Func<object>> ClockFunctionsOrdinalIgnoreCase =
        BuildClockFunctions().ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    internal static bool TryInvokeClockFunction(
        string name,
        object?[] args,
        bool isCaseSensitive,
        out object? value)
    {
        value = null;
        if (args.Length != 0)
            return false;

        var lookup = isCaseSensitive ? ClockFunctions : ClockFunctionsOrdinalIgnoreCase;
        if (!lookup.TryGetValue(name, out var factory))
            return false;

        value = factory();
        return true;
    }
}
