using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Vergeben, Ändern und Entfernen des eigenen Anzeigenamens in der Detailansicht.
/// </summary>
public class StationDetailViewModelTests_CustomName : StationDetailViewModelTestBase
{
    private async Task LoadAsync(string? customName = null)
    {
        var station = CreateStation(Now.AddMinutes(-5), Now.AddMinutes(-5));
        Prices.DetailResult = new StationDetailResult(
            new StationInfo
            {
                Id = station.Id,
                Name = "Klaus Heinrich",
                Brand = "Westfalen",
                CustomName = customName,
                Prices = station.Prices,
                DetailsUpdatedUtc = station.DetailsUpdatedUtc,
            },
            PriceDataSource.Live,
            PriceFailure.None);
        ViewModel.Show(CreateOrigin());
        await AppearAsync();
    }

    /// <summary>
    /// Prüft, dass nach dem Laden Marke, Stammdatenname und der bereits vergebene eigene Name im Eingabefeld stehen.
    /// </summary>
    [Fact]
    public async Task Load_ShowsBrandSubNameAndExistingCustomName()
    {
        await LoadAsync("Zuhause");

        Assert.Equal("Zuhause", ViewModel.Detail!.Name);
        Assert.Equal("Klaus Heinrich", ViewModel.Detail.SubNameText);
        Assert.Equal("Zuhause", ViewModel.CustomNameInput);
        Assert.True(ViewModel.HasCustomName);
    }

    /// <summary>
    /// Prüft, dass das Speichern den Namen an den Dienst gibt, die Anzeige sofort ändert und die Rückmeldung zeigt.
    /// </summary>
    [Fact]
    public async Task Save_ChangesDisplayedNameAndConfirms()
    {
        await LoadAsync();
        Assert.Equal("Westfalen", ViewModel.Detail!.Name);

        ViewModel.CustomNameInput = "  Mein Tanken ";
        ViewModel.SaveCustomNameCommand.Execute(null);
        await ViewModel.LastCustomNameTask;

        Assert.Equal([(StationId, "Mein Tanken")], Prices.CustomNameRequests);
        Assert.Equal("Mein Tanken", ViewModel.Detail.Name);
        Assert.Equal("Klaus Heinrich", ViewModel.Detail.SubNameText);
        Assert.Equal("Mein Tanken", ViewModel.CustomNameInput);
        Assert.Equal(DetailTexts.CustomNameSaved, ViewModel.CustomNameMessage);
        Assert.True(ViewModel.HasCustomNameMessage);
        Assert.True(ViewModel.HasCustomName);
    }

    /// <summary>
    /// Prüft, dass das Entfernen zur Marke zurückkehrt und das Eingabefeld leert.
    /// </summary>
    [Fact]
    public async Task Remove_RestoresBrandAndClearsInput()
    {
        await LoadAsync("Zuhause");

        ViewModel.RemoveCustomNameCommand.Execute(null);
        await ViewModel.LastCustomNameTask;

        Assert.Equal([(StationId, string.Empty)], Prices.CustomNameRequests);
        Assert.Equal("Westfalen", ViewModel.Detail!.Name);
        Assert.Equal(string.Empty, ViewModel.CustomNameInput);
        Assert.Equal(DetailTexts.CustomNameRemoved, ViewModel.CustomNameMessage);
        Assert.False(ViewModel.HasCustomName);
    }

    /// <summary>
    /// Prüft, dass ein leeres Eingabefeld beim Speichern den Namen entfernt.
    /// </summary>
    [Fact]
    public async Task Save_BlankInput_RemovesCustomName()
    {
        await LoadAsync("Zuhause");

        ViewModel.CustomNameInput = "   ";
        ViewModel.SaveCustomNameCommand.Execute(null);
        await ViewModel.LastCustomNameTask;

        Assert.Equal(DetailTexts.CustomNameRemoved, ViewModel.CustomNameMessage);
        Assert.Equal("Westfalen", ViewModel.Detail!.Name);
    }

    /// <summary>
    /// Prüft die Meldungen bei zu langem Namen und bei unbekannter Tankstelle; die Anzeige bleibt unverändert.
    /// </summary>
    /// <param name="result">Das vom Dienst gemeldete Ergebnis.</param>
    /// <returns>Ein Task, der nach der Prüfung abgeschlossen ist.</returns>
    [Theory]
    [InlineData(StationNameResult.TooLong)]
    [InlineData(StationNameResult.NotFound)]
    public async Task Save_Rejected_ShowsMessageAndKeepsDisplay(StationNameResult result)
    {
        await LoadAsync();
        Prices.CustomNameResult = result;

        ViewModel.CustomNameInput = "Name";
        ViewModel.SaveCustomNameCommand.Execute(null);
        await ViewModel.LastCustomNameTask;

        var expected = result == StationNameResult.TooLong
            ? DetailTexts.FormatCustomNameTooLong(StationNaming.MaxCustomNameLength)
            : DetailTexts.CustomNameUnknownStation;
        Assert.Equal(expected, ViewModel.CustomNameMessage);
        Assert.Equal("Westfalen", ViewModel.Detail!.Name);
    }

    /// <summary>
    /// Prüft, dass ein Speicherfehler gemeldet und nur mit dem Ausnahmetyp protokolliert wird.
    /// </summary>
    [Fact]
    public async Task Save_StorageFailure_ShowsErrorWithoutDetails()
    {
        await LoadAsync();
        Prices.CustomNameException = new InvalidOperationException("geheim");

        ViewModel.CustomNameInput = "Name";
        ViewModel.SaveCustomNameCommand.Execute(null);
        await ViewModel.LastCustomNameTask;

        Assert.Equal(DetailTexts.CustomNameFailed, ViewModel.CustomNameMessage);
        Assert.DoesNotContain(Logger.Messages, message => message.Contains("geheim", StringComparison.Ordinal));
    }

    /// <summary>
    /// Prüft, dass ein erneutes Laden den eingegebenen, noch nicht gespeicherten Text nicht überschreibt.
    /// </summary>
    [Fact]
    public async Task Reload_KeepsTypedInput()
    {
        await LoadAsync();
        ViewModel.CustomNameInput = "Noch nicht gespeichert";

        ViewModel.RefreshCommand.Execute(null);
        await ViewModel.LastLoadTask;

        Assert.Equal("Noch nicht gespeichert", ViewModel.CustomNameInput);
    }

    /// <summary>
    /// Prüft, dass das Verlassen ohne geladene Tankstelle das Speichern wirkungslos lässt.
    /// </summary>
    [Fact]
    public async Task Save_WithoutStation_DoesNothing()
    {
        ViewModel.SaveCustomNameCommand.Execute(null);
        await ViewModel.LastCustomNameTask;

        Assert.Empty(Prices.CustomNameRequests);
        Assert.False(ViewModel.HasCustomNameMessage);
    }

    /// <summary>
    /// Prüft, dass eine Eingabe vor dem Ende des ersten Ladens nicht vom Vorbelegen überschrieben wird.
    /// </summary>
    [Fact]
    public async Task TypedBeforeLoad_IsNotOverwritten()
    {
        var station = CreateStation(Now.AddMinutes(-5), Now.AddMinutes(-5));
        Prices.DetailResult = new StationDetailResult(
            new StationInfo { Id = station.Id, Name = "Klaus Heinrich", CustomName = "Alt", Prices = station.Prices, DetailsUpdatedUtc = station.DetailsUpdatedUtc },
            PriceDataSource.Live,
            PriceFailure.None);
        ViewModel.Show(CreateOrigin());

        ViewModel.CustomNameInput = "Neu";
        await AppearAsync();

        Assert.Equal("Neu", ViewModel.CustomNameInput);
    }

    /// <summary>
    /// Prüft, dass ein Laden, das den alten Stand ohne den inzwischen gespeicherten Namen liefert, den gespeicherten Namen nicht verdrängt.
    /// </summary>
    [Fact]
    public async Task Reload_WithStaleResult_KeepsSavedCustomName()
    {
        await LoadAsync();
        ViewModel.CustomNameInput = "Gespeichert";
        ViewModel.SaveCustomNameCommand.Execute(null);
        await ViewModel.LastCustomNameTask;

        ViewModel.RefreshCommand.Execute(null);
        await ViewModel.LastLoadTask;

        Assert.Equal("Gespeichert", ViewModel.Detail!.Name);
        Assert.True(ViewModel.HasCustomName);
    }
}
