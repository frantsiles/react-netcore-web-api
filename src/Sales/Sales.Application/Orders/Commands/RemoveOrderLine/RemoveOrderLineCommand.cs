using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Sales.Application.Common.Dtos;
using Sales.Application.Common.Mappings;
using Sales.Domain.Repositories;

namespace Sales.Application.Orders.Commands.RemoveOrderLine;

public record RemoveOrderLineCommand(Guid OrderId, Guid LineId) : IRequest<SalesOrderDto>;

public class RemoveOrderLineCommandValidator : AbstractValidator<RemoveOrderLineCommand>
{
    public RemoveOrderLineCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
    }
}

public class RemoveOrderLineCommandHandler : IRequestHandler<RemoveOrderLineCommand, SalesOrderDto>
{
    private readonly ISalesOrderRepository _repo;

    public RemoveOrderLineCommandHandler(ISalesOrderRepository repo) => _repo = repo;

    public async Task<SalesOrderDto> Handle(RemoveOrderLineCommand cmd, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(cmd.OrderId, ct)
            ?? throw new DomainException($"SalesOrder '{cmd.OrderId}' not found.");

        order.RemoveLine(cmd.LineId);
        await _repo.UpdateAsync(order, ct);
        return order.ToDto();
    }
}
