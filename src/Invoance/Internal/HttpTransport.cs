using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Invoance.Exceptions;

namespace Invoance.Internal;

/// <summary>
/// Low-level HTTP transport over a reused <see cref="HttpClient"/>.
///
/// Network-level failures (DNS, connection, TLS) surface as
/// <see cref="NetworkException"/>; timeouts as <see cref="TimeoutException"/>.
/// A single <c>catch (InvoanceException)</c> is exhaustive.
/// </summary>
public sealed class HttpTransport : IDisposable
{
    private readonly ResolvedConfig _cfg;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;

    internal HttpTransport(ResolvedConfig cfg, HttpClient? http = null)
    {
        _cfg = cfg;
        _ownsHttp = http is null;
        _http = http ?? new HttpClient();
        // Give a generous transport timeout; per-request timeout is enforced
        // via a linked CancellationTokenSource so we can distinguish it from
        // caller cancellation.
        _http.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
    }

    // ── Public verbs ──────────────────────────────────────────

    public Task<T> GetAsync<T>(string path, IReadOnlyDictionary<string, object?>? query = null,
        CancellationToken cancellationToken = default) =>
        SendJsonAsync<T>(HttpMethod.Get, path, query, null, null, cancellationToken);

    public Task<T> PostAsync<T>(string path, object? body = null, string? idempotencyKey = null,
        CancellationToken cancellationToken = default) =>
        SendJsonAsync<T>(HttpMethod.Post, path, null, body, idempotencyKey, cancellationToken);

    public Task<T> PutAsync<T>(string path, object? body = null,
        CancellationToken cancellationToken = default) =>
        SendJsonAsync<T>(HttpMethod.Put, path, null, body, null, cancellationToken);

    public Task<T> PatchAsync<T>(string path, object? body = null,
        CancellationToken cancellationToken = default) =>
        SendJsonAsync<T>(HttpMethod.Patch, path, null, body, null, cancellationToken);

    public Task<T> DeleteAsync<T>(string path, CancellationToken cancellationToken = default) =>
        SendJsonAsync<T>(HttpMethod.Delete, path, null, null, null, cancellationToken);

    /// <summary>GET returning raw bytes (Accept: application/octet-stream).</summary>
    public async Task<byte[]> GetBytesAsync(string path, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(path, null);
        var request = new RequestContext("GET", path);

        using var msg = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyHeaders(msg, accept: "application/octet-stream", includeContentType: false);

        var resp = await SendAsync(msg, request, cancellationToken).ConfigureAwait(false);
        using (resp)
        {
            if (!resp.IsSuccessStatusCode)
            {
                var body = await TryReadJsonAsync(resp).ConfigureAwait(false);
                Errors.ThrowForStatus((int)resp.StatusCode, body, request, ParseRetryAfter(resp));
            }
            return await resp.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>GET returning the decoded JSON as an untyped value (JsonNode).</summary>
    public Task<JsonNode?> GetRawAsync(string path, IReadOnlyDictionary<string, object?>? query = null,
        CancellationToken cancellationToken = default) =>
        SendRawAsync(HttpMethod.Get, path, null, null, cancellationToken, query);

    /// <summary>Binary/source back-compat overload for the v0.1.0 signature.</summary>
    public Task<JsonNode?> GetRawAsync(string path, CancellationToken cancellationToken) =>
        GetRawAsync(path, null, cancellationToken);

    /// <summary>POST returning the decoded JSON as an untyped value (JsonNode).</summary>
    public Task<JsonNode?> PostRawAsync(string path, object? body = null, string? idempotencyKey = null,
        CancellationToken cancellationToken = default) =>
        SendRawAsync(HttpMethod.Post, path, body, idempotencyKey, cancellationToken);

    /// <summary>PUT returning the decoded JSON as an untyped value (JsonNode).</summary>
    public Task<JsonNode?> PutRawAsync(string path, object? body = null,
        CancellationToken cancellationToken = default) =>
        SendRawAsync(HttpMethod.Put, path, body, null, cancellationToken);

    /// <summary>PATCH returning the decoded JSON as an untyped value (JsonNode).</summary>
    public Task<JsonNode?> PatchRawAsync(string path, object? body = null,
        CancellationToken cancellationToken = default) =>
        SendRawAsync(HttpMethod.Patch, path, body, null, cancellationToken);

    /// <summary>DELETE returning the decoded JSON as an untyped value (JsonNode).</summary>
    public Task<JsonNode?> DeleteRawAsync(string path, CancellationToken cancellationToken = default) =>
        SendRawAsync(HttpMethod.Delete, path, null, null, cancellationToken);

    private async Task<JsonNode?> SendRawAsync(
        HttpMethod method, string path, object? body, string? idempotencyKey, CancellationToken cancellationToken,
        IReadOnlyDictionary<string, object?>? query = null)
    {
        var url = BuildUrl(path, query);
        var request = new RequestContext(method.Method, path);

        using var msg = new HttpRequestMessage(method, url);
        ApplyHeaders(msg, accept: "application/json", includeContentType: false);

        if (body != null)
        {
            var json = Json.Serialize(body);
            msg.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var idem = idempotencyKey ?? _cfg.IdempotencyKey;
        if (!string.IsNullOrEmpty(idem) && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
        {
            msg.Headers.TryAddWithoutValidation("Idempotency-Key", idem);
        }

        var resp = await SendAsync(msg, request, cancellationToken).ConfigureAwait(false);
        using (resp)
        {
            var text = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            JsonNode? node = null;
            IReadOnlyDictionary<string, object?>? errBody = null;
            if (!string.IsNullOrEmpty(text))
            {
                try
                {
                    node = JsonNode.Parse(text);
                    errBody = Json.NodeToClr(node) as IReadOnlyDictionary<string, object?>;
                }
                catch { /* non-json */ }
            }
            Errors.ThrowForStatus((int)resp.StatusCode, errBody, request, ParseRetryAfter(resp));
            return node;
        }
    }

    // ── Internals ─────────────────────────────────────────────

    private async Task<T> SendJsonAsync<T>(
        HttpMethod method,
        string path,
        IReadOnlyDictionary<string, object?>? query,
        object? body,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var url = BuildUrl(path, query);
        var request = new RequestContext(method.Method, path);

        using var msg = new HttpRequestMessage(method, url);
        ApplyHeaders(msg, accept: "application/json", includeContentType: false);

        if (body != null)
        {
            var json = Json.Serialize(body);
            msg.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var idem = idempotencyKey ?? _cfg.IdempotencyKey;
        if (!string.IsNullOrEmpty(idem) && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
        {
            msg.Headers.TryAddWithoutValidation("Idempotency-Key", idem);
        }

        var resp = await SendAsync(msg, request, cancellationToken).ConfigureAwait(false);
        using (resp)
        {
            var text = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            IReadOnlyDictionary<string, object?>? body2 = null;
            JsonNode? node = null;
            if (!string.IsNullOrEmpty(text))
            {
                try
                {
                    node = JsonNode.Parse(text);
                    body2 = Json.NodeToClr(node) as IReadOnlyDictionary<string, object?>;
                }
                catch { /* empty or non-json */ }
            }

            Errors.ThrowForStatus((int)resp.StatusCode, body2, request, ParseRetryAfter(resp));

            if (typeof(T) == typeof(JsonNode) || typeof(T) == typeof(object))
            {
                return (T)(object)node!;
            }
            if (string.IsNullOrEmpty(text))
            {
                return default!;
            }
            return Json.Deserialize<T>(text);
        }
    }

    private string BuildUrl(string path, IReadOnlyDictionary<string, object?>? query)
    {
        var baseUrl = $"{_cfg.BaseUrl}/{_cfg.ApiVersion}{path}";
        if (query == null) return baseUrl;

        var parts = new List<string>();
        foreach (var kv in query)
        {
            if (kv.Value == null) continue;
            // Booleans must go out as JSON-style lowercase ("true"/"false");
            // Convert.ToString would produce "True"/"False".
            var v = kv.Value switch
            {
                bool b => b ? "true" : "false",
                _ => Convert.ToString(kv.Value, CultureInfo.InvariantCulture) ?? "",
            };
            parts.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(v)}");
        }
        return parts.Count > 0 ? $"{baseUrl}?{string.Join("&", parts)}" : baseUrl;
    }

    private void ApplyHeaders(HttpRequestMessage msg, string accept, bool includeContentType)
    {
        msg.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_cfg.ApiKey}");
        msg.Headers.TryAddWithoutValidation("Accept", accept);
        msg.Headers.UserAgent.ParseAdd($"invoance-dotnet/{Invoance.Version.SdkVersion}");
        foreach (var kv in _cfg.ExtraHeaders)
        {
            msg.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        }
        // Content-Type is set on the StringContent when there's a body; the
        // includeContentType flag is retained for parity with the reference
        // transport but no header is needed on bodyless requests.
        _ = includeContentType;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage msg, RequestContext request, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(_cfg.Timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        try
        {
            return await _http.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, linked.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new Invoance.Exceptions.TimeoutException(
                $"Request timed out after {_cfg.Timeout.TotalMilliseconds}ms on {request.Method} {request.Path}",
                requestContext: request, innerException: ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Genuine caller cancellation — propagate untouched.
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new NetworkException(
                $"Network failure on {request.Method} {request.Path}: {ex.Message}",
                requestContext: request, innerException: ex);
        }
    }

    private static async Task<IReadOnlyDictionary<string, object?>?> TryReadJsonAsync(HttpResponseMessage resp)
    {
        try
        {
            var text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(text)) return null;
            return Json.NodeToClr(JsonNode.Parse(text)) as IReadOnlyDictionary<string, object?>;
        }
        catch
        {
            return null;
        }
    }

    private static double? ParseRetryAfter(HttpResponseMessage resp)
    {
        if (!resp.Headers.TryGetValues("Retry-After", out var values)) return null;
        var value = values.FirstOrDefault();
        if (string.IsNullOrEmpty(value)) return null;

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            && !double.IsNaN(seconds) && !double.IsInfinity(seconds) && seconds >= 0)
        {
            return seconds;
        }
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var when))
        {
            var delta = (when - DateTimeOffset.UtcNow).TotalSeconds;
            return Math.Max(0, delta);
        }
        return null;
    }

    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }
}
