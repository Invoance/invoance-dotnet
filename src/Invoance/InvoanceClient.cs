using System.Text.Json.Nodes;
using Invoance.Exceptions;
using Invoance.Internal;
using Invoance.Resources;

namespace Invoance;

/// <summary>Outcome of <see cref="InvoanceClient.ValidateAsync"/>.</summary>
public sealed record ValidationResult(bool Valid, string? Reason, string BaseUrl);

/// <summary>
/// Top-level SDK client for the Invoance compliance API.
/// </summary>
/// <example>
/// <code>
/// // Reads INVOANCE_API_KEY / INVOANCE_BASE_URL from the environment
/// var client = new InvoanceClient();
///
/// var ev = await client.Events.IngestAsync(new IngestEventParams
/// {
///     EventType = "user.login",
///     Payload = new Dictionary&lt;string, object?&gt; { ["userId"] = "u_42" },
/// });
/// </code>
/// </example>
public sealed class InvoanceClient : IDisposable
{
    public EventsResource Events { get; }
    public DocumentsResource Documents { get; }
    public AttestationsResource Attestations { get; }
    public TracesResource Traces { get; }
    public AuditResource Audit { get; }

    private readonly HttpTransport _transport;
    private readonly ResolvedConfig _config;

    /// <summary>Create a client from environment variables.</summary>
    public InvoanceClient() : this(new InvoanceClientOptions()) { }

    /// <summary>Create a client with an explicit API key (other settings default).</summary>
    public InvoanceClient(string apiKey) : this(new InvoanceClientOptions { ApiKey = apiKey }) { }

    /// <summary>Create a client with full options.</summary>
    public InvoanceClient(InvoanceClientOptions options) : this(options, null) { }

    /// <summary>Create a client with full options and a caller-supplied <see cref="HttpClient"/>.</summary>
    public InvoanceClient(InvoanceClientOptions options, HttpClient? httpClient)
    {
        _config = ResolvedConfig.Resolve(options);
        _transport = new HttpTransport(_config, httpClient);

        Events = new EventsResource(_transport);
        Documents = new DocumentsResource(_transport);
        Attestations = new AttestationsResource(_transport);
        Traces = new TracesResource(_transport);
        Audit = new AuditResource(_transport);
    }

    /// <summary>The resolved base URL in use.</summary>
    public string BaseUrl => _config.BaseUrl;

    /// <summary>
    /// Probe the key-introspection endpoint (<c>GET /v1/me</c>) to confirm the
    /// API key works. The endpoint requires no scope, so keys limited to e.g.
    /// <c>audit:*</c> validate correctly. Never throws — every failure mode is
    /// turned into a <see cref="ValidationResult"/>.
    /// </summary>
    public async Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = _config.BaseUrl;
        try
        {
            await MeAsync(cancellationToken).ConfigureAwait(false);
            return new ValidationResult(true, null, baseUrl);
        }
        catch (AuthenticationException)
        {
            return new ValidationResult(false, "Authentication failed — check INVOANCE_API_KEY", baseUrl);
        }
        catch (ForbiddenException)
        {
            return new ValidationResult(true, "API key authenticated but request blocked by IP access rules", baseUrl);
        }
        catch (QuotaExceededException)
        {
            return new ValidationResult(true, "API key authenticated but currently rate limited", baseUrl);
        }
        catch (Exception e) when (e is NetworkException or Exceptions.TimeoutException)
        {
            return new ValidationResult(false, $"Server unreachable: {e.Message}", baseUrl);
        }
        catch (InvoanceException e)
        {
            return new ValidationResult(false, e.Message, baseUrl);
        }
    }

    /// <summary>
    /// <c>GET /v1/me</c> — introspect the current API key. Returns the decoded
    /// response (<c>organization</c>, <c>tenant</c>, <c>api_key</c>,
    /// <c>limits</c>) as an untyped <see cref="JsonNode"/>. Throws the usual
    /// <see cref="InvoanceException"/> hierarchy on failure.
    /// </summary>
    public Task<JsonNode?> MeAsync(CancellationToken cancellationToken = default) =>
        _transport.GetRawAsync("/me", null, cancellationToken);

    public void Dispose() => _transport.Dispose();
}
