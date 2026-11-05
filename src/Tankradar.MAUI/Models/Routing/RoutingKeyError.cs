namespace Tankradar.MAUI.Models.Routing;

/// <summary>
/// Fehler bei der Prüfung der Schreibweise eines eingegebenen Routing-Schlüssels.
/// </summary>
public enum RoutingKeyError
{
    /// <summary>
    /// Die Eingabe ist zulässig.
    /// </summary>
    None,

    /// <summary>
    /// Es wurde nichts eingegeben.
    /// </summary>
    Empty,

    /// <summary>
    /// Die Eingabe ist zu lang.
    /// </summary>
    TooLong,

    /// <summary>
    /// Die Eingabe enthält Leerzeichen, Steuer- oder Nicht-ASCII-Zeichen.
    /// </summary>
    InvalidCharacters,
}
