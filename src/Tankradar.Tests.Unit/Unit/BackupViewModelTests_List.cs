using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Laden und Anzeigen der Liste der Sicherungen im <see cref="Tankradar.MAUI.ViewModels.BackupViewModel"/>.
/// </summary>
public class BackupViewModelTests_List : BackupViewModelTestBase
{
    /// <summary>
    /// Prüft, dass die Liste beim Erscheinen in der gelieferten Reihenfolge mit 1-basierten Schlüsseln, Datum und Größe geladen wird.
    /// </summary>
    [Fact]
    public async Task OnAppearing_LoadsItemsInOrderWithOneBasedKeys()
    {
        await AppearAsync();

        Assert.Equal(["1", "2"], ViewModel.Items.Select(item => item.AutomationKey));
        Assert.Equal([1, 2], ViewModel.Items.Select(item => item.Index));
        Assert.Equal(BackupTexts.FormatDate(Service.Files[0].CreatedUtc), ViewModel.Items[0].Title);
        Assert.Equal("1,2 MB", ViewModel.Items[0].SizeText);
        Assert.Equal("900 B", ViewModel.Items[1].SizeText);
        Assert.True(ViewModel.HasItems);
        Assert.False(ViewModel.IsEmpty);
    }

    /// <summary>
    /// Prüft, dass eine leere Liste den Leerhinweis zeigt.
    /// </summary>
    [Fact]
    public async Task EmptyList_ShowsEmptyHint()
    {
        Service.Files = [];

        await AppearAsync();

        Assert.Empty(ViewModel.Items);
        Assert.True(ViewModel.IsEmpty);
        Assert.False(ViewModel.HasItems);
        Assert.Contains("noch keine Sicherung", ViewModel.EmptyText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass ein nicht lesbarer Eintrag gekennzeichnet wird.
    /// </summary>
    [Fact]
    public async Task UnreadableItem_IsMarked()
    {
        Service.Files = [new MAUI.Models.Backup.BackupFileInfo("tankatlas-backup-x.zip", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 5, string.Empty, false)];

        await AppearAsync();

        Assert.EndsWith("(nicht lesbar)", ViewModel.Items[0].Title, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass ein Fehler beim Lesen der Liste gemeldet wird, ohne abzustürzen.
    /// </summary>
    [Fact]
    public async Task ListFailure_ShowsMessage()
    {
        Service.Exception = new IOException("kaputt");

        await AppearAsync();

        Assert.Equal(BackupTexts.ListFailed, ViewModel.StatusMessage);
        Assert.Contains(BackupTexts.ListFailed, Announcer.Announcements);
    }

    /// <summary>
    /// Prüft die Texte und Titel des ViewModels für die Oberfläche.
    /// </summary>
    [Fact]
    public void Texts_ComeFromCentralClass()
    {
        Assert.Equal(BackupTexts.Heading, ViewModel.Heading);
        Assert.Equal(BackupTexts.Hint, ViewModel.Hint);
        Assert.Equal(BackupTexts.CreateButton, ViewModel.CreateText);
        Assert.Equal(BackupTexts.ImportButton, ViewModel.ImportText);
        Assert.False(ViewModel.HasStatusMessage);
    }
}
