using Inventory.Domain.Inventory;
using Inventory.Domain.Warehouses;

namespace Inventory.Application.Common.Dtos;

public record WarehouseDto(
    Guid Id,
    string Code,
    string Name,
    string? Address,
    WarehouseStatus Status,
    DateTime CreatedAt);

public record InventoryItemDto(
    Guid Id,
    Guid CatalogItemId,
    Guid WarehouseId,
    string SKU,
    decimal QuantityOnHand,
    decimal QuantityReserved,
    decimal QuantityAvailable,
    decimal? ReorderPoint,
    bool IsLowStock,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record StockMovementDto(
    Guid Id,
    MovementType Type,
    decimal Quantity,
    string? ReferenceNumber,
    Guid? ReferenceId,
    string? Reason,
    string? Notes,
    DateTime OccurredAt);
