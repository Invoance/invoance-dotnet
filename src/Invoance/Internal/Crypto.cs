using System.Security.Cryptography;
using System.Text;

namespace Invoance.Internal;

/// <summary>Low-level crypto/encoding helpers.</summary>
public static class Crypto
{
    /// <summary>SHA-256 of the given bytes, lowercase hex.</summary>
    public static string Sha256Hex(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return ToHex(hash);
    }

    /// <summary>SHA-256 of the UTF-8 bytes of <paramref name="text"/>, lowercase hex.</summary>
    public static string Sha256Hex(string text) => Sha256Hex(Encoding.UTF8.GetBytes(text));

    /// <summary>Lowercase hex encoding.</summary>
    public static string ToHex(ReadOnlySpan<byte> bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    /// <summary>Decode a hex string into bytes. Throws on malformed input.</summary>
    public static byte[] FromHex(string hex) => Convert.FromHexString(hex);
}
