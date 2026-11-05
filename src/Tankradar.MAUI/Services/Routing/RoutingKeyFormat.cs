using Tankradar.MAUI.Models.Routing;

namespace Tankradar.MAUI.Services.Routing;

/// <summary>
/// Prüft die Schreibweise eines vom Anwender eingegebenen OpenRouteService-Schlüssels, bevor er gespeichert oder an den Dienst geschickt wird.
/// Der Schlüssel steht später in einem Anfragekopf; Steuer-, Leer- und Nicht-ASCII-Zeichen würden den Kopf verfälschen und sind deshalb ausgeschlossen.
/// </summary>
public static class RoutingKeyFormat
{
    /// <summary>
    /// Die größte zulässige Länge eines Schlüssels.
    /// </summary>
    public const int MaxLength = 512;

    /// <summary>
    /// Prüft die Eingabe und liefert den bereinigten Schlüssel (ohne umgebende Leerzeichen).
    /// </summary>
    /// <param name="input">Die Eingabe.</param>
    /// <param name="key">Der bereinigte Schlüssel bei Erfolg, sonst eine leere Zeichenfolge.</param>
    /// <returns>Der Fehler oder <see cref="RoutingKeyError.None"/>.</returns>
    public static RoutingKeyError Validate(string? input, out string key)
    {
        key = string.Empty;
        var trimmed = input?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return RoutingKeyError.Empty;
        }

        if (trimmed.Length > MaxLength)
        {
            return RoutingKeyError.TooLong;
        }

        foreach (var character in trimmed)
        {
            if (character is < '!' or > '~')
            {
                return RoutingKeyError.InvalidCharacters;
            }
        }

        key = trimmed;
        return RoutingKeyError.None;
    }
}
