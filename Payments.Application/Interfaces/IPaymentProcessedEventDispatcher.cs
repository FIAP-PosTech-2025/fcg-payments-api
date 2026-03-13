using Payments.Application.Events;

namespace Payments.Application.Interfaces;

public interface IPaymentProcessedEventDispatcher
{
    Task DispatchAsync(PaymentProcessedEvent paymentProcessedEvent, CancellationToken ct);
}
