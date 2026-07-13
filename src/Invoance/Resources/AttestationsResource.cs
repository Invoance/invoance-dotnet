using System.Text;
using System.Text.Json.Nodes;
using Invoance.Internal;
using Invoance.Models;

namespace Invoance.Resources;

/// <summary>AI Attestations resource — <c>client.Attestations</c>.</summary>
public sealed class AttestationsResource
{
    private readonly HttpTransport _t;
    internal AttestationsResource(HttpTransport t) => _t = t;

    /// <summary>POST /ai/attestations — anchor an AI attestation.</summary>
    public Task<IngestAttestationResponse> IngestAsync(IngestAttestationParams p, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?>
        {
            ["type"] = p.Type,
            ["payload"] = new Dictionary<string, object?>
            {
                ["input"] = p.Input,
                ["output"] = p.Output,
            },
            ["context"] = new Dictionary<string, object?>
            {
                ["model_provider"] = p.ModelProvider,
                ["model_name"] = p.ModelName,
                ["model_version"] = p.ModelVersion,
            },
        };

        if (p.Subject != null)
        {
            var subject = new Dictionary<string, object?>();
            if (p.Subject.UserId != null) subject["user_id"] = p.Subject.UserId;
            if (p.Subject.SessionId != null) subject["session_id"] = p.Subject.SessionId;
            if (p.Subject.Extra != null)
            {
                foreach (var kv in p.Subject.Extra) subject[kv.Key] = kv.Value;
            }
            if (subject.Count > 0) body["subject"] = subject;
        }

        if (p.TraceId != null) body["trace_id"] = p.TraceId;

        return _t.PostAsync<IngestAttestationResponse>("/ai/attestations", body, p.IdempotencyKey, cancellationToken);
    }

    /// <summary>GET /ai/attestations — paginated attestation listing.</summary>
    public Task<ListAttestationsResponse> ListAsync(ListAttestationsParams? p = null, CancellationToken cancellationToken = default)
    {
        p ??= new ListAttestationsParams();
        var query = new Dictionary<string, object?>
        {
            ["page"] = p.Page,
            ["limit"] = p.Limit,
            ["date_from"] = p.DateFrom,
            ["date_to"] = p.DateTo,
            ["attestation_type"] = p.AttestationType,
            ["model_provider"] = p.ModelProvider,
        };
        return _t.GetAsync<ListAttestationsResponse>("/ai/attestations", query, cancellationToken);
    }

    /// <summary>GET /ai/attestations/{id} — retrieve a single attestation.</summary>
    public Task<AiAttestation> GetAsync(string attestationId, CancellationToken cancellationToken = default) =>
        _t.GetAsync<AiAttestation>($"/ai/attestations/{attestationId}", null, cancellationToken);

    /// <summary>POST /ai/attestations/{id}/verify — hash verification.</summary>
    public Task<VerifyAttestationResponse> VerifyAsync(string attestationId, VerifyAttestationParams p, CancellationToken cancellationToken = default)
    {
        Validate.AssertSha256Hex("contentHash", p.ContentHash);
        var body = new Dictionary<string, object?> { ["content_hash"] = p.ContentHash };
        return _t.PostAsync<VerifyAttestationResponse>($"/ai/attestations/{attestationId}/verify", body, null, cancellationToken);
    }

    /// <summary>
    /// GET /ai/attestations/{id}/raw — retrieve the original canonical JSON
    /// payload as an untyped node.
    /// </summary>
    public Task<JsonNode?> GetRawAsync(string attestationId, CancellationToken cancellationToken = default) =>
        _t.GetRawAsync($"/ai/attestations/{attestationId}/raw", null, cancellationToken);

    /// <summary>
    /// Verify by raw payload — hashes client-side, then calls verify. The
    /// canonical JSON PRESERVES key order from the source (the server hashes
    /// with struct field order <c>type/payload/context/subject</c>, NOT
    /// alphabetical). Passing the raw JSON string exactly as stored in the
    /// dashboard's "Raw immutable record" viewer is the safe input.
    /// </summary>
    public Task<VerifyAttestationResponse> VerifyPayloadAsync(string attestationId, string payload, CancellationToken cancellationToken = default)
    {
        // JSON.parse -> JSON.stringify equivalent: parse preserves object
        // property order; re-serialize compact with the relaxed encoder.
        var node = Canonical.Parse(payload);
        var canonical = Canonical.CompactPreserveOrder(node);
        return HashAndVerify(attestationId, canonical, cancellationToken);
    }

    /// <inheritdoc cref="VerifyPayloadAsync(string,string,CancellationToken)"/>
    public Task<VerifyAttestationResponse> VerifyPayloadAsync(string attestationId, byte[] payload, CancellationToken cancellationToken = default) =>
        VerifyPayloadAsync(attestationId, Encoding.UTF8.GetString(payload), cancellationToken);

    /// <summary>
    /// Verify by raw payload from an object/node — preserves insertion order
    /// (does NOT sort). Prefer the string overload when order matters and you
    /// have the exact source bytes.
    /// </summary>
    public Task<VerifyAttestationResponse> VerifyPayloadAsync(string attestationId, JsonNode payload, CancellationToken cancellationToken = default)
    {
        var canonical = Canonical.CompactPreserveOrder(payload);
        return HashAndVerify(attestationId, canonical, cancellationToken);
    }

    /// <inheritdoc cref="VerifyPayloadAsync(string,JsonNode,CancellationToken)"/>
    public Task<VerifyAttestationResponse> VerifyPayloadAsync(string attestationId, IDictionary<string, object?> payload, CancellationToken cancellationToken = default)
    {
        var canonical = Canonical.CompactPreserveOrder(payload);
        return HashAndVerify(attestationId, canonical, cancellationToken);
    }

    private Task<VerifyAttestationResponse> HashAndVerify(string attestationId, string canonical, CancellationToken cancellationToken)
    {
        var contentHash = Crypto.Sha256Hex(canonical);
        return VerifyAsync(attestationId, new VerifyAttestationParams { ContentHash = contentHash }, cancellationToken);
    }

    /// <summary>
    /// Verify the Ed25519 signature of an attestation, fully client-side.
    /// Fetches the attestation and checks the signature over
    /// <c>signed_payload</c> using <c>public_key</c> (both hex-decoded).
    /// </summary>
    public async Task<SignatureVerificationResult> VerifySignatureAsync(string attestationId, CancellationToken cancellationToken = default)
    {
        var att = await GetAsync(attestationId, cancellationToken).ConfigureAwait(false);

        bool valid;
        string? reason = null;
        try
        {
            var signedPayloadBytes = Crypto.FromHex(att.SignedPayload);
            var signatureBytes = Crypto.FromHex(att.Signature);
            var publicKeyBytes = Crypto.FromHex(att.PublicKey);

            valid = Ed25519.Verify(signedPayloadBytes, signatureBytes, publicKeyBytes);
            if (!valid)
            {
                reason = "Signature does not match signed_payload + public_key";
            }

            Dictionary<string, object?>? signedData = null;
            try
            {
                var node = JsonNode.Parse(Encoding.UTF8.GetString(signedPayloadBytes));
                signedData = Json.NodeToClr(node) as Dictionary<string, object?>;
            }
            catch { /* not valid JSON */ }

            return new SignatureVerificationResult
            {
                Valid = valid,
                Reason = reason,
                Attestation = att,
                SignedData = signedData,
            };
        }
        catch (Exception ex)
        {
            return new SignatureVerificationResult
            {
                Valid = false,
                Reason = ex.Message,
                Attestation = att,
                SignedData = null,
            };
        }
    }
}
