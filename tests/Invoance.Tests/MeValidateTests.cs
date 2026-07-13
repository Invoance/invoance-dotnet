using System.Net;
using System.Text;
using Xunit;

namespace Invoance.Tests;

/// <summary>
/// Wire-shape tests for <see cref="InvoanceClient.ValidateAsync"/> and
/// <see cref="InvoanceClient.MeAsync"/> — both hit the scope-free
/// key-introspection endpoint <c>GET /v1/me</c>.
/// </summary>
public class MeValidateTests
{
    private const string MeJson =
        "{\"valid\":true," +
        "\"organization\":{\"id\":\"org_1\",\"name\":\"Acme\",\"issuer_name\":\"Acme Corp\"," +
        "\"primary_domain\":\"acme.test\",\"domain_verified\":true,\"plan_tier\":\"growth\"}," +
        "\"tenant\":{\"id\":\"ten_1\",\"name\":\"Acme\"}," +
        "\"api_key\":{\"id\":\"key_1\",\"name\":\"ci\",\"key_prefix\":\"sk_live_\",\"key_last4\":\"abcd\"," +
        "\"scopes\":[\"audit:read\",\"audit:write\"],\"created_at\":\"2026-07-01T00:00:00Z\",\"last_used_at\":null}," +
        "\"limits\":{\"rate_limit_per_sec\":50}}";

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _responseJson;
        private readonly HttpStatusCode _status;

        public HttpRequestMessage? Request;

        public RecordingHandler(string responseJson, HttpStatusCode status = HttpStatusCode.OK)
        {
            _responseJson = responseJson;
            _status = status;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json"),
            });
        }
    }

    private static (InvoanceClient Client, RecordingHandler Handler) MockClient(
        string responseJson, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new RecordingHandler(responseJson, status);
        var client = new InvoanceClient(
            new InvoanceClientOptions { ApiKey = "sk_test_x", BaseUrl = "https://api.example.test" },
            new HttpClient(handler));
        return (client, handler);
    }

    [Fact]
    public async Task ValidateAsync_SendsGetToV1Me()
    {
        var (client, handler) = MockClient(MeJson);

        var result = await client.ValidateAsync();

        Assert.Equal(HttpMethod.Get, handler.Request!.Method);
        Assert.Equal("/v1/me", handler.Request.RequestUri!.AbsolutePath);
        Assert.Equal("", handler.Request.RequestUri.Query);
        Assert.True(result.Valid);
        Assert.Null(result.Reason);
        Assert.Equal("https://api.example.test", result.BaseUrl);
    }

    [Fact]
    public async Task ValidateAsync_SucceedsForAuditOnlyScopedKey()
    {
        // /v1/me requires no scope — a key holding only audit:* scopes must
        // validate cleanly (the old GET /v1/events probe could 403 on scope).
        var (client, _) = MockClient(MeJson);

        var result = await client.ValidateAsync();

        Assert.True(result.Valid);
        Assert.Null(result.Reason);
    }

    [Fact]
    public async Task ValidateAsync_401_MapsToInvalidKey()
    {
        var (client, _) = MockClient("{\"error\":\"invalid_api_key\"}", HttpStatusCode.Unauthorized);

        var result = await client.ValidateAsync();

        Assert.False(result.Valid);
        Assert.Contains("INVOANCE_API_KEY", result.Reason);
    }

    [Fact]
    public async Task ValidateAsync_403_MapsToAuthenticatedButIpBlocked()
    {
        var (client, _) = MockClient("{\"error\":\"ip_blocked\"}", HttpStatusCode.Forbidden);

        var result = await client.ValidateAsync();

        Assert.True(result.Valid);
        Assert.Contains("IP access rules", result.Reason);
    }

    [Fact]
    public async Task MeAsync_SendsGetToV1Me_AndReturnsDecodedBody()
    {
        var (client, handler) = MockClient(MeJson);

        var me = await client.MeAsync();

        Assert.Equal(HttpMethod.Get, handler.Request!.Method);
        Assert.Equal("/v1/me", handler.Request.RequestUri!.AbsolutePath);
        Assert.True((bool?)me!["valid"]);
        Assert.Equal("Acme", (string?)me["organization"]!["name"]);
        Assert.Equal("growth", (string?)me["organization"]!["plan_tier"]);
        Assert.Equal("ten_1", (string?)me["tenant"]!["id"]);
        Assert.Equal("abcd", (string?)me["api_key"]!["key_last4"]);
        Assert.Equal("audit:read", (string?)me["api_key"]!["scopes"]![0]);
        Assert.Equal(50, (int?)me["limits"]!["rate_limit_per_sec"]);
    }

    [Fact]
    public async Task MeAsync_401_ThrowsAuthenticationException()
    {
        var (client, _) = MockClient("{\"error\":\"invalid_api_key\"}", HttpStatusCode.Unauthorized);

        var ex = await Assert.ThrowsAsync<Invoance.Exceptions.AuthenticationException>(() => client.MeAsync());

        Assert.Equal("invalid_api_key", ex.ErrorCode);
    }
}
