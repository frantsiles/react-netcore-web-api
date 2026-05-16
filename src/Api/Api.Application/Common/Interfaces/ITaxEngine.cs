using Api.Domain.Common;

namespace Api.Application.Common.Interfaces;

public interface ITaxEngine
{
    string CountryCode { get; }

    /// <summary>
    /// Calculates the tax amount for a given net price and tax category.
    /// taxCategory examples: "STANDARD", "EXEMPT", "REDUCED"
    /// </summary>
    Money CalculateTax(Money netPrice, string taxCategory);

    decimal GetRate(string taxCategory);
}
