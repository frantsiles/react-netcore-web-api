using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Purchasing.Application.Common.Dtos;
using Purchasing.Application.Common.Mappings;
using Purchasing.Domain.PurchaseOrders;
using Purchasing.Domain.Repositories;

namespace Purchasing.Application.PurchaseOrders.Commands.CreatePurchaseOrder;

public record CreatePurchaseOrderCommand(
    Guid SupplierId,
    string CurrencyCode,
    string CountryCode,
    DateTime? ExpectedDeliveryDate = null,
    string? Notes = null) : IRequest<PurchaseOrderDto>;

public class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.CountryCode).NotEmpty().Length(2);
    }
}

public class CreatePurchaseOrderCommandHandler : IRequestHandler<CreatePurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IEventPublisher _events;

    public CreatePurchaseOrderCommandHandler(
        IPurchaseOrderRepository repo, ITenantContext tenant, IEventPublisher events)
        => (_repo, _tenant, _events) = (repo, tenant, events);

    public async Task<PurchaseOrderDto> Handle(CreatePurchaseOrderCommand cmd, CancellationToken ct)
    {
        var poNumber = $"PO-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        var po = PurchaseOrder.Create(
            _tenant.TenantId, poNumber, cmd.SupplierId,
            cmd.CurrencyCode, cmd.CountryCode,
            cmd.ExpectedDeliveryDate, cmd.Notes);

        await _repo.AddAsync(po, ct);
        foreach (var e in po.DomainEvents)
            await _events.PublishAsync(e, ct);

        return po.ToDto();
    }
}
