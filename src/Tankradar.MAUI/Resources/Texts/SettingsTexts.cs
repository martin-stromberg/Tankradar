using Tankradar.MAUI.Models;

namespace Tankradar.MAUI.Resources.Texts;

/// <summary>
/// Zentrale deutsche UI-Texte der Optionen-Seite einschließlich der Anzeigenamen je Enum-Wert.
/// </summary>
public static class SettingsTexts
{
    /// <summary>
    /// Überschrift der Karte „Spritsorten“.
    /// </summary>
    public const string FuelTypesHeading = "Spritsorten";

    /// <summary>
    /// Hinweis zur Karte „Spritsorten“.
    /// </summary>
    public const string FuelTypesHint = "Wähle die Sorten aus, die dich interessieren, und lege mit den Pfeilen die Reihenfolge fest.";

    /// <summary>
    /// Überschrift der Karte „Standort“.
    /// </summary>
    public const string GpsHeading = "Standort und GPS";

    /// <summary>
    /// Hinweis zur Karte „Standort“.
    /// </summary>
    public const string GpsHint = "Voreingestellt ist „Nur bei Nutzung“: Der Standort wird nur verwendet, solange die App geöffnet ist.";

    /// <summary>
    /// Überschrift der Karte „Standardansicht“.
    /// </summary>
    public const string ViewHeading = "Standardansicht der Suchergebnisse";

    /// <summary>
    /// Überschrift der Karte „Standardsortierung“.
    /// </summary>
    public const string SortHeading = "Standardsortierung";

    /// <summary>
    /// Meldung, wenn die Einstellungen nicht geladen werden konnten.
    /// </summary>
    public const string LoadFailed = "Die Einstellungen konnten nicht geladen werden.";

    /// <summary>
    /// Meldung, wenn eine Einstellung nicht gespeichert werden konnte.
    /// </summary>
    public const string SaveFailed = "Die Einstellung konnte nicht gespeichert werden.";

    /// <summary>
    /// Meldung, wenn die letzte ausgewählte Spritsorte abgewählt werden soll.
    /// </summary>
    public const string LastFuelTypeRequired = "Mindestens eine Spritsorte muss ausgewählt bleiben.";

    /// <summary>
    /// Beschreibung für Screenreader der Schaltfläche zum Verschieben nach oben.
    /// </summary>
    public const string MoveUpDescription = "Nach oben verschieben";

    /// <summary>
    /// Beschreibung für Screenreader der Schaltfläche zum Verschieben nach unten.
    /// </summary>
    public const string MoveDownDescription = "Nach unten verschieben";

    /// <summary>
    /// Liefert den Anzeigenamen einer Spritsorte.
    /// </summary>
    /// <param name="fuelType">Die Spritsorte.</param>
    /// <returns>Der Anzeigename.</returns>
    public static string GetLabel(FuelType fuelType)
    {
        return fuelType switch
        {
            FuelType.SuperE5 => "Super E5",
            FuelType.SuperE10 => "Super E10",
            FuelType.Diesel => "Diesel",
            _ => fuelType.ToString(),
        };
    }

    /// <summary>
    /// Liefert den Anzeigenamen einer Standortnutzung.
    /// </summary>
    /// <param name="gpsUsage">Die Standortnutzung.</param>
    /// <returns>Der Anzeigename.</returns>
    public static string GetLabel(GpsUsage gpsUsage)
    {
        return gpsUsage switch
        {
            GpsUsage.Always => "Immer",
            GpsUsage.WhileInUse => "Nur bei Nutzung",
            GpsUsage.Never => "Nie",
            _ => gpsUsage.ToString(),
        };
    }

    /// <summary>
    /// Liefert den Anzeigenamen einer Ergebnisansicht.
    /// </summary>
    /// <param name="resultView">Die Ergebnisansicht.</param>
    /// <returns>Der Anzeigename.</returns>
    public static string GetLabel(ResultView resultView)
    {
        return resultView switch
        {
            ResultView.List => "Liste",
            ResultView.Map => "Karte",
            _ => resultView.ToString(),
        };
    }

    /// <summary>
    /// Liefert den Anzeigenamen einer Ergebnissortierung.
    /// </summary>
    /// <param name="resultSortOrder">Die Ergebnissortierung.</param>
    /// <returns>Der Anzeigename.</returns>
    public static string GetLabel(ResultSortOrder resultSortOrder)
    {
        return resultSortOrder switch
        {
            ResultSortOrder.Price => "Preis",
            ResultSortOrder.Distance => "Entfernung",
            ResultSortOrder.Name => "Name",
            _ => resultSortOrder.ToString(),
        };
    }
}
