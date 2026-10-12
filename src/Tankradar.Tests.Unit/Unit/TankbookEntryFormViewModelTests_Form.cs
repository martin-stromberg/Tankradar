using Tankradar.MAUI.Models.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Formular <see cref="MAUI.ViewModels.TankbookEntryFormViewModel"/>: Vorbelegung, Arten je Fahrzeug, Speichern, Feldfehler und Kilometerfehler des Dienstes.
/// </summary>
public class TankbookEntryFormViewModelTests_Form : TankbookEntriesViewModelTestBase
{
    /// <summary>
    /// Prüft die Vorbelegung: Datum und Uhrzeit jetzt (minutengenau), Art nach Fahrzeug fest, Teilbetankung aus, Platzhalter mit letztem Stand.
    /// </summary>
    [Fact]
    public async Task Add_PrefillsNowAndKindByVehicle()
    {
        var vehicle = await CreateVehicleAsync();
        await Fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-1), 10_500));
        var viewModel = await OpenAsync(vehicle.Id);

        viewModel.AddEntryCommand.Execute(null);

        var form = viewModel.Form;
        var now = Fixture.Now;
        Assert.True(form.IsOpen);
        Assert.False(form.IsClosed);
        Assert.Equal("Neuer Vorgang", form.FormTitle);
        Assert.Equal(now.Date, form.EntryDate);
        Assert.Equal(new TimeSpan(now.Hour, now.Minute, 0), form.EntryTime);
        Assert.Equal(now.Date, form.MaximumDate);
        Assert.False(form.ShowKindChoice);
        Assert.True(form.ShowKindText);
        Assert.Equal("Kraftstoff", form.KindText);
        Assert.Equal("L", form.QuantityUnit);
        Assert.Equal("€/L", form.UnitPriceUnit);
        Assert.False(form.IsPartial);
        Assert.Equal("Letzter Stand 10.500 km", form.OdometerPlaceholder);
        Assert.Equal(string.Empty, form.Quantity);
    }

    /// <summary>
    /// Prüft, dass ein Hybridfahrzeug beide Arten als Chips anbietet (Kraftstoff vorgewählt) und Strom die Einheiten wechselt.
    /// </summary>
    [Fact]
    public async Task Hybrid_OffersBothKinds()
    {
        var vehicle = await CreateVehicleAsync(VehicleFuelKind.Hybrid);
        var viewModel = await OpenAsync(vehicle.Id);
        viewModel.AddEntryCommand.Execute(null);
        var form = viewModel.Form;

        Assert.True(form.ShowKindChoice);
        Assert.Equal([TankbookEntryKind.Fuel, TankbookEntryKind.Electricity], form.KindOptions.Select(option => option.Value));
        Assert.Equal(TankbookEntryKind.Fuel, form.SelectedKind);

        form.KindOptions[1].SelectCommand.Execute(null);

        Assert.Equal(TankbookEntryKind.Electricity, form.SelectedKind);
        Assert.False(form.KindOptions[0].IsSelected);
        Assert.Equal("kWh", form.QuantityUnit);
        Assert.Equal("€/kWh", form.UnitPriceUnit);
        Assert.Equal("Preis pro kWh", form.UnitPriceLabel);
    }

    /// <summary>
    /// Prüft das Speichern: Der berechnete Gesamtbetrag wird übernommen, das Formular schließt und die Liste lädt neu.
    /// </summary>
    [Fact]
    public async Task Save_Valid_CreatesAndReloads()
    {
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);

        await CreateViaFormAsync(viewModel, "40", "1,859", string.Empty, "10500");

        Assert.False(viewModel.Form.IsOpen);
        var entry = Assert.Single(viewModel.Entries).Entry;
        Assert.Equal(74.36m, entry.TotalAmount);
        Assert.Equal(TankbookEntryKind.Fuel, entry.Kind);
        Assert.Equal(Fixture.Now, entry.OccurredAt);
        Assert.Equal("74,36", viewModel.Form.TotalAmount);
        Assert.Empty(Announcer.Announcements);
    }

    /// <summary>
    /// Prüft, dass alle Feldfehler gleichzeitig erscheinen, der erste angesagt und nichts gespeichert wird.
    /// </summary>
    [Fact]
    public async Task Save_InvalidFields_ShowsAllErrorsAndAnnouncesFirst()
    {
        var vehicle = await CreateVehicleAsync();
        var viewModel = await OpenAsync(vehicle.Id);
        viewModel.AddEntryCommand.Execute(null);
        var form = viewModel.Form;
        form.Quantity = "abc";
        form.EntryDate = Fixture.Now.Date.AddDays(1);

        await PressFormAsync(viewModel, form.SaveCommand);

        Assert.Equal("Der Zeitpunkt darf nicht in der Zukunft liegen.", form.DateError);
        Assert.Equal("Bitte gib eine Tankstelle oder einen Ladeort ein oder wähle eine bekannte Tankstelle.", form.StationError);
        Assert.Equal("Bitte gib die Menge in L als Zahl ein, z. B. 40,5.", form.QuantityError);
        Assert.Equal("Bitte gib den Kilometerstand ein.", form.OdometerError);
        Assert.True(form.HasDateError && form.HasStationError && form.HasQuantityError && form.HasOdometerError);
        Assert.False(form.HasUnitPriceError);
        Assert.Equal([form.DateError], Announcer.Announcements);
        Assert.True(form.IsOpen);
        Assert.Empty(await Fixture.Service.GetEntriesAsync(vehicle.Id));
    }

    /// <summary>
    /// Prüft, dass ein Kilometerstand unter dem Vorgänger als Feldfehler mit Grenzwert erscheint und angesagt wird.
    /// </summary>
    [Fact]
    public async Task Save_OdometerBelowPrevious_ShowsBoundError()
    {
        var vehicle = await CreateVehicleAsync();
        await Fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-1), 10_500));
        var viewModel = await OpenAsync(vehicle.Id);

        await CreateViaFormAsync(viewModel, "40", "1,859", string.Empty, "10400");

        Assert.Equal("Der Kilometerstand darf nicht unter 10.500 km (vorheriger Vorgang) liegen.", viewModel.Form.OdometerError);
        Assert.Equal([viewModel.Form.OdometerError], Announcer.Announcements);
        Assert.True(viewModel.Form.IsOpen);
        Assert.Single(viewModel.Entries);
    }
}
