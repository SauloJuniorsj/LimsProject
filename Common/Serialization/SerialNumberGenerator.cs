namespace LimsProject.Common.Serialization;

public static class SerialNumberGenerator
{
    /// <summary>Compact serial from Guid (base32, unpadded).</summary>
    public static string Create()
    {
        Span<byte> bytes = stackalloc byte[16];
        Guid.NewGuid().TryWriteBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant()[..16];
    }
}
