using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Backup;

namespace Tankradar.TestSupport;

/// <summary>
/// Erzeugt und verändert verschlüsselte Sicherungscontainer für Tests über den produktiven <see cref="BackupCrypto"/> (nur Unit- und Integrationstests mit Verweis auf das Hauptprojekt).
/// </summary>
public static class BackupEncryptedTestArchive
{
    /// <summary>
    /// Verschlüsselt ein Archiv mit dem produktiven Code als Container.
    /// </summary>
    /// <param name="zipPath">Das unverschlüsselte ZIP-Archiv.</param>
    /// <param name="containerPath">Der Zielpfad des Containers.</param>
    /// <param name="password">Das Passwort.</param>
    /// <returns>Eine Aufgabe.</returns>
    public static Task CreateAsync(string zipPath, string containerPath, string password)
    {
        return BackupCrypto.EncryptAsync(zipPath, containerPath, password);
    }

    /// <summary>
    /// Überschreibt die Containerversion im unverschlüsselten Header (Bytes 8–9, Little-Endian), ohne die Nutzlast neu zu authentifizieren.
    /// </summary>
    /// <param name="containerPath">Der Container.</param>
    /// <param name="version">Die neue Versionsnummer.</param>
    public static void WriteVersion(string containerPath, ushort version)
    {
        using var stream = new FileStream(containerPath, FileMode.Open, FileAccess.Write, FileShare.None);
        stream.Position = BackupFormat.EncryptedMagic.Length;
        Span<byte> bytes = stackalloc byte[2];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bytes, version);
        stream.Write(bytes);
    }

    /// <summary>
    /// Überschreibt die Iterationszahl der Schlüsselableitung im unverschlüsselten Header (Bytes 12–15, Little-Endian), ohne die Nutzlast neu zu authentifizieren.
    /// </summary>
    /// <param name="containerPath">Der Container.</param>
    /// <param name="iterations">Die neue Iterationszahl.</param>
    public static void WriteIterations(string containerPath, int iterations)
    {
        using var stream = new FileStream(containerPath, FileMode.Open, FileAccess.Write, FileShare.None);
        stream.Position = BackupFormat.EncryptedMagic.Length + 2 + 2;
        Span<byte> bytes = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes, iterations);
        stream.Write(bytes);
    }

    /// <summary>
    /// Vertauscht die Nutzlast der ersten beiden Chunks mitsamt ihren Tags (erfordert einen Container mit mindestens zwei Chunks).
    /// </summary>
    /// <param name="containerPath">Der Container.</param>
    public static void SwapFirstTwoChunks(string containerPath)
    {
        var bytes = File.ReadAllBytes(containerPath);
        var header = BackupCrypto.ReadHeader(new MemoryStream(bytes, writable: false));
        var headerLength = header!.Bytes.Length;
        var firstLength = header.ChunkSize + BackupFormat.EncryptedTagBytes;
        var secondLength = bytes.Length - headerLength - firstLength;
        if (secondLength <= 0)
        {
            throw new InvalidOperationException("Der Container hat nur einen Chunk.");
        }

        var swapped = new byte[bytes.Length];
        Buffer.BlockCopy(bytes, 0, swapped, 0, headerLength);
        Buffer.BlockCopy(bytes, headerLength + firstLength, swapped, headerLength, secondLength);
        Buffer.BlockCopy(bytes, headerLength, swapped, headerLength + secondLength, firstLength);
        File.WriteAllBytes(containerPath, swapped);
    }

    /// <summary>
    /// Kippt das letzte Byte des Containers (manipuliertes Authentifikations-Tag des letzten Chunks).
    /// </summary>
    /// <param name="containerPath">Der Container.</param>
    public static void FlipLastByte(string containerPath)
    {
        using var stream = new FileStream(containerPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        stream.Position = stream.Length - 1;
        var value = stream.ReadByte();
        stream.Position = stream.Length - 1;
        stream.WriteByte((byte)(value ^ 0xFF));
    }

    /// <summary>
    /// Kürzt den Container auf eine Länge.
    /// </summary>
    /// <param name="containerPath">Der Container.</param>
    /// <param name="keepBytes">Die verbleibende Länge in Bytes.</param>
    public static void Truncate(string containerPath, long keepBytes)
    {
        using var stream = new FileStream(containerPath, FileMode.Open, FileAccess.Write, FileShare.None);
        stream.SetLength(keepBytes);
    }
}
