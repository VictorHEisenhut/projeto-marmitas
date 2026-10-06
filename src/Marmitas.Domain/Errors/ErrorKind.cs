namespace Marmitas.Domain.Errors;

public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
    Unauthorized,
    Unexpected
}

