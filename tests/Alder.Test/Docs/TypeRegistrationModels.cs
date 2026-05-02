namespace Billing;

public sealed record Money(decimal Amount)
{
    public static Money FromDollars(decimal amount) => new(amount);
}

public static class MoneyExtensions
{
    public static bool IsHighValue(this Money money, decimal threshold) => money.Amount >= threshold;
}
