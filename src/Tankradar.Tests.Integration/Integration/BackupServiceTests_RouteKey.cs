using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Models.Backup;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Routing;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass Schlüssel und Schalter der Routensuche nicht in der lokalen Sicherung stehen und eine Wiederherstellung sie nicht verändert:
/// Auf einem Gerät ohne Schlüssel ist die Routensuche nach der Wiederherstellung aus.
/// </summary>
[Collection(BackupCollection.Name)]
public class BackupServiceTests_RouteKey : IDisposable
{
    private const string Key = "ZZPLATZHALTER-SICHERUNG-ZZ";
    private static readonly byte[] Photo = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    private readonly BackupTestEnvironment _environment = new();

    private static RouteSearchAccess CreateAccess()
    {
        return new RouteSearchAccess(new InMemoryApiKeyStore(), new InMemoryApiKeyStore(), NullLogger<RouteSearchAccess>.Instance);
    }

    /// <summary>
    /// Prüft, dass kein Archivinhalt (auch nicht die entpackte Datenbank) den Schlüssel enthält.
    /// </summary>
    [Fact]
    public async Task Create_DoesNotContainRouteKey()
    {
        var access = CreateAccess();
        await access.SaveKeyAsync(Key);
        await access.SetEnabledAsync(true);
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();

        var outcome = await service.CreateAsync();

        Assert.Equal(BackupResult.Ok, outcome.Result);
        using var archive = ZipFile.OpenRead(service.GetFullPath(outcome.File!.FileName)!);
        var needle = Encoding.UTF8.GetBytes(Key);
        foreach (var entry in archive.Entries)
        {
            using var stream = entry.Open();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            Assert.True(memory.ToArray().AsSpan().IndexOf(needle) < 0, $"Der Schlüssel steht in {entry.FullName}.");
        }

        Assert.DoesNotContain(archive.Entries, entry => entry.FullName.Contains("key", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Prüft, dass eine Wiederherstellung Schlüssel und Schalter nicht berührt und ein Gerät ohne Schlüssel danach die Routensuche nicht aktiviert hat.
    /// </summary>
    [Fact]
    public async Task Restore_LeavesRouteSearchStateUntouched_AndNewDeviceHasItOff()
    {
        var access = CreateAccess();
        await access.SaveKeyAsync(Key);
        await access.SetEnabledAsync(true);
        await _environment.SeedAsync(Photo);
        var service = _environment.CreateService();
        var outcome = await service.CreateAsync();

        var restore = await service.RestoreAsync(outcome.File!.FileName);

        Assert.Equal(BackupResult.Ok, restore);
        Assert.True(await access.IsEnabledAsync());
        Assert.Equal(Key, await access.GetStoredKeyAsync());
        var otherDevice = CreateAccess();
        Assert.False(await otherDevice.HasKeyAsync());
        Assert.False(await otherDevice.IsEnabledAsync());
        Assert.Null(await otherDevice.GetApiKeyAsync());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _environment.Dispose();
    }
}
