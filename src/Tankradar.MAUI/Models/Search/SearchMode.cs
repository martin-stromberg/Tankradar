namespace Tankradar.MAUI.Models.Search;

/// <summary>
/// Die Art der Umkreissuche: am aktuellen Standort oder rund um eine eingegebene Adresse.
/// </summary>
public enum SearchMode
{
    /// <summary>
    /// Suche am aktuellen Standort (benötigt die Standortnutzung).
    /// </summary>
    CurrentLocation,

    /// <summary>
    /// Suche rund um eine eingegebene Adresse, einen Ort oder eine Postleitzahl (benötigt keinen Standort).
    /// </summary>
    Address,

    /// <summary>
    /// Suche entlang einer Route von einem Start zu einem Ziel (Start: aktueller Standort oder Adresse).
    /// </summary>
    Route,
}
