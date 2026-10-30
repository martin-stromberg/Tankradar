using System.Windows.Input;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Trefferliste am unteren Rand (Auf- und Zuklappen, Kopfzeile mit Trefferanzahl) und Suchdialog über der Karte (Öffnen und Schließen).
/// </summary>
public partial class MapViewModel
{
    private bool _isBottomSheetExpanded = true;
    private bool _isSearchOverlayOpen;

    /// <summary>
    /// Gibt an, ob die Trefferliste am unteren Rand ausgeklappt ist (Liste mit Filter und Sortierung) oder nur die Kopfzeile mit der Trefferanzahl zeigt.
    /// Der Startzustand folgt der Standardansicht der Einstellungen („Liste“: ausgeklappt, „Karte“: eingeklappt); der Zustand wird nicht gespeichert.
    /// </summary>
    public bool IsBottomSheetExpanded
    {
        get => _isBottomSheetExpanded;
        private set
        {
            if (SetProperty(ref _isBottomSheetExpanded, value))
            {
                OnPropertyChanged(nameof(ShowMoreVisible));
            }
        }
    }

    /// <summary>
    /// Die Kopfzeile der Trefferliste: die Anzahl der Tankstellen des aktuellen Ergebnisses („1 Tankstelle“, „12 Tankstellen“) oder „Keine Ergebnisse“.
    /// </summary>
    public string StationCountText
    {
        get { return TotalStationCount > 0 ? SearchTexts.FormatStationCount(TotalStationCount) : SearchTexts.NoResults; }
    }

    /// <summary>
    /// Befehl zum Auf- und Zuklappen der Trefferliste.
    /// </summary>
    public ICommand ToggleBottomSheetCommand { get; private set; } = null!;

    /// <summary>
    /// Gibt an, ob der Suchdialog (Suchart, Adresse, Route, Radius) über der Karte geöffnet ist.
    /// </summary>
    public bool IsSearchOverlayOpen
    {
        get => _isSearchOverlayOpen;
        private set => SetProperty(ref _isSearchOverlayOpen, value);
    }

    /// <summary>
    /// Befehl zum Öffnen des Suchdialogs.
    /// </summary>
    public ICommand OpenSearchOverlayCommand { get; private set; } = null!;

    /// <summary>
    /// Befehl zum Schließen des Suchdialogs ohne Suche; die Eingaben bleiben erhalten.
    /// </summary>
    public ICommand CloseSearchOverlayCommand { get; private set; } = null!;

    /// <summary>
    /// Gibt an, ob die Schaltfläche „Weitere anzeigen“ erscheint (nur bei ausgeklappter Trefferliste, wenn weitere Tankstellen vorhanden sind).
    /// </summary>
    public bool ShowMoreVisible => HasMore && IsBottomSheetExpanded;

    private void InitializePanels()
    {
        ToggleBottomSheetCommand = new Command(ToggleBottomSheet);
        OpenSearchOverlayCommand = new Command(OpenSearchOverlay);
        CloseSearchOverlayCommand = new Command(CloseSearchOverlay);
    }

    private void SelectView(ResultView view)
    {
        // Die Standardansicht legt nur den Startzustand der Trefferliste fest: „Liste“ ausgeklappt, „Karte“ eingeklappt.
        IsBottomSheetExpanded = view == ResultView.List;
    }

    private void ToggleBottomSheet()
    {
        IsBottomSheetExpanded = !IsBottomSheetExpanded;
    }

    private void OpenSearchOverlay()
    {
        IsSearchOverlayOpen = true;
    }

    private void CloseSearchOverlay()
    {
        IsSearchOverlayOpen = false;
    }
}
