using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Sales.Application.Common.Dtos;
using Sales.Application.Common.Mappings;
using Sales.Domain.Quotes;
using Sales.Domain.Repositories;

namespace Sales.Application.Orders.Commands.UpdateOrderLine;

public record UpdateOrderLineCommand(
    Guid OrderId,
    Guid LineId,
    decimal Quantity,
    decimal UnitPrice,
    string CurrencyCode,
    decimal DiscountPercent,
    string? Notes) : IRequest<SalesOrderDto>;

public class UpdateOrderLineCommandValidator : AbstractValidator<UpdateOrderLineCommand>
{
    public UpdateOrderLineCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
    }
}

public class UpdateOrderLineCommandHandler : IRequestHandler<UpdateOrderLineCommand, SalesOrderDto>
{
    private readonly ISalesOrderRepository _repo;

    public UpdateOrderLineCommandHandler(ISalesOrderRepository repo) => _repo = repo;

    public async Task<SalesOrderDto> Handle(UpdateOrderLineCommand cmd, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(cmd.OrderId, ct)
            ?? throw new DomainException($"SalesOrder '{cmd.OrderId}' not found.");

        order.UpdateLine(cmd.LineId, cmd.Quantity,
            Money.Of(cmd.UnitPrice, cmd.CurrencyCode), cmd.DiscountPercent, cmd.Notes);

        await _repo.UpdateAsync(order, ct);
        return order.ToDto();
    }
}
