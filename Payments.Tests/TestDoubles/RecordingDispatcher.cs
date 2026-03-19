using Payments.Application.Events;
using Payments.Application.Interfaces;

namespace Payments.Tests.TestDoubles;

public sealed class RecordingDispatcher : IPaymentProcessedEventDispatcher
{
    private readonly List<PaymentProcessedEvent> _events = new();

    public IReadOnlyList<PaymentProcessedEvent> Events => _events;

    public Task DispatchAsync(PaymentProcessedEvent paymentProcessedEvent, CancellationToken ct)
    {
        _events.Add(paymentProcessedEvent);
        return Task.CompletedTask;
    }
}
