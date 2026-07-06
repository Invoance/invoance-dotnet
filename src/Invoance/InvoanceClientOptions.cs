namespace Invoance;

/// <summary>
/// Configuration for <see cref="InvoanceClient"/>.
///
/// All fields are optional — when omitted the SDK reads from environment
/// variables: <c>INVOANCE_API_KEY</c> (required) and <c>INVOANCE_BASE_URL</c>.
/// </summary>
public sealed class InvoanceClientOptions
{
    /// <summary>
    /// Your Invoance API key. Falls back to the <c>INVOANCE_API_KEY</c>
    /// environment variable.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Override the API host. Falls back to <c>INVOANCE_BASE_URL</c>, then
    /// <c>https://api.invoance.com</c>.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// API version prefix (default <c>"v1"</c>), prepended to every request
    /// path — <c>/events</c> becomes <c>/v1/events</c>.
    /// </summary>
    public string ApiVersion { get; set; } = "v1";

    /// <summary>HTTP request timeout (default 30 seconds).</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Default idempotency key sent with every mutating request.</summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>Additional headers merged into every request.</summary>
    public IDictionary<string, string>? ExtraHeaders { get; set; }
}

/// <summary>Resolved, validated configuration.</summary>
internal sealed class ResolvedConfig
{
    public required string ApiKey { get; init; }
    public required string BaseUrl { get; init; }
    public required string ApiVersion { get; init; }
    public required TimeSpan Timeout { get; init; }
    public string? IdempotencyKey { get; init; }
    public required IReadOnlyDictionary<string, string> ExtraHeaders { get; init; }

    private const string DefaultBaseUrl = "https://api.invoance.com";
    private const string EnvApiKey = "INVOANCE_API_KEY";
    private const string EnvBaseUrl = "INVOANCE_BASE_URL";

    public static ResolvedConfig Resolve(InvoanceClientOptions options)
    {
        var apiKey = !string.IsNullOrEmpty(options.ApiKey)
            ? options.ApiKey
            : Environment.GetEnvironmentVariable(EnvApiKey) ?? "";

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException(
                $"ApiKey is required. Pass it explicitly or set the {EnvApiKey} environment variable.");
        }

        var baseUrl = (!string.IsNullOrEmpty(options.BaseUrl)
                ? options.BaseUrl
                : Environment.GetEnvironmentVariable(EnvBaseUrl) ?? DefaultBaseUrl)
            .TrimEnd('/');

        var apiVersion = (options.ApiVersion ?? "v1").Trim('/');

        return new ResolvedConfig
        {
            ApiKey = apiKey,
            BaseUrl = baseUrl,
            ApiVersion = apiVersion,
            Timeout = options.Timeout,
            IdempotencyKey = options.IdempotencyKey,
            ExtraHeaders = options.ExtraHeaders != null
                ? new Dictionary<string, string>(options.ExtraHeaders)
                : new Dictionary<string, string>(),
        };
    }
}
