using Invoance.Exceptions;
using Invoance.Internal;
using Xunit;

namespace Invoance.Tests;

public class ValidateTests
{
    [Fact]
    public void AssertSha256Hex_AcceptsValid()
    {
        var hex = new string('a', 64);
        Validate.AssertSha256Hex("h", hex); // no throw
    }

    [Theory]
    [InlineData(null)]                       // null
    [InlineData("abc")]                      // too short
    [InlineData("ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789")] // uppercase
    [InlineData("g000000000000000000000000000000000000000000000000000000000000000")] // 64 chars but non-hex 'g'
    public void AssertSha256Hex_RejectsInvalid(string? value)
    {
        Assert.Throws<ValidationException>(() => Validate.AssertSha256Hex("h", value));
    }

    [Fact]
    public void AssertSha256Hex_RejectsUppercaseSameLength()
    {
        var upper = new string('A', 64);
        Assert.Throws<ValidationException>(() => Validate.AssertSha256Hex("h", upper));
    }
}
