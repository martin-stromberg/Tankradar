namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Art des Zeitraums, auf den ein Export eingeschränkt wird.
/// </summary>
public enum TankbookExportRangeKind
{
    /// <summary>
    /// Alle Vorgänge ohne Einschränkung.
    /// </summary>
    All,

    /// <summary>
    /// Vom 1. Januar des laufenden Jahres bis heute.
    /// </summary>
    ThisYear,

    /// <summary>
    /// Das gesamte Vorjahr.
    /// </summary>
    LastYear,

    /// <summary>
    /// Die letzten zwölf Monate bis heute.
    /// </summary>
    Last12Months,

    /// <summary>
    /// Ein vom Anwender gewählter Zeitraum.
    /// </summary>
    Custom,
}
