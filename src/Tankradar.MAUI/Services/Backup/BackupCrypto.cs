using System.Buffers.Binary;
using System.Security.Cryptography;
using Tankradar.MAUI.Models.Backup;

namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Passwort-Verschlüsselung der Sicherungsdateien (ADR 0019): unverschlüsselter Header (Magic, Containerversion, KDF-Parameter, Salt, Nonce-Basis,
/// Chunk-Größe) plus das bisherige ZIP-Archiv als AES-256-GCM-Nutzlast in Chunks. Die Schlüsselableitung ist PBKDF2-HMAC-SHA256;
/// der abgeleitete Schlüssel wird nach jedem Vorgang mit <see cref="CryptographicOperations.ZeroMemory"/> verworfen.
/// </summary>
public static class BackupCrypto
{
    /// <summary>
    /// Prüft, ob der Strom mit den Magic-Bytes des verschlüsselten Containers beginnt. Bei einem seekbaren Strom bleibt die Position erhalten.
    /// </summary>
    /// <param name="stream">Der Strom (gelesen wird ab der aktuellen Position).</param>
    /// <returns><see langword="true"/>, wenn der Anfang die Magic-Bytes trägt.</returns>
    public static bool IsEncrypted(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var position = stream.CanSeek ? stream.Position : 0;
        try
        {
            Span<byte> buffer = stackalloc byte[BackupFormat.EncryptedMagic.Length];
            return FillExactly(stream, buffer) && buffer.SequenceEqual(BackupFormat.EncryptedMagic);
        }
        finally
        {
            if (stream.CanSeek)
            {
                stream.Position = position;
            }
        }
    }

    /// <summary>
    /// Liest den unverschlüsselten Container-Header vom Anfang des Stroms. Bei einem seekbaren Strom bleibt die Position erhalten.
    /// </summary>
    /// <param name="stream">Der Strom (gelesen wird ab der aktuellen Position).</param>
    /// <returns>
    /// Der Header; bei einer höheren Containerversion als der der App nur mit <see cref="BackupEncryptionHeader.ContainerVersion"/> belegt;
    /// <see langword="null"/>, wenn die Magic-Bytes fehlen oder der Header der eigenen Version defekt ist.
    /// </returns>
    public static BackupEncryptionHeader? ReadHeader(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var position = stream.CanSeek ? stream.Position : 0;
        try
        {
            return ReadHeaderCore(stream);
        }
        finally
        {
            if (stream.CanSeek)
            {
                stream.Position = position;
            }
        }
    }

    /// <summary>
    /// Verschlüsselt eine Datei als Container (Header plus chunked AES-256-GCM-Strom).
    /// </summary>
    /// <param name="sourcePath">Das unverschlüsselte ZIP-Archiv.</param>
    /// <param name="targetPath">Der neue Container.</param>
    /// <param name="password">Das Passwort der Schlüsselableitung (wird wörtlich verwendet, nicht normalisiert).</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Eine Aufgabe.</returns>
    public static async Task EncryptAsync(string sourcePath, string targetPath, string password, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        var header = new BackupEncryptionHeader
        {
            ContainerVersion = BackupFormat.EncryptedContainerVersion,
            KdfId = BackupFormat.EncryptedKdfPbkdf2Sha256,
            Iterations = BackupFormat.Pbkdf2Iterations,
            Salt = RandomNumberGenerator.GetBytes(BackupFormat.EncryptedSaltBytes),
            NonceBase = RandomNumberGenerator.GetBytes(BackupFormat.EncryptedNonceBytes),
            ChunkSize = BackupFormat.EncryptedChunkSize,
        };
        header.Bytes = header.ToBytes();

        var key = await Task.Run(() => DeriveKey(password, header), cancellationToken).ConfigureAwait(false);
        try
        {
            await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, BackupLimits.BufferSize, useAsync: true);
            await using var target = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, BackupLimits.BufferSize, useAsync: true);
            await target.WriteAsync(header.Bytes, cancellationToken).ConfigureAwait(false);
            await EncryptChunksAsync(source, target, header, key, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    /// <summary>
    /// Entschlüsselt einen Container zurück in das ZIP-Archiv.
    /// </summary>
    /// <param name="sourcePath">Der verschlüsselte Container.</param>
    /// <param name="targetPath">Das entschlüsselte ZIP-Archiv.</param>
    /// <param name="password">Das Passwort der Schlüsselableitung.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Eine Aufgabe.</returns>
    /// <exception cref="InvalidDataException">Der Header ist defekt oder gehört zu einer anderen Containerversion.</exception>
    /// <exception cref="CryptographicException">Das Passwort ist falsch oder der Chiffretext ist beschädigt/manipuliert (AEAD-Authentifikation, Länge, Abschluss).</exception>
    public static async Task DecryptAsync(string sourcePath, string targetPath, string password, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, BackupLimits.BufferSize, useAsync: true);
        var header = ReadHeaderCore(source);
        if (header is null || header.ContainerVersion != BackupFormat.EncryptedContainerVersion)
        {
            throw new InvalidDataException("Der Header der verschlüsselten Sicherung ist defekt oder gehört zu einer anderen Containerversion.");
        }

        var key = await Task.Run(() => DeriveKey(password, header), cancellationToken).ConfigureAwait(false);
        try
        {
            await using var target = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, BackupLimits.BufferSize, useAsync: true);
            await DecryptChunksAsync(source, target, header, key, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static byte[] DeriveKey(string password, BackupEncryptionHeader header)
    {
        return Rfc2898DeriveBytes.Pbkdf2(password, header.Salt, header.Iterations, HashAlgorithmName.SHA256, BackupFormat.EncryptedKeyBytes);
    }

    private static async Task EncryptChunksAsync(Stream source, Stream target, BackupEncryptionHeader header, byte[] key, CancellationToken cancellationToken)
    {
        var plain = new byte[header.ChunkSize];
        var cipher = new byte[header.ChunkSize];
        var tag = new byte[BackupFormat.EncryptedTagBytes];
        var nonce = new byte[BackupFormat.EncryptedNonceBytes];
        var aad = NewAad(header);
        using var gcm = new AesGcm(key, BackupFormat.EncryptedTagBytes);
        var index = 0L;
        while (true)
        {
            var filled = 0;
            while (filled < plain.Length)
            {
                var read = await source.ReadAsync(plain.AsMemory(filled), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                filled += read;
            }

            var isLast = filled < plain.Length || source.Position == source.Length;
            WriteAad(aad, index, isLast);
            BuildNonce(header.NonceBase, index, nonce);
            gcm.Encrypt(nonce, plain.AsSpan(0, filled), cipher.AsSpan(0, filled), tag, aad);
            await target.WriteAsync(cipher.AsMemory(0, filled), cancellationToken).ConfigureAwait(false);
            await target.WriteAsync(tag, cancellationToken).ConfigureAwait(false);
            index++;
            if (isLast)
            {
                return;
            }
        }
    }

    private static async Task DecryptChunksAsync(Stream source, Stream target, BackupEncryptionHeader header, byte[] key, CancellationToken cancellationToken)
    {
        var cipher = new byte[header.ChunkSize];
        var plain = new byte[header.ChunkSize];
        var tag = new byte[BackupFormat.EncryptedTagBytes];
        var nonce = new byte[BackupFormat.EncryptedNonceBytes];
        var aad = NewAad(header);
        using var gcm = new AesGcm(key, BackupFormat.EncryptedTagBytes);
        var remaining = source.Length - source.Position;
        var index = 0L;
        try
        {
            while (remaining > 0)
            {
                var isLast = remaining <= header.ChunkSize + BackupFormat.EncryptedTagBytes;
                var cipherLength = isLast ? remaining - BackupFormat.EncryptedTagBytes : header.ChunkSize;
                if (cipherLength < 0)
                {
                    throw new CryptographicException("Der Chiffretext der verschlüsselten Sicherung ist unvollständig.");
                }

                await source.ReadExactlyAsync(cipher.AsMemory(0, (int)cipherLength), cancellationToken).ConfigureAwait(false);
                await source.ReadExactlyAsync(tag, cancellationToken).ConfigureAwait(false);
                WriteAad(aad, index, isLast);
                BuildNonce(header.NonceBase, index, nonce);
                gcm.Decrypt(nonce, cipher.AsSpan(0, (int)cipherLength), tag, plain.AsSpan(0, (int)cipherLength), aad);
                await target.WriteAsync(plain.AsMemory(0, (int)cipherLength), cancellationToken).ConfigureAwait(false);
                remaining -= cipherLength + BackupFormat.EncryptedTagBytes;
                index++;
            }
        }
        catch (EndOfStreamException ex)
        {
            throw new CryptographicException("Der Chiffretext der verschlüsselten Sicherung ist unvollständig.", ex);
        }

        if (index == 0)
        {
            throw new CryptographicException("Der Chiffretext der verschlüsselten Sicherung fehlt.");
        }
    }

    private static BackupEncryptionHeader? ReadHeaderCore(Stream stream)
    {
        var raw = new List<byte>(64);
        var buffer = new byte[64];
        if (!ReadField(stream, raw, buffer, BackupFormat.EncryptedMagic.Length)
            || !raw.ToArray().AsSpan().StartsWith(BackupFormat.EncryptedMagic)
            || !ReadField(stream, raw, buffer, 2))
        {
            return null;
        }

        var version = BinaryPrimitives.ReadUInt16LittleEndian(raw.ToArray().AsSpan(8));
        if (version > BackupFormat.EncryptedContainerVersion)
        {
            // Die ersten zehn Bytes (Magic und Containerversion) sind über alle Versionen invariant (ADR 0019).
            return new BackupEncryptionHeader { ContainerVersion = version };
        }

        if (!ReadField(stream, raw, buffer, 2 + 4 + 1))
        {
            return null;
        }

        var saltLength = raw[^1];
        if (!ReadField(stream, raw, buffer, saltLength) || !ReadField(stream, raw, buffer, 1))
        {
            return null;
        }

        var nonceLength = raw[^1];
        if (!ReadField(stream, raw, buffer, nonceLength) || !ReadField(stream, raw, buffer, 4))
        {
            return null;
        }

        return BackupEncryptionHeader.TryParse(raw.ToArray(), out var header) ? header : null;
    }

    private static bool ReadField(Stream stream, List<byte> raw, byte[] buffer, int count)
    {
        var total = 0;
        while (total < count)
        {
            var read = stream.Read(buffer, 0, Math.Min(buffer.Length, count - total));
            if (read == 0)
            {
                return false;
            }

            raw.AddRange(buffer.AsSpan(0, read));
            total += read;
        }

        return true;
    }

    private static bool FillExactly(Stream stream, Span<byte> buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = stream.Read(buffer[total..]);
            if (read == 0)
            {
                return false;
            }

            total += read;
        }

        return true;
    }

    private static byte[] NewAad(BackupEncryptionHeader header)
    {
        // Zusätzlich authentifizierte Daten je Chunk: Header-Bytes, Chunkindex (8 Byte, Little-Endian) und Letztes-Chunk-Kennzeichen.
        var aad = new byte[header.Bytes.Length + 8 + 1];
        header.Bytes.CopyTo(aad, 0);
        return aad;
    }

    private static void WriteAad(byte[] aad, long index, bool isLast)
    {
        BinaryPrimitives.WriteInt64LittleEndian(aad.AsSpan(aad.Length - 9), index);
        aad[^1] = isLast ? (byte)1 : (byte)0;
    }

    private static void BuildNonce(byte[] nonceBase, long index, Span<byte> nonce)
    {
        // Nonce je Chunk: Nonce-Basis, deren letzte acht Bytes mit dem Chunkindex (Little-Endian) XOR-verknüpft werden.
        nonceBase.CopyTo(nonce);
        BinaryPrimitives.WriteUInt64LittleEndian(nonce[4..], BinaryPrimitives.ReadUInt64LittleEndian(nonce[4..]) ^ (ulong)index);
    }
}
