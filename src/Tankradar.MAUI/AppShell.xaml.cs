using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Navigation;
using Tankradar.MAUI.ViewModels;
using Tankradar.MAUI.Views;

namespace Tankradar.MAUI;

/// <summary>
/// Hauptnavigations-Container mit den vier Bereichen Favoriten, Karte, Tankbuch und Optionen als Bottom-TabBar.
/// Beim ersten Start überlagert die Einrichtung (Wahl der Spritsorten) die Shell samt TabBar (ADR 0017).
/// </summary>
public partial class AppShell : Shell
{
    private readonly ISetupStatusService _setupStatusService;
    private readonly SetupViewModel _setupViewModel;
    private readonly ILogger<AppShell> _logger;
    private ContentPage? _setupOverlay;

    /// <summary>
    /// Erstellt die AppShell, richtet die dynamische Titel-Aktualisierung bei Tab-Wechsel ein und prüft nach dem Laden, ob die Einrichtung angezeigt werden muss.
    /// </summary>
    /// <param name="setupStatusService">Ermittelt nach der Datenbankinitialisierung, ob die Einrichtung aussteht.</param>
    /// <param name="setupViewModel">Das ViewModel der Einrichtung.</param>
    /// <param name="logger">Logger für Fehler beim Prüfen, Anzeigen und Ausblenden der Einrichtung.</param>
    public AppShell(ISetupStatusService setupStatusService, SetupViewModel setupViewModel, ILogger<AppShell> logger)
    {
        _setupStatusService = setupStatusService;
        _setupViewModel = setupViewModel;
        _logger = logger;

        InitializeComponent();
        Routing.RegisterRoute(ShellStationNavigator.DetailRoute, typeof(StationDetailPage));
        Routing.RegisterRoute(ShellFavoriteGroupNavigator.GroupRoute, typeof(FavoriteGroupPage));
        Routing.RegisterRoute(ShellTankbookNavigator.EntriesRoute, typeof(TankbookEntriesPage));
        Navigated += OnNavigated;
        _setupViewModel.Completed += OnSetupCompleted;
        Loaded += OnLoaded;
    }

    /// <summary>
    /// Wartet auf die Datenbankinitialisierung (über <see cref="ISetupStatusService"/>), fragt den Einrichtungsstatus ab und blendet die Einrichtung bei Bedarf ein.
    /// Ein Fehler wird protokolliert; die Einrichtung erscheint dann nicht, damit die Shell bedienbar bleibt.
    /// </summary>
    /// <returns>Ein Task, der nach der Prüfung (und ggf. dem Einblenden) abgeschlossen ist.</returns>
    private async Task CheckSetupAsync()
    {
        try
        {
            if (!await _setupStatusService.IsSetupRequiredAsync().ConfigureAwait(true))
            {
                return;
            }

            var template = (DataTemplate)Resources["SetupOverlayTemplate"];
            var overlay = new ContentPage
            {
                Title = SetupTexts.Heading,
                BindingContext = _setupViewModel,
                Content = (View)template.CreateContent(),
            };
            await Navigation.PushModalAsync(overlay, animated: false).ConfigureAwait(true);
            _setupOverlay = overlay;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Einrichtung beim ersten Start konnte nicht geprüft oder angezeigt werden.");
        }
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        Loaded -= OnLoaded;
        _ = CheckSetupAsync();
    }

    private void OnNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        if (CurrentPage is { } currentPage)
        {
            Title = currentPage.Title;
        }
    }

    private void OnSetupCompleted(object? sender, EventArgs e)
    {
        _ = HideSetupAsync();
    }

    private async Task HideSetupAsync()
    {
        var overlay = _setupOverlay;
        _setupOverlay = null;
        if (overlay is null)
        {
            return;
        }

        // PopModalAsync entfernt die oberste Modalseite; liegt eine andere Seite über der Einrichtung, wird nichts geschlossen.
        var modalStack = Navigation.ModalStack;
        if (modalStack.Count == 0 || !ReferenceEquals(modalStack[^1], overlay))
        {
            _logger.LogWarning("Die Einrichtung ist nicht die oberste Modalseite (Anzahl Modalseiten: {ModalCount}) und wird nicht ausgeblendet.", modalStack.Count);
            return;
        }

        try
        {
            await Navigation.PopModalAsync(animated: false).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Einrichtung konnte nicht ausgeblendet werden.");
        }
    }
}
