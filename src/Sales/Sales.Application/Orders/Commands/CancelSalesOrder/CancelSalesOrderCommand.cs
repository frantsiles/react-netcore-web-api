using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Sales.Application.Common.Dtos;
using Sales.Application.Common.Mappings;
using Sales.Domain.Repositories;

namespace Sales.Application.Orders.Commands.CancelSalesOrder;

public record CancelSalesOrderCommand(Guid OrderId, string Reason) : IRequest<SalesOrderDto>;

public class CancelSalesOrderCommandValidator : AbstractValidator<CancelSalesOrderCommand>
{
    public CancelSalesOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty();
    }
}

public class CancelSalesOrderCommandHandler : IRequestHandler<CancelSalesOrderCommand, SalesOrderDto>
{
    private readonly ISalesOrderRepository _repo;
    private readonly IEventPublisher _events;

    public CancelSalesOrderCommandHandler(ISalesOrderRepository repo, IEventPublisher events)
        => (_repo, _events) = (repo, events);

    public async Task<SalesOrderDto> Handle(CancelSalesOrderCommand cmd, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(cmd.OrderId, ct)
            ?? throw new DomainException($"SalesOrder '{cmd.OrderId}' not found.");

        order.Cancel(cmd.Reason);
        await _repo.UpdateAsync(order, ct);
        foreach (var e in order.DomainEvents)
            await _events.PublishAsync(e, ct);

        return order.ToDto();
    }
}
