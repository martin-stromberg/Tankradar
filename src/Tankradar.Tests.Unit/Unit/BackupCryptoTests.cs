using System.Security.Cryptography;
using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Backup;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft <see cref="BackupCrypto"/> und <see cref="BackupEncryptionHeader"/>: Containerformat, Schlüsselableitung, Integrität und Fehlerfälle.
/// </summary>
public class BackupCryptoTests : BaseTest
{
    private const string Password = "geheim-123";
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "backup-crypto-" + Guid.NewGuid().ToString("N"));

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            TempDirectoryCleanup.DeleteBestEffort(_directory);
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Prüft, dass verschlüsselte Inhalte mehrerer Chunks bitgenau wieder hergestellt werden.
    /// </summary>
    [Fact]
    public async Task RoundTrip_RestoresContent()
    {
        var random = new Random(7);
        foreach (var size in new[] { 0, 5, BackupFormat.EncryptedChunkSize, BackupFormat.EncryptedChunkSize + 1234, 2 * BackupFormat.EncryptedChunkSize + 7 })
        {
            var source = Path.Combine(_directory, $"plain-{size}.bin");
            var container = source + ".enc";
            var target = source + ".out";
            Directory.CreateDirectory(_directory);
            var content = new byte[size];
            random.NextBytes(content);
            await File.WriteAllBytesAsync(source, content);

            await BackupCrypto.EncryptAsync(source, container, Password);
            await BackupCrypto.DecryptAsync(container, target, Password);

            Assert.Equal(content, await File.ReadAllBytesAsync(target));
        }
    }

    /// <summary>
    /// Prüft, dass die Magic-Bytes den Container von einer ZIP-Datei unterscheiden.
    /// </summary>
    [Fact]
    public async Task IsEncrypted_DetectsMagic()
    {
        var (container, _, _) = await CreateContainerAsync();
        using var encrypted = File.OpenRead(container);
        Assert.True(BackupCrypto.IsEncrypted(encrypted));
        Assert.True(File.ReadAllBytes(container).AsSpan().StartsWith(BackupFormat.EncryptedMagic));
        Assert.False(File.ReadAllBytes(container).AsSpan().StartsWith("PK"u8));

        var plain = Path.Combine(_directory, "plain.zip");
        await File.WriteAllBytesAsync(plain, "PK\x03\x04rest"u8.ToArray());
        using var zip = File.OpenRead(plain);
        Assert.False(BackupCrypto.IsEncrypted(zip));
    }

    /// <summary>
    /// Prüft, dass der gelesene Header alle KDF- und Chunk-Parameter trägt und die Position des Stroms erhält.
    /// </summary>
    [Fact]
    public async Task ReadHeader_ReadsParameters()
    {
        var (container, _, _) = await CreateContainerAsync();
        using var stream = File.OpenRead(container);

        var header = BackupCrypto.ReadHeader(stream);

        Assert.NotNull(header);
        Assert.Equal(BackupFormat.EncryptedContainerVersion, header.ContainerVersion);
        Assert.Equal(BackupFormat.EncryptedKdfPbkdf2Sha256, header.KdfId);
        Assert.Equal(BackupFormat.Pbkdf2Iterations, header.Iterations);
        Assert.Equal(BackupFormat.EncryptedSaltBytes, header.Salt.Length);
        Assert.Equal(BackupFormat.EncryptedNonceBytes, header.NonceBase.Length);
        Assert.Equal(BackupFormat.EncryptedChunkSize, header.ChunkSize);
        Assert.True(header.Bytes.Length <= BackupLimits.MaxEncryptedHeaderBytes);
        Assert.Equal(0, stream.Position);
    }

    /// <summary>
    /// Prüft, dass eine höhere Containerversion nur die Version liefert und ein defekter Header <see langword="null"/>.
    /// </summary>
    [Fact]
    public async Task ReadHeader_TooNewAndBroken()
    {
        var (container, _, _) = await CreateContainerAsync();
        BackupEncryptedTestArchive.WriteVersion(container, 99);
        using (var stream = File.OpenRead(container))
        {
            var header = BackupCrypto.ReadHeader(stream);
            Assert.NotNull(header);
            Assert.Equal(99, header.ContainerVersion);
        }

        var broken = Path.Combine(_directory, "broken.enc");
        await File.WriteAllBytesAsync(broken, [.. BackupFormat.EncryptedMagic.ToArray(), 1, 0]);
        using (var stream = File.OpenRead(broken))
        {
            Assert.Null(BackupCrypto.ReadHeader(stream));
        }

        var plain = Path.Combine(_directory, "plain.bin");
        await File.WriteAllBytesAsync(plain, "PK\x03\x04"u8.ToArray());
        using (var stream = File.OpenRead(plain))
        {
            Assert.Null(BackupCrypto.ReadHeader(stream));
        }
    }

    /// <summary>
    /// Prüft, dass ein falsches Passwort als Authentifikationsfehler sichtbar wird.
    /// </summary>
    [Fact]
    public async Task Decrypt_WrongPassword_Throws()
    {
        var (container, _, _) = await CreateContainerAsync();

        await Assert.ThrowsAnyAsync<CryptographicException>(() => BackupCrypto.DecryptAsync(container, Path.Combine(_directory, "out.bin"), "falsch"));
    }

    /// <summary>
    /// Prüft, dass ein manipuliertes Ende und eine Verkürzung des Chiffretexts als Authentifikationsfehler sichtbar werden.
    /// </summary>
    [Fact]
    public async Task Decrypt_ManipulatedOrTruncated_Throws()
    {
        var (tampered, _, _) = await CreateContainerAsync();
        BackupEncryptedTestArchive.FlipLastByte(tampered);
        await Assert.ThrowsAnyAsync<CryptographicException>(() => BackupCrypto.DecryptAsync(tampered, Path.Combine(_directory, "out1.bin"), Password));

        var (truncated, _, _) = await CreateContainerAsync();
        BackupEncryptedTestArchive.Truncate(truncated, new FileInfo(truncated).Length - 3);
        await Assert.ThrowsAnyAsync<CryptographicException>(() => BackupCrypto.DecryptAsync(truncated, Path.Combine(_directory, "out2.bin"), Password));
    }

    /// <summary>
    /// Prüft, dass eine Datei ohne Container-Header abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task Decrypt_NonContainer_ThrowsInvalidData()
    {
        Directory.CreateDirectory(_directory);
        var plain = Path.Combine(_directory, "plain.bin");
        await File.WriteAllBytesAsync(plain, "PK\x03\x04inhalt"u8.ToArray());

        await Assert.ThrowsAsync<InvalidDataException>(() => BackupCrypto.DecryptAsync(plain, Path.Combine(_directory, "out.bin"), Password));
    }

    /// <summary>
    /// Prüft an einem unveränderlich eingebetteten Referenzcontainer, dass die Containerversion 1 bytegenau lesbar bleibt (ADR 0019).
    /// Der Vektor wurde unabhängig vom Produktivcode erzeugt: Magic <c>TKBENC01</c>, Version 1, PBKDF2-HMAC-SHA256 mit 600.000 Iterationen,
    /// Salt <c>1122…0F</c>, Nonce-Basis <c>01…0C</c>, Chunk-Größe 1 MiB, ein Chunk mit AAD aus Header ‖ Index ‖ Endkennzeichen.
    /// </summary>
    [Fact]
    public async Task Decrypt_FixedReferenceContainer_RestoresPlaintext()
    {
        const string containerBase64 = "VEtCRU5DMDEBAAEAwCcJABARIjNEVWZ3iJmqu8zd7vAPDAECAwQFBgcICQoLDAAAEADzrCYE/rbd6Hg0JpzLXZrfo9zzvr7Wm8BCvpZ5xCL8rLyRE7dvi2KlMUx72rxXLAdeOkZ9PugfggY66R/bRl3L";
        var container = Path.Combine(_directory, "reference.enc");
        var target = Path.Combine(_directory, "reference.out");
        Directory.CreateDirectory(_directory);
        await File.WriteAllBytesAsync(container, Convert.FromBase64String(containerBase64));

        using (var stream = File.OpenRead(container))
        {
            Assert.True(BackupCrypto.IsEncrypted(stream));
            var header = BackupCrypto.ReadHeader(stream);
            Assert.NotNull(header);
            Assert.Equal(BackupFormat.EncryptedContainerVersion, header.ContainerVersion);
            Assert.Equal(BackupFormat.EncryptedKdfPbkdf2Sha256, header.KdfId);
            Assert.Equal(BackupFormat.Pbkdf2Iterations, header.Iterations);
            Assert.Equal(50, header.Bytes.Length);
        }

        await BackupCrypto.DecryptAsync(container, target, "testvektor-geheim");

        Assert.Equal("Tankatlas-Referenzcontainer, Containerversion 1.", await File.ReadAllTextAsync(target));
    }

    /// <summary>
    /// Prüft, dass das Vertauschen zweier Chunks an der Indexbindung der zusätzlich authentifizierten Daten scheitert.
    /// </summary>
    [Fact]
    public async Task Decrypt_SwappedChunks_Throws()
    {
        var (container, _, _) = await CreateContainerAsync(BackupFormat.EncryptedChunkSize + 100);
        BackupEncryptedTestArchive.SwapFirstTwoChunks(container);

        await Assert.ThrowsAnyAsync<CryptographicException>(() => BackupCrypto.DecryptAsync(container, Path.Combine(_directory, "out.bin"), Password));
    }

    /// <summary>
    /// Prüft, dass eine Header-Feldänderung in zulässigen Grenzen (Iterationszahl) den Header lesbar lässt, die Entschlüsselung aber an der Headerbindung scheitert.
    /// </summary>
    [Fact]
    public async Task Decrypt_ModifiedIterations_Throws()
    {
        var (container, _, _) = await CreateContainerAsync();
        BackupEncryptedTestArchive.WriteIterations(container, BackupFormat.Pbkdf2Iterations + 1);

        using (var stream = File.OpenRead(container))
        {
            var header = BackupCrypto.ReadHeader(stream);
            Assert.NotNull(header);
            Assert.Equal(BackupFormat.Pbkdf2Iterations + 1, header.Iterations);
        }

        await Assert.ThrowsAnyAsync<CryptographicException>(() => BackupCrypto.DecryptAsync(container, Path.Combine(_directory, "out.bin"), Password));
    }

    private async Task<(string Container, string Source, byte[] Content)> CreateContainerAsync(int size = 4096)
    {
        Directory.CreateDirectory(_directory);
        var source = Path.Combine(_directory, Guid.NewGuid().ToString("N") + ".bin");
        var container = source + ".enc";
        var content = new byte[size];
        new Random(size).NextBytes(content);
        await File.WriteAllBytesAsync(source, content);
        await BackupEncryptedTestArchive.CreateAsync(source, container, Password);
        return (container, source, content);
    }
}
