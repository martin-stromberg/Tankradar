using System.Text;
using System.Text.RegularExpressions;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Liest Struktur und Text einer vom <see cref="MAUI.Services.Export.PdfDocumentBuilder"/> erzeugten PDF-Datei (unkomprimierte Inhaltsströme, ohne Fremdpaket).
/// </summary>
public sealed partial class PdfTestReader
{
    private static readonly Dictionary<byte, char> WinAnsiSpecials = new()
    {
        [0x80] = '€',
        [0x85] = '…',
        [0x95] = '•',
        [0x96] = '–',
        [0x97] = '—',
        [0x99] = '™',
    };

    /// <summary>
    /// Liest die Datei.
    /// </summary>
    /// <param name="bytes">Die Bytes der Datei.</param>
    public PdfTestReader(byte[] bytes)
    {
        Bytes = bytes;
        Text = Encoding.Latin1.GetString(bytes);
        foreach (Match match in ObjectRegex().Matches(Text))
        {
            Objects[int.Parse(match.Groups["n"].Value, System.Globalization.CultureInfo.InvariantCulture)] = match.Groups["body"].Value;
            ObjectOffsets[int.Parse(match.Groups["n"].Value, System.Globalization.CultureInfo.InvariantCulture)] = match.Index;
        }

        var pagesObject = Objects.Values.Single(body => body.Contains("/Type /Pages", StringComparison.Ordinal));
        var kids = KidsRegex().Match(pagesObject).Groups["kids"].Value;
        foreach (Match kid in ReferenceRegex().Matches(kids))
        {
            var pageNumber = int.Parse(kid.Groups["n"].Value, System.Globalization.CultureInfo.InvariantCulture);
            PageObjectNumbers.Add(pageNumber);
            var contentNumber = int.Parse(ContentsRegex().Match(Objects[pageNumber]).Groups["n"].Value, System.Globalization.CultureInfo.InvariantCulture);
            var content = Objects[contentNumber];
            var start = content.IndexOf("stream\n", StringComparison.Ordinal) + "stream\n".Length;
            var end = content.LastIndexOf("endstream", StringComparison.Ordinal);
            var stream = content[start..end];
            PageStreams.Add(stream);
            Pages.Add(TextRegex().Matches(stream).Select(match => Decode(match.Groups["t"].Value)).ToList());
        }
    }

    /// <summary>
    /// Die Bytes der Datei.
    /// </summary>
    public byte[] Bytes { get; }

    /// <summary>
    /// Die Datei als Text (jedes Byte ein Zeichen).
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Die Objekte nach Nummer.
    /// </summary>
    public Dictionary<int, string> Objects { get; } = [];

    /// <summary>
    /// Die Position von „n 0 obj“ je Objektnummer.
    /// </summary>
    public Dictionary<int, int> ObjectOffsets { get; } = [];

    /// <summary>
    /// Die Objektnummern der Seiten in Reihenfolge.
    /// </summary>
    public List<int> PageObjectNumbers { get; } = [];

    /// <summary>
    /// Die Inhaltsströme der Seiten.
    /// </summary>
    public List<string> PageStreams { get; } = [];

    /// <summary>
    /// Die Textstellen je Seite in Zeichenreihenfolge.
    /// </summary>
    public List<List<string>> Pages { get; } = [];

    /// <summary>
    /// Alle Textstellen aller Seiten.
    /// </summary>
    public IEnumerable<string> AllText
    {
        get { return Pages.SelectMany(page => page); }
    }

    /// <summary>
    /// Liest die Textstellen einer Seite mit Position, Schrift und Größe.
    /// </summary>
    /// <param name="pageIndex">Der nullbasierte Seitenindex.</param>
    /// <returns>Die Textstellen in Zeichenreihenfolge.</returns>
    public List<PlacedText> ReadPlacedTexts(int pageIndex)
    {
        return PlacedTextRegex().Matches(PageStreams[pageIndex]).Select(match => new PlacedText(
            Decode(match.Groups["t"].Value),
            match.Groups["f"].Value == "F2",
            double.Parse(match.Groups["s"].Value, System.Globalization.CultureInfo.InvariantCulture),
            double.Parse(match.Groups["x"].Value, System.Globalization.CultureInfo.InvariantCulture),
            double.Parse(match.Groups["y"].Value, System.Globalization.CultureInfo.InvariantCulture))).ToList();
    }

    /// <summary>
    /// Liest die Offsets der Querverweistabelle (Objekt 1 bis n).
    /// </summary>
    /// <returns>Die Offsets.</returns>
    public List<int> ReadXrefOffsets()
    {
        var start = int.Parse(StartXrefRegex().Match(Text).Groups["o"].Value, System.Globalization.CultureInfo.InvariantCulture);
        var table = Text[start..];
        var lines = table.Split('\n');
        var count = int.Parse(lines[1].Split(' ')[1], System.Globalization.CultureInfo.InvariantCulture);
        return lines.Skip(3).Take(count - 1).Select(line => int.Parse(line[..10], System.Globalization.CultureInfo.InvariantCulture)).ToList();
    }

    /// <summary>
    /// Der Wert hinter <c>startxref</c>.
    /// </summary>
    /// <returns>Die Position der Querverweistabelle.</returns>
    public int ReadStartXref()
    {
        return int.Parse(StartXrefRegex().Match(Text).Groups["o"].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string Decode(string literal)
    {
        var builder = new StringBuilder();
        for (var index = 0; index < literal.Length; index++)
        {
            var character = literal[index];
            if (character != '\\')
            {
                builder.Append(character);
                continue;
            }

            index++;
            if (char.IsDigit(literal[index]))
            {
                var digits = literal[index..Math.Min(index + 3, literal.Length)];
                var value = (byte)Convert.ToInt32(digits, 8);
                builder.Append(WinAnsiSpecials.TryGetValue(value, out var special) ? special : (char)value);
                index += 2;
            }
            else
            {
                builder.Append(literal[index]);
            }
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"(?<n>\d+) 0 obj\n(?<body>.*?)\nendobj", RegexOptions.Singleline)]
    private static partial Regex ObjectRegex();

    [GeneratedRegex(@"/Kids \[(?<kids>[^\]]*)\]")]
    private static partial Regex KidsRegex();

    [GeneratedRegex(@"(?<n>\d+) 0 R")]
    private static partial Regex ReferenceRegex();

    [GeneratedRegex(@"/Contents (?<n>\d+) 0 R")]
    private static partial Regex ContentsRegex();

    [GeneratedRegex(@"\((?<t>(?:\\.|[^\\)])*)\) Tj")]
    private static partial Regex TextRegex();

    [GeneratedRegex(@"BT /(?<f>F\d) (?<s>[\d.]+) Tf [\d.]+ g (?<x>-?[\d.]+) (?<y>-?[\d.]+) Td \((?<t>(?:\\.|[^\\)])*)\) Tj ET")]
    private static partial Regex PlacedTextRegex();

    [GeneratedRegex(@"startxref\n(?<o>\d+)\n%%EOF")]
    private static partial Regex StartXrefRegex();
}

/// <summary>
/// Eine Textstelle einer PDF-Seite.
/// </summary>
public sealed class PlacedText
{
    /// <summary>
    /// Erzeugt die Textstelle.
    /// </summary>
    /// <param name="text">Der Text.</param>
    /// <param name="bold">Gibt an, ob die fette Schrift verwendet wird.</param>
    /// <param name="size">Die Schriftgröße in Punkt.</param>
    /// <param name="x">Der linke Rand in Punkt.</param>
    /// <param name="y">Die Grundlinie in Punkt von der Unterkante der Seite.</param>
    public PlacedText(string text, bool bold, double size, double x, double y)
    {
        Text = text;
        Bold = bold;
        Size = size;
        X = x;
        Y = y;
    }

    /// <summary>
    /// Der Text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gibt an, ob die fette Schrift verwendet wird.
    /// </summary>
    public bool Bold { get; }

    /// <summary>
    /// Die Schriftgröße in Punkt.
    /// </summary>
    public double Size { get; }

    /// <summary>
    /// Der linke Rand in Punkt.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Die Grundlinie in Punkt von der Unterkante der Seite.
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Die Breite des Textes in Punkt.
    /// </summary>
    public double Width
    {
        get { return MAUI.Services.Export.PdfFontMetrics.Measure(Text, Bold, Size); }
    }
}
