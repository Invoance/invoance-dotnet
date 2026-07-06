using Invoance.Internal;
using Xunit;

namespace Invoance.Tests;

public class AttestationPayloadTests
{
    [Fact]
    public void CompactPreserveOrder_KeepsSourceOrder_NotAlphabetical()
    {
        // Pretty-printed source with struct field order type/payload/context.
        const string raw = "{\n  \"type\": \"output\",\n  \"payload\": {\"input\":\"a\",\"output\":\"b\"},\n  \"context\": {\"model_provider\":\"openai\"}\n}";

        var node = Canonical.Parse(raw);
        var compact = Canonical.CompactPreserveOrder(node);

        // Order preserved (NOT sorted), whitespace stripped.
        Assert.Equal(
            "{\"type\":\"output\",\"payload\":{\"input\":\"a\",\"output\":\"b\"},\"context\":{\"model_provider\":\"openai\"}}",
            compact);

        // And the content hash matches the reference SDK's value.
        Assert.Equal(
            "4475a21ad067ab8decb7dbf2101fbe57353e4d07ecaa0ec03b241ae4d71c63be",
            Crypto.Sha256Hex(compact));
    }

    [Fact]
    public void StableStringify_SortsKeys_PreservesNulls()
    {
        var value = new Dictionary<string, object?> { ["b"] = null, ["a"] = 1 };
        Assert.Equal("{\"a\":1,\"b\":null}", Canonical.StableStringify(value));
    }

    [Fact]
    public void CanonicalStringify_StripsNulls()
    {
        var value = new Dictionary<string, object?> { ["b"] = null, ["a"] = 1 };
        Assert.Equal("{\"a\":1}", Canonical.CanonicalStringify(value));
    }
}
