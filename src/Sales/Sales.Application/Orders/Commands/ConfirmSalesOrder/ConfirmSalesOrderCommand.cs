using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Sales.Application.Common.Dtos;
using Sales.Application.Common.Mappings;
using Sales.Domain.Repositories;

namespace Sales.Application.Orders.Commands.ConfirmSalesOrder;

public record ConfirmSalesOrderCommand(Guid OrderId) : IRequest<SalesOrderDto>;

public class ConfirmSalesOrderCommandValidator : AbstractValidator<ConfirmSalesOrderCommand>
{
    public ConfirmSalesOrderCommandValidator() => RuleFor(x => x.OrderId).NotEmpty();
}

public class ConfirmSalesOrderCommandHandler : IRequestHandler<ConfirmSalesOrderCommand, SalesOrderDto>
{
    private readonly ISalesOrderRepository _repo;
    private readonly IEventPublisher _events;

    public ConfirmSalesOrderCommandHandler(ISalesOrderRepository repo, IEventPublisher events)
        => (_repo, _events) = (repo, events);

    public async Task<SalesOrderDto> Handle(ConfirmSalesOrderCommand cmd, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(cmd.OrderId, ct)
            ?? throw new DomainException($"SalesOrder '{cmd.OrderId}' not found.");

        order.Confirm();
        await _repo.UpdateAsync(order, ct);
        foreach (var e in order.DomainEvents)
            await _events.PublishAsync(e, ct);

        return order.ToDto();
    }
}
