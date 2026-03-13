using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Payments.Application.Events;
using Payments.Application.Interfaces;
using Payments.Application.Options;
using Payments.Infra.Messaging;
using Payments.Tests.TestDoubles;

namespace Payments.Tests.Api;

public class PaymentsControllerIntegrationTests
{
    [Fact]
    public async Task PostOrderPlaced_Valido_DeveRetornar204()
    {
        await using var factory = new SuccessWebApplicationFactory();
        using var client = factory.CreateClient();

        var body = new
        {
            userId = Guid.NewGuid(),
            jogoId = Guid.NewGuid(),
            preco = 150m
        };

        var response = await client.PostAsJsonAsync("/api/payments/order-placed", body);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        Assert.Single(factory.CatalogHandler.Requests);
        Assert.Single(factory.NotificationsHandler.Requests);

        var catalogPayload = JsonSerializer.Deserialize<PaymentProcessedEvent>(
            factory.CatalogHandler.Requests.Single().Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(catalogPayload);
        Assert.Equal(body.userId, catalogPayload!.UserId);
        Assert.Equal(body.jogoId, catalogPayload.JogoId);
        Assert.Equal(2, catalogPayload.Status);
        Assert.NotEqual(Guid.Empty, catalogPayload.PayId);
    }

    [Fact]
    public async Task PostOrderPlaced_Invalido_DeveRetornar400()
    {
        await using var factory = new SuccessWebApplicationFactory();
        using var client = factory.CreateClient();

        var body = new
        {
            userId = Guid.Empty,
            jogoId = Guid.NewGuid(),
            preco = 150m
        };

        var response = await client.PostAsJsonAsync("/api/payments/order-placed", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostOrderPlaced_FalhaNoDownstream_DeveRetornar502()
    {
        await using var factory = new FailureWebApplicationFactory();
        using var client = factory.CreateClient();

        var body = new
        {
            userId = Guid.NewGuid(),
            jogoId = Guid.NewGuid(),
            preco = 150m
        };

        var response = await client.PostAsJsonAsync("/api/payments/order-placed", body);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    private sealed class SuccessWebApplicationFactory : WebApplicationFactory<Program>
    {
        public CatalogRecordingHandler CatalogHandler { get; } = new();
        public NotificationsRecordingHandler NotificationsHandler { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.PostConfigure<DownstreamOptions>(options =>
                {
                    options.CatalogBaseUrl = "http://catalog.test";
                    options.NotificationsBaseUrl = "http://notifications.test";
                });

                services.AddSingleton(CatalogHandler);
                services.AddSingleton(NotificationsHandler);

                services.RemoveAll<IPaymentProcessedEventDispatcher>();
                services.AddScoped<IPaymentProcessedEventDispatcher, HttpPaymentProcessedEventDispatcher>();

                services.AddHttpClient("CatalogApi").ConfigurePrimaryHttpMessageHandler(sp =>
                    sp.GetRequiredService<CatalogRecordingHandler>());

                services.AddHttpClient("NotificationsApi").ConfigurePrimaryHttpMessageHandler(sp =>
                    sp.GetRequiredService<NotificationsRecordingHandler>());
            });
        }
    }

    private sealed class FailureWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPaymentProcessedEventDispatcher>();
                services.AddScoped<IPaymentProcessedEventDispatcher, FailingDispatcher>();
            });
        }
    }

    private sealed class FailingDispatcher : IPaymentProcessedEventDispatcher
    {
        public Task DispatchAsync(PaymentProcessedEvent paymentProcessedEvent, CancellationToken ct)
        {
            throw new Payments.Application.Exceptions.DownstreamDispatchException("Falha simulada no downstream.");
        }
    }

    private sealed class CatalogRecordingHandler : RecordingHandler
    {
        public CatalogRecordingHandler() : base(HttpStatusCode.NoContent)
        {
        }
    }

    private sealed class NotificationsRecordingHandler : RecordingHandler
    {
        public NotificationsRecordingHandler() : base(HttpStatusCode.NoContent)
        {
        }
    }
}
