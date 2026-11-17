using Tankradar.MAUI.Services.Diagnostics;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Redaktion von Ortsdaten und Dateipfaden.
/// </summary>
public class DiagnosticsRedactorTests_Locations : BaseTest
{
    /// <summary>
    /// Prüft, dass Koordinatenparameter wie <c>lat=…</c> redigiert werden, der Parametername aber erhalten bleibt.
    /// </summary>
    /// <param name="input">Der zu prüfende Koordinatenausdruck.</param>
    [Theory]
    [InlineData("lat=48,1371")]
    [InlineData("latitude:48.137")]
    [InlineData("longitude=11,5755")]
    [InlineData("lon:9.8")]
    public void Redact_CoordinateParameter_Removed(string input)
    {
        var result = DiagnosticsRedactor.Redact("Standort " + input + " übergeben");

        Assert.Contains(DiagnosticsRedactor.Placeholder, result);
        Assert.DoesNotContain(input.Split('=', ':')[1], result);
    }

    /// <summary>
    /// Prüft, dass freistehende Koordinatenpaare entfernt werden.
    /// </summary>
    [Fact]
    public void Redact_CoordinatePair_Removed()
    {
        var result = DiagnosticsRedactor.Redact("Route von 48.1371, 11.5755 ermittelt");

        Assert.DoesNotContain("48.1371", result);
        Assert.DoesNotContain("11.5755", result);
    }

    /// <summary>
    /// Prüft, dass Windows-Pfade entfernt werden.
    /// </summary>
    [Fact]
    public void Redact_WindowsPath_Removed()
    {
        var result = DiagnosticsRedactor.Redact(@"Fehler beim Lesen von C:\Users\Martin\AppData\file.db");

        Assert.DoesNotContain(@"C:\Users\Martin", result);
        Assert.Contains(DiagnosticsRedactor.Placeholder, result);
    }

    /// <summary>
    /// Prüft, dass Unix-Benutzerpfade entfernt werden.
    /// </summary>
    [Fact]
    public void Redact_UnixPath_Removed()
    {
        var result = DiagnosticsRedactor.Redact("Fehler beim Lesen von /home/martin/tankatlas/file.db");

        Assert.DoesNotContain("/home/martin", result);
        Assert.Contains(DiagnosticsRedactor.Placeholder, result);
    }

    /// <summary>
    /// Prüft, dass Stationsnamen und Suchbegriffe in Protokollzeilen nicht als solche übertragen werden:
    /// Sie werden über die Pfad- und Wertmuster erfasst; ein freistehender Name verbleibt nur,
    /// wenn er unverfänglich ist — der Paketdienst redigiert deshalb zusätzlich wörtliche Werte.
    /// </summary>
    [Fact]
    public void Redact_StationValue_Removed_WhenLiteral()
    {
        const string station = "Supermarkt-Tankstelle-Musterstadt";

        var result = DiagnosticsRedactor.Redact($"Abruf von {station} fehlgeschlagen", [station]);

        Assert.DoesNotContain(station, result);
    }
}
