using System.Text.Json.Serialization;

namespace Invoance.Models;

// ── Params ──────────────────────────────────────────────────

public sealed class AnchorDocumentParams
{
    public required string DocumentHash { get; set; }
    public string? DocumentRef { get; set; }
    public string? EventType { get; set; }
    public string? OriginalBytesB64 { get; set; }
    public IDictionary<string, object?>? Metadata { get; set; }
    public string? TraceId { get; set; }
    public string? IdempotencyKey { get; set; }
}

public sealed class AnchorFileParams
{
    /// <summary>File path on disk. Mutually exclusive with <see cref="Bytes"/>.</summary>
    public string? Path { get; set; }

    /// <summary>File contents. Mutually exclusive with <see cref="Path"/>.</summary>
    public byte[]? Bytes { get; set; }

    /// <summary>Human-readable reference. Defaults to filename when a path is given.</summary>
    public string? DocumentRef { get; set; }

    public string? EventType { get; set; }
    public IDictionary<string, object?>? Metadata { get; set; }
    public string? IdempotencyKey { get; set; }

    /// <summary>Skip uploading the original file bytes. Default false.</summary>
    public bool SkipOriginal { get; set; }

    public string? TraceId { get; set; }
}

public sealed class ListDocumentsParams
{
    public int? Page { get; set; }
    public int? Limit { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public string? DocumentRef { get; set; }
}

public sealed class VerifyDocumentParams
{
    public required string DocumentHash { get; set; }
}

// ── Responses ───────────────────────────────────────────────

public sealed class AnchorDocumentResponse
{
    [JsonPropertyName("event_id")] public string EventId { get; set; } = "";
    [JsonPropertyName("created_at")] public string CreatedAt { get; set; } = "";
    [JsonPropertyName("document_hash")] public string DocumentHash { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
}

public sealed class DocumentListItem
{
    [JsonPropertyName("event_id")] public string EventId { get; set; } = "";
    [JsonPropertyName("document_ref")] public string DocumentRef { get; set; } = "";
    [JsonPropertyName("document_hash")] public string DocumentHash { get; set; } = "";
    [JsonPropertyName("event_type")] public string EventType { get; set; } = "";
    [JsonPropertyName("has_original")] public bool HasOriginal { get; set; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; set; } = "";
}

public sealed class ListDocumentsResponse
{
    [JsonPropertyName("documents")] public List<DocumentListItem> Documents { get; set; } = new();
    [JsonPropertyName("page")] public int Page { get; set; }
    [JsonPropertyName("limit")] public int Limit { get; set; }
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("has_more")] public bool HasMore { get; set; }
}

public sealed class DocumentEvent
{
    [JsonPropertyName("event_id")] public string EventId { get; set; } = "";
    [JsonPropertyName("tenant_id")] public string TenantId { get; set; } = "";
    [JsonPropertyName("document_ref")] public string DocumentRef { get; set; } = "";
    [JsonPropertyName("document_hash")] public string DocumentHash { get; set; } = "";
    [JsonPropertyName("signature_b64")] public string SignatureB64 { get; set; } = "";
    [JsonPropertyName("signed_payload_b64")] public string SignedPayloadB64 { get; set; } = "";
    [JsonPropertyName("public_key_b64")] public string PublicKeyB64 { get; set; } = "";
    [JsonPropertyName("has_original")] public bool HasOriginal { get; set; }
    [JsonPropertyName("metadata")] public Dictionary<string, object?>? Metadata { get; set; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; set; } = "";
    [JsonPropertyName("organization")] public OrganizationPublic? Organization { get; set; }
}

public sealed class VerifyDocumentResponse
{
    [JsonPropertyName("event_id")] public string EventId { get; set; } = "";
    [JsonPropertyName("match_result")] public bool MatchResult { get; set; }
    [JsonPropertyName("document_ref")] public string DocumentRef { get; set; } = "";
    [JsonPropertyName("anchored_hash")] public string AnchoredHash { get; set; } = "";
    [JsonPropertyName("submitted_hash")] public string SubmittedHash { get; set; } = "";
    [JsonPropertyName("anchored_at")] public string AnchoredAt { get; set; } = "";
    [JsonPropertyName("organization")] public OrganizationPublic? Organization { get; set; }
}
