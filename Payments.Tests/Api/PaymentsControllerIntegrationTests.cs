using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Payments.Application.Events;
using Payments.Application.Exceptions;
using Payments.Application.Interfaces;
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

        var published = Assert.Single(factory.Dispatcher.Events);
        Assert.Equal(body.userId, published.UserId);
        Assert.Equal(body.jogoId, published.JogoId);
        Assert.Equal(2, published.Status);
        Assert.NotEqual(Guid.Empty, published.PayId);
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
        public RecordingDispatcher Dispatcher { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(Dispatcher);

                services.RemoveAll<IPaymentProcessedEventDispatcher>();
                services.AddScoped<IPaymentProcessedEventDispatcher>(sp => sp.GetRequiredService<RecordingDispatcher>());
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
            throw new MessageDispatchException("Falha simulada no downstream.");
        }
    }
}
