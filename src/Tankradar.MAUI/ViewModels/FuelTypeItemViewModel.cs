using System.Windows.Input;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Eine Zeile der Spritsortenliste mit Auswahlschalter und Schaltflächen zum Verschieben.
/// </summary>
public class FuelTypeItemViewModel : BaseViewModel
{
    private readonly Action<FuelTypeItemViewModel> _onSelectionChanged;
    private bool _isSelected;
    private bool _canMoveUp;
    private bool _canMoveDown;

    /// <summary>
    /// Erstellt eine Zeile.
    /// </summary>
    /// <param name="fuelType">Die Spritsorte der Zeile.</param>
    /// <param name="isSelected">Der anfängliche Auswahlzustand.</param>
    /// <param name="onSelectionChanged">Wird aufgerufen, wenn der Anwender den Auswahlzustand ändert.</param>
    /// <param name="onMove">Wird aufgerufen, wenn der Anwender die Zeile verschiebt (-1 nach oben, 1 nach unten).</param>
    public FuelTypeItemViewModel(
        FuelType fuelType,
        bool isSelected,
        Action<FuelTypeItemViewModel> onSelectionChanged,
        Action<FuelTypeItemViewModel, int> onMove)
    {
        FuelType = fuelType;
        DisplayName = SettingsTexts.GetLabel(fuelType);
        AutomationKey = fuelType.ToString();
        _isSelected = isSelected;
        _onSelectionChanged = onSelectionChanged;
        MoveUpCommand = new Command(() => onMove(this, -1));
        MoveDownCommand = new Command(() => onMove(this, 1));
    }

    /// <summary>
    /// Die Spritsorte der Zeile.
    /// </summary>
    public FuelType FuelType { get; }

    /// <summary>
    /// Der Anzeigename der Spritsorte.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Schlüssel für die AutomationId (Name der Spritsorte).
    /// </summary>
    public string AutomationKey { get; }

    /// <summary>
    /// Gibt an, ob die Spritsorte ausgewählt ist; Änderungen werden an den Besitzer gemeldet.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                _onSelectionChanged(this);
            }
        }
    }

    /// <summary>
    /// Gibt an, ob die Zeile nach oben verschoben werden kann.
    /// </summary>
    public bool CanMoveUp
    {
        get => _canMoveUp;
        internal set => SetProperty(ref _canMoveUp, value);
    }

    /// <summary>
    /// Gibt an, ob die Zeile nach unten verschoben werden kann.
    /// </summary>
    public bool CanMoveDown
    {
        get => _canMoveDown;
        internal set => SetProperty(ref _canMoveDown, value);
    }

    /// <summary>
    /// Verschiebt die Zeile um eine Position nach oben.
    /// </summary>
    public ICommand MoveUpCommand { get; }

    /// <summary>
    /// Verschiebt die Zeile um eine Position nach unten.
    /// </summary>
    public ICommand MoveDownCommand { get; }
}
