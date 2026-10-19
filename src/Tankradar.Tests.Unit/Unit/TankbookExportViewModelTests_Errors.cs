using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Validierung und Fehlerfälle im <see cref="MAUI.ViewModels.TankbookExportViewModel"/>: ungültiger Zeitraum, leeres Ergebnis, Fehler beim Erstellen und Teilen, Abbruch.
/// </summary>
public class TankbookExportViewModelTests_Errors : TankbookExportViewModelTestBase
{
    private async Task SelectCustomAsync(DateTime from, DateTime to)
    {
        await PrepareVehicleAsync();
        ViewModel.RangeOptions.Single(option => option.Value == TankbookExportRangeKind.Custom).SelectCommand.Execute(null);
        ViewModel.FromDate = from;
        ViewModel.ToDate = to;
    }

    /// <summary>
    /// Prüft, dass ein Startdatum nach dem Enddatum den Fehler am Feld zeigt, ansagt und nichts exportiert.
    /// </summary>
    [Fact]
    public async Task Export_StartAfterEnd_ShowsRangeErrorAndDoesNotExport()
    {
        await SelectCustomAsync(Fixture.Now.Date, Fixture.Now.Date.AddDays(-3));

        await ExportAsync();

        Assert.Equal("Das Startdatum liegt nach dem Enddatum.", ViewModel.RangeError);
        Assert.True(ViewModel.HasRangeError);
        Assert.Contains("Das Startdatum liegt nach dem Enddatum.", Announcer.Announcements);
        Assert.Empty(Sharer.Shared);
        Assert.False(ViewModel.IsBusy);
    }

    /// <summary>
    /// Prüft, dass ein Enddatum in der Zukunft den Fehler am Feld zeigt.
    /// </summary>
    [Fact]
    public async Task Export_EndInFuture_ShowsRangeError()
    {
        await SelectCustomAsync(Fixture.Now.Date.AddDays(-3), Fixture.Now.Date.AddDays(2));

        await ExportAsync();

        Assert.Equal("Das Enddatum liegt in der Zukunft.", ViewModel.RangeError);
        Assert.Empty(Sharer.Shared);
    }

    /// <summary>
    /// Prüft, dass eine Änderung am Datum den Fehler entfernt.
    /// </summary>
    [Fact]
    public async Task RangeError_IsClearedWhenDateChanges()
    {
        await SelectCustomAsync(Fixture.Now.Date, Fixture.Now.Date.AddDays(-3));
        await ExportAsync();

        ViewModel.FromDate = Fixture.Now.Date.AddDays(-10);

        Assert.Null(ViewModel.RangeError);
    }

    /// <summary>
    /// Prüft die Meldung für ein Fahrzeug ohne Vorgänge: keine Datei, keine Weitergabe, Hinweis ohne Fehlerdarstellung.
    /// </summary>
    [Fact]
    public async Task Export_VehicleWithoutEntries_ShowsNoEntriesAtAllMessage()
    {
        var vehicle = await Fixture.CreateVehicleAsync("Leer");
        ViewModel.SetVehicle(vehicle);

        await ExportAsync();

        Assert.Equal("Für dieses Fahrzeug gibt es noch keine Vorgänge zum Exportieren.", ViewModel.StatusMessage);
        Assert.False(ViewModel.StatusIsError);
        Assert.Contains("Für dieses Fahrzeug gibt es noch keine Vorgänge zum Exportieren.", Announcer.Announcements);
        Assert.Empty(Sharer.Shared);
    }

    /// <summary>
    /// Prüft die Meldung für einen Zeitraum ohne Vorgänge.
    /// </summary>
    [Fact]
    public async Task Export_RangeWithoutEntries_ShowsNoEntriesInRangeMessage()
    {
        await SelectCustomAsync(new DateTime(2000, 1, 1), new DateTime(2000, 1, 31));

        await ExportAsync();

        Assert.Equal("Im gewählten Zeitraum gibt es keine Vorgänge.", ViewModel.StatusMessage);
        Assert.Empty(Sharer.Shared);
    }

    /// <summary>
    /// Prüft die Meldung, wenn das Fahrzeug nicht mehr existiert.
    /// </summary>
    [Fact]
    public async Task Export_VehicleGone_ShowsVehicleNotFound()
    {
        ViewModel.SetVehicle(new Vehicle(4711, "Weg", VehicleFuelKind.Petrol, 0, null));

        await ExportAsync();

        Assert.Equal("Das Fahrzeug wurde nicht gefunden.", ViewModel.StatusMessage);
        Assert.Empty(Sharer.Shared);
    }

    /// <summary>
    /// Prüft, dass ein Fehler beim Teilen gemeldet, nur mit dem Ausnahmetyp protokolliert und die Datei trotzdem gelöscht wird.
    /// </summary>
    [Fact]
    public async Task Export_ShareFails_ShowsErrorLogsTypeAndDiscardsFile()
    {
        await PrepareVehicleAsync();
        Sharer.Exception = new InvalidOperationException("Geheimer Pfad");

        await ExportAsync();

        Assert.Equal("Der Export konnte nicht weitergegeben werden.", ViewModel.StatusMessage);
        Assert.True(ViewModel.StatusIsError);
        Assert.Contains("Der Export konnte nicht weitergegeben werden.", Announcer.Announcements);
        Assert.Contains("InvalidOperationException", Logger.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain("Geheim", Logger.AllText, StringComparison.Ordinal);
        Assert.Empty(Directory.GetFiles(Fixture.Temp.Combine("cache", "exports")));
        Assert.False(ViewModel.IsBusy);
    }

    /// <summary>
    /// Prüft, dass der Abbruch des Teilen-Dialogs kein Fehler ist und die Datei gelöscht wird.
    /// </summary>
    [Fact]
    public async Task Export_ShareCancelled_IsNoError()
    {
        await PrepareVehicleAsync();
        Sharer.Exception = new OperationCanceledException();

        await ExportAsync();

        Assert.Null(ViewModel.StatusMessage);
        Assert.False(ViewModel.StatusIsError);
        Assert.Empty(Logger.Messages);
        Assert.Empty(Directory.GetFiles(Fixture.Temp.Combine("cache", "exports")));
    }

    /// <summary>
    /// Prüft, dass ein Fehler beim Erstellen der Datei gemeldet wird und nichts geteilt wird.
    /// </summary>
    [Fact]
    public async Task Export_CreateFails_ShowsCreateFailed()
    {
        await PrepareVehicleAsync();
        Directory.CreateDirectory(Fixture.Temp.Combine("cache"));
        await File.WriteAllTextAsync(Fixture.Temp.Combine("cache", "exports"), "blockiert");

        await ExportAsync();

        Assert.Equal("Der Export konnte nicht erstellt werden.", ViewModel.StatusMessage);
        Assert.True(ViewModel.StatusIsError);
        Assert.Empty(Sharer.Shared);
    }

    /// <summary>
    /// Prüft, dass eine alte Rückmeldung beim erneuten Übergeben des Fahrzeugs (z. B. nach einer Wiederherstellung) entfernt wird.
    /// </summary>
    [Fact]
    public async Task SetVehicle_ClearsOldStatus()
    {
        var vehicle = await PrepareVehicleAsync();
        await ExportAsync();
        Assert.NotNull(ViewModel.StatusMessage);

        ViewModel.SetVehicle(vehicle);

        Assert.Null(ViewModel.StatusMessage);
        Assert.False(ViewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass <c>Reset</c> den Bereich leert.
    /// </summary>
    [Fact]
    public async Task Reset_ClearsVehicleAndStatus()
    {
        await PrepareVehicleAsync();
        await ExportAsync();

        ViewModel.Reset();
        await ExportAsync();

        Assert.Null(ViewModel.StatusMessage);
        Assert.Equal(string.Empty, ViewModel.StartHint);
        Assert.Single(Sharer.Shared);
    }
}
