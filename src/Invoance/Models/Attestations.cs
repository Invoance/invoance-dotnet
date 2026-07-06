using System.Text.Json.Serialization;

namespace Invoance.Models;

// ── Params ──────────────────────────────────────────────────

/// <summary>
/// Subject context for an attestation. <see cref="UserId"/> and
/// <see cref="SessionId"/> are well-known; any additional entries in
/// <see cref="Extra"/> are passed through as custom context. All fields
/// become part of the attestation hash.
/// </summary>
public sealed class AttestationSubject
{
    public string? UserId { get; set; }
    public string? SessionId { get; set; }

    /// <summary>Additional tenant-specific context keys (verbatim wire keys).</summary>
    public IDictionary<string, object?>? Extra { get; set; }
}

public sealed class IngestAttestationParams
{
    public required string Type { get; set; }
    public required string Input { get; set; }
    public required string Output { get; set; }
    public required string ModelProvider { get; set; }
    public required string ModelName { get; set; }
    public required string ModelVersion { get; set; }
    public AttestationSubject? Subject { get; set; }
    public string? TraceId { get; set; }
    public string? IdempotencyKey { get; set; }
}

public sealed class ListAttestationsParams
{
    public int? Page { get; set; }
    public int? Limit { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public string? AttestationType { get; set; }
    public string? ModelProvider { get; set; }
}

public sealed class VerifyAttestationParams
{
    public required string ContentHash { get; set; }
}

// ── Responses ───────────────────────────────────────────────

public sealed class IngestAttestationResponse
{
    [JsonPropertyName("attestation_id")] public string AttestationId { get; set; } = "";
    [JsonPropertyName("created_at")] public string CreatedAt { get; set; } = "";
    [JsonPropertyName("input_hash")] public string InputHash { get; set; } = "";
    [JsonPropertyName("output_hash")] public string OutputHash { get; set; } = "";
    [JsonPropertyName("payload_hash")] public string PayloadHash { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
}

public sealed class AttestationListItem
{
    [JsonPropertyName("attestation_id")] public string AttestationId { get; set; } = "";
    [JsonPropertyName("attestation_type")] public string AttestationType { get; set; } = "";
    [JsonPropertyName("attestation_hash")] public string AttestationHash { get; set; } = "";
    [JsonPropertyName("model_provider")] public string? ModelProvider { get; set; }
    [JsonPropertyName("model_name")] public string? ModelName { get; set; }
    [JsonPropertyName("retention_policy")] public string RetentionPolicy { get; set; } = "";
    [JsonPropertyName("created_at")] public string CreatedAt { get; set; } = "";
}

public sealed class ListAttestationsResponse
{
    [JsonPropertyName("attestations")] public List<AttestationListItem> Attestations { get; set; } = new();
    [JsonPropertyName("page")] public int Page { get; set; }
    [JsonPropertyName("limit")] public int Limit { get; set; }
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("has_more")] public bool HasMore { get; set; }
}

public sealed class AiAttestation
{
    [JsonPropertyName("attestation_id")] public string AttestationId { get; set; } = "";
    [JsonPropertyName("tenant_id")] public string TenantId { get; set; } = "";
    [JsonPropertyName("attestation_type")] public string AttestationType { get; set; } = "";
    [JsonPropertyName("attestation_hash")] public string AttestationHash { get; set; } = "";
    [JsonPropertyName("input_hash")] public string? InputHash { get; set; }
    [JsonPropertyName("output_hash")] public string? OutputHash { get; set; }
    [JsonPropertyName("signed_payload")] public string SignedPayload { get; set; } = "";
    [JsonPropertyName("signature")] public string Signature { get; set; } = "";
    [JsonPropertyName("public_key")] public string PublicKey { get; set; } = "";
    [JsonPropertyName("signature_alg")] public string SignatureAlg { get; set; } = "";
    [JsonPropertyName("model_provider")] public string? ModelProvider { get; set; }
    [JsonPropertyName("model_name")] public string? ModelName { get; set; }
    [JsonPropertyName("model_version")] public string? ModelVersion { get; set; }
    [JsonPropertyName("retention_policy")] public string RetentionPolicy { get; set; } = "";
    [JsonPropertyName("created_at")] public string CreatedAt { get; set; } = "";
    [JsonPropertyName("organization")] public OrganizationPublic? Organization { get; set; }
}

/// <summary>Result of client-side Ed25519 signature verification.</summary>
public sealed class SignatureVerificationResult
{
    /// <summary>Whether the signature is valid.</summary>
    public bool Valid { get; init; }

    /// <summary>Human-readable reason if invalid, <c>null</c> if valid.</summary>
    public string? Reason { get; init; }

    /// <summary>The attestation that was verified.</summary>
    public required AiAttestation Attestation { get; init; }

    /// <summary>The parsed JSON covered by the signature, or <c>null</c>.</summary>
    public Dictionary<string, object?>? SignedData { get; init; }
}

public sealed class VerifyAttestationResponse
{
    [JsonPropertyName("attestation_id")] public string AttestationId { get; set; } = "";
    [JsonPropertyName("match_result")] public bool MatchResult { get; set; }
    [JsonPropertyName("matched_field")] public string? MatchedField { get; set; }
    [JsonPropertyName("anchored_hash")] public string AnchoredHash { get; set; } = "";
    [JsonPropertyName("submitted_hash")] public string SubmittedHash { get; set; } = "";
    [JsonPropertyName("anchored_at")] public string AnchoredAt { get; set; } = "";
    [JsonPropertyName("organization")] public OrganizationPublic? Organization { get; set; }
}
