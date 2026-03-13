using Microsoft.Extensions.Options;
using Payments.Application.Events;
using Payments.Application.Options;
using Payments.Application.Services;

namespace Payments.Tests.Application;

public class DeterministicOrderPaymentProcessorTests
{
    private readonly DeterministicOrderPaymentProcessor _processor;

    public DeterministicOrderPaymentProcessorTests()
    {
        var options = Options.Create(new PaymentRulesOptions { MaxPrecoAprovado = 200m });
        _processor = new DeterministicOrderPaymentProcessor(options);
    }

    [Fact]
    public void DeveAprovarQuandoPrecoAbaixoDoLimite()
    {
        var order = new OrderPlacedEvent(Guid.NewGuid(), Guid.NewGuid(), 199.99m);

        var result = _processor.Process(order);

        Assert.Equal(2, result.Status);
    }

    [Fact]
    public void DeveAprovarQuandoPrecoNoLimite()
    {
        var order = new OrderPlacedEvent(Guid.NewGuid(), Guid.NewGuid(), 200m);

        var result = _processor.Process(order);

        Assert.Equal(2, result.Status);
    }

    [Fact]
    public void DeveReprovarQuandoPrecoAcimaDoLimite()
    {
        var order = new OrderPlacedEvent(Guid.NewGuid(), Guid.NewGuid(), 200.01m);

        var result = _processor.Process(order);

        Assert.Equal(3, result.Status);
    }

    [Fact]
    public void DeveGerarPayIdValido()
    {
        var order = new OrderPlacedEvent(Guid.NewGuid(), Guid.NewGuid(), 50m);

        var result = _processor.Process(order);

        Assert.NotEqual(Guid.Empty, result.PayId);
    }

    [Fact]
    public void DeveRetornarApenasStatus2Ou3()
    {
        var below = _processor.Process(new OrderPlacedEvent(Guid.NewGuid(), Guid.NewGuid(), 50m));
        var above = _processor.Process(new OrderPlacedEvent(Guid.NewGuid(), Guid.NewGuid(), 500m));

        Assert.Contains(below.Status, new[] { 2, 3 });
        Assert.Contains(above.Status, new[] { 2, 3 });
    }
}
