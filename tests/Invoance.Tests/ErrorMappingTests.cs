using Invoance.Exceptions;
using Invoance.Internal;
using Xunit;

namespace Invoance.Tests;

public class ErrorMappingTests
{
    [Theory]
    [InlineData(400, typeof(ValidationException))]
    [InlineData(401, typeof(AuthenticationException))]
    [InlineData(403, typeof(ForbiddenException))]
    [InlineData(404, typeof(NotFoundException))]
    [InlineData(409, typeof(ConflictException))]
    [InlineData(429, typeof(QuotaExceededException))]
    [InlineData(500, typeof(ServerException))]
    [InlineData(503, typeof(ServerException))]
    [InlineData(418, typeof(InvoanceException))]
    public void ThrowForStatus_MapsStatusToType(int status, Type expected)
    {
        var request = new RequestContext("GET", "/events");
        var ex = Record.Exception(() => Errors.ThrowForStatus(status, null, request, null));
        Assert.NotNull(ex);
        Assert.IsType(expected, ex);
        var invo = Assert.IsAssignableFrom<InvoanceException>(ex);
        Assert.Equal(status, invo.StatusCode);
    }

    [Fact]
    public void ThrowForStatus_2xx_IsNoOp()
    {
        var request = new RequestContext("GET", "/events");
        Errors.ThrowForStatus(200, null, request, null);
        Errors.ThrowForStatus(204, null, request, null);
    }

    [Fact]
    public void ThrowForStatus_UsesServerMessageAndErrorCode()
    {
        var request = new RequestContext("POST", "/events");
        var body = new Dictionary<string, object?> { ["error"] = "bad_request", ["message"] = "payload required" };
        var ex = Assert.Throws<ValidationException>(() => Errors.ThrowForStatus(400, body, request, null));
        Assert.Equal("payload required", ex.Message);
        Assert.Equal("bad_request", ex.ErrorCode);
    }

    [Fact]
    public void ThrowForStatus_429_RateLimitMessageWhenNoBody()
    {
        var request = new RequestContext("GET", "/events");
        var ex = Assert.Throws<QuotaExceededException>(() => Errors.ThrowForStatus(429, null, request, 12));
        Assert.Contains("rate limited, retry after 12s", ex.Message);
        Assert.Equal(12, ex.RetryAfterSeconds);
    }

    [Fact]
    public void ThrowForStatus_UnknownErrorCode_WhenAbsent()
    {
        var request = new RequestContext("GET", "/events");
        var ex = Assert.Throws<NotFoundException>(() => Errors.ThrowForStatus(404, null, request, null));
        Assert.Equal("unknown", ex.ErrorCode);
        Assert.Contains("HTTP 404 on GET /events", ex.Message);
    }
}
