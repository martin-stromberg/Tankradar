using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Fehlerfälle der Detailansicht: Einstellungen nicht ladbar, Ausnahme oder Ablehnung des Preisdienstes, fehlgeschlagene Navigation.
/// </summary>
public class StationDetailViewModelTests_Errors : StationDetailViewModelTestBase
{
    /// <summary>
    /// Prüft, dass ein Fehler der Einstellungen gemeldet wird und keine Detailabfrage erfolgt.
    /// </summary>
    [Fact]
    public async Task Appearing_SettingsFail_ShowsMessageWithoutQuery()
    {
        Settings.LoadException = new InvalidOperationException("kaputt");
        ViewModel.Show(CreateOrigin());

        await AppearAsync();

        Assert.Equal(SettingsTexts.LoadFailed, ViewModel.StatusMessage);
        Assert.Empty(Prices.DetailRequests);
        Assert.True(ViewModel.HasDetail);
    }

    /// <summary>
    /// Prüft, dass eine unerwartete Ausnahme des Preisdienstes eine Meldung ohne Rohtext liefert und die Anfangsanzeige erhalten bleibt.
    /// </summary>
    [Fact]
    public async Task Appearing_ServiceThrows_ShowsGenericMessageAndKeepsListData()
    {
        Prices.DetailException = new InvalidOperationException("geheimer Schlüssel abc");
        ViewModel.Show(CreateOrigin());

        await AppearAsync();

        Assert.Equal(DetailTexts.NotAvailable, ViewModel.StatusMessage);
        Assert.True(ViewModel.HasDetail);
        Assert.DoesNotContain("abc", Logger.AllText, StringComparison.Ordinal);
        Assert.False(ViewModel.IsBusy);
    }

    /// <summary>
    /// Prüft, dass eine Ablehnung des Preisdienstes eine Meldung zeigt, ohne die bekannten Daten zu verlieren.
    /// </summary>
    [Fact]
    public async Task Appearing_Rejected_ShowsFailureMessage()
    {
        Prices.DetailResult = new StationDetailResult(null, PriceDataSource.OfflineFallback, PriceFailure.Rejected);
        ViewModel.Show(CreateOrigin());

        await AppearAsync();

        Assert.Equal(SearchTexts.GetFailureMessage(PriceFailure.Rejected, true), ViewModel.StatusMessage);
        Assert.True(ViewModel.HasDetail);
    }

    /// <summary>
    /// Prüft, dass eine fehlgeschlagene Rückkehr nicht unbeobachtet bleibt und die Ansicht bedienbar bleibt.
    /// </summary>
    [Fact]
    public void BackCommand_NavigationFails_DoesNotThrow()
    {
        Navigator.BackException = new InvalidOperationException("kaputt");

        ViewModel.BackCommand.Execute(null);

        Assert.Equal(1, Navigator.BackCount);
        Assert.Contains("InvalidOperationException", Logger.AllText, StringComparison.Ordinal);
    }
}
