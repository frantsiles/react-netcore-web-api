using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Sales.Application.Common.Dtos;
using Sales.Application.Common.Mappings;
using Sales.Domain.Orders;
using Sales.Domain.Repositories;

namespace Sales.Application.Quotes.Commands.ConvertQuoteToOrder;

public record ConvertQuoteToOrderCommand(
    Guid QuoteId,
    DateOnly? RequestedDeliveryDate,
    string? Notes) : IRequest<SalesOrderDto>;

public class ConvertQuoteToOrderCommandValidator : AbstractValidator<ConvertQuoteToOrderCommand>
{
    public ConvertQuoteToOrderCommandValidator() => RuleFor(x => x.QuoteId).NotEmpty();
}

public class ConvertQuoteToOrderCommandHandler
    : IRequestHandler<ConvertQuoteToOrderCommand, SalesOrderDto>
{
    private readonly IQuoteRepository _quoteRepo;
    private readonly ISalesOrderRepository _orderRepo;
    private readonly IEventPublisher _events;

    public ConvertQuoteToOrderCommandHandler(IQuoteRepository quoteRepo,
        ISalesOrderRepository orderRepo, IEventPublisher events)
        => (_quoteRepo, _orderRepo, _events) = (quoteRepo, orderRepo, events);

    public async Task<SalesOrderDto> Handle(ConvertQuoteToOrderCommand cmd, CancellationToken ct)
    {
        var quote = await _quoteRepo.GetByIdAsync(cmd.QuoteId, ct)
            ?? throw new DomainException($"Quote '{cmd.QuoteId}' not found.");

        var orderNumber = GenerateOrderNumber();
        var order = SalesOrder.Create(
            orderNumber, quote.CustomerId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            quote.CurrencyCode, quote.CountryCode,
            cmd.RequestedDeliveryDate, originQuoteId: quote.Id,
            cmd.Notes ?? quote.Notes);

        foreach (var ql in quote.Lines)
        {
            var line = SalesOrderLine.Create(ql.CatalogItemId, ql.SKU, ql.ItemName,
                ql.Quantity, ql.UnitPrice, ql.DiscountPercent, ql.Notes);
            order.AddLine(line);
        }

        // Marks Quote as ConvertedToOrder and raises QuoteConvertedToOrderEvent
        quote.MarkConvertedToOrder();

        await _orderRepo.AddAsync(order, ct);
        await _quoteRepo.UpdateAsync(quote, ct);

        foreach (var e in order.DomainEvents.Concat(quote.DomainEvents))
            await _events.PublishAsync(e, ct);

        return order.ToDto();
    }

    private static string GenerateOrderNumber() =>
        $"SO-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
