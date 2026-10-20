using System.Collections.Frozen;
using System.Globalization;
using System.Text;

namespace Tankradar.MAUI.Services.Export;

/// <summary>
/// Bildet den Dateinamen eines Exports aus Fahrzeugname, Datum und Endung.
/// </summary>
public static class TankbookExportFileNamer
{
    /// <summary>
    /// Die größte Länge des Fahrzeugnamens im Dateinamen.
    /// </summary>
    public const int MaxVehicleNameLength = 40;

    /// <summary>
    /// Der Name, der statt eines leeren oder unbrauchbaren Fahrzeugnamens verwendet wird.
    /// </summary>
    public const string FallbackVehicleName = "Fahrzeug";

    // Feste, plattformunabhängige Menge (Windows): Path.GetInvalidFileNameChars() liefert z. B. unter iOS nur \0 und /.
    private static readonly FrozenSet<char> InvalidCharacters = "\0:\"/\\*?<>|".ToFrozenSet();

    /// <summary>
    /// Bildet den Dateinamen <c>Tankbuch_&lt;Fahrzeug&gt;_&lt;JJJJ-MM-TT&gt;.&lt;Endung&gt;</c>.
    /// </summary>
    /// <param name="vehicleName">Der Name des Fahrzeugs.</param>
    /// <param name="date">Das Erstellungsdatum.</param>
    /// <param name="extension">Die Endung ohne Punkt.</param>
    /// <returns>Der Dateiname.</returns>
    public static string Create(string vehicleName, DateTime date, string extension)
    {
        ArgumentNullException.ThrowIfNull(extension);
        return $"Tankbuch_{SanitizeVehicleName(vehicleName)}_{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.{extension}";
    }

    private static string SanitizeVehicleName(string? vehicleName)
    {
        var builder = new StringBuilder();
        foreach (var character in vehicleName ?? string.Empty)
        {
            var replace = char.IsWhiteSpace(character) || char.IsControl(character) || InvalidCharacters.Contains(character) || character is '.' or ',' or ';';
            if (replace)
            {
                if (builder.Length > 0 && builder[^1] != '_')
                {
                    builder.Append('_');
                }
            }
            else
            {
                builder.Append(character);
            }
        }

        var name = builder.ToString().Trim('_');
        if (name.Length > MaxVehicleNameLength)
        {
            name = name[..MaxVehicleNameLength];
            if (char.IsHighSurrogate(name[^1]))
            {
                name = name[..^1];
            }

            name = name.Trim('_');
        }

        return name.Length == 0 ? FallbackVehicleName : name;
    }
}
