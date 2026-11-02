using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.PracticeLocations;

public sealed record ConsultationPrice
{
    public const decimal MaxAmount = 9_999_999_999_999_999.99m;
    public decimal Amount { get; }
    public string Currency { get; }
    private ConsultationPrice(decimal amount) { Amount = amount; Currency = "EGP"; }
    public static Result<ConsultationPrice> Create(decimal amount, string? currency)
    {
        if (currency?.Trim().ToUpperInvariant() != "EGP")
            return Error.Validation("PracticeLocation.Currency", "Only EGP is supported in version 1.", "Currency");
        if (amount < 0 || amount > MaxAmount || decimal.Round(amount, 2) != amount)
            return Error.Validation("PracticeLocation.Fee", "Provide a nonnegative fee with at most two decimal places.", "ConsultationFee");
        return new ConsultationPrice(amount);
    }
}
