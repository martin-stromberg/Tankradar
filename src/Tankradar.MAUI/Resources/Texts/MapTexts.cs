using System.Globalization;
using Tankradar.MAUI.Models.Map;

namespace Tankradar.MAUI.Resources.Texts;

/// <summary>
/// Zentrale deutsche UI-Texte der Kartenansicht (Quellenangabe, Bedienelemente, Preisniveaus, Zähler).
/// </summary>
public static class MapTexts
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>
    /// Quellenangabe der Kartendaten (Nutzungsbedingung von OpenStreetMap).
    /// </summary>
    public const string Attribution = "© OpenStreetMap-Mitwirkende";

    /// <summary>
    /// Adresse der Urheber- und Lizenzhinweise von OpenStreetMap, auf die die Quellenangabe verweist.
    /// </summary>
    public const string AttributionUrl = "https://www.openstreetmap.org/copyright";

    /// <summary>
    /// Hinweis der Quellenangabe für Bedienhilfen (die Angabe ist ein Link).
    /// </summary>
    public const string AttributionHint = "Öffnet die Urheber- und Lizenzhinweise von OpenStreetMap im Browser";

    /// <summary>
    /// Überschrift der Umschaltung zwischen Liste und Karte.
    /// </summary>
    public const string ViewHeading = "Ansicht";

    /// <summary>
    /// Beschriftung „Vergrößern“.
    /// </summary>
    public const string ZoomIn = "Vergrößern";

    /// <summary>
    /// Beschriftung „Verkleinern“.
    /// </summary>
    public const string ZoomOut = "Verkleinern";

    /// <summary>
    /// Beschriftung der Schaltfläche, die den Kartenausschnitt wieder auf alle Ergebnisse einpasst.
    /// </summary>
    public const string Recenter = "Ausschnitt zurücksetzen";

    /// <summary>
    /// Beschriftung „Ausschnitt nach Norden verschieben“.
    /// </summary>
    public const string PanNorth = "Ausschnitt nach Norden verschieben";

    /// <summary>
    /// Beschriftung „Ausschnitt nach Süden verschieben“.
    /// </summary>
    public const string PanSouth = "Ausschnitt nach Süden verschieben";

    /// <summary>
    /// Beschriftung „Ausschnitt nach Osten verschieben“.
    /// </summary>
    public const string PanEast = "Ausschnitt nach Osten verschieben";

    /// <summary>
    /// Beschriftung „Ausschnitt nach Westen verschieben“.
    /// </summary>
    public const string PanWest = "Ausschnitt nach Westen verschieben";

    /// <summary>
    /// Hinweis, solange noch keine Ergebnisse für die Karte vorliegen.
    /// </summary>
    public const string NoResults = "Noch keine Ergebnisse. Starte eine Suche, um die Tankstellen auf der Karte zu sehen.";

    /// <summary>
    /// Beschreibung der Markierung des eigenen Standorts.
    /// </summary>
    public const string OriginCurrentLocation = "Mein Standort";

    /// <summary>
    /// Beschreibung der Markierung der gesuchten Position.
    /// </summary>
    public const string OriginSearchedPlace = "Gesuchte Position";

    /// <summary>
    /// Überschrift der Farblegende.
    /// </summary>
    public const string LegendHeading = "Preisniveau";

    /// <summary>
    /// Bezeichnung des Preisniveaus „günstigster Preis“.
    /// </summary>
    public const string LevelCheapest = "Günstigster Preis";

    /// <summary>
    /// Bezeichnung des Preisniveaus „oberes Drittel der Preisspanne“.
    /// </summary>
    public const string LevelExpensive = "Hoher Preis";

    /// <summary>
    /// Bezeichnung des Preisniveaus „übrige Preise“.
    /// </summary>
    public const string LevelNormal = "Mittlerer Preis";

    /// <summary>
    /// Bezeichnung einer geschlossenen Tankstelle.
    /// </summary>
    public const string LevelClosed = "Geschlossen";

    /// <summary>
    /// Liefert die Beschreibung der markierten Suchposition.
    /// </summary>
    /// <param name="kind">Die Art der Position.</param>
    /// <returns>Der Text.</returns>
    public static string GetOriginLabel(MapOriginKind kind)
    {
        return kind == MapOriginKind.SearchedPlace ? OriginSearchedPlace : OriginCurrentLocation;
    }

    /// <summary>
    /// Liefert die Bezeichnung eines Preisniveaus (Legende und Bedienhilfen).
    /// </summary>
    /// <param name="level">Das Preisniveau.</param>
    /// <returns>Der Text.</returns>
    public static string GetLevelLabel(PriceLevel level)
    {
        return level switch
        {
            PriceLevel.Cheapest => LevelCheapest,
            PriceLevel.Expensive => LevelExpensive,
            PriceLevel.Closed => LevelClosed,
            _ => LevelNormal,
        };
    }

    /// <summary>
    /// Formatiert den Zähler der Markierungen im Kartenausschnitt („3 von 12 Stationen sichtbar“).
    /// </summary>
    /// <param name="visible">Die Anzahl der Tankstellen im Ausschnitt.</param>
    /// <param name="total">Die Gesamtzahl der Tankstellen des Ergebnisses.</param>
    /// <returns>Der Text.</returns>
    public static string FormatStationCount(int visible, int total)
    {
        var noun = total == 1 ? "Station" : "Stationen";
        return $"{visible.ToString(German)} von {total.ToString(German)} {noun} sichtbar";
    }

    /// <summary>
    /// Formatiert den Zähler, wenn wegen der Obergrenze nicht alle Markierungen im Ausschnitt dargestellt werden („120 von 300 Stationen sichtbar, 100 dargestellt – für alle hineinzoomen“).
    /// </summary>
    /// <param name="visible">Die Anzahl der Tankstellen im Ausschnitt.</param>
    /// <param name="total">Die Gesamtzahl der Tankstellen des Ergebnisses.</param>
    /// <param name="shown">Die Anzahl der dargestellten Markierungen.</param>
    /// <returns>Der Text.</returns>
    public static string FormatStationCountLimited(int visible, int total, int shown)
    {
        return $"{FormatStationCount(visible, total)}, {shown.ToString(German)} dargestellt – für alle hineinzoomen";
    }

    /// <summary>
    /// Formatiert die Zoomstufe („Zoom 14“).
    /// </summary>
    /// <param name="zoom">Die Zoomstufe.</param>
    /// <returns>Der Text.</returns>
    public static string FormatZoom(int zoom)
    {
        return "Zoom " + zoom.ToString(German);
    }

    /// <summary>
    /// Formatiert die Beschreibung einer Markierung für Bedienhilfen („Alpha, Super E5 1,859 Euro pro Liter“).
    /// </summary>
    /// <param name="name">Der Name der Tankstelle.</param>
    /// <param name="fuelLabel">Der Name der Spritsorte.</param>
    /// <param name="spokenPrice">Der Preis als gesprochener Text; leer, wenn kein Preis vorliegt.</param>
    /// <returns>Der Text.</returns>
    public static string FormatMarkerDescription(string name, string fuelLabel, string spokenPrice)
    {
        if (fuelLabel.Length == 0)
        {
            return name;
        }

        return spokenPrice.Length > 0 ? $"{name}, {fuelLabel} {spokenPrice}" : $"{name}, kein Preis für {fuelLabel}";
    }
}
