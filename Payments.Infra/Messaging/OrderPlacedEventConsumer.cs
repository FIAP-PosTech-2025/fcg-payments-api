using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payments.Application.Events;
using Payments.Application.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Payments.Infra.Messaging;

public sealed class OrderPlacedEventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderPlacedEventConsumer> _logger;
    private readonly RabbitMqOptions _options;

    private IConnection? _connection;
    private IModel? _channel;

    public OrderPlacedEventConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderPlacedEventConsumer> logger,
        IOptions<RabbitMqOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return ExecuteWithRetryAsync(stoppingToken);
    }

    private async Task ExecuteWithRetryAsync(CancellationToken stoppingToken)
    {
        var attempt = 0;

        if (string.IsNullOrWhiteSpace(_options.HostName))
        {
            _logger.LogWarning("RabbitMq__HostName nao configurado. Consumidor de eventos sera desativado.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                attempt++;

                var factory = new ConnectionFactory
                {
                    HostName = _options.HostName,
                    Port = _options.Port,
                    UserName = _options.UserName,
                    Password = _options.Password,
                    VirtualHost = _options.VirtualHost,
                    DispatchConsumersAsync = true
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(
                    queue: _options.OrderPlacedQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += async (_, ea) =>
                {
                    try
                    {
                        var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var evt = JsonSerializer.Deserialize<OrderPlacedEvent>(message);

                        if (evt is null)
                        {
                            _logger.LogWarning("Mensagem de OrderPlacedEvent invalida: {Message}", message);
                            _channel.BasicNack(ea.DeliveryTag, false, false);
                            return;
                        }

                        await ProcessMessageAsync(evt, stoppingToken);
                        _channel.BasicAck(ea.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao processar OrderPlacedEvent");
                        _channel.BasicNack(ea.DeliveryTag, false, true);
                    }
                };

                _channel.BasicConsume(
                    queue: _options.OrderPlacedQueue,
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation(
                    "Consumidor RabbitMQ iniciado para fila {Queue} (host: {Host}).",
                    _options.OrderPlacedQueue,
                    _options.HostName);

                attempt = 0;
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _channel?.Dispose();
                _connection?.Dispose();
                _channel = null;
                _connection = null;

                if (attempt <= 3 || attempt % 6 == 0)
                {
                    _logger.LogWarning(
                        ex,
                        "Falha ao conectar no RabbitMQ (tentativa {Attempt}) em {Host}:{Port}. Tentando novamente em 10 segundos.",
                        attempt,
                        _options.HostName,
                        _options.Port);
                }
                else
                {
                    _logger.LogWarning(
                        "Falha ao conectar no RabbitMQ (tentativa {Attempt}) em {Host}:{Port}. Tentando novamente em 10 segundos.",
                        attempt,
                        _options.HostName,
                        _options.Port);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(OrderPlacedEvent orderPlacedEvent, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var paymentFlowService = scope.ServiceProvider.GetRequiredService<IPaymentFlowService>();
        await paymentFlowService.ProcessOrderPlacedAsync(orderPlacedEvent, ct);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
