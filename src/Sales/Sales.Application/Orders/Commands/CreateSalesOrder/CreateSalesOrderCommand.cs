using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Sales.Application.Common.Dtos;
using Sales.Application.Common.Mappings;
using Sales.Domain.Orders;
using Sales.Domain.Repositories;

namespace Sales.Application.Orders.Commands.CreateSalesOrder;

public record CreateSalesOrderCommand(
    Guid CustomerId,
    DateOnly OrderDate,
    string CurrencyCode,
    string CountryCode,
    DateOnly? RequestedDeliveryDate,
    string? Notes) : IRequest<SalesOrderDto>;

public class CreateSalesOrderCommandValidator : AbstractValidator<CreateSalesOrderCommand>
{
    public CreateSalesOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.CountryCode).NotEmpty().Length(2);
    }
}

public class CreateSalesOrderCommandHandler : IRequestHandler<CreateSalesOrderCommand, SalesOrderDto>
{
    private readonly ISalesOrderRepository _repo;
    private readonly IEventPublisher _events;

    public CreateSalesOrderCommandHandler(ISalesOrderRepository repo, IEventPublisher events)
        => (_repo, _events) = (repo, events);

    public async Task<SalesOrderDto> Handle(CreateSalesOrderCommand cmd, CancellationToken ct)
    {
        var orderNumber = GenerateOrderNumber();
        var order = SalesOrder.Create(orderNumber, cmd.CustomerId, cmd.OrderDate,
            cmd.CurrencyCode, cmd.CountryCode, cmd.RequestedDeliveryDate, null, cmd.Notes);

        await _repo.AddAsync(order, ct);
        foreach (var e in order.DomainEvents)
            await _events.PublishAsync(e, ct);

        return order.ToDto();
    }

    private static string GenerateOrderNumber() =>
        $"SO-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
