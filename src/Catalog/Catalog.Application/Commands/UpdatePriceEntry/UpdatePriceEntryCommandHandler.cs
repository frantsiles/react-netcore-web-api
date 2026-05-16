using Api.Domain.Common;
using Catalog.Application.Common.Dtos;
using Catalog.Application.Common.Mappings;
using Catalog.Domain.Repositories;
using MediatR;

namespace Catalog.Application.Commands.UpdatePriceEntry;

public class UpdatePriceEntryCommandHandler(IPriceListRepository repository)
    : IRequestHandler<UpdatePriceEntryCommand, PriceListDto>
{
    public async Task<PriceListDto> Handle(UpdatePriceEntryCommand request, CancellationToken ct)
    {
        var pl = await repository.GetByIdAsync(request.PriceListId, ct)
            ?? throw new DomainException($"Price list '{request.PriceListId}' not found.");

        pl.UpdateEntry(request.EntryId,
            Money.Of(request.UnitPrice, pl.CurrencyCode),
            request.MinQuantity);

        await repository.UpdateAsync(pl, ct);
        return pl.ToDto();
    }
}
