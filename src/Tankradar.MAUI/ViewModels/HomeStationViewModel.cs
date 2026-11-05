using System.Windows.Input;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Eine Tankstellenkachel der Startseite (in einer Favoritengruppe oder im Bereich „In der Nähe“): Name, Entfernung, Preiszeilen mit Alter, Hinweise und Notiz.
/// Antippen öffnet die Detailansicht.
/// </summary>
public sealed class HomeStationViewModel : BaseViewModel
{
    /// <summary>
    /// Erstellt die Kachel.
    /// </summary>
    /// <param name="station">Die aufbereitete Tankstelle.</param>
    /// <param name="automationPrefix">Das Präfix der AutomationIds („Favorites.Station“ oder „Nearby.Station“).</param>
    /// <param name="open">Wird aufgerufen, wenn der Anwender die Detailansicht öffnet.</param>
    public HomeStationViewModel(HomeStation station, string automationPrefix, Action<HomeStationViewModel> open)
    {
        ArgumentNullException.ThrowIfNull(station);
        Station = station;
        AutomationPrefix = automationPrefix;
        PriceLines = station.Item.PriceLines.Select(line => new HomePriceLineViewModel(line, automationPrefix)).ToList();
        OpenCommand = new Command(() => open(this));
    }

    /// <summary>
    /// Die aufbereitete Tankstelle.
    /// </summary>
    public HomeStation Station { get; }

    /// <summary>
    /// Die Tankstellenzeile mit Preiszeilen, Hinweisen und Entfernung.
    /// </summary>
    public StationListItem Item => Station.Item;

    /// <summary>
    /// Das Präfix der AutomationIds.
    /// </summary>
    public string AutomationPrefix { get; }

    /// <summary>
    /// Der Name der Tankstelle.
    /// </summary>
    public string Name => Item.Name;

    /// <summary>
    /// Der klein darunter angezeigte Stammdatenname; leer, wenn er dem Namen entspricht.
    /// </summary>
    public string SubName => Item.SubName;

    /// <summary>
    /// Gibt an, ob der Stammdatenname angezeigt wird.
    /// </summary>
    public bool HasSubName => Item.HasSubName;

    /// <summary>
    /// Die Entfernung als Text; leer, wenn kein Standort vorliegt.
    /// </summary>
    public string DistanceText => Item.DistanceText;

    /// <summary>
    /// Gibt an, ob eine Entfernung angezeigt wird.
    /// </summary>
    public bool HasDistance => Item.HasDistance;

    /// <summary>
    /// Die Adresse in einer Zeile; leer, wenn keine bekannt ist.
    /// </summary>
    public string AddressText => Item.AddressText;

    /// <summary>
    /// Gibt an, ob eine Adresse angezeigt wird.
    /// </summary>
    public bool HasAddress => Item.HasAddress;

    /// <summary>
    /// Die Preiszeilen der gewählten Sorten in der Reihenfolge der Einstellungen.
    /// </summary>
    public IReadOnlyList<HomePriceLineViewModel> PriceLines { get; }

    /// <summary>
    /// Gibt an, ob mindestens ein Preis bekannt ist.
    /// </summary>
    public bool HasPrices => Station.HasPrices;

    /// <summary>
    /// Gibt an, ob der Hinweis „Keine Preise bekannt“ angezeigt wird.
    /// </summary>
    public bool ShowNoPrices => !Station.HasPrices;

    /// <summary>
    /// Der Hinweis „Keine Preise bekannt“.
    /// </summary>
    public string NoPricesText => FavoritesTexts.NoPricesKnown;

    /// <summary>
    /// Gibt an, ob der Hinweis „Preis unbestätigt“ gilt.
    /// </summary>
    public bool HasUnconfirmedPrice => Item.HasUnconfirmedPrice;

    /// <summary>
    /// Gibt an, ob der Hinweis „Automatentankstelle“ gilt.
    /// </summary>
    public bool IsAutomatedStation => Item.IsAutomatedStation;

    /// <summary>
    /// Gibt an, ob mindestens ein Hinweis angezeigt wird.
    /// </summary>
    public bool HasHints => Item.HasHints;

    /// <summary>
    /// Gibt an, ob mindestens eine Preiszeile veraltet ist (Alter von mindestens 60 Minuten).
    /// </summary>
    public bool HasStalePrice
    {
        get { return PriceLines.Any(line => line.IsStale); }
    }

    /// <summary>
    /// Die Notiz des Favoriten; leer, wenn keine hinterlegt ist.
    /// </summary>
    public string Note => Station.Note ?? string.Empty;

    /// <summary>
    /// Gibt an, ob eine Notiz angezeigt wird.
    /// </summary>
    public bool HasNote => Station.HasNote;

    /// <summary>
    /// Die Beschreibung der Kachel für Bedienhilfen (Name, Entfernung, Preise).
    /// </summary>
    public string SpokenDescription
    {
        get
        {
            var parts = new List<string> { Name };
            if (HasSubName)
            {
                parts.Add(SubName);
            }

            if (HasDistance)
            {
                parts.Add(DistanceText);
            }

            parts.AddRange(PriceLines.Select(line => line.SpokenText));
            return string.Join(", ", parts);
        }
    }

    /// <summary>
    /// Befehl, der die Detailansicht öffnet.
    /// </summary>
    public ICommand OpenCommand { get; }
}
