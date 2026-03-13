using Payments.Application.Events;

namespace Payments.Application.Interfaces;

public interface IOrderPaymentProcessor
{
    PaymentProcessedEvent Process(OrderPlacedEvent orderPlacedEvent);
}
