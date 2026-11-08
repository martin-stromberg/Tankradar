using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Navigation;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass der Karten-Öffner im Testmodus und ohne Adresse nie eine externe App startet.
/// </summary>
public class EssentialsMapOpenerTests : BaseTest
{
    /// <summary>
    /// Prüft, dass im Testmodus weder per Position noch per Adresse etwas geöffnet wird (kein Fehler, kein Start).
    /// </summary>
    [Fact]
    public async Task OpenAsync_TestMode_DoesNothing()
    {
        var opener = new EssentialsMapOpener(testMode: true);
        await opener.OpenAsync(new GeoPosition(52.52, 13.405), "Alpha Tankstelle");
        await opener.OpenAsync("Hauptstraße 1, 10115 Berlin", "Alpha Tankstelle");
    }

    /// <summary>
    /// Prüft, dass eine leere Adresse nie geöffnet wird (kein Fehler, kein Start).
    /// </summary>
    [Fact]
    public async Task OpenAsync_EmptyAddress_DoesNothing()
    {
        var opener = new EssentialsMapOpener(testMode: false);
        await opener.OpenAsync("   ", "Alpha Tankstelle");
    }

    /// <summary>
    /// Prüft, dass fehlende Argumente eine <see cref="ArgumentNullException"/> auslösen.
    /// </summary>
    [Fact]
    public async Task OpenAsync_NullArguments_Throw()
    {
        var opener = new EssentialsMapOpener(testMode: true);
        await Assert.ThrowsAsync<ArgumentNullException>(() => opener.OpenAsync((GeoPosition)null!, "Alpha"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => opener.OpenAsync(new GeoPosition(52.52, 13.405), null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => opener.OpenAsync((string)null!, "Alpha"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => opener.OpenAsync("Hauptstraße 1", null!));
    }
}
