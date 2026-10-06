namespace Marmitas.Domain.Errors;

public static class ErrorCodes
{

    public const string ValidationError = "VALIDATION_ERROR";
    public const string RequestMalformed = "REQUEST_MALFORMED";
    public const string NotFound = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string MethodNotAllowed = "METHOD_NOT_ALLOWED";
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const string InternalError = "INTERNAL_ERROR";

    public const string AuthInvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string AuthTokenExpired = "AUTH_TOKEN_EXPIRED";
    public const string AuthEmailAlreadyInUse = "AUTH_EMAIL_ALREADY_IN_USE";
    public const string AuthUserBlocked = "AUTH_USER_BLOCKED";

    public const string OrderEmptyCart = "ORDER_EMPTY_CART";
    public const string OrderProductUnavailable = "ORDER_PRODUCT_UNAVAILABLE";
    public const string OrderInvalidStatusTransition = "ORDER_INVALID_STATUS_TRANSITION";

    public const string RestaurantNotApproved = "RESTAURANT_NOT_APPROVED";

    public const string PaymentDeclined = "PAYMENT_DECLINED";
    public const string PaymentWebhookSignatureInvalid = "PAYMENT_WEBHOOK_SIGNATURE_INVALID";
}

