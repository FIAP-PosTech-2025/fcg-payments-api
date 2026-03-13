using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Payments.Application.Events;
using Payments.Application.Exceptions;
using Payments.Infra.Messaging;
using Payments.Tests.TestDoubles;

namespace Payments.Tests.Infra;

public class HttpPaymentProcessedEventDispatcherTests
{
    [Fact]
    public async Task DeveEnviarEventoParaCatalogENotificationsComPayloadCorreto()
    {
        var catalogHandler = new RecordingHandler(HttpStatusCode.NoContent);
        var notificationsHandler = new RecordingHandler(HttpStatusCode.NoContent);

        var clients = new Dictionary<string, HttpClient>
        {
            ["CatalogApi"] = new HttpClient(catalogHandler) { BaseAddress = new Uri("http://catalog.local") },
            ["NotificationsApi"] = new HttpClient(notificationsHandler) { BaseAddress = new Uri("http://notifications.local") }
        };

        var factory = new FakeHttpClientFactory(clients);
        var dispatcher = new HttpPaymentProcessedEventDispatcher(factory, NullLogger<HttpPaymentProcessedEventDispatcher>.Instance);

        var evt = new PaymentProcessedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2);

        await dispatcher.DispatchAsync(evt, CancellationToken.None);

        Assert.Single(catalogHandler.Requests);
        Assert.Single(notificationsHandler.Requests);

        var catalogRequest = catalogHandler.Requests.Single();
        Assert.Equal(HttpMethod.Post, catalogRequest.Method);
        Assert.Equal("http://catalog.local/api/payment-events/processed", catalogRequest.Uri?.ToString());

        var notificationRequest = notificationsHandler.Requests.Single();
        Assert.Equal(HttpMethod.Post, notificationRequest.Method);
        Assert.Equal("http://notifications.local/api/notifications/payment-processed", notificationRequest.Uri?.ToString());

        var payloadCatalog = JsonSerializer.Deserialize<PaymentProcessedEvent>(catalogRequest.Body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var payloadNotification = JsonSerializer.Deserialize<PaymentProcessedEvent>(notificationRequest.Body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(payloadCatalog);
        Assert.NotNull(payloadNotification);

        Assert.Equal(evt.UserId, payloadCatalog!.UserId);
        Assert.Equal(evt.JogoId, payloadCatalog.JogoId);
        Assert.Equal(evt.PayId, payloadCatalog.PayId);
        Assert.Equal(evt.Status, payloadCatalog.Status);

        Assert.Equal(evt.UserId, payloadNotification!.UserId);
        Assert.Equal(evt.JogoId, payloadNotification.JogoId);
        Assert.Equal(evt.PayId, payloadNotification.PayId);
        Assert.Equal(evt.Status, payloadNotification.Status);
    }

    [Fact]
    public async Task DeveFalharQuandoDownstreamRetornaErro()
    {
        var catalogHandler = new RecordingHandler(HttpStatusCode.InternalServerError);
        var notificationsHandler = new RecordingHandler(HttpStatusCode.NoContent);

        var clients = new Dictionary<string, HttpClient>
        {
            ["CatalogApi"] = new HttpClient(catalogHandler) { BaseAddress = new Uri("http://catalog.local") },
            ["NotificationsApi"] = new HttpClient(notificationsHandler) { BaseAddress = new Uri("http://notifications.local") }
        };

        var factory = new FakeHttpClientFactory(clients);
        var dispatcher = new HttpPaymentProcessedEventDispatcher(factory, NullLogger<HttpPaymentProcessedEventDispatcher>.Instance);

        var evt = new PaymentProcessedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2);

        await Assert.ThrowsAsync<DownstreamDispatchException>(() => dispatcher.DispatchAsync(evt, CancellationToken.None));
    }
}
