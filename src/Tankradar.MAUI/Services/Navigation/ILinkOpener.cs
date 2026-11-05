namespace Tankradar.MAUI.Services.Navigation;

/// <summary>
/// Öffnet eine Internetadresse im Browser des Geräts (nur auf ausdrücklichen Wunsch des Anwenders).
/// </summary>
public interface ILinkOpener
{
    /// <summary>
    /// Öffnet die Adresse.
    /// </summary>
    /// <param name="uri">Die Adresse (HTTPS).</param>
    /// <returns>Ein Task, der nach dem Öffnen abgeschlossen ist.</returns>
    Task OpenAsync(Uri uri);
}

/// <summary>
/// Öffnet Adressen über den Starter des Betriebssystems (<see cref="Launcher"/>); im Testmodus wird nie ein Browser gestartet.
/// </summary>
public sealed class LauncherLinkOpener : ILinkOpener
{
    private readonly bool _testMode;

    /// <summary>
    /// Erstellt den Öffner.
    /// </summary>
    /// <param name="testMode">Gibt an, ob der Testmodus gilt (dann geschieht nichts).</param>
    public LauncherLinkOpener(bool testMode)
    {
        _testMode = testMode;
    }

    /// <inheritdoc />
    public async Task OpenAsync(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (_testMode || uri.Scheme != Uri.UriSchemeHttps)
        {
            return;
        }

        await Launcher.Default.OpenAsync(uri).ConfigureAwait(true);
    }
}
