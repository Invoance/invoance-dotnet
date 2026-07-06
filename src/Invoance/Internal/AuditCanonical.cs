using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Invoance.Internal;

/// <summary>
/// <c>invoance.audit/1</c> canonical serializer (client-side).
///
/// Reproduces the server's frozen canonicalization so an event's signature
/// can be checked offline. Canonical bytes = build the signed object (signed
/// fields present + non-null, timestamps normalized, forced <c>schema_id</c>),
/// strip null members recursively, sort every object's keys, emit compact UTF-8.
/// </summary>
public static partial class AuditCanonical
{
    /// <summary>The audit schema identifier baked into every canonical form.</summary>
    public const string AuditSchemaId = "invoance.audit/1";

    private static readonly string[] SignedFields =
    {
        "org_id", "event_id", "seq", "ingested_at", "action",
        "occurred_at", "actor", "targets", "context", "metadata",
    };

    private static readonly string[] RequiredFields =
    {
        "org_id", "event_id", "seq", "ingested_at", "action",
        "occurred_at", "actor", "targets",
    };

    [GeneratedRegex(@"^(\d{4})-(\d{2})-(\d{2})[Tt](\d{2}):(\d{2}):(\d{2})(?:\.(\d+))?(Z|z|[+-]\d{2}:\d{2})$")]
    private static partial Regex Rfc3339();

    /// <summary>
    /// RFC3339 → the one canonical form: UTC, exactly 3 fractional digits
    /// (TRUNCATE, not round), trailing <c>Z</c>.
    /// </summary>
    public static string NormalizeTs(string value)
    {
        if (value is null) throw new ArgumentException("timestamp must be a string");
        var m = Rfc3339().Match(value.Trim());
        if (!m.Success) throw new ArgumentException($"invalid RFC3339 timestamp: {value}");

        int yr = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        int mo = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        int dy = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
        int hh = int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture);
        int mi = int.Parse(m.Groups[5].Value, CultureInfo.InvariantCulture);
        int ss = int.Parse(m.Groups[6].Value, CultureInfo.InvariantCulture);
        string frac = m.Groups[7].Success ? m.Groups[7].Value : "";
        string off = m.Groups[8].Value;

        int millis = int.Parse((frac + "000")[..3], CultureInfo.InvariantCulture); // truncate

        var dt = new DateTime(yr, mo, dy, hh, mi, ss, DateTimeKind.Utc)
            .AddMilliseconds(millis);

        if (off != "Z" && off != "z")
        {
            int sign = off[0] == '+' ? 1 : -1;
            int oh = int.Parse(off.Substring(1, 2), CultureInfo.InvariantCulture);
            int om = int.Parse(off.Substring(4, 2), CultureInfo.InvariantCulture);
            dt = dt.AddSeconds(-sign * (oh * 3600 + om * 60));
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0:D4}-{1:D2}-{2:D2}T{3:D2}:{4:D2}:{5:D2}.{6:D3}Z",
            dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
    }

    /// <summary>
    /// Build the signed object: require all required fields non-null; copy
    /// signed fields that are present and non-null, normalizing
    /// <c>occurred_at</c> / <c>ingested_at</c>; force <c>schema_id</c>.
    /// </summary>
    public static Dictionary<string, object?> BuildSignedObject(IReadOnlyDictionary<string, object?> ev)
    {
        foreach (var f in RequiredFields)
        {
            if (!ev.TryGetValue(f, out var v) || v is null)
            {
                throw new ArgumentException($"missing required field: {f}");
            }
        }

        var outObj = new Dictionary<string, object?>();
        foreach (var f in SignedFields)
        {
            if (!ev.TryGetValue(f, out var v) || v is null) continue;
            if (f is "occurred_at" or "ingested_at")
            {
                outObj[f] = NormalizeTs((string)v!);
            }
            else
            {
                outObj[f] = v;
            }
        }
        outObj["schema_id"] = AuditSchemaId;
        return outObj;
    }

    /// <summary>The canonical signed bytes for an audit event.</summary>
    public static byte[] CanonicalAuditBytes(IReadOnlyDictionary<string, object?> ev)
    {
        var signed = BuildSignedObject(ev);
        var json = Canonical.CanonicalStringify(signed);
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary><c>payload_hash = SHA-256(canonical bytes)</c>, lowercase hex.</summary>
    public static string PayloadHashHex(byte[] canonical) => Crypto.Sha256Hex(canonical);
}
