using Microsoft.Extensions.Logging;
using Payments.Application.Events;
using Payments.Application.Interfaces;

namespace Payments.Application.Services;

public class PaymentFlowService : IPaymentFlowService
{
    private readonly IOrderPaymentProcessor _orderPaymentProcessor;
    private readonly IPaymentProcessedEventDispatcher _paymentProcessedEventDispatcher;
    private readonly ILogger<PaymentFlowService> _logger;

    public PaymentFlowService(
        IOrderPaymentProcessor orderPaymentProcessor,
        IPaymentProcessedEventDispatcher paymentProcessedEventDispatcher,
        ILogger<PaymentFlowService> logger)
    {
        _orderPaymentProcessor = orderPaymentProcessor;
        _paymentProcessedEventDispatcher = paymentProcessedEventDispatcher;
        _logger = logger;
    }

    public async Task ProcessOrderPlacedAsync(OrderPlacedEvent orderPlacedEvent, CancellationToken ct)
    {
        Validate(orderPlacedEvent);

        var paymentProcessedEvent = _orderPaymentProcessor.Process(orderPlacedEvent);

        _logger.LogInformation(
            "Pagamento processado | UserId: {UserId} | JogoId: {JogoId} | Preco: {Preco} | PayId: {PayId} | Status: {Status}",
            orderPlacedEvent.UserId,
            orderPlacedEvent.JogoId,
            orderPlacedEvent.Preco,
            paymentProcessedEvent.PayId,
            paymentProcessedEvent.Status);

        await _paymentProcessedEventDispatcher.DispatchAsync(paymentProcessedEvent, ct);
    }

    private static void Validate(OrderPlacedEvent orderPlacedEvent)
    {
        if (orderPlacedEvent.UserId == Guid.Empty)
            throw new ArgumentException("UserId invalido.");

        if (orderPlacedEvent.JogoId == Guid.Empty)
            throw new ArgumentException("JogoId invalido.");

        if (orderPlacedEvent.Preco <= 0)
            throw new ArgumentException("Preco deve ser maior que zero.");
    }
}
