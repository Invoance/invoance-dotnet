using System.Text.RegularExpressions;
using Invoance.Exceptions;

namespace Invoance.Internal;

/// <summary>Shared client-side input validators.</summary>
public static partial class Validate
{
    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex HexSha256();

    /// <summary>
    /// Validate that <paramref name="value"/> is a 64-char lowercase hex
    /// SHA-256 digest. Throws <see cref="ValidationException"/> otherwise.
    /// </summary>
    public static void AssertSha256Hex(string fieldName, string? value)
    {
        if (value is null)
        {
            throw new ValidationException(
                $"{fieldName} must be a string containing a 64-char hex SHA-256 digest (got null)");
        }
        if (value.Length != 64)
        {
            throw new ValidationException(
                $"{fieldName} must be 64 hex chars (got {value.Length} chars)");
        }
        if (!HexSha256().IsMatch(value))
        {
            var prefix = value.Length >= 16 ? value[..16] : value;
            throw new ValidationException(
                $"{fieldName} must be lowercase hex [0-9a-f]; \"{prefix}…\" is not");
        }
    }
}
