namespace Invoance.Internal;

/// <summary>Result of an offline audit-event signature verification.</summary>
public sealed record AuditVerifyResult(
    bool Valid,
    string? Reason,
    string PayloadHash,
    string KeySource);

/// <summary>
/// Offline, client-side signature verification for audit events.
///
/// Reconstructs the canonical signed bytes from an event returned by the API
/// and checks the Ed25519 signature. By default it verifies against the key
/// embedded in the event (<c>signing_public_key</c>). For a real tamper
/// guarantee, pass the tenant's pinned public key.
/// </summary>
public static class AuditVerify
{
    /// <summary>Verify one audit event's signature offline.</summary>
    /// <param name="ev">The event as an untyped map (wire keys).</param>
    /// <param name="publicKey">Optional pinned public key (hex string or raw bytes).</param>
    public static AuditVerifyResult VerifyAuditEvent(
        IReadOnlyDictionary<string, object?> ev,
        object? publicKey = null)
    {
        var keySource = publicKey != null ? "pinned" : "event";

        object? Get(string key) => ev.TryGetValue(key, out var v) ? v : null;

        var signedInput = new Dictionary<string, object?>
        {
            ["org_id"] = Get("org_id"),
            ["event_id"] = Get("id") ?? Get("event_id"),
            ["seq"] = Get("seq"),
            ["ingested_at"] = Get("ingested_at"),
            ["action"] = Get("action"),
            ["occurred_at"] = Get("occurred_at"),
            ["actor"] = Get("actor"),
            ["targets"] = Get("targets"),
        };
        if (Get("context") != null) signedInput["context"] = Get("context");
        if (Get("metadata") != null) signedInput["metadata"] = Get("metadata");

        byte[] canonical;
        try
        {
            canonical = AuditCanonical.CanonicalAuditBytes(signedInput);
        }
        catch
        {
            return new AuditVerifyResult(false, "canonicalization_failed", "", keySource);
        }

        var recomputed = AuditCanonical.PayloadHashHex(canonical);

        var payloadHash = Get("payload_hash") as string;
        if (payloadHash != null && payloadHash != recomputed)
        {
            return new AuditVerifyResult(false, "payload_hash_mismatch", recomputed, keySource);
        }

        var key = publicKey ?? Get("signing_public_key");
        if (key == null)
        {
            return new AuditVerifyResult(false, "no_public_key", recomputed, keySource);
        }

        var signature = Get("signature");
        if (signature == null)
        {
            return new AuditVerifyResult(false, "no_signature", recomputed, keySource);
        }

        byte[] keyBytes, sigBytes;
        try
        {
            keyBytes = ToBytes(key);
            sigBytes = ToBytes(signature);
        }
        catch
        {
            return new AuditVerifyResult(false, "signature_invalid", recomputed, keySource);
        }

        var ok = Ed25519.Verify(canonical, sigBytes, keyBytes);
        return ok
            ? new AuditVerifyResult(true, null, recomputed, keySource)
            : new AuditVerifyResult(false, "signature_invalid", recomputed, keySource);
    }

    private static byte[] ToBytes(object value) => value switch
    {
        byte[] b => b,
        string s => Crypto.FromHex(s),
        _ => throw new ArgumentException("expected hex string or byte[]"),
    };
}
