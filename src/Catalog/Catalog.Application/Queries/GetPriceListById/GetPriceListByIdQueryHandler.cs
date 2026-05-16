using Api.Domain.Common;
using Catalog.Application.Common.Dtos;
using Catalog.Application.Common.Mappings;
using Catalog.Domain.Repositories;
using MediatR;

namespace Catalog.Application.Queries.GetPriceListById;

public class GetPriceListByIdQueryHandler(IPriceListRepository repository)
    : IRequestHandler<GetPriceListByIdQuery, PriceListDto>
{
    public async Task<PriceListDto> Handle(GetPriceListByIdQuery request, CancellationToken ct)
    {
        var pl = await repository.GetByIdAsync(request.PriceListId, ct)
            ?? throw new DomainException($"Price list '{request.PriceListId}' not found.");
        return pl.ToDto();
    }
}
