using System.Text;

namespace Tankradar.MAUI.Services.Export;

/// <summary>
/// Kodiert Text für die PDF-Grundschriften (<c>WinAnsiEncoding</c>): Zeichen außerhalb des Zeichensatzes werden durch „?“ ersetzt, Klammern, Rückstrich
/// und Zeichen ab 128 werden als Escape-Folge geschrieben, sodass die Datei rein aus ASCII besteht.
/// </summary>
public static class PdfTextEncoder
{
    /// <summary>
    /// Das Ersatzzeichen für nicht abbildbare Zeichen: das Fragezeichen.
    /// </summary>
    public const byte Replacement = 63;

    private static readonly Dictionary<int, byte> SpecialCodes = new()
    {
        [0x20AC] = 0x80,
        [0x201A] = 0x82,
        [0x0192] = 0x83,
        [0x201E] = 0x84,
        [0x2026] = 0x85,
        [0x2020] = 0x86,
        [0x2021] = 0x87,
        [0x02C6] = 0x88,
        [0x2030] = 0x89,
        [0x0160] = 0x8A,
        [0x2039] = 0x8B,
        [0x0152] = 0x8C,
        [0x017D] = 0x8E,
        [0x2018] = 0x91,
        [0x2019] = 0x92,
        [0x201C] = 0x93,
        [0x201D] = 0x94,
        [0x2022] = 0x95,
        [0x2013] = 0x96,
        [0x2014] = 0x97,
        [0x02DC] = 0x98,
        [0x2122] = 0x99,
        [0x0161] = 0x9A,
        [0x203A] = 0x9B,
        [0x0153] = 0x9C,
        [0x017E] = 0x9E,
        [0x0178] = 0x9F,
    };

    /// <summary>
    /// Wandelt Text in Bytes der WinAnsi-Kodierung um.
    /// </summary>
    /// <param name="text">Der Text.</param>
    /// <returns>Die Bytes; Steuerzeichen werden zu Leerzeichen, nicht abbildbare Zeichen zu „?“.</returns>
    public static byte[] ToWinAnsi(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var bytes = new List<byte>(text.Length);
        foreach (var rune in text.EnumerateRunes())
        {
            bytes.Add(Map(rune.Value));
        }

        return [.. bytes];
    }

    /// <summary>
    /// Wandelt Text in eine PDF-Zeichenkette in runden Klammern um, z. B. <c>(K\344se)</c>.
    /// </summary>
    /// <param name="text">Der Text.</param>
    /// <returns>Die Zeichenkette aus ASCII-Zeichen.</returns>
    public static string ToLiteral(string text)
    {
        var builder = new StringBuilder("(");
        foreach (var value in ToWinAnsi(text))
        {
            if (value is (byte)'(' or (byte)')' or (byte)'\\')
            {
                builder.Append('\\').Append((char)value);
            }
            else if (value >= 0x80)
            {
                builder.Append('\\').Append(Convert.ToString(value, 8));
            }
            else
            {
                builder.Append((char)value);
            }
        }

        return builder.Append(')').ToString();
    }

    private static byte Map(int codePoint)
    {
        if (codePoint < 0x20 || codePoint == 0x7F)
        {
            return (byte)' ';
        }

        if (codePoint < 0x7F || (codePoint >= 0xA0 && codePoint <= 0xFF))
        {
            return (byte)codePoint;
        }

        return SpecialCodes.TryGetValue(codePoint, out var code) ? code : Replacement;
    }
}
