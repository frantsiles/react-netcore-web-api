using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Sales.Application.Common.Dtos;
using Sales.Application.Common.Mappings;
using Sales.Domain.Orders;
using Sales.Domain.Repositories;

namespace Sales.Application.Orders.Commands.AddOrderLine;

public record AddOrderLineCommand(
    Guid OrderId,
    Guid CatalogItemId,
    string SKU,
    string ItemName,
    decimal Quantity,
    decimal UnitPrice,
    string CurrencyCode,
    decimal DiscountPercent,
    string? Notes) : IRequest<SalesOrderDto>;

public class AddOrderLineCommandValidator : AbstractValidator<AddOrderLineCommand>
{
    public AddOrderLineCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CatalogItemId).NotEmpty();
        RuleFor(x => x.SKU).NotEmpty();
        RuleFor(x => x.ItemName).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
    }
}

public class AddOrderLineCommandHandler : IRequestHandler<AddOrderLineCommand, SalesOrderDto>
{
    private readonly ISalesOrderRepository _repo;

    public AddOrderLineCommandHandler(ISalesOrderRepository repo) => _repo = repo;

    public async Task<SalesOrderDto> Handle(AddOrderLineCommand cmd, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(cmd.OrderId, ct)
            ?? throw new DomainException($"SalesOrder '{cmd.OrderId}' not found.");

        var line = SalesOrderLine.Create(cmd.CatalogItemId, cmd.SKU, cmd.ItemName,
            cmd.Quantity, Money.Of(cmd.UnitPrice, cmd.CurrencyCode),
            cmd.DiscountPercent, cmd.Notes);

        order.AddLine(line);
        await _repo.UpdateAsync(order, ct);
        return order.ToDto();
    }
}
