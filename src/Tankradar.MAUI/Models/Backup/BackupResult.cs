namespace Tankradar.MAUI.Models.Backup;

/// <summary>
/// Ergebnis einer Sicherungsoperation.
/// </summary>
public enum BackupResult
{
    /// <summary>
    /// Die Operation wurde ausgeführt.
    /// </summary>
    Ok,

    /// <summary>
    /// Die Sicherung wurde nicht gefunden (oder der Dateiname ist nicht zulässig).
    /// </summary>
    NotFound,

    /// <summary>
    /// Die Datei ist keine gültige Sicherung oder beschädigt.
    /// </summary>
    InvalidFile,

    /// <summary>
    /// Die Sicherung stammt aus einer neueren Version der App.
    /// </summary>
    TooNew,

    /// <summary>
    /// Es läuft bereits eine andere Sicherungsoperation.
    /// </summary>
    Busy,

    /// <summary>
    /// Die Operation ist aus einem anderen Grund fehlgeschlagen.
    /// </summary>
    Failed,
}
