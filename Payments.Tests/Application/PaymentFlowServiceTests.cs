using Microsoft.Extensions.Logging.Abstractions;
using Payments.Application.Events;
using Payments.Application.Interfaces;
using Payments.Application.Services;
using Payments.Tests.TestDoubles;

namespace Payments.Tests.Application;

public class PaymentFlowServiceTests
{
    [Fact]
    public async Task DeveProcessarPedidoEDispararEvento()
    {
        var dispatcher = new RecordingDispatcher();
        var service = new PaymentFlowService(
            new StubOrderPaymentProcessor(),
            dispatcher,
            NullLogger<PaymentFlowService>.Instance);

        var orderPlacedEvent = new OrderPlacedEvent(Guid.NewGuid(), Guid.NewGuid(), 90m);

        await service.ProcessOrderPlacedAsync(orderPlacedEvent, CancellationToken.None);

        var published = Assert.Single(dispatcher.Events);
        Assert.Equal(orderPlacedEvent.UserId, published.UserId);
        Assert.Equal(orderPlacedEvent.JogoId, published.JogoId);
        Assert.Equal(2, published.Status);
    }

    [Fact]
    public async Task DeveFalharQuandoPrecoForInvalido()
    {
        var dispatcher = new RecordingDispatcher();
        var service = new PaymentFlowService(
            new StubOrderPaymentProcessor(),
            dispatcher,
            NullLogger<PaymentFlowService>.Instance);

        var orderPlacedEvent = new OrderPlacedEvent(Guid.NewGuid(), Guid.NewGuid(), 0m);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ProcessOrderPlacedAsync(orderPlacedEvent, CancellationToken.None));
        Assert.Empty(dispatcher.Events);
    }

    private sealed class StubOrderPaymentProcessor : IOrderPaymentProcessor
    {
        public PaymentProcessedEvent Process(OrderPlacedEvent orderPlacedEvent)
        {
            return new PaymentProcessedEvent(
                orderPlacedEvent.UserId,
                orderPlacedEvent.JogoId,
                Guid.NewGuid(),
                2);
        }
    }
}
