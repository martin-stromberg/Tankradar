using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Opt-in-Ablauf der Diagnose-Karte: Vorschau vor dem Teilen, kein automatisches Erzeugen, Fehlerstatus.
/// </summary>
public class DiagnosticsViewModelTests : BaseTest
{
    /// <summary>
    /// Die ersetzten Dienste und das zu testende ViewModel.
    /// </summary>
    public DiagnosticsViewModelTests()
    {
        ViewModel = new DiagnosticsViewModel(PackageService, Sharer, new ListLogger<DiagnosticsViewModel>());
    }

    /// <summary>
    /// Der ersetzte Paketdienst.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeDiagnosticsPackageService PackageService { get; } = new();

    /// <summary>
    /// Die ersetzte Weitergabe.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeDiagnosticsSharer Sharer { get; } = new();

    /// <summary>
    /// Das zu testende ViewModel.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected DiagnosticsViewModel ViewModel { get; }

    /// <summary>
    /// Prüft, dass ohne Benutzeraktion weder ein Paket erzeugt noch etwas geteilt wird.
    /// </summary>
    [Fact]
    public void Ctor_NothingHappensAutomatically()
    {
        Assert.Equal(0, PackageService.CreateCount);
        Assert.Empty(Sharer.Shared);
        Assert.False(ViewModel.HasPreview);
        Assert.False(ViewModel.ShareCommand.CanExecute(null));
        Assert.False(ViewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass die Teilen-Schaltfläche erst nach der Vorschau freigegeben wird.
    /// </summary>
    [Fact]
    public async Task Preview_ThenShare_EnablesShare()
    {
        ViewModel.PreviewCommand.Execute(null);
        await ViewModel.LastTask;

        Assert.True(ViewModel.HasPreview);
        Assert.Contains("Vorschau", ViewModel.PreviewText);
        Assert.True(ViewModel.ShareCommand.CanExecute(null));
    }

    /// <summary>
    /// Prüft, dass die Freigabe der Teilen-Schaltfläche nach Abschluss der Vorschau auch als Ereignis signalisiert wird.
    /// Die gebundene Schaltfläche in der App wertet ausschließlich dieses Ereignis aus.
    /// </summary>
    [Fact]
    public async Task Preview_Completion_RaisesShareCanExecuteChanged()
    {
        var enabledWhenRaised = new List<bool>();
        ViewModel.ShareCommand.CanExecuteChanged += (_, _) => enabledWhenRaised.Add(ViewModel.ShareCommand.CanExecute(null));

        ViewModel.PreviewCommand.Execute(null);
        await ViewModel.LastTask;

        Assert.Contains(true, enabledWhenRaised);
    }

    /// <summary>
    /// Prüft, dass nach Vorschau und Teilen das Paket erzeugt und weitergegeben wird.
    /// </summary>
    [Fact]
    public async Task Share_AfterPreview_CreatesAndShares()
    {
        ViewModel.PreviewCommand.Execute(null);
        await ViewModel.LastTask;
        ViewModel.ShareCommand.Execute(null);
        await ViewModel.LastTask;

        Assert.Equal(1, PackageService.CreateCount);
        Assert.Equal([PackageService.PackagePath], Sharer.Shared);
        Assert.True(ViewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein Fehler beim Teilen als Statusmeldung erscheint statt durchzuschlagen.
    /// </summary>
    [Fact]
    public async Task Share_Failure_ShowsStatusMessage()
    {
        Sharer.Throw = true;
        ViewModel.PreviewCommand.Execute(null);
        await ViewModel.LastTask;
        ViewModel.ShareCommand.Execute(null);
        await ViewModel.LastTask;

        Assert.True(ViewModel.HasStatusMessage);
        Assert.Contains("nicht", ViewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein Fehler der Vorschau als Statusmeldung erscheint.
    /// </summary>
    [Fact]
    public async Task Preview_Failure_ShowsStatusMessage()
    {
        PackageService.Throw = true;

        ViewModel.PreviewCommand.Execute(null);
        await ViewModel.LastTask;

        Assert.True(ViewModel.HasStatusMessage);
        Assert.False(ViewModel.HasPreview);
    }
}
