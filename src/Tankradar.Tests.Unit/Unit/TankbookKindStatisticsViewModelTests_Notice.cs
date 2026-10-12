using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.ViewModels;
using static Tankradar.Tests.Unit.Unit.Support.TankbookEntryFactory;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Hinweis bei zu wenigen vollen Vorgängen: Der Verweis auf den Startverbrauch erscheint nur, wenn die Art die Startart des Fahrzeugs ist
/// und noch kein Startverbrauch hinterlegt ist.
/// </summary>
public class TankbookKindStatisticsViewModelTests_Notice : BaseTest
{
    private static TankbookKindStatisticsViewModel Block(TankbookStatistics statistics, TankbookEntryKind kind)
    {
        return new TankbookKindStatisticsViewModel(statistics.Kinds.Single(block => block.Kind == kind));
    }

    /// <summary>
    /// Prüft die Kennzeichnung der Berechnung: Ein Kraftstoff-Fahrzeug ohne Startverbrauch bietet ihn an, mit Startverbrauch nicht.
    /// </summary>
    [Fact]
    public void Calculator_FuelVehicle_OffersStartConsumptionOnlyWithoutOne()
    {
        var without = Statistics([Create(1, 10_000, 40m)]).Kinds.Single();
        var with = Statistics([Create(1, 10_000, 40m)], startConsumption: 5.4m).Kinds.Single();

        Assert.True(without.OffersStartConsumption);
        Assert.False(with.OffersStartConsumption);
    }

    /// <summary>
    /// Prüft Kraftstoff ohne Startverbrauch: Hinweis mit Verweis.
    /// </summary>
    [Fact]
    public void Fuel_WithoutStartConsumption_NoticeMentionsStartConsumption()
    {
        var block = Block(Statistics([Create(1, 10_000, 40m)]), TankbookEntryKind.Fuel);

        Assert.Equal(TankbookStatisticsTexts.GetTooFewEntriesNotice(true), block.Notice);
        Assert.Contains("Startverbrauch", block.Notice, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft Kraftstoff mit Startverbrauch: kein Hinweis, der Startverbrauch gilt als gekennzeichneter Wert.
    /// </summary>
    [Fact]
    public void Fuel_WithStartConsumption_NoNotice()
    {
        var block = Block(Statistics([Create(1, 10_000, 40m)], startConsumption: 5.4m), TankbookEntryKind.Fuel);

        Assert.False(block.HasNotice);
        Assert.True(block.HasAverageSource);
    }

    /// <summary>
    /// Prüft Strom bei einem Elektro-Fahrzeug ohne Startverbrauch: Hinweis mit Verweis.
    /// </summary>
    [Fact]
    public void Electric_WithoutStartConsumption_NoticeMentionsStartConsumption()
    {
        var block = Block(Statistics([Create(1, 10_000, 20m, kind: TankbookEntryKind.Electricity)], VehicleFuelKind.Electric), TankbookEntryKind.Electricity);

        Assert.Contains("Startverbrauch", block.Notice, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft Strom bei einem Elektro-Fahrzeug mit Startverbrauch: kein Hinweis.
    /// </summary>
    [Fact]
    public void Electric_WithStartConsumption_NoNotice()
    {
        var block = Block(Statistics([Create(1, 10_000, 20m, kind: TankbookEntryKind.Electricity)], VehicleFuelKind.Electric, 16.5m), TankbookEntryKind.Electricity);

        Assert.False(block.HasNotice);
    }

    /// <summary>
    /// Prüft den Strom-Block eines Hybrids ohne Startverbrauch: Der Verweis fehlt, denn der Startverbrauch gilt dort nur für Kraftstoff;
    /// der Kraftstoff-Block verweist dagegen darauf.
    /// </summary>
    [Fact]
    public void Hybrid_WithoutStartConsumption_ElectricityNoticeHasNoStartHint()
    {
        var statistics = Statistics([Create(1, 10_000, 40m), Create(2, 10_100, 15m, kind: TankbookEntryKind.Electricity)], VehicleFuelKind.Hybrid);

        var electricity = Block(statistics, TankbookEntryKind.Electricity);
        var fuel = Block(statistics, TankbookEntryKind.Fuel);

        Assert.Equal(TankbookStatisticsTexts.TooFewEntriesNotice, electricity.Notice);
        Assert.DoesNotContain("Startverbrauch", electricity.Notice, StringComparison.Ordinal);
        Assert.Contains("Startverbrauch", fuel.Notice, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft den Strom-Block eines Hybrids mit hinterlegtem Startverbrauch: kein Verweis; der Kraftstoff-Block hat keinen Hinweis.
    /// </summary>
    [Fact]
    public void Hybrid_WithStartConsumption_ElectricityNoticeHasNoStartHint()
    {
        var statistics = Statistics([Create(1, 10_000, 40m), Create(2, 10_100, 15m, kind: TankbookEntryKind.Electricity)], VehicleFuelKind.Hybrid, 5.4m);

        var electricity = Block(statistics, TankbookEntryKind.Electricity);
        var fuel = Block(statistics, TankbookEntryKind.Fuel);

        Assert.Equal(TankbookStatisticsTexts.TooFewEntriesNotice, electricity.Notice);
        Assert.False(fuel.HasNotice);
    }

    /// <summary>
    /// Prüft eine Art, die nicht zur Startart gehört (Strom bei einem Diesel-Fahrzeug, Kraftstoff bei einem Elektro-Fahrzeug): kein Verweis.
    /// </summary>
    [Fact]
    public void NonStartKind_NoticeHasNoStartHint()
    {
        var electricityOnDiesel = Block(Statistics([Create(1, 10_000, 20m, kind: TankbookEntryKind.Electricity)]), TankbookEntryKind.Electricity);
        var fuelOnElectric = Block(Statistics([Create(1, 10_000, 40m)], VehicleFuelKind.Electric), TankbookEntryKind.Fuel);

        Assert.Equal(TankbookStatisticsTexts.TooFewEntriesNotice, electricityOnDiesel.Notice);
        Assert.Equal(TankbookStatisticsTexts.TooFewEntriesNotice, fuelOnElectric.Notice);
    }
}
