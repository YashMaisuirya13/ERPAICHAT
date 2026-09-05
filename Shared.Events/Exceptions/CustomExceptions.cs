namespace Shared.Events.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class BadRequestException : Exception
{
    public List<string> Errors { get; }

    public BadRequestException(string message, List<string>? errors = null) : base(message)
    {
        Errors = errors ?? new();
    }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Unauthorized access") : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Forbidden access") : base(message) { }
}
