using System.Text;
using Invoance.Internal;
using Xunit;

namespace Invoance.Tests;

public class CanonicalTests
{
    // A known audit event. The expected canonical string + payload hash were
    // produced by the reference Node SDK's canonicalization logic and pinned
    // here to guarantee byte-parity.
    private static Dictionary<string, object?> GoldenEvent() => new()
    {
        ["org_id"] = "aorg_123",
        ["event_id"] = "aevt_456",
        ["seq"] = 7,
        ["ingested_at"] = "2026-01-02T03:04:05.6789Z",
        ["action"] = "user.login",
        ["occurred_at"] = "2026-01-02T03:04:05+02:00",
        ["actor"] = new Dictionary<string, object?> { ["id"] = "u_1", ["type"] = "user", ["name"] = "Zoe" },
        ["targets"] = new List<object?>
        {
            new Dictionary<string, object?> { ["id"] = "doc_9", ["type"] = "document" },
        },
        ["context"] = new Dictionary<string, object?> { ["ip"] = "10.0.0.1" },
        ["metadata"] = new Dictionary<string, object?> { ["note"] = "héllo/world" },
    };

    private const string ExpectedCanonical =
        "{\"action\":\"user.login\",\"actor\":{\"id\":\"u_1\",\"name\":\"Zoe\",\"type\":\"user\"}," +
        "\"context\":{\"ip\":\"10.0.0.1\"},\"event_id\":\"aevt_456\"," +
        "\"ingested_at\":\"2026-01-02T03:04:05.678Z\",\"metadata\":{\"note\":\"héllo/world\"}," +
        "\"occurred_at\":\"2026-01-02T01:04:05.000Z\",\"org_id\":\"aorg_123\"," +
        "\"schema_id\":\"invoance.audit/1\",\"seq\":7,\"targets\":[{\"id\":\"doc_9\",\"type\":\"document\"}]}";

    private const string ExpectedHash =
        "057e04ba828ca319cd5f137a2f6bde2d7f44fb36b62c1ec834f9752f790f09b7";

    [Fact]
    public void CanonicalAuditBytes_MatchesGoldenString()
    {
        var bytes = AuditCanonical.CanonicalAuditBytes(GoldenEvent());
        var actual = Encoding.UTF8.GetString(bytes);
        Assert.Equal(ExpectedCanonical, actual);
    }

    [Fact]
    public void PayloadHashHex_MatchesGolden()
    {
        var bytes = AuditCanonical.CanonicalAuditBytes(GoldenEvent());
        Assert.Equal(ExpectedHash, AuditCanonical.PayloadHashHex(bytes));
    }

    [Fact]
    public void ByteParity_RelaxedEncoder_EmitsSlashAndUnicodeLiterally()
    {
        // The default System.Text.Json encoder would escape "/" and non-ASCII;
        // the relaxed encoder must not. Assert against a known compact string.
        var value = new Dictionary<string, object?> { ["note"] = "héllo/world", ["a"] = 1 };
        var actual = Canonical.CanonicalStringify(value);
        Assert.Equal("{\"a\":1,\"note\":\"héllo/world\"}", actual);
    }

    [Fact]
    public void NormalizeTs_TruncatesFractionAndConvertsToUtc()
    {
        Assert.Equal("2026-01-02T01:04:05.000Z", AuditCanonical.NormalizeTs("2026-01-02T03:04:05+02:00"));
        Assert.Equal("2026-01-02T03:04:05.678Z", AuditCanonical.NormalizeTs("2026-01-02T03:04:05.6789Z"));
        Assert.Equal("2026-01-02T03:04:05.000Z", AuditCanonical.NormalizeTs("2026-01-02T03:04:05Z"));
    }

    [Fact]
    public void ContentIdempotencyKey_IsStableAndSorted()
    {
        var body = new Dictionary<string, object?>
        {
            ["organization_id"] = "aorg_123",
            ["action"] = "user.login",
            ["occurred_at"] = "2026-01-02T03:04:05Z",
            ["actor"] = new Dictionary<string, object?> { ["id"] = "u_1", ["type"] = "user" },
            ["targets"] = new List<object?>(),
        };
        var key = Invoance.Resources.AuditResource.ContentIdempotencyKey(body);
        Assert.Equal("idem_6caa290d5f202b820ab8321e30b49dbc7806f2df740edca705dfc6ac34cdff39", key);
    }

    [Fact]
    public void ContentIdempotencyKey_KeyOrderIndependent()
    {
        var a = new Dictionary<string, object?> { ["b"] = 2, ["a"] = 1 };
        var b = new Dictionary<string, object?> { ["a"] = 1, ["b"] = 2 };
        Assert.Equal(
            Invoance.Resources.AuditResource.ContentIdempotencyKey(a),
            Invoance.Resources.AuditResource.ContentIdempotencyKey(b));
    }
}
