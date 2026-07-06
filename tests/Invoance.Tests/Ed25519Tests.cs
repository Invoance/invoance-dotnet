using System.Text;
using Invoance.Internal;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using Xunit;

namespace Invoance.Tests;

public class Ed25519Tests
{
    private static (byte[] pub, byte[] priv) GenKeyPair()
    {
        var gen = new Ed25519KeyPairGenerator();
        gen.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
        var kp = gen.GenerateKeyPair();
        var pub = ((Ed25519PublicKeyParameters)kp.Public).GetEncoded();
        var priv = ((Ed25519PrivateKeyParameters)kp.Private).GetEncoded();
        return (pub, priv);
    }

    private static byte[] Sign(byte[] message, byte[] privRaw)
    {
        var priv = new Ed25519PrivateKeyParameters(privRaw, 0);
        var signer = new Ed25519Signer();
        signer.Init(true, priv);
        signer.BlockUpdate(message, 0, message.Length);
        return signer.GenerateSignature();
    }

    [Fact]
    public void Verify_RoundTrip_Succeeds()
    {
        var (pub, priv) = GenKeyPair();
        var msg = Encoding.UTF8.GetBytes("the quick brown fox");
        var sig = Sign(msg, priv);

        Assert.True(Ed25519.Verify(msg, sig, pub));
    }

    [Fact]
    public void Verify_TamperedMessage_Fails()
    {
        var (pub, priv) = GenKeyPair();
        var msg = Encoding.UTF8.GetBytes("original");
        var sig = Sign(msg, priv);
        var tampered = Encoding.UTF8.GetBytes("tampered!");

        Assert.False(Ed25519.Verify(tampered, sig, pub));
    }

    [Fact]
    public void Verify_MalformedKey_ReturnsFalseNotThrow()
    {
        var (_, priv) = GenKeyPair();
        var msg = Encoding.UTF8.GetBytes("x");
        var sig = Sign(msg, priv);
        Assert.False(Ed25519.Verify(msg, sig, new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public void AuditVerify_SignedEvent_VerifiesThroughSdkPath()
    {
        var (pub, priv) = GenKeyPair();

        var ev = new Dictionary<string, object?>
        {
            ["org_id"] = "aorg_1",
            ["id"] = "aevt_1",
            ["seq"] = 1,
            ["ingested_at"] = "2026-01-01T00:00:00Z",
            ["action"] = "test.action",
            ["occurred_at"] = "2026-01-01T00:00:00Z",
            ["actor"] = new Dictionary<string, object?> { ["id"] = "u_1" },
            ["targets"] = new List<object?>(),
        };

        // Build canonical the same way the verifier will, then sign it.
        var signedInput = new Dictionary<string, object?>
        {
            ["org_id"] = ev["org_id"],
            ["event_id"] = ev["id"],
            ["seq"] = ev["seq"],
            ["ingested_at"] = ev["ingested_at"],
            ["action"] = ev["action"],
            ["occurred_at"] = ev["occurred_at"],
            ["actor"] = ev["actor"],
            ["targets"] = ev["targets"],
        };
        var canonical = AuditCanonical.CanonicalAuditBytes(signedInput);
        var sig = Sign(canonical, priv);

        ev["payload_hash"] = AuditCanonical.PayloadHashHex(canonical);
        ev["signature"] = Crypto.ToHex(sig);
        ev["signing_public_key"] = Crypto.ToHex(pub);

        var result = AuditVerify.VerifyAuditEvent(ev);
        Assert.True(result.Valid);
        Assert.Null(result.Reason);
        Assert.Equal("event", result.KeySource);

        // Pinned key path.
        var pinned = AuditVerify.VerifyAuditEvent(ev, Crypto.ToHex(pub));
        Assert.True(pinned.Valid);
        Assert.Equal("pinned", pinned.KeySource);
    }

    [Fact]
    public void AuditVerify_TamperedPayloadHash_ReportsMismatch()
    {
        var ev = new Dictionary<string, object?>
        {
            ["org_id"] = "aorg_1",
            ["id"] = "aevt_1",
            ["seq"] = 1,
            ["ingested_at"] = "2026-01-01T00:00:00Z",
            ["action"] = "test.action",
            ["occurred_at"] = "2026-01-01T00:00:00Z",
            ["actor"] = new Dictionary<string, object?> { ["id"] = "u_1" },
            ["targets"] = new List<object?>(),
            ["payload_hash"] = new string('0', 64),
        };
        var result = AuditVerify.VerifyAuditEvent(ev);
        Assert.False(result.Valid);
        Assert.Equal("payload_hash_mismatch", result.Reason);
    }
}
