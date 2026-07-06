using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Invoance.Internal;

/// <summary>
/// Deterministic JSON serialization helpers used by the audit canonicalizer
/// and content-idempotency key derivation.
///
/// These reproduce the reference SDK's serialization semantics exactly:
/// <list type="bullet">
/// <item>compact output (no whitespace)</item>
/// <item>the JS-compatible relaxed encoder (slashes / non-ASCII emitted literally)</item>
/// <item>keys optionally sorted with <see cref="StringComparer.Ordinal"/> deeply</item>
/// </list>
/// </summary>
public static class Canonical
{
    // Reused encoder for hand-rolled escaping matching System.Text.Json + relaxed encoder.
    private static readonly JavaScriptEncoder Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

    /// <summary>
    /// Serialize a value tree to compact JSON, sorting every object's keys
    /// deeply (ordinal). Nulls are preserved (matching Node's
    /// <c>stableStringify</c>). Accepts <see cref="JsonNode"/> or plain CLR
    /// values (<see cref="IDictionary{TKey,TValue}"/>, <see cref="IEnumerable{T}"/>,
    /// primitives).
    /// </summary>
    public static string StableStringify(object? value)
    {
        var sb = new StringBuilder();
        WriteValue(sb, ToNode(value), sortKeys: true, stripNulls: false);
        return sb.ToString();
    }

    /// <summary>
    /// Serialize a value tree to compact JSON, sorting keys deeply (ordinal)
    /// AND stripping null members recursively. Used by the audit canonicalizer.
    /// </summary>
    public static string CanonicalStringify(object? value)
    {
        var sb = new StringBuilder();
        WriteValue(sb, ToNode(value), sortKeys: true, stripNulls: true);
        return sb.ToString();
    }

    /// <summary>
    /// Serialize a value tree to compact JSON PRESERVING key order (no sort,
    /// no null-strip). Used by attestation payload verification, which must
    /// match the server's struct-field order.
    /// </summary>
    public static string CompactPreserveOrder(object? value)
    {
        var sb = new StringBuilder();
        WriteValue(sb, ToNode(value), sortKeys: false, stripNulls: false);
        return sb.ToString();
    }

    /// <summary>Parse a JSON string into a <see cref="JsonNode"/> (preserves object property order).</summary>
    public static JsonNode? Parse(string json) => JsonNode.Parse(json);

    // ── Core writer ───────────────────────────────────────────

    private static void WriteValue(StringBuilder sb, JsonNode? node, bool sortKeys, bool stripNulls)
    {
        switch (node)
        {
            case null:
                sb.Append("null");
                return;
            case JsonObject obj:
                WriteObject(sb, obj, sortKeys, stripNulls);
                return;
            case JsonArray arr:
                WriteArray(sb, arr, sortKeys, stripNulls);
                return;
            case JsonValue val:
                WriteScalar(sb, val);
                return;
            default:
                sb.Append("null");
                return;
        }
    }

    private static void WriteObject(StringBuilder sb, JsonObject obj, bool sortKeys, bool stripNulls)
    {
        var entries = new List<KeyValuePair<string, JsonNode?>>(obj);
        if (sortKeys)
        {
            entries.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
        }

        sb.Append('{');
        var first = true;
        foreach (var kv in entries)
        {
            if (stripNulls && kv.Value is null) continue;
            if (!first) sb.Append(',');
            first = false;
            WriteJsonString(sb, kv.Key);
            sb.Append(':');
            WriteValue(sb, kv.Value, sortKeys, stripNulls);
        }
        sb.Append('}');
    }

    private static void WriteArray(StringBuilder sb, JsonArray arr, bool sortKeys, bool stripNulls)
    {
        sb.Append('[');
        var first = true;
        foreach (var item in arr)
        {
            if (!first) sb.Append(',');
            first = false;
            // Array elements: null is preserved even when stripNulls is set,
            // matching JS stripNulls which only removes object members.
            WriteValue(sb, item, sortKeys, stripNulls);
        }
        sb.Append(']');
    }

    private static void WriteScalar(StringBuilder sb, JsonValue val)
    {
        // Delegate scalar formatting (numbers/bools/strings) to System.Text.Json
        // with the relaxed encoder so escaping matches JS JSON.stringify.
        sb.Append(val.ToJsonString(Json.Compact));
    }

    private static void WriteJsonString(StringBuilder sb, string s)
    {
        // Emit a JSON string literal with the same escaping as System.Text.Json
        // relaxed encoder (used for object keys).
        var node = JsonValue.Create(s);
        sb.Append(node!.ToJsonString(Json.Compact));
    }

    // ── CLR -> JsonNode coercion ──────────────────────────────

    private static JsonNode? ToNode(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case JsonNode node:
                // Deep-clone so we never mutate a caller's node while iterating.
                return node.DeepClone();
            case JsonElement el:
                return JsonNode.Parse(el.GetRawText());
            case string s:
                return JsonValue.Create(s);
            case bool b:
                return JsonValue.Create(b);
            case IDictionary<string, object?> dict:
            {
                var obj = new JsonObject();
                foreach (var kv in dict) obj[kv.Key] = ToNode(kv.Value);
                return obj;
            }
            case System.Collections.IEnumerable en when value is not string:
            {
                var arr = new JsonArray();
                foreach (var item in en) arr.Add(ToNode(item));
                return arr;
            }
            default:
                // Numbers and other primitives — round-trip through the serializer.
                return JsonNode.Parse(JsonSerializer.Serialize(value, Json.Compact));
        }
    }
}
