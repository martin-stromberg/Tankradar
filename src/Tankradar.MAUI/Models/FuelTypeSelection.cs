namespace Tankradar.MAUI.Models;

/// <summary>
/// Eine Spritsorte mit ihrem Auswahlzustand.
/// </summary>
public sealed record FuelTypeSelection
{
    /// <summary>
    /// Erstellt eine Auswahl.
    /// </summary>
    /// <param name="fuelType">Die Spritsorte.</param>
    /// <param name="isSelected">Gibt an, ob die Spritsorte ausgewählt ist.</param>
    public FuelTypeSelection(FuelType fuelType, bool isSelected)
    {
        FuelType = fuelType;
        IsSelected = isSelected;
    }

    /// <summary>
    /// Die Spritsorte.
    /// </summary>
    public FuelType FuelType { get; init; }

    /// <summary>
    /// Gibt an, ob die Spritsorte ausgewählt ist.
    /// </summary>
    public bool IsSelected { get; init; }
}
