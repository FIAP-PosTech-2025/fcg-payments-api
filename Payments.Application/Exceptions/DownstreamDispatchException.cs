namespace Payments.Application.Exceptions;

public class DownstreamDispatchException : Exception
{
    public DownstreamDispatchException(string message)
        : base(message)
    {
    }

    public DownstreamDispatchException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
