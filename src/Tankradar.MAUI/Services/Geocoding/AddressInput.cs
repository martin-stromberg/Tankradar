using System.Text;

namespace Tankradar.MAUI.Services.Geocoding;

/// <summary>
/// Das Ergebnis der Prüfung einer Adresseingabe.
/// </summary>
public enum AddressInputError
{
    /// <summary>
    /// Die Eingabe ist gültig.
    /// </summary>
    None,

    /// <summary>
    /// Es wurde nichts eingegeben.
    /// </summary>
    Empty,

    /// <summary>
    /// Die Eingabe ist zu kurz.
    /// </summary>
    TooShort,

    /// <summary>
    /// Die Eingabe ist zu lang.
    /// </summary>
    TooLong,

    /// <summary>
    /// Die Eingabe enthält unzulässige Zeichen (z. B. Steuerzeichen oder Klammern von Markup).
    /// </summary>
    InvalidCharacters,
}

/// <summary>
/// Prüfung und Normalisierung der Adresseingabe, bevor sie an den externen Geokodierungsdienst geht.
/// </summary>
public static class AddressInput
{
    /// <summary>
    /// Die Mindestlänge der normalisierten Eingabe.
    /// </summary>
    public const int MinLength = 3;

    /// <summary>
    /// Die Höchstlänge der normalisierten Eingabe.
    /// </summary>
    public const int MaxLength = 120;

    private const string AllowedPunctuation = ".,-'/()&+#:";

    /// <summary>
    /// Normalisiert die Eingabe (typografische Apostrophe ’ ‘ ʼ werden zu ', Gedanken- und Spiegelstriche – — ‐ ‑ − zu -; Zeilenumbrüche und Mehrfach-Leerzeichen werden zu einem Leerzeichen, Ränder entfernt) und prüft sie.
    /// Erlaubt sind Buchstaben, Ziffern, Leerzeichen und gängige Satzzeichen (Punkt, Komma, Bindestrich, Apostroph, Schrägstrich, Klammern, &amp;, +, #, Doppelpunkt).
    /// </summary>
    /// <param name="input">Die Eingabe des Anwenders.</param>
    /// <param name="normalized">Die normalisierte Eingabe bei Erfolg, sonst eine leere Zeichenfolge.</param>
    /// <returns><see cref="AddressInputError.None"/> bei gültiger Eingabe, sonst der Grund.</returns>
    public static AddressInputError Validate(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return AddressInputError.Empty;
        }

        var builder = new StringBuilder(input.Length);
        var pendingSpace = false;
        foreach (var character in input)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            // Typografische Zeichen der iOS-Tastatur (’ – —) werden vor der Prüfung auf ihre ASCII-Entsprechung abgebildet.
            var mapped = NormalizeTypographic(character);
            if (!char.IsLetterOrDigit(mapped) && !AllowedPunctuation.Contains(mapped))
            {
                return AddressInputError.InvalidCharacters;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(mapped);
        }

        var text = builder.ToString();
        if (!text.Any(char.IsLetterOrDigit))
        {
            return AddressInputError.Empty;
        }

        if (text.Length < MinLength)
        {
            return AddressInputError.TooShort;
        }

        if (text.Length > MaxLength)
        {
            return AddressInputError.TooLong;
        }

        normalized = text;
        return AddressInputError.None;
    }

    private static char NormalizeTypographic(char character)
    {
        return character switch
        {
            '’' or '‘' or 'ʼ' => '\'',
            '–' or '—' or '‐' or '‑' or '−' => '-',
            _ => character,
        };
    }
}
