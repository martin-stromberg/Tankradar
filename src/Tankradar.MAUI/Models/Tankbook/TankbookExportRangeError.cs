namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Fehler eines Zeitraums für den Export.
/// </summary>
public enum TankbookExportRangeError
{
    /// <summary>
    /// Der Zeitraum ist gültig.
    /// </summary>
    None,

    /// <summary>
    /// Der Starttag liegt nach dem Endtag.
    /// </summary>
    StartAfterEnd,

    /// <summary>
    /// Der Endtag liegt in der Zukunft.
    /// </summary>
    EndInFuture,
}
