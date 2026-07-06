using System.Text.Json.Serialization;

namespace Invoance.Models;

// ── Params ──────────────────────────────────────────────────

public sealed class CreateTraceParams
{
    public required string Label { get; set; }
    public IDictionary<string, object?>? Metadata { get; set; }
}

public sealed class ListTracesParams
{
    public int? Page { get; set; }
    public int? Limit { get; set; }
    /// <summary>Filter by status: <c>open</c> or <c>sealed</c>.</summary>
    public string? Status { get; set; }
}

public sealed class GetTraceParams
{
    /// <summary>Page number for events (1-based, default 1).</summary>
    public int? EventPage { get; set; }
    /// <summary>Max events per page (default 50, max 200).</summary>
    public int? EventLimit { get; set; }
}

// ── Responses ───────────────────────────────────────────────

public sealed class CreateTraceResponse
{
    [JsonPropertyName("trace_id")] public string TraceId { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("created_at")] public string CreatedAt { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
}

public sealed class TraceListItem
{
    [JsonPropertyName("trace_id")] public string TraceId { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("event_count")] public int? EventCount { get; set; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; set; } = "";
    [JsonPropertyName("sealed_at")] public string? SealedAt { get; set; }
    [JsonPropertyName("composite_hash")] public string? CompositeHash { get; set; }
}

public sealed class ListTracesResponse
{
    [JsonPropertyName("traces")] public List<TraceListItem> Traces { get; set; } = new();
    [JsonPropertyName("page")] public int Page { get; set; }
    [JsonPropertyName("limit")] public int Limit { get; set; }
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("has_more")] public bool HasMore { get; set; }
}

public sealed class TraceEventSummary
{
    [JsonPropertyName("event_id")] public string EventId { get; set; } = "";
    [JsonPropertyName("event_type")] public string EventType { get; set; } = "";
    [JsonPropertyName("payload_hash")] public string PayloadHash { get; set; } = "";
    [JsonPropertyName("ingested_at")] public string IngestedAt { get; set; } = "";
}

public sealed class TraceDetail
{
    [JsonPropertyName("trace_id")] public string TraceId { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("event_count")] public int? EventCount { get; set; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; set; } = "";
    [JsonPropertyName("sealed_at")] public string? SealedAt { get; set; }
    [JsonPropertyName("composite_hash")] public string? CompositeHash { get; set; }
    [JsonPropertyName("seal_event_id")] public string? SealEventId { get; set; }
    [JsonPropertyName("metadata")] public Dictionary<string, object?>? Metadata { get; set; }
    [JsonPropertyName("events")] public List<TraceEventSummary> Events { get; set; } = new();
    [JsonPropertyName("event_page")] public int EventPage { get; set; }
    [JsonPropertyName("event_limit")] public int EventLimit { get; set; }
    [JsonPropertyName("event_total")] public int EventTotal { get; set; }
    [JsonPropertyName("event_has_more")] public bool EventHasMore { get; set; }
}

public sealed class DeleteTraceResponse
{
    [JsonPropertyName("trace_id")] public string TraceId { get; set; } = "";
    [JsonPropertyName("deleted")] public bool Deleted { get; set; }
}

public sealed class SealTraceResponse
{
    [JsonPropertyName("trace_id")] public string TraceId { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("message")] public string Message { get; set; } = "";
}

public sealed class TraceProofEvent
{
    [JsonPropertyName("event_id")] public string EventId { get; set; } = "";
    [JsonPropertyName("event_type")] public string EventType { get; set; } = "";
    [JsonPropertyName("payload")] public Dictionary<string, object?> Payload { get; set; } = new();
    [JsonPropertyName("content_hash")] public string ContentHash { get; set; } = "";
    [JsonPropertyName("timestamp")] public string Timestamp { get; set; } = "";
    [JsonPropertyName("signature")] public string Signature { get; set; } = "";
    [JsonPropertyName("public_key")] public string PublicKey { get; set; } = "";
}

public sealed class TraceProofSealEvent
{
    [JsonPropertyName("event_id")] public string EventId { get; set; } = "";
    [JsonPropertyName("event_type")] public string EventType { get; set; } = "";
    [JsonPropertyName("content_hash")] public string ContentHash { get; set; } = "";
    [JsonPropertyName("timestamp")] public string Timestamp { get; set; } = "";
    [JsonPropertyName("signature")] public string Signature { get; set; } = "";
    [JsonPropertyName("public_key")] public string PublicKey { get; set; } = "";
}

public sealed class TraceProofVerification
{
    [JsonPropertyName("composite_hash_valid")] public bool CompositeHashValid { get; set; }
    [JsonPropertyName("all_signatures_valid")] public bool AllSignaturesValid { get; set; }
}

public sealed class TraceProofBundle
{
    [JsonPropertyName("version")] public string Version { get; set; } = "";
    [JsonPropertyName("trace_id")] public string TraceId { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("tenant_domain")] public string TenantDomain { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("created_at")] public string CreatedAt { get; set; } = "";
    [JsonPropertyName("sealed_at")] public string SealedAt { get; set; } = "";
    [JsonPropertyName("composite_hash")] public string CompositeHash { get; set; } = "";
    [JsonPropertyName("event_count")] public int EventCount { get; set; }
    [JsonPropertyName("events")] public List<TraceProofEvent> Events { get; set; } = new();
    [JsonPropertyName("seal_event")] public TraceProofSealEvent? SealEvent { get; set; }
    [JsonPropertyName("verification")] public TraceProofVerification? Verification { get; set; }
}
