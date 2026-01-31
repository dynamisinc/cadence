namespace Cadence.Core.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated.
/// This indicates a request is technically valid but violates domain rules.
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException() : base()
    {
    }

    public BusinessRuleException(string message) : base(message)
    {
    }

    public BusinessRuleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
