namespace BFF.Application.Inventory;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record WarehouseBffDto(
    Guid Id, string Code, string Name, string? Address, string Status, DateTime CreatedAt);

public record InventoryItemBffDto(
    Guid Id, Guid CatalogItemId, Guid WarehouseId, string SKU,
    decimal QuantityOnHand, decimal QuantityReserved, decimal QuantityAvailable,
    decimal? ReorderPoint, bool IsLowStock, DateTime CreatedAt, DateTime UpdatedAt);

public record StockMovementBffDto(
    Guid Id, string Type, decimal Quantity, string? ReferenceNumber,
    Guid? ReferenceId, string? Reason, string? Notes, DateTime OccurredAt);

// ── Queries ───────────────────────────────────────────────────────────────────

public record ListWarehousesBffQuery(string Token)
    : MediatR.IRequest<IReadOnlyList<WarehouseBffDto>>;

public record SearchInventoryBffQuery(
    string Token, Guid? WarehouseId, string? Sku, bool? BelowReorderPoint, int Skip, int Take)
    : MediatR.IRequest<IReadOnlyList<InventoryItemBffDto>>;

public record GetStockMovementsBffQuery(
    string Token, Guid ItemId, string? Type, int Skip, int Take)
    : MediatR.IRequest<IReadOnlyList<StockMovementBffDto>>;

// ── Commands ──────────────────────────────────────────────────────────────────

public record ReceiveStockBffCommand(
    string Token, Guid CatalogItemId, Guid WarehouseId, string SKU,
    decimal Quantity, string? ReferenceNumber, string? Notes, decimal? ReorderPoint)
    : MediatR.IRequest<InventoryItemBffDto>;

public record AdjustStockBffCommand(
    string Token, Guid ItemId, decimal Delta, string Reason, string? Notes)
    : MediatR.IRequest<InventoryItemBffDto>;

public record WriteOffStockBffCommand(
    string Token, Guid ItemId, decimal Quantity, string Reason, string? Notes)
    : MediatR.IRequest<InventoryItemBffDto>;

public record CreateWarehouseBffCommand(
    string Token, string Code, string Name, string? Address)
    : MediatR.IRequest<WarehouseBffDto>;
