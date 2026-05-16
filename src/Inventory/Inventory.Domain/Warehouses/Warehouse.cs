using Api.Domain.Common;
using Inventory.Domain.DomainEvents;

namespace Inventory.Domain.Warehouses;

public class Warehouse : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string? Address { get; private set; }
    public WarehouseStatus Status { get; private set; }

    private Warehouse() : base() { }

    public static Warehouse Create(string code, string name, string? address = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Warehouse code cannot be empty.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Warehouse name cannot be empty.");

        var wh = new Warehouse
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Address = address?.Trim(),
            Status = WarehouseStatus.Active
        };
        wh.RaiseDomainEvent(new WarehouseCreatedEvent(wh.Id, wh.Code, wh.Name, DateTimeOffset.UtcNow));
        return wh;
    }

    public void Deactivate()
    {
        if (Status == WarehouseStatus.Inactive)
            throw new DomainException("Warehouse is already inactive.");
        Status = WarehouseStatus.Inactive;
        SetUpdatedAt();
    }

    public void UpdateName(string name, string? address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Warehouse name cannot be empty.");
        Name = name.Trim();
        Address = address?.Trim();
        SetUpdatedAt();
    }
}
