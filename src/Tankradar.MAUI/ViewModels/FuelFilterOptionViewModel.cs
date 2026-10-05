using Tankradar.MAUI.Models;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Eine Option des Spritsortenfilters der Suche: „Alle“ oder eine der in den Einstellungen gewählten Sorten.
/// </summary>
public class FuelFilterOptionViewModel : BaseViewModel, IChoiceOption
{
    private readonly Action<FuelFilterOptionViewModel> _onSelected;
    private bool _isSelected;

    /// <summary>
    /// Erstellt eine Filteroption.
    /// </summary>
    /// <param name="fuelType">Die Sorte; <see langword="null"/> für „Alle“.</param>
    /// <param name="onSelected">Wird aufgerufen, wenn der Anwender die Option auswählt.</param>
    public FuelFilterOptionViewModel(FuelType? fuelType, Action<FuelFilterOptionViewModel> onSelected)
    {
        FuelType = fuelType;
        Label = fuelType is { } fuel ? SettingsTexts.GetLabel(fuel) : SearchTexts.FilterAll;
        AutomationKey = fuelType is { } value ? value.ToString() : "All";
        _onSelected = onSelected;
    }

    /// <summary>
    /// Die Sorte; <see langword="null"/> für „Alle“.
    /// </summary>
    public FuelType? FuelType { get; }

    /// <summary>
    /// Der Anzeigetext der Option.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Schlüssel für die AutomationId („All“ oder Name der Sorte).
    /// </summary>
    public string AutomationKey { get; }

    /// <summary>
    /// Gibt an, ob die Option ausgewählt ist; ein Auswählen durch den Anwender meldet die Änderung an den Besitzer.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value) && value)
            {
                _onSelected(this);
            }
        }
    }

    /// <summary>
    /// Setzt den Auswahlzustand, ohne die Änderung zu melden (für Aufbau und Abgleich der Gruppe).
    /// </summary>
    /// <param name="isSelected">Der neue Auswahlzustand.</param>
    internal void SetSelectedSilently(bool isSelected)
    {
        SetProperty(ref _isSelected, isSelected, nameof(IsSelected));
    }
}
