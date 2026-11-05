using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Einbettung der Karte „Routensuche“ in das <see cref="SettingsViewModel"/>.
/// </summary>
public class SettingsViewModelTests_RouteSearch : BaseTest
{
    private readonly FakeSettingsService _settings = new();
    private readonly FakeRouteSearchAccess _access = new() { Key = "ZZPLATZHALTER-ZZ", Switched = true };

    private RouteSearchSettingsViewModel CreateCard()
    {
        return new RouteSearchSettingsViewModel(_access, new FakeRoutingKeyChecker(), new FakeConnectionMonitor(), new FakeLinkOpener(), NullLogger<RouteSearchSettingsViewModel>.Instance);
    }

    /// <summary>
    /// Prüft, dass ohne Routensuche die Karte nicht angezeigt wird.
    /// </summary>
    [Fact]
    public void WithoutRouteSearch_HasRouteSearchIsFalse()
    {
        var viewModel = new SettingsViewModel(_settings, new ListLogger<SettingsViewModel>());

        Assert.Null(viewModel.RouteSearch);
        Assert.False(viewModel.HasRouteSearch);
        viewModel.OnAppearing();
        viewModel.OnDisappearing();
    }

    /// <summary>
    /// Prüft, dass Erscheinen und Verlassen der Seite an die Karte weitergegeben werden (Zustand lesen, Schlüsselfeld leeren).
    /// </summary>
    [Fact]
    public async Task AppearingAndDisappearing_AreForwardedToCard()
    {
        var card = CreateCard();
        var viewModel = new SettingsViewModel(_settings, new ListLogger<SettingsViewModel>(), null, null, card);

        viewModel.OnAppearing();
        await card.LastActionTask;
        await viewModel.LoadAsync();

        Assert.True(viewModel.HasRouteSearch);
        Assert.Same(card, viewModel.RouteSearch);
        Assert.True(card.HasStoredKey);
        Assert.True(card.IsRouteSearchEnabled);

        card.KeyInput = "ZZNEU";
        viewModel.OnDisappearing();

        Assert.Equal(string.Empty, card.KeyInput);
    }

    /// <summary>
    /// Prüft, dass der Schalter und der Schlüssel nicht zu den gespeicherten Einstellungen gehören: Eine Änderung der Routensuche speichert keine Einstellungen,
    /// und das Laden der Einstellungen (z. B. nach einer Wiederherstellung) berührt weder Schlüssel noch Schalter.
    /// </summary>
    [Fact]
    public async Task RouteSearchState_IsIndependentOfSettingsAndBackup()
    {
        var card = CreateCard();
        var viewModel = new SettingsViewModel(_settings, new ListLogger<SettingsViewModel>(), null, null, card);
        await viewModel.LoadAsync();
        var saves = _settings.Saved.Count;

        card.OnAppearing();
        await card.LastActionTask;
        card.IsRouteSearchEnabled = false;
        await card.LastActionTask;
        await viewModel.LoadAsync();

        Assert.Equal(saves, _settings.Saved.Count);
        Assert.Equal("ZZPLATZHALTER-ZZ", _access.Key);
        Assert.False(_access.Switched);
    }
}
