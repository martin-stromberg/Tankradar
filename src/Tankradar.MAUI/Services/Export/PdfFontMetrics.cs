namespace Tankradar.MAUI.Services.Export;

/// <summary>
/// Zeichenbreiten der PDF-Grundschriften Helvetica und Helvetica-Bold (Adobe-Core-14-Metriken, Einheit: Tausendstel der Schriftgröße) für Ausrichtung,
/// Kürzen und Umbruch. Die Zeichen 32 bis 126 stammen aus den Schriftmetriken; Akzentbuchstaben übernehmen die Breite des Grundbuchstabens,
/// Sonderzeichen sind einzeln hinterlegt.
/// </summary>
public static class PdfFontMetrics
{
    private const string Ellipsis = "…";
    private const int DefaultWidth = 556;

    private static readonly int[] RegularAscii =
    [
        278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556,
        1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556,
        333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556,
        556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584,
    ];

    private static readonly int[] BoldAscii =
    [
        278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 333, 333, 584, 584, 584, 611,
        975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556,
        333, 556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611,
        611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500, 389, 280, 389, 584,
    ];

    private static readonly Dictionary<int, (int Regular, int Bold)> Extended = new()
    {
        [0x80] = (556, 556),
        [0x82] = (222, 278),
        [0x83] = (556, 556),
        [0x84] = (333, 500),
        [0x85] = (1000, 1000),
        [0x86] = (556, 556),
        [0x87] = (556, 556),
        [0x88] = (333, 333),
        [0x89] = (1000, 1000),
        [0x8A] = (667, 667),
        [0x8B] = (333, 333),
        [0x8C] = (1000, 1000),
        [0x8E] = (611, 611),
        [0x91] = (222, 278),
        [0x92] = (222, 278),
        [0x93] = (333, 500),
        [0x94] = (333, 500),
        [0x95] = (350, 350),
        [0x96] = (556, 556),
        [0x97] = (1000, 1000),
        [0x98] = (333, 333),
        [0x99] = (1000, 1000),
        [0x9A] = (500, 556),
        [0x9B] = (333, 333),
        [0x9C] = (944, 944),
        [0x9E] = (500, 500),
        [0x9F] = (667, 667),
        [0xA0] = (278, 278),
        [0xA1] = (333, 333),
        [0xA2] = (556, 556),
        [0xA3] = (556, 556),
        [0xA4] = (556, 556),
        [0xA5] = (556, 556),
        [0xA6] = (260, 280),
        [0xA7] = (556, 556),
        [0xA8] = (333, 333),
        [0xA9] = (737, 737),
        [0xAA] = (370, 370),
        [0xAB] = (556, 556),
        [0xAC] = (584, 584),
        [0xAD] = (333, 333),
        [0xAE] = (737, 737),
        [0xAF] = (333, 333),
        [0xB0] = (400, 400),
        [0xB1] = (584, 584),
        [0xB2] = (333, 333),
        [0xB3] = (333, 333),
        [0xB4] = (333, 333),
        [0xB5] = (556, 611),
        [0xB6] = (537, 556),
        [0xB7] = (278, 278),
        [0xB8] = (333, 333),
        [0xB9] = (333, 333),
        [0xBA] = (365, 365),
        [0xBB] = (556, 556),
        [0xBC] = (834, 834),
        [0xBD] = (834, 834),
        [0xBE] = (834, 834),
        [0xBF] = (611, 611),
        [0xC6] = (1000, 1000),
        [0xD0] = (722, 722),
        [0xD7] = (584, 584),
        [0xD8] = (778, 778),
        [0xDE] = (667, 667),
        [0xDF] = (611, 611),
        [0xE6] = (889, 889),
        [0xF0] = (556, 611),
        [0xF7] = (584, 584),
        [0xF8] = (611, 611),
        [0xFE] = (556, 611),
    };

    // Grundbuchstaben der Akzentbuchstaben 0xC0 bis 0xFF (Sonderfälle stehen in Extended).
    private const string BaseLetters = "AAAAAAACEEEEIIIIDNOOOOOxOUUUUYTsaaaaaaaceeeeiiiidnooooo/ouuuuyty";

    /// <summary>
    /// Liefert die Breite eines Zeichens der WinAnsi-Kodierung in Tausendsteln der Schriftgröße.
    /// </summary>
    /// <param name="winAnsi">Das Zeichen als Byte der WinAnsi-Kodierung.</param>
    /// <param name="bold">Gibt an, ob die fette Schrift gemeint ist.</param>
    /// <returns>Die Breite.</returns>
    public static int GetWidth(byte winAnsi, bool bold)
    {
        if (winAnsi >= 32 && winAnsi <= 126)
        {
            return (bold ? BoldAscii : RegularAscii)[winAnsi - 32];
        }

        if (Extended.TryGetValue(winAnsi, out var width))
        {
            return bold ? width.Bold : width.Regular;
        }

        if (winAnsi >= 0xC0)
        {
            return GetWidth((byte)BaseLetters[winAnsi - 0xC0], bold);
        }

        return DefaultWidth;
    }

    /// <summary>
    /// Misst die Breite eines Textes in Punkt.
    /// </summary>
    /// <param name="text">Der Text.</param>
    /// <param name="bold">Gibt an, ob die fette Schrift gemeint ist.</param>
    /// <param name="fontSize">Die Schriftgröße in Punkt.</param>
    /// <returns>Die Breite in Punkt.</returns>
    public static double Measure(string text, bool bold, double fontSize)
    {
        ArgumentNullException.ThrowIfNull(text);
        var total = 0;
        foreach (var value in PdfTextEncoder.ToWinAnsi(text))
        {
            total += GetWidth(value, bold);
        }

        return total * fontSize / 1000.0;
    }

    /// <summary>
    /// Kürzt einen Text mit einem Auslassungszeichen, bis er in die Breite passt.
    /// </summary>
    /// <param name="text">Der Text.</param>
    /// <param name="bold">Gibt an, ob die fette Schrift gemeint ist.</param>
    /// <param name="fontSize">Die Schriftgröße in Punkt.</param>
    /// <param name="maxWidth">Die größte Breite in Punkt.</param>
    /// <returns>Der Text, unverändert wenn er passt.</returns>
    public static string Truncate(string text, bool bold, double fontSize, double maxWidth)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (Measure(text, bold, fontSize) <= maxWidth)
        {
            return text;
        }

        var runes = text.EnumerateRunes().Select(rune => rune.ToString()).ToList();
        var low = 0;
        var high = runes.Count - 1;
        var best = string.Empty;
        while (low <= high)
        {
            var middle = (low + high) / 2;
            var candidate = string.Concat(runes.Take(middle)).TrimEnd() + Ellipsis;
            if (Measure(candidate, bold, fontSize) <= maxWidth)
            {
                best = candidate;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return best;
    }

    /// <summary>
    /// Bricht einen Text an Leerzeichen so um, dass jede Zeile in die Breite passt; zu lange Wörter werden gekürzt.
    /// </summary>
    /// <param name="text">Der Text.</param>
    /// <param name="bold">Gibt an, ob die fette Schrift gemeint ist.</param>
    /// <param name="fontSize">Die Schriftgröße in Punkt.</param>
    /// <param name="maxWidth">Die größte Breite einer Zeile in Punkt.</param>
    /// <returns>Die Zeilen; mindestens eine.</returns>
    public static IReadOnlyList<string> Wrap(string text, bool bold, double fontSize, double maxWidth)
    {
        ArgumentNullException.ThrowIfNull(text);
        var lines = new List<string>();
        var current = string.Empty;
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = current.Length == 0 ? word : current + " " + word;
            if (Measure(candidate, bold, fontSize) <= maxWidth)
            {
                current = candidate;
                continue;
            }

            if (current.Length > 0)
            {
                lines.Add(current);
            }

            current = Truncate(word, bold, fontSize, maxWidth);
        }

        lines.Add(current);
        return lines;
    }
}
