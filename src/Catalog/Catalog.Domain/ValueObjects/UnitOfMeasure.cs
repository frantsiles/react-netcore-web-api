using Api.Domain.Common;

namespace Catalog.Domain.ValueObjects;

public sealed class UnitOfMeasure : ValueObject
{
    // Common UN/CEFACT codes
    public static readonly UnitOfMeasure Each = new("EA");
    public static readonly UnitOfMeasure Kilogram = new("KGM");
    public static readonly UnitOfMeasure Hour = new("HUR");
    public static readonly UnitOfMeasure Month = new("MON");
    public static readonly UnitOfMeasure Liter = new("LTR");
    public static readonly UnitOfMeasure Box = new("BX");

    public string Code { get; private set; } = "";

    private UnitOfMeasure() { }
    private UnitOfMeasure(string code) => Code = code;

    public static UnitOfMeasure Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Unit of measure code cannot be empty.");
        if (code.Trim().Length > 10)
            throw new DomainException("Unit of measure code cannot exceed 10 characters.");

        return new UnitOfMeasure(code.Trim().ToUpperInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }

    public override string ToString() => Code;
}
