using Tankradar.MAUI.Models;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft <see cref="AppSettings.Normalize"/> und <see cref="AppSettings.Validate"/>.
/// </summary>
public class AppSettingsTests_Validation : BaseTest
{
    /// <summary>
    /// Prüft, dass fehlende Spritsorten unausgewählt ans Ende angehängt werden.
    /// </summary>
    [Fact]
    public void Normalize_AppendsMissingFuelTypesUnselected()
    {
        var settings = AppSettings.CreateDefault() with { FuelTypes = [new FuelTypeSelection(FuelType.Diesel, true)] };

        var normalized = settings.Normalize();

        Assert.Equal([FuelType.Diesel, FuelType.SuperE5, FuelType.SuperE10], normalized.FuelTypes.Select(f => f.FuelType));
        Assert.True(normalized.FuelTypes[0].IsSelected);
        Assert.False(normalized.FuelTypes[1].IsSelected);
        Assert.False(normalized.FuelTypes[2].IsSelected);
    }

    /// <summary>
    /// Prüft, dass die statische Normalisierung die vorhandene Reihenfolge und Auswahl beibehält und Fehlendes ergänzt.
    /// </summary>
    [Fact]
    public void NormalizeFuelTypes_KeepsOrderAndAppendsMissing()
    {
        var normalized = AppSettings.NormalizeFuelTypes([new FuelTypeSelection(FuelType.SuperE10, true)]);

        Assert.Equal([FuelType.SuperE10, FuelType.SuperE5, FuelType.Diesel], normalized.Select(f => f.FuelType));
        Assert.Equal([true, false, false], normalized.Select(f => f.IsSelected));
    }

    /// <summary>
    /// Prüft, dass eine vollständige Liste unverändert bleibt.
    /// </summary>
    [Fact]
    public void Normalize_CompleteList_StaysUnchanged()
    {
        var settings = AppSettings.CreateDefault();

        Assert.Equal(settings.FuelTypes, settings.Normalize().FuelTypes);
    }

    /// <summary>
    /// Prüft, dass doppelte Spritsorten abgelehnt werden.
    /// </summary>
    [Fact]
    public void Validate_DuplicateFuelType_Throws()
    {
        var settings = AppSettings.CreateDefault() with
        {
            FuelTypes = [new FuelTypeSelection(FuelType.Diesel, true), new FuelTypeSelection(FuelType.Diesel, false)],
        };

        Assert.Throws<ArgumentException>(settings.Validate);
    }

    /// <summary>
    /// Prüft, dass mindestens eine ausgewählte Spritsorte verlangt wird.
    /// </summary>
    [Fact]
    public void Validate_NoSelectedFuelType_Throws()
    {
        var settings = AppSettings.CreateDefault() with
        {
            FuelTypes = AppSettings.CreateDefault().FuelTypes.Select(f => f with { IsSelected = false }).ToList(),
        };

        Assert.Throws<ArgumentException>(settings.Validate);
    }

    /// <summary>
    /// Prüft, dass eine undefinierte Spritsorte abgelehnt wird.
    /// </summary>
    [Fact]
    public void Validate_UndefinedFuelType_Throws()
    {
        var settings = AppSettings.CreateDefault() with { FuelTypes = [new FuelTypeSelection((FuelType)99, true)] };

        Assert.Throws<ArgumentException>(settings.Validate);
    }

    /// <summary>
    /// Prüft, dass undefinierte Werte für Standort, Ansicht und Sortierung abgelehnt werden.
    /// </summary>
    [Fact]
    public void Validate_UndefinedEnumValues_Throw()
    {
        var defaults = AppSettings.CreateDefault();

        Assert.Throws<ArgumentException>((defaults with { GpsUsage = (GpsUsage)99 }).Validate);
        Assert.Throws<ArgumentException>((defaults with { ResultView = (ResultView)99 }).Validate);
        Assert.Throws<ArgumentException>((defaults with { ResultSortOrder = (ResultSortOrder)99 }).Validate);
    }
}
