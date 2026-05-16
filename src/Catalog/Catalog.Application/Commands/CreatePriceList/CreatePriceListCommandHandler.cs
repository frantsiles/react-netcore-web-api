using Api.Application.Common.Interfaces;
using Catalog.Application.Common.Dtos;
using Catalog.Application.Common.Mappings;
using Catalog.Domain.Catalog;
using Catalog.Domain.Repositories;
using MediatR;

namespace Catalog.Application.Commands.CreatePriceList;

public class CreatePriceListCommandHandler(
    IPriceListRepository repository,
    IEventPublisher eventPublisher) : IRequestHandler<CreatePriceListCommand, PriceListDto>
{
    public async Task<PriceListDto> Handle(CreatePriceListCommand request, CancellationToken ct)
    {
        if (request.IsDefault)
        {
            var current = await repository.GetDefaultAsync(ct);
            if (current is not null) { current.UnsetDefault(); await repository.UpdateAsync(current, ct); }
        }

        var pl = PriceList.Create(request.Name, request.CurrencyCode,
            request.ValidFrom, request.ValidTo, request.IsDefault);

        await repository.AddAsync(pl, ct);

        foreach (var e in pl.DomainEvents) await eventPublisher.PublishAsync(e, ct);
        pl.ClearDomainEvents();

        return pl.ToDto();
    }
}
