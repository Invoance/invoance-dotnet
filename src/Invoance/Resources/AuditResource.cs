using System.Text.Json.Nodes;
using Invoance.Internal;
using Invoance.Models;

namespace Invoance.Resources;

/// <summary>
/// Audit Logs resource — <c>client.Audit.*</c>. An append-only, per-tenant
/// signed event ledger with end-customer orgs, webhook streams, hosted-viewer
/// portal links, and async exports. Methods resolve to the server's JSON.
/// For an offline signature check see <see cref="Internal.AuditVerify"/>.
/// </summary>
public sealed class AuditResource
{
    public AuditEventsResource Events { get; }
    public AuditOrgsResource Orgs { get; }
    public AuditStreamsResource Streams { get; }
    public AuditPortalSessionsResource PortalSessions { get; }
    public AuditExportsResource Exports { get; }

    internal AuditResource(HttpTransport t)
    {
        Events = new AuditEventsResource(t);
        Orgs = new AuditOrgsResource(t);
        Streams = new AuditStreamsResource(t);
        PortalSessions = new AuditPortalSessionsResource(t);
        Exports = new AuditExportsResource(t);
    }

    /// <summary>
    /// Derive a stable Idempotency-Key from an event body (safe-retry helper):
    /// <c>"idem_" + sha256hex(stableStringify(body))</c> where stableStringify
    /// is compact JSON with all object keys sorted deeply (no null-strip).
    /// </summary>
    public static string ContentIdempotencyKey(IDictionary<string, object?> body)
    {
        var stable = Canonical.StableStringify(body);
        return "idem_" + Crypto.Sha256Hex(stable);
    }
}

/// <summary><c>client.Audit.Events</c>.</summary>
public sealed class AuditEventsResource
{
    private readonly HttpTransport _t;
    internal AuditEventsResource(HttpTransport t) => _t = t;

    /// <summary>POST /audit/events — append one signed event.</summary>
    public Task<JsonNode?> IngestAsync(IngestAuditEventParams p, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?>
        {
            ["organization_id"] = p.OrganizationId,
            ["action"] = p.Action,
            ["occurred_at"] = p.OccurredAt ?? DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
            ["actor"] = p.Actor,
            ["targets"] = p.Targets ?? new List<IDictionary<string, object?>>(),
        };
        if (p.Context != null) body["context"] = p.Context;
        if (p.Metadata != null) body["metadata"] = p.Metadata;

        // The ledger requires an Idempotency-Key; derive a content-stable one if absent.
        var idem = p.IdempotencyKey ?? AuditResource.ContentIdempotencyKey(body);
        return _t.PostRawAsync("/audit/events", body, idem, cancellationToken);
    }

    /// <summary>GET /audit/events — keyset-paginated listing.</summary>
    public Task<ListAuditEventsResponse> ListAsync(ListAuditEventsParams? p = null, CancellationToken cancellationToken = default)
    {
        p ??= new ListAuditEventsParams();
        var query = new Dictionary<string, object?>
        {
            ["organization_id"] = p.OrganizationId,
            ["actions"] = p.Actions,
            ["actor_id"] = p.ActorId,
            ["target_id"] = p.TargetId,
            ["range_start"] = p.RangeStart,
            ["range_end"] = p.RangeEnd,
            ["limit"] = p.Limit,
            ["cursor"] = p.Cursor,
        };
        return _t.GetAsync<ListAuditEventsResponse>("/audit/events", query, cancellationToken);
    }

    /// <summary>GET /audit/events/{id}.</summary>
    public Task<AuditEvent> GetAsync(string eventId, CancellationToken cancellationToken = default) =>
        _t.GetAsync<AuditEvent>($"/audit/events/{eventId}", null, cancellationToken);

    /// <summary>GET /audit/events/{id}/verify — server-side verify (pinned key).</summary>
    public Task<JsonNode?> VerifyAsync(string eventId, CancellationToken cancellationToken = default) =>
        _t.GetRawAsync($"/audit/events/{eventId}/verify", cancellationToken);
}

/// <summary><c>client.Audit.Orgs</c>.</summary>
public sealed class AuditOrgsResource
{
    private readonly HttpTransport _t;
    internal AuditOrgsResource(HttpTransport t) => _t = t;

    public Task<JsonNode?> CreateAsync(CreateAuditOrgParams p, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?> { ["organization_id"] = p.OrganizationId };
        if (p.Name != null) body["name"] = p.Name;
        return _t.PostRawAsync("/audit/orgs", body, null, cancellationToken);
    }

    public Task<JsonNode?> ListAsync(CancellationToken cancellationToken = default) =>
        _t.GetRawAsync("/audit/orgs", cancellationToken);

    public Task<JsonNode?> IntegrityAsync(string organizationId, CancellationToken cancellationToken = default) =>
        _t.GetRawAsync($"/audit/orgs/{organizationId}/integrity", cancellationToken);

    public Task<JsonNode?> SetRetentionAsync(string organizationId, int days, CancellationToken cancellationToken = default) =>
        _t.PutRawAsync($"/audit/orgs/{organizationId}/retention", new Dictionary<string, object?> { ["days"] = days }, cancellationToken);
}

/// <summary><c>client.Audit.Streams</c>.</summary>
public sealed class AuditStreamsResource
{
    private readonly HttpTransport _t;
    internal AuditStreamsResource(HttpTransport t) => _t = t;

    /// <summary>Create a webhook stream; the signing secret is returned ONCE.</summary>
    public Task<JsonNode?> CreateAsync(string organizationId, CreateAuditStreamParams p, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?>
        {
            ["type"] = p.Type ?? "webhook",
            ["url"] = p.Url,
        };
        return _t.PostRawAsync($"/audit/orgs/{organizationId}/streams", body, null, cancellationToken);
    }

    public Task<JsonNode?> ListAsync(string organizationId, CancellationToken cancellationToken = default) =>
        _t.GetRawAsync($"/audit/orgs/{organizationId}/streams", cancellationToken);

    public Task<JsonNode?> DeleteAsync(string organizationId, string streamId, CancellationToken cancellationToken = default) =>
        _t.DeleteRawAsync($"/audit/orgs/{organizationId}/streams/{streamId}", cancellationToken);

    public Task<JsonNode?> TestAsync(string organizationId, string streamId, CancellationToken cancellationToken = default) =>
        _t.PostRawAsync($"/audit/orgs/{organizationId}/streams/{streamId}/test", null, null, cancellationToken);
}

/// <summary><c>client.Audit.PortalSessions</c>.</summary>
public sealed class AuditPortalSessionsResource
{
    private readonly HttpTransport _t;
    internal AuditPortalSessionsResource(HttpTransport t) => _t = t;

    /// <summary>Mint a one-time hosted-viewer link.</summary>
    public Task<JsonNode?> CreateAsync(CreatePortalSessionParams p, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?>
        {
            ["organization_id"] = p.OrganizationId,
            ["intent"] = p.Intent,
        };
        if (p.SessionDurationSeconds != null) body["session_duration_seconds"] = p.SessionDurationSeconds;
        if (p.LinkDurationSeconds != null) body["link_duration_seconds"] = p.LinkDurationSeconds;
        return _t.PostRawAsync("/audit/portal_sessions", body, null, cancellationToken);
    }
}

/// <summary><c>client.Audit.Exports</c>.</summary>
public sealed class AuditExportsResource
{
    private readonly HttpTransport _t;
    internal AuditExportsResource(HttpTransport t) => _t = t;

    /// <summary>Queue an async CSV/NDJSON export job.</summary>
    public Task<JsonNode?> CreateAsync(CreateAuditExportParams p, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?>
        {
            ["organization_id"] = p.OrganizationId,
            ["format"] = p.Format,
        };
        if (p.Filters != null) body["filters"] = p.Filters;
        return _t.PostRawAsync("/audit/exports", body, null, cancellationToken);
    }

    /// <summary>Poll a job; when <c>status == "ready"</c> the response has <c>download_url</c>.</summary>
    public Task<JsonNode?> GetAsync(string exportId, CancellationToken cancellationToken = default) =>
        _t.GetRawAsync($"/audit/exports/{exportId}", cancellationToken);
}
