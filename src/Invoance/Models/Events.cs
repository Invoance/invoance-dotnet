using System.Text.Json.Serialization;

namespace Invoance.Models;

// ── Params ──────────────────────────────────────────────────

public sealed class IngestEventParams
{
    public required string EventType { get; set; }
    public required IDictionary<string, object?> Payload { get; set; }
    public string? EventTime { get; set; }
    public string? TraceId { get; set; }
    public string? IdempotencyKey { get; set; }
}

public sealed class ListEventsParams
{
    public int? Page { get; set; }
    public int? Limit { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public string? EventType { get; set; }
}

public sealed class VerifyEventParams
{
    /// <summary>A 64-char hex SHA-256 payload hash.</summary>
    public string? PayloadHash { get; set; }

    /// <summary>Raw payload — the server canonicalizes and hashes it.</summary>
    public IDictionary<string, object?>? Payload { get; set; }
}

// ── Responses ───────────────────────────────────────────────

public sealed class IngestEventResponse
{
    [JsonPropertyName("event_id")] public string EventId { get; set; } = "";
    [JsonPropertyName("ingested_at")] public string IngestedAt { get; set; } = "";
}

public sealed class EventListItem
{
    [JsonPropertyName("event_id")] public string EventId { get; set; } = "";
    [JsonPropertyName("event_type")] public string EventType { get; set; } = "";
    [JsonPropertyName("payload_hash")] public string PayloadHash { get; set; } = "";
    [JsonPropertyName("event_hash")] public string EventHash { get; set; } = "";
    [JsonPropertyName("retention_policy")] public string RetentionPolicy { get; set; } = "";
    [JsonPropertyName("ingested_at")] public string IngestedAt { get; set; } = "";
    [JsonPropertyName("event_time")] public string? EventTime { get; set; }
    [JsonPropertyName("idempotency_key")] public string? IdempotencyKey { get; set; }
}

public sealed class ListEventsResponse
{
    [JsonPropertyName("events")] public List<EventListItem> Events { get; set; } = new();
    [JsonPropertyName("page")] public int Page { get; set; }
    [JsonPropertyName("limit")] public int Limit { get; set; }
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("has_more")] public bool HasMore { get; set; }
}

public sealed class ComplianceEvent
{
    [JsonPropertyName("event_id")] public string EventId { get; set; } = "";
    [JsonPropertyName("tenant_id")] public string TenantId { get; set; } = "";
    [JsonPropertyName("event_type")] public string EventType { get; set; } = "";
    [JsonPropertyName("payload")] public Dictionary<string, object?> Payload { get; set; } = new();
    [JsonPropertyName("event_time")] public string? EventTime { get; set; }
    [JsonPropertyName("retention_policy")] public string RetentionPolicy { get; set; } = "";
    // Not returned by every endpoint (e.g. the single-event GET omits it).
    [JsonPropertyName("access_tier")] public string? AccessTier { get; set; }
    [JsonPropertyName("expires_at")] public string? ExpiresAt { get; set; }
    [JsonPropertyName("api_key_id")] public string? ApiKeyId { get; set; }
    [JsonPropertyName("user_id")] public string? UserId { get; set; }
    [JsonPropertyName("ingested_at")] public string IngestedAt { get; set; } = "";
    [JsonPropertyName("payload_hash")] public string PayloadHash { get; set; } = "";
    [JsonPropertyName("request_hash")] public string RequestHash { get; set; } = "";
    [JsonPropertyName("event_hash")] public string EventHash { get; set; } = "";
    [JsonPropertyName("idempotency_key")] public string? IdempotencyKey { get; set; }
    [JsonPropertyName("organization")] public OrganizationPublic? Organization { get; set; }
}

public sealed class VerifyEventResponse
{
    [JsonPropertyName("event_id")] public string EventId { get; set; } = "";
    [JsonPropertyName("match_result")] public bool MatchResult { get; set; }
    [JsonPropertyName("matched_field")] public string? MatchedField { get; set; }
    [JsonPropertyName("anchored_hash")] public string AnchoredHash { get; set; } = "";
    [JsonPropertyName("submitted_hash")] public string SubmittedHash { get; set; } = "";
    [JsonPropertyName("anchored_at")] public string AnchoredAt { get; set; } = "";
    [JsonPropertyName("method")] public string Method { get; set; } = "";
    [JsonPropertyName("organization")] public OrganizationPublic? Organization { get; set; }
}
