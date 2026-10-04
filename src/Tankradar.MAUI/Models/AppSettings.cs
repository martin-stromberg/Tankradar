namespace Tankradar.MAUI.Models;

/// <summary>
/// Die Einstellungen der App: Spritsorten (geordnet), Standortnutzung, Standardansicht und Standardsortierung.
/// </summary>
public sealed record AppSettings
{
    /// <summary>
    /// Erstellt die Einstellungen.
    /// </summary>
    /// <param name="fuelTypes">Die Spritsorten in der Reihenfolge ihrer Priorität.</param>
    /// <param name="gpsUsage">Die Standortnutzung.</param>
    /// <param name="resultView">Die Standardansicht der Suchergebnisse.</param>
    /// <param name="resultSortOrder">Die Standardsortierung der Suchergebnisse.</param>
    public AppSettings(IReadOnlyList<FuelTypeSelection> fuelTypes, GpsUsage gpsUsage, ResultView resultView, ResultSortOrder resultSortOrder)
    {
        FuelTypes = fuelTypes;
        GpsUsage = gpsUsage;
        ResultView = resultView;
        ResultSortOrder = resultSortOrder;
    }

    /// <summary>
    /// Die Spritsorten in der Reihenfolge ihrer Priorität.
    /// </summary>
    public IReadOnlyList<FuelTypeSelection> FuelTypes { get; init; }

    /// <summary>
    /// Die Standortnutzung.
    /// </summary>
    public GpsUsage GpsUsage { get; init; }

    /// <summary>
    /// Die Standardansicht der Suchergebnisse.
    /// </summary>
    public ResultView ResultView { get; init; }

    /// <summary>
    /// Die Standardsortierung der Suchergebnisse.
    /// </summary>
    public ResultSortOrder ResultSortOrder { get; init; }

    /// <summary>
    /// Erzeugt die Standardeinstellungen: Standort nur bei Nutzung, Listenansicht, Sortierung nach Preis, alle Spritsorten ausgewählt.
    /// </summary>
    /// <returns>Die Standardeinstellungen.</returns>
    public static AppSettings CreateDefault()
    {
        return new AppSettings(
            Enum.GetValues<FuelType>().Select(fuelType => new FuelTypeSelection(fuelType, true)).ToList(),
            GpsUsage.WhileInUse,
            ResultView.List,
            ResultSortOrder.Price);
    }

    /// <summary>
    /// Ergänzt fehlende Spritsorten unausgewählt am Ende der Liste.
    /// </summary>
    /// <returns>Die Einstellungen mit vollständiger Spritsortenliste.</returns>
    public AppSettings Normalize()
    {
        return this with { FuelTypes = NormalizeFuelTypes(FuelTypes) };
    }

    /// <summary>
    /// Ergänzt in einer Spritsortenliste fehlende Sorten unausgewählt am Ende; die vorhandene Reihenfolge bleibt erhalten.
    /// </summary>
    /// <param name="fuelTypes">Die vorhandenen Spritsorten.</param>
    /// <returns>Die vollständige Spritsortenliste.</returns>
    public static IReadOnlyList<FuelTypeSelection> NormalizeFuelTypes(IEnumerable<FuelTypeSelection> fuelTypes)
    {
        var existing = fuelTypes.ToList();
        var present = existing.Select(selection => selection.FuelType).ToHashSet();
        var missing = Enum.GetValues<FuelType>()
            .Where(fuelType => !present.Contains(fuelType))
            .Select(fuelType => new FuelTypeSelection(fuelType, false));

        return existing.Concat(missing).ToList();
    }

    /// <summary>
    /// Prüft die Einstellungen und löst bei Verstößen eine <see cref="ArgumentException"/> aus.
    /// </summary>
    /// <exception cref="ArgumentException">Eine Spritsorte kommt mehrfach vor, keine Spritsorte ist ausgewählt oder ein Enum-Wert ist undefiniert.</exception>
    public void Validate()
    {
        if (FuelTypes.Any(selection => !Enum.IsDefined(selection.FuelType)))
        {
            throw new ArgumentException("Die Spritsorten enthalten einen undefinierten Wert.", nameof(FuelTypes));
        }

        if (FuelTypes.GroupBy(selection => selection.FuelType).Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Jede Spritsorte darf höchstens einmal vorkommen.", nameof(FuelTypes));
        }

        if (!FuelTypes.Any(selection => selection.IsSelected))
        {
            throw new ArgumentException("Mindestens eine Spritsorte muss ausgewählt sein.", nameof(FuelTypes));
        }

        if (!Enum.IsDefined(GpsUsage))
        {
            throw new ArgumentException("Die Standortnutzung ist undefiniert.", nameof(GpsUsage));
        }

        if (!Enum.IsDefined(ResultView))
        {
            throw new ArgumentException("Die Ergebnisansicht ist undefiniert.", nameof(ResultView));
        }

        if (!Enum.IsDefined(ResultSortOrder))
        {
            throw new ArgumentException("Die Ergebnissortierung ist undefiniert.", nameof(ResultSortOrder));
        }
    }
}
