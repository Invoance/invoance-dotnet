using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Invoance.Internal;

/// <summary>
/// Shared <see cref="JsonSerializerOptions"/> and helpers.
///
/// The relaxed encoder matches JavaScript's <c>JSON.stringify</c> escaping:
/// slashes and non-ASCII characters are emitted literally rather than as
/// <c>\uXXXX</c>. This is essential for byte-parity in canonicalization.
/// </summary>
public static class Json
{
    /// <summary>Compact serializer with the JS-compatible relaxed encoder.</summary>
    public static readonly JsonSerializerOptions Compact = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Deserializer options (ignores unknown members by default).</summary>
    public static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = false,
    };

    /// <summary>Serialize a value to compact JSON text using the relaxed encoder.</summary>
    public static string Serialize(object? value) => JsonSerializer.Serialize(value, Compact);

    /// <summary>Deserialize JSON text into <typeparamref name="T"/>.</summary>
    public static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, DeserializeOptions)!;

    /// <summary>
    /// Convert a <see cref="JsonNode"/> into a plain CLR value tree
    /// (<see cref="Dictionary{TKey,TValue}"/> / <see cref="List{T}"/> /
    /// primitives) so callers get an untyped, easily-inspected structure.
    /// </summary>
    public static object? NodeToClr(JsonNode? node)
    {
        switch (node)
        {
            case null:
                return null;
            case JsonObject obj:
            {
                var dict = new Dictionary<string, object?>();
                foreach (var kv in obj)
                {
                    dict[kv.Key] = NodeToClr(kv.Value);
                }
                return dict;
            }
            case JsonArray arr:
            {
                var list = new List<object?>();
                foreach (var item in arr)
                {
                    list.Add(NodeToClr(item));
                }
                return list;
            }
            case JsonValue val:
            {
                if (val.TryGetValue(out bool b)) return b;
                if (val.TryGetValue(out long l)) return l;
                if (val.TryGetValue(out double d)) return d;
                if (val.TryGetValue(out string? s)) return s;
                return val.ToJsonString();
            }
            default:
                return null;
        }
    }

    /// <summary>Parse JSON text into an untyped <c>Dictionary&lt;string, object?&gt;</c>.</summary>
    public static Dictionary<string, object?> ToDictionary(string json)
    {
        var node = JsonNode.Parse(json);
        return NodeToClr(node) as Dictionary<string, object?> ?? new Dictionary<string, object?>();
    }
}
