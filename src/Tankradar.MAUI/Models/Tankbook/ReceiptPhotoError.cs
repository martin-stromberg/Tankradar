namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Fehler beim Übernehmen eines Belegfotos in die Ablage.
/// </summary>
public enum ReceiptPhotoError
{
    /// <summary>
    /// Das Foto wurde übernommen.
    /// </summary>
    None,

    /// <summary>
    /// Das Dateiformat wird nicht unterstützt.
    /// </summary>
    UnsupportedFormat,

    /// <summary>
    /// Das Foto ist zu groß.
    /// </summary>
    TooLarge,

    /// <summary>
    /// Das Foto konnte nicht gespeichert werden.
    /// </summary>
    Failed,
}
