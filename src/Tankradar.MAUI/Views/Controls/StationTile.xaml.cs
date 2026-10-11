using Tankradar.MAUI.ViewModels;

namespace Tankradar.MAUI.Views.Controls;

/// <summary>
/// Wiederverwendbare Tankstellenkachel der Startseite (Favoritengruppen und „In der Nähe“): Name, Entfernung, Preiszeilen mit Alter, Hinweise und Schaltfläche „Details öffnen“.
/// Die AutomationIds der Elemente leiten sich vom Präfix des <see cref="HomeStationViewModel"/> ab („Favorites.Station“ bzw. „Nearby.Station“).
/// </summary>
public partial class StationTile : ContentView
{
    private bool _idsApplied;

    /// <summary>
    /// Erstellt die Kachel; der Bindungskontext ist das <see cref="HomeStationViewModel"/> der Kachel.
    /// </summary>
    public StationTile()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        // Eine AutomationId lässt sich nur einmal setzen; der Bindungskontext kann mehrfach wechseln.
        if (BindingContext is not HomeStationViewModel station || _idsApplied)
        {
            return;
        }

        _idsApplied = true;
        var prefix = station.AutomationPrefix;
        NameLabel.AutomationId = $"{prefix}.Name";
        AddressLabel.AutomationId = $"{prefix}.Address";
        DistanceLabel.AutomationId = $"{prefix}.Distance";
        NoPricesLabel.AutomationId = $"{prefix}.NoPrices";
        UnconfirmedLabel.AutomationId = $"{prefix}.Hint.Unconfirmed";
        AutomatedLabel.AutomationId = $"{prefix}.Hint.Automated";
        NoteLabel.AutomationId = $"{prefix}.Note";
        OpenButton.AutomationId = $"{prefix}.Open";
    }
}
