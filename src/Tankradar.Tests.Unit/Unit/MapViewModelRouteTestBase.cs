using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Gemeinsamer Aufbau der Tests des Routenmodus im <see cref="MAUI.ViewModels.MapViewModel"/>.
/// </summary>
public abstract class MapViewModelRouteTestBase : MapViewModelTestBase
{
    /// <summary>
    /// Der Starttext.
    /// </summary>
    protected const string StartText = "Berlin Mitte";

    /// <summary>
    /// Der Zieltext.
    /// </summary>
    protected const string DestinationText = "Oranienburg";

    /// <summary>
    /// Wählt den Routenmodus, lässt die Seite erscheinen und gibt Start (Adresse) und Ziel ein.
    /// </summary>
    /// <param name="startAddress"><see langword="true"/> für den Adressstart, <see langword="false"/> für den aktuellen Standort.</param>
    /// <returns>Ein Task, der nach der Vorbereitung abgeschlossen ist.</returns>
    protected async Task PrepareRouteAsync(bool startAddress = true)
    {
        await AppearAsync();
        ViewModel.ModeOptions.Single(option => option.Value == SearchMode.Route).SelectCommand.Execute(null);
        if (startAddress)
        {
            ViewModel.RouteStartOptions.Single(option => option.Value == RouteStartKind.Address).SelectCommand.Execute(null);
            ViewModel.StartInput = StartText;
        }

        ViewModel.DestinationInput = DestinationText;
    }

    /// <summary>
    /// Erzeugt eine Station im Abstandsband der Route.
    /// </summary>
    /// <param name="number">Laufende Nummer.</param>
    /// <param name="name">Der Name.</param>
    /// <param name="alongKm">Die Streckenposition.</param>
    /// <param name="offsetKm">Der Abstand zur Route.</param>
    /// <param name="e5">Preis Super E5.</param>
    /// <param name="diesel">Preis Diesel.</param>
    /// <returns>Die Station.</returns>
    protected RouteStation RouteStationAt(int number, string name, double alongKm, double offsetKm, decimal e5, decimal diesel)
    {
        var station = StationFactory.CreateCustom(number, name, 99, Now, true, true, (FuelType.SuperE5, e5), (FuelType.Diesel, diesel));
        return new RouteStation(station, alongKm, offsetKm);
    }

    /// <summary>
    /// Wartet bis zu zwei Sekunden auf eine Bedingung (für Meldungen, die über den Kontext der Oberfläche eintreffen).
    /// </summary>
    /// <param name="condition">Die Bedingung.</param>
    /// <returns>Ein Task, der abgeschlossen ist, sobald die Bedingung erfüllt ist.</returns>
    protected static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }

        Assert.True(condition(), "Die erwartete Bedingung wurde nicht rechtzeitig erfüllt.");
    }
}
