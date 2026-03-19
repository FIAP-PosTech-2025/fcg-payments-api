namespace Payments.Application.Exceptions;

public class MessageDispatchException : Exception
{
    public MessageDispatchException(string message)
        : base(message)
    {
    }

    public MessageDispatchException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
