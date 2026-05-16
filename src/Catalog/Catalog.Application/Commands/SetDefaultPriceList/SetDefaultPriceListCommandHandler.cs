using Api.Domain.Common;
using Catalog.Domain.Repositories;
using MediatR;

namespace Catalog.Application.Commands.SetDefaultPriceList;

public class SetDefaultPriceListCommandHandler(IPriceListRepository repository)
    : IRequestHandler<SetDefaultPriceListCommand>
{
    public async Task Handle(SetDefaultPriceListCommand request, CancellationToken ct)
    {
        var newDefault = await repository.GetByIdAsync(request.PriceListId, ct)
            ?? throw new DomainException($"Price list '{request.PriceListId}' not found.");

        var current = await repository.GetDefaultAsync(ct);
        if (current is not null && current.Id != request.PriceListId)
        {
            current.UnsetDefault();
            await repository.UpdateAsync(current, ct);
        }

        newDefault.SetAsDefault();
        await repository.UpdateAsync(newDefault, ct);
    }
}
