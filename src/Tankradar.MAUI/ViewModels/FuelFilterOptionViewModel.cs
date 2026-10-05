using Tankradar.MAUI.Models;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Eine Option des Spritsortenfilters der Suche: „Alle“ oder eine der in den Einstellungen gewählten Sorten.
/// </summary>
public class FuelFilterOptionViewModel : ChoiceOptionBase
{
    private readonly Action<FuelFilterOptionViewModel> _onSelected;

    /// <summary>
    /// Erstellt eine Filteroption.
    /// </summary>
    /// <param name="fuelType">Die Sorte; <see langword="null"/> für „Alle“.</param>
    /// <param name="onSelected">Wird aufgerufen, wenn der Anwender die Option auswählt.</param>
    public FuelFilterOptionViewModel(FuelType? fuelType, Action<FuelFilterOptionViewModel> onSelected)
        : base(
            fuelType is { } fuel ? SettingsTexts.GetLabel(fuel) : SearchTexts.FilterAll,
            fuelType is { } value ? value.ToString() : "All")
    {
        FuelType = fuelType;
        _onSelected = onSelected;
    }

    /// <summary>
    /// Die Sorte; <see langword="null"/> für „Alle“.
    /// </summary>
    public FuelType? FuelType { get; }

    /// <inheritdoc />
    protected override void NotifySelected()
    {
        _onSelected(this);
    }
}
