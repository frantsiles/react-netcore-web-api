using Api.Domain.Common;
using Catalog.Application.Common.Dtos;
using Catalog.Application.Common.Mappings;
using Catalog.Domain.Repositories;
using MediatR;

namespace Catalog.Application.Commands.UpdatePriceList;

public class UpdatePriceListCommandHandler(IPriceListRepository repository)
    : IRequestHandler<UpdatePriceListCommand, PriceListDto>
{
    public async Task<PriceListDto> Handle(UpdatePriceListCommand request, CancellationToken ct)
    {
        var pl = await repository.GetByIdAsync(request.PriceListId, ct)
            ?? throw new DomainException($"Price list '{request.PriceListId}' not found.");

        pl.Update(request.Name, request.ValidFrom, request.ValidTo);
        await repository.UpdateAsync(pl, ct);

        return pl.ToDto();
    }
}
