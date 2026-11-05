using Tankradar.MAUI.Models.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Kilometerregeln im <see cref="MAUI.Services.Tankbook.TankbookEntryService"/>: nicht unter Start und Vorgänger, nicht über dem Nachfolger.
/// </summary>
public class TankbookEntryServiceTests_Odometer : BaseTest
{
    private readonly TankbookEntryFixture _fixture = new();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fixture.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Prüft, dass ein Kilometerstand unter dem Start-Kilometerstand mit Grenzwert abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task CreateEntry_BelowVehicleStart_IsRejectedWithBound()
    {
        var vehicle = await _fixture.CreateVehicleAsync(startOdometerKm: 10_000);

        var result = await _fixture.Service.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now, 9_999));

        Assert.Equal(TankbookEntryResult.OdometerBelowVehicleStart, result.Result);
        Assert.Equal(10_000, result.OdometerBoundKm);
    }

    /// <summary>
    /// Prüft, dass ein Kilometerstand unter dem des chronologisch vorherigen Vorgangs mit Grenzwert abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task CreateEntry_BelowPrevious_IsRejectedWithBound()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 10_500));

        var result = await _fixture.Service.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now, 10_400));

        Assert.Equal(TankbookEntryResult.OdometerBelowPrevious, result.Result);
        Assert.Equal(10_500, result.OdometerBoundKm);
    }

    /// <summary>
    /// Prüft, dass ein nachgetragener Vorgang den Kilometerstand des chronologisch nächsten Vorgangs nicht übersteigen darf.
    /// </summary>
    [Fact]
    public async Task CreateEntry_Backdated_AboveNext_IsRejected()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-5), 10_500));
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 11_000));

        var result = await _fixture.Service.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-3), 11_200));

        Assert.Equal(TankbookEntryResult.OdometerAboveNext, result.Result);
        Assert.Equal(11_000, result.OdometerBoundKm);
        Assert.Equal(TankbookEntryResult.Ok, (await _fixture.Service.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-3), 10_800))).Result);
    }

    /// <summary>
    /// Prüft, dass ein Kilometerstand gleich dem des Vorgängers erlaubt ist.
    /// </summary>
    [Fact]
    public async Task CreateEntry_EqualToPrevious_IsAllowed()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 10_500));

        var result = await _fixture.Service.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now, 10_500));

        Assert.Equal(TankbookEntryResult.Ok, result.Result);
    }

    /// <summary>
    /// Prüft, dass beim Korrigieren der Vorgang selbst nicht als Vorgänger oder Nachfolger zählt, die Nachbarn aber schon.
    /// </summary>
    [Fact]
    public async Task UpdateEntry_IgnoresItselfAsNeighbour()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-3), 10_500));
        var middle = await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-2), 10_800));
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 11_000));

        var lowered = await _fixture.Service.UpdateEntryAsync(middle.Id, TankbookEntryFixture.Input(middle.OccurredAt, 10_600));
        var tooHigh = await _fixture.Service.UpdateEntryAsync(middle.Id, TankbookEntryFixture.Input(middle.OccurredAt, 11_001));

        Assert.Equal(TankbookEntryResult.Ok, lowered.Result);
        Assert.Equal(TankbookEntryResult.OdometerAboveNext, tooHigh.Result);
        Assert.Equal(11_000, tooHigh.OdometerBoundKm);
    }
}
