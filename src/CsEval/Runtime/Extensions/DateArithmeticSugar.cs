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

        if (string.Equals(memberName, ExtendedBuiltInNames.Day, comparison) ||
            string.Equals(memberName, ExtendedBuiltInNames.Days, comparison))
        {
            value = TimeSpan.FromDays(amount);
            return true;
        }

        if (string.Equals(memberName, ExtendedBuiltInNames.Hour, comparison) ||
            string.Equals(memberName, ExtendedBuiltInNames.Hours, comparison))
        {
            value = TimeSpan.FromHours(amount);
            return true;
        }

        if (string.Equals(memberName, ExtendedBuiltInNames.Minute, comparison) ||
            string.Equals(memberName, ExtendedBuiltInNames.Minutes, comparison))
        {
            value = TimeSpan.FromMinutes(amount);
            return true;
        }

        if (string.Equals(memberName, ExtendedBuiltInNames.Second, comparison) ||
            string.Equals(memberName, ExtendedBuiltInNames.Seconds, comparison))
        {
            value = TimeSpan.FromSeconds(amount);
            return true;
        }

        if (string.Equals(memberName, ExtendedBuiltInNames.Millisecond, comparison) ||
            string.Equals(memberName, ExtendedBuiltInNames.Milliseconds, comparison))
        {
            value = TimeSpan.FromMilliseconds(amount);
            return true;
        }

        if (string.Equals(memberName, ExtendedBuiltInNames.Week, comparison) ||
            string.Equals(memberName, ExtendedBuiltInNames.Weeks, comparison))
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
        if (string.Equals(name, ExtendedBuiltInNames.Now, comparison))
        {
            value = DateTime.Now;
            return true;
        }

        if (string.Equals(name, ExtendedBuiltInNames.Today, comparison))
        {
            value = DateTime.Today;
            return true;
        }

        return false;
    }
}
