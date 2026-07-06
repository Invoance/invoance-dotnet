using Invoance.Exceptions;

namespace Invoance.Internal;

/// <summary>
/// Maps HTTP status codes to <see cref="InvoanceException"/> subclasses,
/// mirroring the reference SDK's <c>throwForStatus</c>.
/// </summary>
internal static class Errors
{
    /// <summary>
    /// No-op on 2xx. Otherwise builds and throws the appropriate
    /// <see cref="InvoanceException"/> subclass.
    /// </summary>
    public static void ThrowForStatus(
        int statusCode,
        IReadOnlyDictionary<string, object?>? body,
        RequestContext? request,
        double? retryAfterSeconds)
    {
        if (statusCode is >= 200 and < 300) return;

        var b = body ?? new Dictionary<string, object?>();
        var errorCode = b.TryGetValue("error", out var ec) && ec is string ecs ? ecs : "unknown";
        var serverMessage = b.TryGetValue("message", out var m) && m is string ms ? ms : null;

        string message;
        if (serverMessage != null)
        {
            message = serverMessage;
        }
        else if (statusCode == 429 && retryAfterSeconds != null)
        {
            message = $"HTTP 429{DescribeRequest(request)} — rate limited, retry after {retryAfterSeconds}s";
        }
        else
        {
            message = $"HTTP {statusCode}{DescribeRequest(request)} (no response body)";
        }

        throw Build(statusCode, message, errorCode, b, retryAfterSeconds, request);
    }

    private static InvoanceException Build(
        int statusCode,
        string message,
        string errorCode,
        IReadOnlyDictionary<string, object?> body,
        double? retryAfterSeconds,
        RequestContext? request)
    {
        return statusCode switch
        {
            400 => new ValidationException(message, statusCode, errorCode, body, retryAfterSeconds, request),
            401 => new AuthenticationException(message, statusCode, errorCode, body, retryAfterSeconds, request),
            403 => new ForbiddenException(message, statusCode, errorCode, body, retryAfterSeconds, request),
            404 => new NotFoundException(message, statusCode, errorCode, body, retryAfterSeconds, request),
            409 => new ConflictException(message, statusCode, errorCode, body, retryAfterSeconds, request),
            429 => new QuotaExceededException(message, statusCode, errorCode, body, retryAfterSeconds, request),
            >= 500 => new ServerException(message, statusCode, errorCode, body, retryAfterSeconds, request),
            _ => new InvoanceException(message, statusCode, errorCode, body, retryAfterSeconds, request),
        };
    }

    private static string DescribeRequest(RequestContext? ctx) =>
        ctx != null ? $" on {ctx.Method} {ctx.Path}" : "";
}
