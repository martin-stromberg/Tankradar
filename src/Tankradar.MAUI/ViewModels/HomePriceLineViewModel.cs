using Tankradar.MAUI.Models.Search;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Eine Preiszeile einer Tankstellenkachel mit den AutomationIds der Startseite.
/// </summary>
public sealed class HomePriceLineViewModel
{
    private readonly StationPriceLine _line;

    /// <summary>
    /// Erstellt die Zeile.
    /// </summary>
    /// <param name="line">Die aufbereitete Preiszeile.</param>
    /// <param name="automationPrefix">Das Präfix der AutomationIds.</param>
    public HomePriceLineViewModel(StationPriceLine line, string automationPrefix)
    {
        _line = line;
        FuelId = $"{automationPrefix}.Fuel.{line.FuelType}";
        PriceId = $"{automationPrefix}.Price.{line.FuelType}";
        AgeId = $"{automationPrefix}.Age.{line.FuelType}";
    }

    /// <summary>
    /// Der Anzeigename der Sorte.
    /// </summary>
    public string FuelLabel => _line.FuelLabel;

    /// <summary>
    /// Der Preis mit Eurozeichen („1,859 €“).
    /// </summary>
    public string PriceText => _line.PriceText;

    /// <summary>
    /// Der Preis ohne die dritte Nachkommastelle („1,85“).
    /// </summary>
    public string PriceMainText => _line.PriceMainText;

    /// <summary>
    /// Die dritte Nachkommastelle („9“), hochgestellt dargestellt.
    /// </summary>
    public string PriceFractionText => _line.PriceFractionText;

    /// <summary>
    /// Die Einheit des Preises („€/L“).
    /// </summary>
    public string PriceUnitText => _line.PriceUnitText;

    /// <summary>
    /// Das Alter des Preises („vor 5 Min.“).
    /// </summary>
    public string AgeText => _line.AgeText;

    /// <summary>
    /// Gibt an, ob der Preis veraltet ist (mindestens 60 Minuten).
    /// </summary>
    public bool IsStale => _line.IsStale;

    /// <summary>
    /// Gibt an, ob der Preis aktuell ist.
    /// </summary>
    public bool IsFresh => !_line.IsStale;

    /// <summary>
    /// Der Preis mit Sorte und Alter für Bedienhilfen.
    /// </summary>
    public string SpokenText => $"{_line.FuelLabel} {_line.PriceSpokenText}, {_line.AgeText}";

    /// <summary>
    /// Die AutomationId der Sortenbezeichnung.
    /// </summary>
    public string FuelId { get; }

    /// <summary>
    /// Die AutomationId des Preises.
    /// </summary>
    public string PriceId { get; }

    /// <summary>
    /// Die AutomationId der Altersangabe.
    /// </summary>
    public string AgeId { get; }
}
