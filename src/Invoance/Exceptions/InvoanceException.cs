namespace Invoance.Exceptions;

/// <summary>
/// Describes the request that produced an error (method + path).
/// </summary>
public sealed record RequestContext(string Method, string Path);

/// <summary>
/// Base exception for everything the SDK raises — API responses, network
/// failures, and client-side validation. A single
/// <c>catch (InvoanceException e)</c> catches anything the SDK throws.
/// </summary>
/// <example>
/// <code>
/// try
/// {
///     await client.Events.IngestAsync(new IngestEventParams { ... });
/// }
/// catch (QuotaExceededException e)
/// {
///     Console.WriteLine($"rate limited, retry in {e.RetryAfterSeconds}s");
/// }
/// catch (InvoanceException e)
/// {
///     Console.WriteLine($"Invoance error: {e.Message}");
/// }
/// </code>
/// </example>
public class InvoanceException : Exception
{
    /// <summary>HTTP status code, when the error originated from an API response.</summary>
    public int? StatusCode { get; }

    /// <summary>Machine-readable error code from the response body's <c>error</c> field.</summary>
    public string? ErrorCode { get; }

    /// <summary>Parsed JSON body of the error response, if any.</summary>
    public IReadOnlyDictionary<string, object?>? Body { get; }

    /// <summary>Seconds to wait before retrying, parsed from a <c>Retry-After</c> header.</summary>
    public double? RetryAfterSeconds { get; }

    /// <summary>The request (method + path) that produced this error.</summary>
    public RequestContext? RequestContext { get; }

    public InvoanceException(
        string message,
        int? statusCode = null,
        string? errorCode = null,
        IReadOnlyDictionary<string, object?>? body = null,
        double? retryAfterSeconds = null,
        RequestContext? requestContext = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Body = body;
        RetryAfterSeconds = retryAfterSeconds;
        RequestContext = requestContext;
    }
}

/// <summary>Raised on HTTP 401 — the API key was rejected.</summary>
public class AuthenticationException : InvoanceException
{
    public AuthenticationException(string message, int? statusCode = null, string? errorCode = null,
        IReadOnlyDictionary<string, object?>? body = null, double? retryAfterSeconds = null,
        RequestContext? requestContext = null, Exception? innerException = null)
        : base(message, statusCode, errorCode, body, retryAfterSeconds, requestContext, innerException) { }
}

/// <summary>Raised on HTTP 403 — the key authenticated but lacks permission.</summary>
public class ForbiddenException : InvoanceException
{
    public ForbiddenException(string message, int? statusCode = null, string? errorCode = null,
        IReadOnlyDictionary<string, object?>? body = null, double? retryAfterSeconds = null,
        RequestContext? requestContext = null, Exception? innerException = null)
        : base(message, statusCode, errorCode, body, retryAfterSeconds, requestContext, innerException) { }
}

/// <summary>Raised on HTTP 404 — resource not found.</summary>
public class NotFoundException : InvoanceException
{
    public NotFoundException(string message, int? statusCode = null, string? errorCode = null,
        IReadOnlyDictionary<string, object?>? body = null, double? retryAfterSeconds = null,
        RequestContext? requestContext = null, Exception? innerException = null)
        : base(message, statusCode, errorCode, body, retryAfterSeconds, requestContext, innerException) { }
}

/// <summary>Raised on HTTP 400, and also client-side for invalid input.</summary>
public class ValidationException : InvoanceException
{
    public ValidationException(string message, int? statusCode = null, string? errorCode = null,
        IReadOnlyDictionary<string, object?>? body = null, double? retryAfterSeconds = null,
        RequestContext? requestContext = null, Exception? innerException = null)
        : base(message, statusCode, errorCode, body, retryAfterSeconds, requestContext, innerException) { }
}

/// <summary>Raised on HTTP 409 — a conflict (e.g. idempotency clash).</summary>
public class ConflictException : InvoanceException
{
    public ConflictException(string message, int? statusCode = null, string? errorCode = null,
        IReadOnlyDictionary<string, object?>? body = null, double? retryAfterSeconds = null,
        RequestContext? requestContext = null, Exception? innerException = null)
        : base(message, statusCode, errorCode, body, retryAfterSeconds, requestContext, innerException) { }
}

/// <summary>Raised on HTTP 429 — rate limited / quota exceeded.</summary>
public class QuotaExceededException : InvoanceException
{
    public QuotaExceededException(string message, int? statusCode = null, string? errorCode = null,
        IReadOnlyDictionary<string, object?>? body = null, double? retryAfterSeconds = null,
        RequestContext? requestContext = null, Exception? innerException = null)
        : base(message, statusCode, errorCode, body, retryAfterSeconds, requestContext, innerException) { }
}

/// <summary>Raised on HTTP 5xx — server error.</summary>
public class ServerException : InvoanceException
{
    public ServerException(string message, int? statusCode = null, string? errorCode = null,
        IReadOnlyDictionary<string, object?>? body = null, double? retryAfterSeconds = null,
        RequestContext? requestContext = null, Exception? innerException = null)
        : base(message, statusCode, errorCode, body, retryAfterSeconds, requestContext, innerException) { }
}

/// <summary>
/// Raised when the request fails before a response is received —
/// DNS failure, connection refused, TLS handshake error, etc.
/// </summary>
public class NetworkException : InvoanceException
{
    public NetworkException(string message, int? statusCode = null, string? errorCode = null,
        IReadOnlyDictionary<string, object?>? body = null, double? retryAfterSeconds = null,
        RequestContext? requestContext = null, Exception? innerException = null)
        : base(message, statusCode, errorCode, body, retryAfterSeconds, requestContext, innerException) { }
}

/// <summary>Raised when the request exceeds the configured timeout.</summary>
public class TimeoutException : InvoanceException
{
    public TimeoutException(string message, int? statusCode = null, string? errorCode = null,
        IReadOnlyDictionary<string, object?>? body = null, double? retryAfterSeconds = null,
        RequestContext? requestContext = null, Exception? innerException = null)
        : base(message, statusCode, errorCode, body, retryAfterSeconds, requestContext, innerException) { }
}
