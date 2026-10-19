using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Backup;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den erfolgreichen Export im <see cref="MAUI.ViewModels.TankbookExportViewModel"/>: nur auf Veranlassung, genau eine Weitergabe, Aufräumen, Ansage, Sperre bei Doppelauslösung.
/// </summary>
public class TankbookExportViewModelTests_Export : TankbookExportViewModelTestBase
{
    /// <summary>
    /// Prüft, dass ohne Befehl weder Datei noch Weitergabe entstehen (auch nicht beim Übergeben des Fahrzeugs).
    /// </summary>
    [Fact]
    public async Task SetVehicle_WithoutCommand_CreatesNoFileAndNoShare()
    {
        await PrepareVehicleAsync();

        Assert.Empty(Sharer.Shared);
        Assert.False(Directory.Exists(Fixture.Temp.Combine("cache", "exports")));
        Assert.Null(ViewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft den Ablauf: genau eine Weitergabe mit Pfad und Titel, Meldung mit Dateiname, Ansage und gelöschte Datei danach.
    /// </summary>
    [Fact]
    public async Task Export_Csv_SharesOnceThenDiscardsFile()
    {
        var vehicle = await PrepareVehicleAsync();
        var existsAtShare = false;
        var sharer = new CheckingSharer(path => existsAtShare = File.Exists(path));
        var viewModel = Fixture.CreateExport(sharer, Announcer, Logger);
        viewModel.SetVehicle(vehicle);

        viewModel.ExportCommand.Execute(null);
        await viewModel.LastTask;

        var path = Assert.Single(sharer.Paths);
        Assert.True(existsAtShare);
        Assert.Equal("Tankbuch-Export", sharer.Title);
        Assert.False(File.Exists(path));
        var expectedName = $"Tankbuch_Golf_{Fixture.Now:yyyy-MM-dd}.csv";
        Assert.Equal($"Export bereit: {expectedName}.", viewModel.StatusMessage);
        Assert.False(viewModel.StatusIsError);
        Assert.Contains($"Export bereit: {expectedName}.", Announcer.Announcements);
        Assert.False(viewModel.IsBusy);
    }

    /// <summary>
    /// Prüft das Format PDF.
    /// </summary>
    [Fact]
    public async Task Export_Pdf_ShareFileHasPdfExtension()
    {
        var sharer = new CheckingSharer(_ => { });
        var viewModel = Fixture.CreateExport(sharer, Announcer, Logger);
        var vehicle = await Fixture.CreateVehicleAsync("Golf");
        await Fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-2), 10_500));
        viewModel.SetVehicle(vehicle);
        viewModel.FormatOptions.Single(option => option.Value == TankbookExportFormat.Pdf).SelectCommand.Execute(null);

        viewModel.ExportCommand.Execute(null);
        await viewModel.LastTask;

        Assert.EndsWith(".pdf", Assert.Single(sharer.Paths), StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass ein zweiter Auslöser während des laufenden Exports ignoriert wird.
    /// </summary>
    [Fact]
    public async Task Export_WhileBusy_SecondTriggerIsIgnored()
    {
        var gate = new TaskCompletionSource();
        var sharer = new CheckingSharer(_ => { }) { Gate = gate.Task };
        var viewModel = Fixture.CreateExport(sharer, Announcer, Logger);
        var vehicle = await Fixture.CreateVehicleAsync("Golf");
        await Fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(Fixture.Now.AddDays(-2), 10_500));
        viewModel.SetVehicle(vehicle);

        viewModel.ExportCommand.Execute(null);
        var first = viewModel.LastTask;
        await sharer.WaitForShareAsync();
        Assert.True(viewModel.IsBusy);
        viewModel.ExportCommand.Execute(null);
        gate.SetResult();
        await first;
        await viewModel.LastTask;

        Assert.Single(sharer.Paths);
        Assert.False(viewModel.IsBusy);
    }

    /// <summary>
    /// Prüft, dass ein eigener Zeitraum die Zahl der exportierten Vorgänge einschränkt (zwei Vorgänge, nur der neuere im Zeitraum).
    /// </summary>
    [Fact]
    public async Task Export_CustomRange_UsesDateFields()
    {
        await PrepareVehicleAsync();
        ViewModel.RangeOptions.Single(option => option.Value == TankbookExportRangeKind.Custom).SelectCommand.Execute(null);
        ViewModel.FromDate = Fixture.Now.Date.AddDays(-5);
        ViewModel.ToDate = Fixture.Now.Date;

        await ExportAsync();

        Assert.Single(Sharer.Shared);
        Assert.Null(ViewModel.RangeError);
        Assert.StartsWith("Export bereit", ViewModel.StatusMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// Weitergabe, die Pfad und Titel festhält, die Datei beim Teilen prüft und auf Wunsch wartet.
    /// </summary>
    private sealed class CheckingSharer : IBackupSharer
    {
        private readonly Action<string> _onShare;
        private readonly TaskCompletionSource _shared = new();

        public CheckingSharer(Action<string> onShare)
        {
            _onShare = onShare;
        }

        public List<string> Paths { get; } = [];

        public string? Title { get; private set; }

        public Task? Gate { get; init; }

        public Task WaitForShareAsync()
        {
            return _shared.Task;
        }

        public async Task ShareAsync(string fullPath, string title, CancellationToken cancellationToken = default)
        {
            Paths.Add(fullPath);
            Title = title;
            _onShare(fullPath);
            _shared.TrySetResult();
            if (Gate is not null)
            {
                await Gate;
            }
        }
    }
}
