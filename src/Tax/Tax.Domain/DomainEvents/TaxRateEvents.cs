using Api.Domain.Common;

namespace Tax.Domain.DomainEvents;

public record TaxRateCreatedEvent(
    Guid TaxRateId,
    Guid TenantId,
    string Code,
    decimal Rate) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
