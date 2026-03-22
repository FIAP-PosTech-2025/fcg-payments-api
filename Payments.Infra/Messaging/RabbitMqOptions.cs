namespace Payments.Infra.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; init; } = null!;
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string VirtualHost { get; init; } = "/";
    public string OrderPlacedQueue { get; init; } = null!;
    public string PaymentProcessedExchange { get; init; } = null!;
    public string PaymentProcessedQueue { get; init; } = null!;
}
