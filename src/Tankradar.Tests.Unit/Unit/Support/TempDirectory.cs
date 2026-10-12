namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Ein eindeutiges temporäres Verzeichnis, das beim Freigeben samt Inhalt gelöscht wird.
/// </summary>
public sealed class TempDirectory : IDisposable
{
    /// <summary>
    /// Legt das Verzeichnis an.
    /// </summary>
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Tankatlas.Tests.Unit", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>
    /// Der Pfad des Verzeichnisses.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Verbindet den Pfad des Verzeichnisses mit weiteren Teilen.
    /// </summary>
    /// <param name="parts">Die Pfadteile.</param>
    /// <returns>Der vollständige Pfad.</returns>
    public string Combine(params string[] parts)
    {
        return System.IO.Path.Combine([Path, .. parts]);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Aufräumen ist nicht kritisch.
        }
    }
}
