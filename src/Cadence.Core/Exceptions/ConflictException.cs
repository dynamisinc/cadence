using System.Diagnostics.CodeAnalysis;

namespace Cadence.Core.Exceptions;

/// <summary>
/// Exception thrown when a request conflicts with the current state of the resource.
/// Typically maps to HTTP 409.
/// </summary>
[ExcludeFromCodeCoverage]
public class ConflictException : Exception
{
    public ConflictException() : base()
    {
    }

    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
