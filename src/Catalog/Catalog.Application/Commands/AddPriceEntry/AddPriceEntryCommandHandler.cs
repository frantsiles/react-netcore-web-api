using Api.Domain.Common;
using Catalog.Application.Common.Dtos;
using Catalog.Application.Common.Mappings;
using Catalog.Domain.Catalog;
using Catalog.Domain.Repositories;
using MediatR;

namespace Catalog.Application.Commands.AddPriceEntry;

public class AddPriceEntryCommandHandler(IPriceListRepository repository)
    : IRequestHandler<AddPriceEntryCommand, PriceListDto>
{
    public async Task<PriceListDto> Handle(AddPriceEntryCommand request, CancellationToken ct)
    {
        var pl = await repository.GetByIdAsync(request.PriceListId, ct)
            ?? throw new DomainException($"Price list '{request.PriceListId}' not found.");

        var entry = PriceListEntry.Create(
            request.CatalogItemId,
            Money.Of(request.UnitPrice, pl.CurrencyCode),
            request.MinQuantity);

        pl.AddEntry(entry);
        await repository.UpdateAsync(pl, ct);

        return pl.ToDto();
    }
}
