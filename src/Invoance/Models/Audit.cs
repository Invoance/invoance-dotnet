using System.Text.Json.Serialization;

namespace Invoance.Models;

// ── Params ──────────────────────────────────────────────────

public sealed class IngestAuditEventParams
{
    /// <summary>Your own id for the org (external id or the <c>aorg_</c> id).</summary>
    public required string OrganizationId { get; set; }
    public required string Action { get; set; }

    /// <summary>The actor object (arbitrary keys, e.g. type/id/name).</summary>
    public required IDictionary<string, object?> Actor { get; set; }

    public string? OccurredAt { get; set; }

    /// <summary>Target objects; defaults to an empty list when null.</summary>
    public IList<IDictionary<string, object?>>? Targets { get; set; }

    public IDictionary<string, object?>? Context { get; set; }
    public IDictionary<string, object?>? Metadata { get; set; }

    /// <summary>Idempotency header for safe retries (see <c>ContentIdempotencyKey</c>).</summary>
    public string? IdempotencyKey { get; set; }
}

public sealed class ListAuditEventsParams
{
    public string? OrganizationId { get; set; }
    public string? Actions { get; set; }
    public string? ActorId { get; set; }
    public string? TargetId { get; set; }
    /// <summary>Inclusive RFC3339 bound on occurred_at.</summary>
    public string? RangeStart { get; set; }
    public string? RangeEnd { get; set; }
    public int? Limit { get; set; }
    public string? Cursor { get; set; }
}

public sealed class CreateAuditOrgParams
{
    public required string OrganizationId { get; set; }
    public string? Name { get; set; }
}

public sealed class CreateAuditStreamParams
{
    public required string Url { get; set; }
    /// <summary>v1 supports <c>webhook</c> only.</summary>
    public string? Type { get; set; }
}

public sealed class CreatePortalSessionParams
{
    public required string OrganizationId { get; set; }
    /// <summary><c>audit_logs</c> or <c>log_streams</c>.</summary>
    public required string Intent { get; set; }
    public int? SessionDurationSeconds { get; set; }
    public int? LinkDurationSeconds { get; set; }
}

public sealed class CreateAuditExportParams
{
    public required string OrganizationId { get; set; }
    /// <summary><c>csv</c> or <c>ndjson</c>.</summary>
    public required string Format { get; set; }
    public IDictionary<string, object?>? Filters { get; set; }
}

// ── Responses ───────────────────────────────────────────────

public sealed class AuditEvent
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("org_id")] public string OrgId { get; set; } = "";
    [JsonPropertyName("seq")] public long Seq { get; set; }
    [JsonPropertyName("occurred_at")] public string OccurredAt { get; set; } = "";
    [JsonPropertyName("ingested_at")] public string IngestedAt { get; set; } = "";
    [JsonPropertyName("action")] public string Action { get; set; } = "";
    [JsonPropertyName("actor")] public Dictionary<string, object?>? Actor { get; set; }
    [JsonPropertyName("targets")] public List<Dictionary<string, object?>>? Targets { get; set; }
    [JsonPropertyName("context")] public Dictionary<string, object?>? Context { get; set; }
    [JsonPropertyName("metadata")] public Dictionary<string, object?>? Metadata { get; set; }
    [JsonPropertyName("payload_hash")] public string PayloadHash { get; set; } = "";
    [JsonPropertyName("signature")] public string Signature { get; set; } = "";
    [JsonPropertyName("signing_public_key")] public string SigningPublicKey { get; set; } = "";
}

public sealed class ListAuditEventsResponse
{
    [JsonPropertyName("events")] public List<AuditEvent> Events { get; set; } = new();
    [JsonPropertyName("next_cursor")] public string? NextCursor { get; set; }
}
