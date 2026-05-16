using Api.Domain.Common;
using Catalog.Application.Common.Dtos;
using Catalog.Application.Common.Mappings;
using Catalog.Domain.Repositories;
using MediatR;

namespace Catalog.Application.Commands.RemovePriceEntry;

public class RemovePriceEntryCommandHandler(IPriceListRepository repository)
    : IRequestHandler<RemovePriceEntryCommand, PriceListDto>
{
    public async Task<PriceListDto> Handle(RemovePriceEntryCommand request, CancellationToken ct)
    {
        var pl = await repository.GetByIdAsync(request.PriceListId, ct)
            ?? throw new DomainException($"Price list '{request.PriceListId}' not found.");

        pl.RemoveEntry(request.EntryId);
        await repository.UpdateAsync(pl, ct);
        return pl.ToDto();
    }
}
