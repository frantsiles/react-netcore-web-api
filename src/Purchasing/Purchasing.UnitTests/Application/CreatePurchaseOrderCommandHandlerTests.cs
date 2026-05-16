using Api.Application.Common.Interfaces;
using FluentAssertions;
using Moq;
using Purchasing.Application.PurchaseOrders.Commands.CreatePurchaseOrder;
using Purchasing.Domain.PurchaseOrders;
using Purchasing.Domain.Repositories;

namespace Purchasing.UnitTests.Application;

public class CreatePurchaseOrderCommandHandlerTests
{
    private readonly Mock<IPurchaseOrderRepository> _repo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IEventPublisher> _events = new();
    private readonly CreatePurchaseOrderCommandHandler _sut;

    public CreatePurchaseOrderCommandHandlerTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(Guid.NewGuid());
        _sut = new CreatePurchaseOrderCommandHandler(_repo.Object, _tenant.Object, _events.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesPurchaseOrder()
    {
        var supplierId = Guid.NewGuid();
        var cmd = new CreatePurchaseOrderCommand(supplierId, "USD", "US");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.SupplierId.Should().Be(supplierId);
        result.Status.Should().Be(PurchaseOrderStatus.Draft);
        result.PoNumber.Should().StartWith("PO-");
        _repo.Verify(r => r.AddAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PublishesDomainEvents()
    {
        var cmd = new CreatePurchaseOrderCommand(Guid.NewGuid(), "USD", "US");

        await _sut.Handle(cmd, CancellationToken.None);

        _events.Verify(e => e.PublishAsync(
            It.IsAny<Api.Domain.Common.IDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
