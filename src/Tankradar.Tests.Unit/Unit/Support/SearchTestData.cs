using Tankradar.MAUI.Models;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Gemeinsame Testdaten der Suche: fester Zeitpunkt und Einstellungen der Spritsorten.
/// </summary>
public static class SearchTestData
{
    /// <summary>
    /// Der Zeitpunkt „jetzt“ der Aufbereitungstests.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public static readonly DateTime Now = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Alle drei Sorten ausgewählt in der Reihenfolge E5, E10, Diesel.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public static IReadOnlyList<FuelTypeSelection> AllFuels { get; } =
    [
        new(FuelType.SuperE5, true),
        new(FuelType.SuperE10, true),
        new(FuelType.Diesel, true),
    ];

    /// <summary>
    /// Erzeugt eine Spritsortenliste, in der nur die angegebenen Sorten in dieser Reihenfolge ausgewählt sind (die übrigen folgen abgewählt).
    /// </summary>
    /// <param name="selected">Die ausgewählten Sorten in Reihenfolge.</param>
    /// <returns>Die Liste.</returns>
    public static IReadOnlyList<FuelTypeSelection> Only(params FuelType[] selected)
    {
        var rest = Enum.GetValues<FuelType>().Where(fuel => !selected.Contains(fuel)).Select(fuel => new FuelTypeSelection(fuel, false));
        return selected.Select(fuel => new FuelTypeSelection(fuel, true)).Concat(rest).ToList();
    }
}
