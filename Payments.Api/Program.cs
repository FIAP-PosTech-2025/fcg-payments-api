using Payments.Api.Middlewares;
using Payments.Application.Interfaces;
using Payments.Application.Options;
using Payments.Application.Services;
using Payments.Infra.Messaging;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", "Payments.Api")
        .WriteTo.Console(new CompactJsonFormatter())
        .WriteTo.File(
            new CompactJsonFormatter(),
            "logs/info-.json",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7)
        .WriteTo.File(
            new CompactJsonFormatter(),
            "logs/error-.json",
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30);
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "FCG Payments API",
        Version = "v1",
        Description = "Microservico de pagamentos do FIAP Cloud Games. Processa OrderPlacedEvent e publica PaymentProcessedEvent."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

builder.Services.AddOptions<PaymentRulesOptions>()
    .Bind(builder.Configuration.GetSection(PaymentRulesOptions.SectionName))
    .Validate(options => options.MaxPrecoAprovado > 0m,
        "PaymentRules:MaxPrecoAprovado deve ser maior que zero.")
    .ValidateOnStart();

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.HostName),
        "RabbitMq:HostName deve ser informado.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.UserName),
        "RabbitMq:UserName deve ser informado.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Password),
        "RabbitMq:Password deve ser informado.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.OrderPlacedQueue),
        "RabbitMq:OrderPlacedQueue deve ser informado.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.PaymentProcessedQueue),
        "RabbitMq:PaymentProcessedQueue deve ser informado.")
    .ValidateOnStart();

builder.Services.AddScoped<IOrderPaymentProcessor, DeterministicOrderPaymentProcessor>();
builder.Services.AddScoped<IPaymentProcessedEventDispatcher, RabbitMqPaymentProcessedEventDispatcher>();
builder.Services.AddScoped<IPaymentFlowService, PaymentFlowService>();
builder.Services.AddSingleton<OrderPlacedEventConsumer>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OrderPlacedEventConsumer>());

var app = builder.Build();

app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});
app.UseMiddleware<ErrorHandlingMiddleware>();
app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI();

app.Run();

public partial class Program { }
