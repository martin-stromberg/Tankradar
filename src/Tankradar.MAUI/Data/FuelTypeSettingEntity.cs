namespace Tankradar.MAUI.Data;

/// <summary>
/// Tabelle <c>FuelTypeSettings</c>: eine Zeile je Spritsorte mit Reihenfolge und Auswahlzustand.
/// </summary>
public class FuelTypeSettingEntity
{
    /// <summary>
    /// Primärschlüssel: Name des <see cref="Models.FuelType"/>-Werts. Unbekannte Schlüssel bleiben erhalten.
    /// </summary>
    public string FuelTypeKey { get; set; } = string.Empty;

    /// <summary>
    /// Position in der Reihenfolge der Spritsorten (0 = oberste).
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gibt an, ob die Spritsorte ausgewählt ist.
    /// </summary>
    public bool IsSelected { get; set; }
}
