using Api.Domain.Common;

namespace Inventory.Domain.Inventory;

public class StockMovement : Entity
{
    public MovementType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? Reason { get; private set; }
    public string? Notes { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private StockMovement() : base() { }

    public static StockMovement Create(MovementType type, decimal quantity,
        string? referenceNumber = null, Guid? referenceId = null,
        string? reason = null, string? notes = null)
    {
        if (quantity == 0)
            throw new DomainException("Movement quantity cannot be zero.");

        return new StockMovement
        {
            Type = type,
            Quantity = quantity,
            ReferenceNumber = referenceNumber?.Trim(),
            ReferenceId = referenceId,
            Reason = reason?.Trim(),
            Notes = notes?.Trim(),
            OccurredAt = DateTime.UtcNow
        };
    }
}
