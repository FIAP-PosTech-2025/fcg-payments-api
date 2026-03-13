using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Payments.Application.Events;
using Payments.Application.Exceptions;
using Payments.Application.Interfaces;

namespace Payments.Infra.Messaging;

public class HttpPaymentProcessedEventDispatcher : IPaymentProcessedEventDispatcher
{
    private const string CatalogProcessedPath = "/api/payment-events/processed";
    private const string NotificationsProcessedPath = "/api/notifications/payment-processed";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpPaymentProcessedEventDispatcher> _logger;

    public HttpPaymentProcessedEventDispatcher(
        IHttpClientFactory httpClientFactory,
        ILogger<HttpPaymentProcessedEventDispatcher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task DispatchAsync(PaymentProcessedEvent paymentProcessedEvent, CancellationToken ct)
    {
        await PostToCatalogAsync(paymentProcessedEvent, ct);
        await PostToNotificationsAsync(paymentProcessedEvent, ct);
    }

    private async Task PostToCatalogAsync(PaymentProcessedEvent paymentProcessedEvent, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CatalogApi");

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync(CatalogProcessedPath, paymentProcessedEvent, ct);
        }
        catch (Exception ex)
        {
            throw new DownstreamDispatchException("Falha ao enviar PaymentProcessedEvent para CatalogAPI.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            throw new DownstreamDispatchException(
                $"CatalogAPI respondeu com {(int)response.StatusCode} ao receber PaymentProcessedEvent. Detail: {detail}");
        }

        _logger.LogInformation(
            "PaymentProcessedEvent entregue ao CatalogAPI | UserId: {UserId} | JogoId: {JogoId} | PayId: {PayId} | Status: {Status}",
            paymentProcessedEvent.UserId,
            paymentProcessedEvent.JogoId,
            paymentProcessedEvent.PayId,
            paymentProcessedEvent.Status);
    }

    private async Task PostToNotificationsAsync(PaymentProcessedEvent paymentProcessedEvent, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("NotificationsApi");

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync(NotificationsProcessedPath, paymentProcessedEvent, ct);
        }
        catch (Exception ex)
        {
            throw new DownstreamDispatchException("Falha ao enviar PaymentProcessedEvent para NotificationsAPI.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            throw new DownstreamDispatchException(
                $"NotificationsAPI respondeu com {(int)response.StatusCode} ao receber PaymentProcessedEvent. Detail: {detail}");
        }

        _logger.LogInformation(
            "PaymentProcessedEvent entregue ao NotificationsAPI | UserId: {UserId} | JogoId: {JogoId} | PayId: {PayId} | Status: {Status}",
            paymentProcessedEvent.UserId,
            paymentProcessedEvent.JogoId,
            paymentProcessedEvent.PayId,
            paymentProcessedEvent.Status);
    }
}
