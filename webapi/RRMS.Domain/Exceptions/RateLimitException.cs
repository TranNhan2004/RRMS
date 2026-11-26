namespace RRMS.Domain.Exceptions;

public class RateLimitException : Exception
{
    public RateLimitException(string message) : base(message)
    {
    }
}
