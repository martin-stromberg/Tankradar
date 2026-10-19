using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Auswahl von Format und Zeitraum im <see cref="MAUI.ViewModels.TankbookExportViewModel"/>: Voreinstellung, Chips, Datumsfelder und Schlüssel der AutomationIds.
/// </summary>
public class TankbookExportViewModelTests_Selection : TankbookExportViewModelTestBase
{
    /// <summary>
    /// Prüft die Voreinstellung: CSV und alle Vorgänge, keine Datumsfelder, höchstens heute wählbar.
    /// </summary>
    [Fact]
    public void Defaults_AreCsvAndAllEntries()
    {
        Assert.Equal(TankbookExportFormat.Csv, ViewModel.SelectedFormat);
        Assert.Equal(TankbookExportRangeKind.All, ViewModel.SelectedRange);
        Assert.False(ViewModel.IsCustomRange);
        Assert.True(ViewModel.FormatOptions.Single(option => option.Value == TankbookExportFormat.Csv).IsSelected);
        Assert.True(ViewModel.RangeOptions.Single(option => option.Value == TankbookExportRangeKind.All).IsSelected);
        Assert.Equal(Fixture.Now.Date, ViewModel.MaximumDate);
        Assert.Equal(Fixture.Now.Date, ViewModel.ToDate);
    }

    /// <summary>
    /// Prüft, dass es für jeden Wert der beiden Enums genau eine Option mit lesbarem Text und Schlüssel gibt.
    /// </summary>
    [Fact]
    public void Options_CoverAllValuesWithLabelsAndKeys()
    {
        Assert.Equal(["CSV", "PDF"], ViewModel.FormatOptions.Select(option => option.Label));
        Assert.Equal(["csv", "pdf"], ViewModel.FormatOptions.Select(option => option.AutomationKey));
        Assert.Equal(
            ["Alle Vorgänge", "Dieses Jahr", "Letztes Jahr", "Letzte 12 Monate", "Eigener Zeitraum"],
            ViewModel.RangeOptions.Select(option => option.Label));
        Assert.Equal(["all", "thisYear", "lastYear", "last12Months", "custom"], ViewModel.RangeOptions.Select(option => option.AutomationKey));
        Assert.Equal(
            [TankbookExportRangeKind.All, TankbookExportRangeKind.ThisYear, TankbookExportRangeKind.LastYear, TankbookExportRangeKind.Last12Months, TankbookExportRangeKind.Custom],
            ViewModel.RangeOptions.Select(option => option.Value));
    }

    /// <summary>
    /// Prüft, dass die Wahl des Formats PDF den Zustand umstellt und genau eine Option ausgewählt ist.
    /// </summary>
    [Fact]
    public void SelectFormat_Pdf_UpdatesSelectionExclusively()
    {
        ViewModel.FormatOptions.Single(option => option.Value == TankbookExportFormat.Pdf).SelectCommand.Execute(null);

        Assert.Equal(TankbookExportFormat.Pdf, ViewModel.SelectedFormat);
        Assert.Equal([TankbookExportFormat.Pdf], ViewModel.FormatOptions.Where(option => option.IsSelected).Select(option => option.Value));
        Assert.Equal("Ausgewählt", ViewModel.FormatOptions.Single(option => option.IsSelected).SelectionHint);
    }

    /// <summary>
    /// Prüft, dass die Datumsfelder nur beim eigenen Zeitraum angezeigt werden.
    /// </summary>
    [Fact]
    public void SelectRange_Custom_ShowsDateFields_OthersHideThem()
    {
        foreach (var kind in Enum.GetValues<TankbookExportRangeKind>())
        {
            ViewModel.RangeOptions.Single(option => option.Value == kind).SelectCommand.Execute(null);

            Assert.Equal(kind, ViewModel.SelectedRange);
            Assert.Equal(kind == TankbookExportRangeKind.Custom, ViewModel.IsCustomRange);
        }
    }

    /// <summary>
    /// Prüft, dass der Hinweis der Schaltfläche Fahrzeug, Format und Zeitraum nennt.
    /// </summary>
    [Fact]
    public async Task StartHint_NamesVehicleFormatAndRange()
    {
        await PrepareVehicleAsync();
        ViewModel.FormatOptions.Single(option => option.Value == TankbookExportFormat.Pdf).SelectCommand.Execute(null);
        ViewModel.RangeOptions.Single(option => option.Value == TankbookExportRangeKind.LastYear).SelectCommand.Execute(null);

        Assert.Equal("Fahrzeug „Golf“, PDF, Letztes Jahr", ViewModel.StartHint);
    }

    /// <summary>
    /// Prüft, dass ein Wechsel des Fahrzeugs die Auswahl zurücksetzt, ein erneutes Übergeben desselben Fahrzeugs sie aber behält.
    /// </summary>
    [Fact]
    public async Task SetVehicle_SameVehicleKeepsSelection_OtherVehicleResets()
    {
        var vehicle = await PrepareVehicleAsync();
        ViewModel.FormatOptions.Single(option => option.Value == TankbookExportFormat.Pdf).SelectCommand.Execute(null);

        ViewModel.SetVehicle(vehicle);
        Assert.Equal(TankbookExportFormat.Pdf, ViewModel.SelectedFormat);

        ViewModel.SetVehicle(new Vehicle(vehicle.Id + 1, "Anderes", VehicleFuelKind.Petrol, 0, null));
        Assert.Equal(TankbookExportFormat.Csv, ViewModel.SelectedFormat);
        Assert.True(ViewModel.FormatOptions.Single(option => option.Value == TankbookExportFormat.Csv).IsSelected);
    }
}
