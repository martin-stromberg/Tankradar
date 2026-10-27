using Tankradar.MAUI.Models;
using Tankradar.MAUI.Services;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft die Erststart-Erkennung von <see cref="SetupStatusService"/> auf einer echten SQLite-Datei.
/// </summary>
public class SetupStatusServiceTests_FirstStart : IDisposable
{
    private readonly TestDatabase _database = new();

    /// <summary>
    /// Prüft, dass auf einer frischen Datenbank ohne Einstellungszeile die Einrichtung nötig ist.
    /// </summary>
    [Fact]
    public async Task IsSetupRequired_NoSettingsRow_ReturnsTrue()
    {
        var required = await CreateStatusService().IsSetupRequiredAsync();

        Assert.True(required);
    }

    /// <summary>
    /// Prüft, dass nach dem Speichern der Einstellungen keine Einrichtung mehr nötig ist.
    /// </summary>
    [Fact]
    public async Task IsSetupRequired_AfterSave_ReturnsFalse()
    {
        var factory = _database.CreateFactory();
        var initializer = _database.CreateInitializer(factory);
        var status = new SetupStatusService(factory, initializer);
        Assert.True(await status.IsSetupRequiredAsync());

        await new SettingsService(factory, initializer).SaveAsync(AppSettings.CreateDefault());

        Assert.False(await status.IsSetupRequiredAsync());
    }

    /// <summary>
    /// Prüft, dass eine neue Dienstinstanz (simulierter Neustart) nach dem Speichern weiterhin keine Einrichtung verlangt.
    /// </summary>
    [Fact]
    public async Task IsSetupRequired_AfterRestart_StaysFalse()
    {
        var factory = _database.CreateFactory();
        await new SettingsService(factory, _database.CreateInitializer(factory)).SaveAsync(AppSettings.CreateDefault());

        var required = await CreateStatusService().IsSetupRequiredAsync();

        Assert.False(required);
    }

    /// <summary>
    /// Räumt die Testumgebung auf.
    /// </summary>
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }

    private SetupStatusService CreateStatusService()
    {
        var factory = _database.CreateFactory();
        return new SetupStatusService(factory, _database.CreateInitializer(factory));
    }
}
