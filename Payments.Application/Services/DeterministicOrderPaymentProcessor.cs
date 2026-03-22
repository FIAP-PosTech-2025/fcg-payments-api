using Microsoft.Extensions.Options;
using Payments.Application.Events;
using Payments.Application.Interfaces;
using Payments.Application.Options;
using Payments.Domain.Enums;

namespace Payments.Application.Services;

public class DeterministicOrderPaymentProcessor : IOrderPaymentProcessor
{
    private readonly PaymentRulesOptions _paymentRules;

    public DeterministicOrderPaymentProcessor(IOptions<PaymentRulesOptions> paymentRules)
    {
        _paymentRules = paymentRules.Value;
    }

    public PaymentProcessedEvent Process(OrderPlacedEvent orderPlacedEvent)
    {
        var status = orderPlacedEvent.Preco <= _paymentRules.MaxPrecoAprovado
            ? PaymentStatus.Aprovado
            : PaymentStatus.Reprovado;

        return new PaymentProcessedEvent(
            orderPlacedEvent.UserId,
            orderPlacedEvent.JogoId,
            Guid.NewGuid(),
            (int)status);
    }
}
