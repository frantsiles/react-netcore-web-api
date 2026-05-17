namespace BFF.Application.Purchasing;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record PurchaseOrderLineBffDto(
    Guid Id,
    Guid CatalogItemId,
    string Sku,
    string ItemName,
    decimal QuantityOrdered,
    decimal QuantityReceived,
    decimal UnitCostAmount,
    string CurrencyCode,
    decimal LineTotal,
    string? Notes);

public record PurchaseOrderBffDto(
    Guid Id,
    string PoNumber,
    Guid SupplierId,
    string Status,
    DateTime? ExpectedDeliveryDate,
    string CurrencyCode,
    string CountryCode,
    string? Notes,
    decimal Subtotal,
    decimal Total,
    IReadOnlyList<PurchaseOrderLineBffDto> Lines,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ── Queries ───────────────────────────────────────────────────────────────────

public record SearchPurchaseOrdersBffQuery(
    string Token,
    Guid? SupplierId,
    string? Status,
    string? PoNumber,
    int Skip,
    int Take) : MediatR.IRequest<IReadOnlyList<PurchaseOrderBffDto>>;

// ── Commands ──────────────────────────────────────────────────────────────────

public record CreatePurchaseOrderBffCommand(
    string Token,
    Guid SupplierId,
    string CurrencyCode,
    string CountryCode,
    DateTime? ExpectedDeliveryDate,
    string? Notes) : MediatR.IRequest<PurchaseOrderBffDto>;

public record AddPoLineBffCommand(
    string Token,
    Guid OrderId,
    Guid CatalogItemId,
    string Sku,
    string ItemName,
    decimal QuantityOrdered,
    decimal UnitCostAmount,
    string? Notes) : MediatR.IRequest<PurchaseOrderBffDto>;

public record SendPurchaseOrderBffCommand(
    string Token,
    Guid OrderId) : MediatR.IRequest<PurchaseOrderBffDto>;

public record ConfirmPurchaseOrderBffCommand(
    string Token,
    Guid OrderId) : MediatR.IRequest<PurchaseOrderBffDto>;

public record ReceivePurchaseOrderBffCommand(
    string Token,
    Guid OrderId,
    Guid LineId,
    Guid WarehouseId,
    decimal QuantityReceived) : MediatR.IRequest<PurchaseOrderBffDto>;

public record CancelPurchaseOrderBffCommand(
    string Token,
    Guid OrderId,
    string Reason) : MediatR.IRequest<PurchaseOrderBffDto>;
