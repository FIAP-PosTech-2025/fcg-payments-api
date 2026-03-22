using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.Application.Events;
using Payments.Application.Interfaces;

namespace Payments.Api.Controllers;

[ApiController]
[Route("api/payments")]
[AllowAnonymous]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentFlowService _paymentFlowService;

    public PaymentsController(IPaymentFlowService paymentFlowService)
    {
        _paymentFlowService = paymentFlowService;
    }

    /// <summary>
    /// Recebe uma ordem de compra e processa o pagamento de forma deterministica.
    /// </summary>
    [HttpPost("order-placed")]
    public async Task<IActionResult> OrderPlaced([FromBody] OrderPlacedEvent orderPlacedEvent, CancellationToken ct)
    {
        await _paymentFlowService.ProcessOrderPlacedAsync(orderPlacedEvent, ct);
        return NoContent();
    }
}
