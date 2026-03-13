namespace Payments.Application.Events;

public sealed record OrderPlacedEvent(Guid UserId, Guid JogoId, decimal Preco);
