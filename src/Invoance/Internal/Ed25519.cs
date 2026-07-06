using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Invoance.Internal;

/// <summary>
/// Ed25519 verification via BouncyCastle (the .NET BCL has no Ed25519).
/// Accepts a raw 32-byte public key and a raw 64-byte signature — no DER
/// wrapper needed. Never throws; malformed input yields <c>false</c>.
/// </summary>
public static class Ed25519
{
    /// <summary>
    /// Verify an Ed25519 signature. <paramref name="publicKey"/> is the raw
    /// 32-byte key, <paramref name="signature"/> the raw 64-byte signature.
    /// </summary>
    public static bool Verify(byte[] message, byte[] signature, byte[] publicKey)
    {
        try
        {
            var pk = new Ed25519PublicKeyParameters(publicKey, 0);
            var signer = new Ed25519Signer();
            signer.Init(false, pk); // false = verify
            signer.BlockUpdate(message, 0, message.Length);
            return signer.VerifySignature(signature);
        }
        catch
        {
            return false;
        }
    }
}
