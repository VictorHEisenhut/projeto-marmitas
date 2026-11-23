using Marmitas.Domain.Errors;

namespace Marmitas.Domain.Exceptions;

public class DomainException : Exception
{
    public string Code { get; }
    public ErrorKind Kind { get; }

    public DomainException(string message, string code = ErrorCodes.ValidationError, ErrorKind kind = ErrorKind.Validation)
        : base(message)
    {
        Code = code;
        Kind = kind;
    }

    public static DomainException Validation(string message, string code = ErrorCodes.ValidationError) =>
        new(message, code, ErrorKind.Validation);

    public static DomainException NotFound(string message, string code = ErrorCodes.NotFound) =>
        new(message, code, ErrorKind.NotFound);

    public static DomainException Conflict(string message, string code = ErrorCodes.ValidationError) =>
        new(message, code, ErrorKind.Conflict);

    public static DomainException Forbidden(string message, string code = ErrorCodes.Forbidden) =>
        new(message, code, ErrorKind.Forbidden);

    public static DomainException Unauthorized(string message, string code = ErrorCodes.Unauthorized) =>
        new(message, code, ErrorKind.Unauthorized);
}
