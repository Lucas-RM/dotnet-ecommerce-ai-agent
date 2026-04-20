namespace ECommerce.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public sealed class NotFoundDomainException : DomainException
{
    public NotFoundDomainException(string message) : base(message) { }
}

public sealed class ConflictDomainException : DomainException
{
    public ConflictDomainException(string message) : base(message) { }
}

public sealed class UnauthorizedDomainException : DomainException
{
    public UnauthorizedDomainException(string message) : base(message) { }
}
