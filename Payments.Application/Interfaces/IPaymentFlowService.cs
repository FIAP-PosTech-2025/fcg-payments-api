using Payments.Application.Events;

namespace Payments.Application.Interfaces;

public interface IPaymentFlowService
{
    Task ProcessOrderPlacedAsync(OrderPlacedEvent orderPlacedEvent, CancellationToken ct);
}
