using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using static Tankradar.Tests.Unit.Unit.Support.TankbookEntryFactory;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Kennzeichnung und den Hinweis zum Verbrauch ab Startstand in Verbrauchszeile, Hinweis, Auswertungstexten und Item-ViewModel.
/// </summary>
public class TankbookEntryTextsTests_FromStart : BaseTest
{
    private static TankbookEntryOverview Overview(decimal? consumption, TankbookConsumptionState state, bool fromStart)
    {
        return new TankbookEntryOverview(Create(1, 10_500, 30m), 500, consumption, state, 30m, 45m, fromStart);
    }

    /// <summary>
    /// Prüft, dass ein Verbrauch ab Startstand gekennzeichnet ist und ein regulärer Verbrauch unverändert bleibt.
    /// </summary>
    [Fact]
    public void ConsumptionLine_MarksFromStart()
    {
        Assert.Equal("Verbrauch 6,00 L/100 km (ab Startstand)", TankbookEntryTexts.GetConsumptionLine(Overview(6m, TankbookConsumptionState.Calculated, true)));
        Assert.Equal("Verbrauch 6,00 L/100 km", TankbookEntryTexts.GetConsumptionLine(Overview(6m, TankbookConsumptionState.Calculated, false)));
    }

    /// <summary>
    /// Prüft den Grund, wenn seit dem Startstand keine Strecke gefahren wurde.
    /// </summary>
    [Fact]
    public void ConsumptionLine_NoDistanceSinceStart()
    {
        Assert.Equal("Kein Verbrauch: keine Strecke seit dem Startstand", TankbookEntryTexts.GetConsumptionLine(Overview(null, TankbookConsumptionState.NoDistance, true)));
        Assert.Equal("Kein Verbrauch: keine Strecke seit dem letzten vollen Vorgang", TankbookEntryTexts.GetConsumptionLine(Overview(null, TankbookConsumptionState.NoDistance, false)));
    }

    /// <summary>
    /// Prüft, dass der Hinweis zur Annahme nur bei einem berechneten Verbrauch ab Startstand erscheint.
    /// </summary>
    [Fact]
    public void ConsumptionNote_OnlyForCalculatedFromStart()
    {
        Assert.Equal(TankbookEntryTexts.FromStartNote, TankbookEntryTexts.GetConsumptionNote(Overview(6m, TankbookConsumptionState.Calculated, true)));
        Assert.Contains("Tank beim Start voll", TankbookEntryTexts.FromStartNote, StringComparison.Ordinal);
        Assert.Equal(string.Empty, TankbookEntryTexts.GetConsumptionNote(Overview(6m, TankbookConsumptionState.Calculated, false)));
        Assert.Equal(string.Empty, TankbookEntryTexts.GetConsumptionNote(Overview(null, TankbookConsumptionState.NoDistance, true)));
    }

    /// <summary>
    /// Prüft, dass das Item-ViewModel Kennzeichnung und Hinweis übernimmt.
    /// </summary>
    [Fact]
    public void ItemViewModel_ExposesNote()
    {
        var item = new TankbookEntryItemViewModel(Overview(6m, TankbookConsumptionState.Calculated, true), false, new FakeReceiptPhotoStore(), _ => { }, _ => { }, _ => { }, () => true);
        var regular = new TankbookEntryItemViewModel(Overview(6m, TankbookConsumptionState.Calculated, false), false, new FakeReceiptPhotoStore(), _ => { }, _ => { }, _ => { }, () => true);

        Assert.True(item.HasConsumptionNote);
        Assert.Equal(TankbookEntryTexts.FromStartNote, item.ConsumptionNote);
        Assert.False(regular.HasConsumptionNote);
    }

    /// <summary>
    /// Prüft die Kennzeichnung des Durchschnitts ab Startstand und die Hinweise für die Startart.
    /// </summary>
    [Fact]
    public void StatisticsTexts_CoverFromStart()
    {
        Assert.Equal(TankbookStatisticsTexts.FromStartSource, TankbookStatisticsTexts.GetSourceText(TankbookAverageSource.FromStart));
        Assert.Contains("Ab Startstand", TankbookStatisticsTexts.FromStartSource, StringComparison.Ordinal);
        Assert.Equal("Startverbrauch – noch keine Strecke aus vollen Vorgängen", TankbookStatisticsTexts.GetSourceText(TankbookAverageSource.StartConsumption));
        Assert.Equal(TankbookStatisticsTexts.TooFewStartEntriesNotice, TankbookStatisticsTexts.GetTooFewEntriesNotice(false, true));
        Assert.Equal($"{TankbookStatisticsTexts.TooFewStartEntriesNotice} {TankbookStatisticsTexts.StartConsumptionHint}", TankbookStatisticsTexts.GetTooFewEntriesNotice(true, true));
        Assert.Equal(TankbookStatisticsTexts.TooFewEntriesNotice, TankbookStatisticsTexts.GetTooFewEntriesNotice(false));
    }

    /// <summary>
    /// Prüft, dass der Block der Startart ohne vollen Vorgang „mindestens ein voller Vorgang“ nennt, der Strom-Block eines Hybrids weiterhin zwei.
    /// </summary>
    [Fact]
    public void KindBlock_NoticeDependsOnStartKind()
    {
        var hybrid = Statistics([Create(1, 10_200, 10m, isPartial: true), Create(2, 10_300, 5m, kind: TankbookEntryKind.Electricity)], VehicleFuelKind.Hybrid, fromStart: true);

        var fuel = new TankbookKindStatisticsViewModel(hybrid.Kinds.Single(block => block.Kind == TankbookEntryKind.Fuel));
        var electricity = new TankbookKindStatisticsViewModel(hybrid.Kinds.Single(block => block.Kind == TankbookEntryKind.Electricity));

        Assert.StartsWith(TankbookStatisticsTexts.TooFewStartEntriesNotice, fuel.Notice, StringComparison.Ordinal);
        Assert.Equal(TankbookStatisticsTexts.TooFewEntriesNotice, electricity.Notice);
    }

    /// <summary>
    /// Prüft, dass die Hinweise bei Strom von Akku und Teilladung statt von Tank und Teilbetankung sprechen.
    /// </summary>
    [Fact]
    public void Texts_ElectricityTalksAboutBattery()
    {
        Assert.Equal(TankbookEntryTexts.PartialHint, TankbookEntryTexts.GetPartialHint(TankbookEntryKind.Fuel));
        Assert.Equal(TankbookEntryTexts.ElectricPartialHint, TankbookEntryTexts.GetPartialHint(TankbookEntryKind.Electricity));
        Assert.DoesNotContain("Tank", TankbookEntryTexts.ElectricPartialHint, StringComparison.Ordinal);
        Assert.DoesNotContain("Tank", TankbookEntryTexts.ElectricFromStartNote, StringComparison.Ordinal);
        Assert.DoesNotContain("Tank", TankbookStatisticsTexts.ElectricFromStartSource, StringComparison.Ordinal);
        Assert.Equal(TankbookStatisticsTexts.ElectricFromStartSource, TankbookStatisticsTexts.GetSourceText(TankbookAverageSource.FromStart, TankbookEntryKind.Electricity));
        var electric = new TankbookEntryOverview(Create(1, 10_500, 30m) with { Kind = TankbookEntryKind.Electricity }, 500, 6m, TankbookConsumptionState.Calculated, 30m, 45m, true);
        Assert.Equal(TankbookEntryTexts.ElectricFromStartNote, TankbookEntryTexts.GetConsumptionNote(electric));
    }
}
