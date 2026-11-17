using System.Buffers.Binary;

namespace Tankradar.MAUI.Models.Backup;

/// <summary>
/// Der unverschlüsselte Header einer verschlüsselten Sicherung (ADR 0019): Containerversion, KDF-Parameter, Salt, Nonce-Basis und Chunk-Größe.
/// Die ersten zehn Bytes (Magic-Bytes und Containerversion) sind über alle Containerversionen invariant; bei einer höheren Version als der der App
/// ist nur <see cref="ContainerVersion"/> belegt.
/// </summary>
public sealed class BackupEncryptionHeader
{
    private const int FixedPrefixLength = 8 + 2 + 2 + 4 + 1;
    private const int FixedSuffixLength = 1 + 4;
    private const int MinSaltBytes = 8;
    private const int MaxSaltBytes = 64;
    private const int MinIterations = 1;
    private const int MaxIterations = 10_000_000;
    private const int MinChunkSize = 1024;
    private const int MaxChunkSize = 64 * 1024 * 1024;

    /// <summary>
    /// Die Containerversion aus dem Header.
    /// </summary>
    public ushort ContainerVersion { get; set; }

    /// <summary>
    /// Die Kennung der Schlüsselableitung (<see cref="BackupFormat.EncryptedKdfPbkdf2Sha256"/>).
    /// </summary>
    public ushort KdfId { get; set; }

    /// <summary>
    /// Die Iterationszahl der Schlüsselableitung.
    /// </summary>
    public int Iterations { get; set; }

    /// <summary>
    /// Das Salt der Schlüsselableitung.
    /// </summary>
    public byte[] Salt { get; set; } = [];

    /// <summary>
    /// Die Nonce-Basis, aus der die Nonce je Chunk abgeleitet wird.
    /// </summary>
    public byte[] NonceBase { get; set; } = [];

    /// <summary>
    /// Die Klartext-Größe eines Verschlüsselungschunks in Bytes.
    /// </summary>
    public int ChunkSize { get; set; }

    /// <summary>
    /// Die serialisierten Header-Bytes (Grundlage der zusätzlich authentifizierten Daten der Chunks).
    /// </summary>
    public byte[] Bytes { get; set; } = [];

    /// <summary>
    /// Serialisiert den Header in die feste Bytefolge des Containerformats.
    /// </summary>
    /// <returns>Die Header-Bytes.</returns>
    public byte[] ToBytes()
    {
        var bytes = new byte[FixedPrefixLength + Salt.Length + FixedSuffixLength + NonceBase.Length];
        BackupFormat.EncryptedMagic.CopyTo(bytes);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(8), ContainerVersion);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(10), KdfId);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12), Iterations);
        bytes[16] = checked((byte)Salt.Length);
        Salt.CopyTo(bytes.AsSpan(17));
        var nonceOffset = 17 + Salt.Length;
        bytes[nonceOffset] = checked((byte)NonceBase.Length);
        NonceBase.CopyTo(bytes.AsSpan(nonceOffset + 1));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(nonceOffset + 1 + NonceBase.Length), ChunkSize);
        return bytes;
    }

    /// <summary>
    /// Zerlegt Header-Bytes und prüft sie gegen die Grenzen der Containerversion 1.
    /// </summary>
    /// <param name="bytes">Die Bytes vom Dateianfang (mindestens die volle Headerlänge).</param>
    /// <param name="header">Der geparste Header oder <see langword="null"/>.</param>
    /// <returns><see langword="true"/>, wenn die Bytes einen gültigen Header der eigenen Containerversion bilden.</returns>
    public static bool TryParse(ReadOnlySpan<byte> bytes, out BackupEncryptionHeader? header)
    {
        header = null;
        if (bytes.Length < FixedPrefixLength + FixedSuffixLength || !bytes.StartsWith(BackupFormat.EncryptedMagic))
        {
            return false;
        }

        var version = BinaryPrimitives.ReadUInt16LittleEndian(bytes[8..]);
        var kdfId = BinaryPrimitives.ReadUInt16LittleEndian(bytes[10..]);
        var iterations = BinaryPrimitives.ReadInt32LittleEndian(bytes[12..]);
        var saltLength = bytes[16];
        var nonceOffset = 17 + saltLength;
        if (bytes.Length < nonceOffset + FixedSuffixLength)
        {
            return false;
        }

        var nonceLength = bytes[nonceOffset];
        var headerLength = nonceOffset + 1 + nonceLength + 4;
        if (bytes.Length < headerLength)
        {
            return false;
        }

        var chunkSize = BinaryPrimitives.ReadInt32LittleEndian(bytes[(headerLength - 4)..]);
        if (version < 1
            || version > BackupFormat.EncryptedContainerVersion
            || kdfId != BackupFormat.EncryptedKdfPbkdf2Sha256
            || iterations is < MinIterations or > MaxIterations
            || saltLength is < MinSaltBytes or > MaxSaltBytes
            || nonceLength != BackupFormat.EncryptedNonceBytes
            || chunkSize is < MinChunkSize or > MaxChunkSize
            || headerLength > BackupLimits.MaxEncryptedHeaderBytes)
        {
            return false;
        }

        header = new BackupEncryptionHeader
        {
            ContainerVersion = version,
            KdfId = kdfId,
            Iterations = iterations,
            Salt = bytes.Slice(17, saltLength).ToArray(),
            NonceBase = bytes.Slice(nonceOffset + 1, nonceLength).ToArray(),
            ChunkSize = chunkSize,
            Bytes = bytes[..headerLength].ToArray(),
        };
        return true;
    }
}
