using Tankradar.MAUI.Services.Navigation;
using Tankradar.MAUI.Views;

namespace Tankradar.MAUI;

/// <summary>
/// Hauptnavigations-Container mit den vier Bereichen Favoriten, Karte, Tankbuch und Optionen als Bottom-TabBar.
/// </summary>
public partial class AppShell : Shell
{
    /// <summary>
    /// Erstellt die AppShell und richtet die dynamische Titel-Aktualisierung bei Tab-Wechsel ein.
    /// </summary>
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(ShellStationNavigator.DetailRoute, typeof(StationDetailPage));
        Routing.RegisterRoute(ShellFavoriteGroupNavigator.GroupRoute, typeof(FavoriteGroupPage));
        Navigated += OnNavigated;
    }

    private void OnNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        if (CurrentPage is { } currentPage)
        {
            Title = currentPage.Title;
        }
    }
}
