using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payments.Application.Events;
using Payments.Application.Exceptions;
using Payments.Application.Interfaces;
using RabbitMQ.Client;

namespace Payments.Infra.Messaging;

public sealed class RabbitMqPaymentProcessedEventDispatcher : IPaymentProcessedEventDispatcher
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqPaymentProcessedEventDispatcher> _logger;

    public RabbitMqPaymentProcessedEventDispatcher(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqPaymentProcessedEventDispatcher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task DispatchAsync(PaymentProcessedEvent paymentProcessedEvent, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: _options.PaymentProcessedQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var body = JsonSerializer.SerializeToUtf8Bytes(paymentProcessedEvent);
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Type = nameof(PaymentProcessedEvent);
            properties.MessageId = paymentProcessedEvent.PayId.ToString("N");
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            channel.BasicPublish(
                exchange: string.Empty,
                routingKey: _options.PaymentProcessedQueue,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "PaymentProcessedEvent publicado na fila {QueueName} para UserId {UserId} e JogoId {JogoId}",
                _options.PaymentProcessedQueue,
                paymentProcessedEvent.UserId,
                paymentProcessedEvent.JogoId);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new MessageDispatchException("Falha ao publicar PaymentProcessedEvent no RabbitMQ.", ex);
        }
    }
}
