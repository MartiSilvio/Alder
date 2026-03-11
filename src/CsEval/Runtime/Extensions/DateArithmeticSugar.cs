namespace CsEval.Runtime.Extensions;

internal static class DateArithmeticSugar
{
    internal static bool TryResolveTimeSpanUnit(
        object target,
        string memberName,
        bool isCaseSensitive,
        out object? value)
    {
        value = null;
        if (!TypeHelpers.IsArithmetic(target))
            return false;

        var amount = Convert.ToDouble(target);
        var comparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        if (string.Equals(memberName, "day", comparison) || string.Equals(memberName, "days", comparison))
        {
            value = TimeSpan.FromDays(amount);
            return true;
        }

        if (string.Equals(memberName, "hour", comparison) || string.Equals(memberName, "hours", comparison))
        {
            value = TimeSpan.FromHours(amount);
            return true;
        }

        if (string.Equals(memberName, "minute", comparison) || string.Equals(memberName, "minutes", comparison))
        {
            value = TimeSpan.FromMinutes(amount);
            return true;
        }

        if (string.Equals(memberName, "second", comparison) || string.Equals(memberName, "seconds", comparison))
        {
            value = TimeSpan.FromSeconds(amount);
            return true;
        }

        if (string.Equals(memberName, "millisecond", comparison) || string.Equals(memberName, "milliseconds", comparison))
        {
            value = TimeSpan.FromMilliseconds(amount);
            return true;
        }

        if (string.Equals(memberName, "week", comparison) || string.Equals(memberName, "weeks", comparison))
        {
            value = TimeSpan.FromDays(amount * 7d);
            return true;
        }

        return false;
    }

    internal static bool TryInvokeClockFunction(
        string name,
        object?[] args,
        bool isCaseSensitive,
        out object? value)
    {
        value = null;
        if (args.Length != 0)
            return false;

        var comparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        if (string.Equals(name, "now", comparison))
        {
            value = DateTime.Now;
            return true;
        }

        if (string.Equals(name, "today", comparison))
        {
            value = DateTime.Today;
            return true;
        }

        return false;
    }
}
