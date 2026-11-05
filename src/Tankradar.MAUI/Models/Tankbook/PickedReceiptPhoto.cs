namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ein vom Nutzer ausgewähltes oder aufgenommenes Belegfoto vor der Übernahme in die Ablage.
/// </summary>
public sealed class PickedReceiptPhoto
{
    private readonly Func<Task<Stream>> _openRead;

    /// <summary>
    /// Erstellt das Foto.
    /// </summary>
    /// <param name="extension">Die Dateiendung samt Punkt, z. B. <c>.jpg</c>.</param>
    /// <param name="openRead">Öffnet den Inhalt zum Lesen.</param>
    public PickedReceiptPhoto(string extension, Func<Task<Stream>> openRead)
    {
        ArgumentNullException.ThrowIfNull(openRead);
        Extension = extension ?? string.Empty;
        _openRead = openRead;
    }

    /// <summary>
    /// Die Dateiendung samt Punkt, z. B. <c>.jpg</c>.
    /// </summary>
    public string Extension { get; }

    /// <summary>
    /// Öffnet den Inhalt des Fotos zum Lesen.
    /// </summary>
    /// <returns>Der Datenstrom; der Aufrufer schließt ihn.</returns>
    public Task<Stream> OpenReadAsync()
    {
        return _openRead();
    }
}
