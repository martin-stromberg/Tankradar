using System.Text.RegularExpressions;

namespace Tankradar.MAUI.Services.Diagnostics;

/// <summary>
/// Redigiert Texte für das Diagnosepaket: ersetzt konfigurierte Geheimnisse sowie musterhafte vertrauliche Inhalte
/// (Schlüsselparameter, E-Mails, Koordinaten, Dateipfade, tokenartige Zeichenfolgen) durch <c>[***]</c>.
/// </summary>
public static partial class DiagnosticsRedactor
{
    /// <summary>
    /// Der einheitliche Ersetzungstext für entfernte Inhalte.
    /// </summary>
    public const string Placeholder = "[***]";

    private const int MinimumSensitiveValueLength = 8;

    /// <summary>
    /// Redigiert einen Text. Zuerst werden die übergebenen Geheimnisse exakt ersetzt, danach die Musterregeln angewendet.
    /// </summary>
    /// <param name="text">Der Eingabetext; <see langword="null"/> oder leer bleibt unverändert.</param>
    /// <param name="sensitiveValues">Bekannte Geheimnisse (z. B. aktive API-Schlüssel), die wörtlich entfernt werden.</param>
    /// <returns>Der redigierte Text.</returns>
    public static string Redact(string? text, IReadOnlyList<string>? sensitiveValues = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        var result = text;
        if (sensitiveValues is not null)
        {
            foreach (var value in sensitiveValues)
            {
                if (!string.IsNullOrEmpty(value) && value.Length >= MinimumSensitiveValueLength)
                {
                    result = result.Replace(value, Placeholder, StringComparison.Ordinal);
                }
            }
        }

        result = SecretParameterPattern().Replace(result, "$1=" + Placeholder);
        result = EmailPattern().Replace(result, Placeholder);
        result = CoordinateParameterPattern().Replace(result, "$1=" + Placeholder);
        result = CoordinatePairPattern().Replace(result, Placeholder);
        result = WindowsPathPattern().Replace(result, Placeholder);
        result = UncPathPattern().Replace(result, Placeholder);
        result = UnixPathPattern().Replace(result, Placeholder);
        result = TokenPattern().Replace(result, Placeholder);
        return result;
    }

    /// <summary>
    /// Schlüssel-, Token- oder Passwortparameter in der Form <c>name=wert</c> (URL-Abfragen, Konfigurationszeilen).
    /// </summary>
    /// <returns>Das Muster.</returns>
    [GeneratedRegex(@"(?i)\b(api[-_]?key|access[-_]?token|auth[-_]?token|token|secret|signature|sig|key|password|passwd|pwd)\s*=\s*[^\s;&""']+", RegexOptions.CultureInvariant)]
    private static partial Regex SecretParameterPattern();

    /// <summary>
    /// E-Mail-Adressen.
    /// </summary>
    /// <returns>Das Muster.</returns>
    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    /// <summary>
    /// Koordinatenparameter wie <c>lat=12,345</c> oder <c>longitude: 8.6</c> (Name und Wert; der Name bleibt erhalten).
    /// </summary>
    /// <returns>Das Muster.</returns>
    [GeneratedRegex(@"(?i)\b(lat|latitude|lon|lng|long|longitude|coords?|coordinates?|bbox|boundingbox)[=:]\s*-?\d{1,3}[\.,]\d+", RegexOptions.CultureInvariant)]
    private static partial Regex CoordinateParameterPattern();

    /// <summary>
    /// Koordinatenpaare in der Form <c>48,1234,9,5678</c> oder <c>9.5678;48.1234</c>.
    /// </summary>
    /// <returns>Das Muster.</returns>
    [GeneratedRegex(@"-?\d{1,3}\.\d{4,}\s*[,;]\s*-?\d{1,3}\.\d{4,}", RegexOptions.CultureInvariant)]
    private static partial Regex CoordinatePairPattern();

    /// <summary>
    /// Windows-Pfade mit Laufwerksbuchstaben oder <c>file:///</c>-Adressen.
    /// </summary>
    /// <returns>Das Muster.</returns>
    [GeneratedRegex(@"(?:[A-Za-z]:[\\/]|file:///)[^\s""'|<>*?]+", RegexOptions.CultureInvariant)]
    private static partial Regex WindowsPathPattern();

    /// <summary>
    /// UNC-Pfade (<c>\\server\share\...</c>).
    /// </summary>
    /// <returns>Das Muster.</returns>
    [GeneratedRegex(@"\\\\[^\s""'|<>*?]+", RegexOptions.CultureInvariant)]
    private static partial Regex UncPathPattern();

    /// <summary>
    /// Unix-Pfade unterhalb typischer Benutzer- oder Datenwurzeln.
    /// </summary>
    /// <returns>Das Muster.</returns>
    [GeneratedRegex(@"(?<![\w:/])/(?:home|Users|user|data|storage|sdcard|private|var|tmp|mnt|media|Library)\b[^\s""'|<>*?]*", RegexOptions.CultureInvariant)]
    private static partial Regex UnixPathPattern();

    /// <summary>
    /// Tokenartige Zeichenfolgen: mindestens 32 Hexadezimalzeichen oder mindestens 40 Base64-ähnliche Zeichen ohne Zwischenraum.
    /// </summary>
    /// <returns>Das Muster.</returns>
    [GeneratedRegex(@"\b(?:[0-9a-fA-F]{32,}|[A-Za-z0-9+/=_-]{40,})\b", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();
}
