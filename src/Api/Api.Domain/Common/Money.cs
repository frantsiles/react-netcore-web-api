namespace Api.Domain.Common;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string CurrencyCode { get; private set; } = "";

    private Money() { }

    private Money(decimal amount, string currencyCode)
    {
        Amount = amount;
        CurrencyCode = currencyCode.ToUpperInvariant();
    }

    public static Money Of(decimal amount, string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
            throw new DomainException($"Invalid currency code: '{currencyCode}'. Must be ISO 4217 (3 letters).");
        if (amount < 0)
            throw new DomainException("Money amount cannot be negative.");

        return new Money(amount, currencyCode);
    }

    public static Money Zero(string currencyCode) => Of(0, currencyCode);

    public Money Add(Money other)
    {
        GuardSameCurrency(other);
        return Of(Amount + other.Amount, CurrencyCode);
    }

    public Money Subtract(Money other)
    {
        GuardSameCurrency(other);
        if (Amount < other.Amount)
            throw new DomainException("Subtraction would result in a negative Money amount.");
        return Of(Amount - other.Amount, CurrencyCode);
    }

    public Money Multiply(decimal factor)
    {
        if (factor < 0)
            throw new DomainException("Money cannot be multiplied by a negative factor.");
        return Of(Math.Round(Amount * factor, 2, MidpointRounding.AwayFromZero), CurrencyCode);
    }

    private void GuardSameCurrency(Money other)
    {
        if (CurrencyCode != other.CurrencyCode)
            throw new DomainException(
                $"Cannot operate on different currencies: {CurrencyCode} and {other.CurrencyCode}.");
    }

    public override string ToString() => $"{Amount:F2} {CurrencyCode}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return CurrencyCode;
    }
}
