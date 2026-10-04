namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Nicht generische Sicht auf eine auswählbare Option (für kompilierte Bindungen in XAML-Vorlagen).
/// </summary>
public interface IChoiceOption
{
    /// <summary>
    /// Der Anzeigetext der Option.
    /// </summary>
    string Label { get; }

    /// <summary>
    /// Schlüssel für die AutomationId (Name des Enum-Werts).
    /// </summary>
    string AutomationKey { get; }

    /// <summary>
    /// Gibt an, ob die Option ausgewählt ist.
    /// </summary>
    bool IsSelected { get; set; }
}

/// <summary>
/// Eine auswählbare Option einer Einstellungsgruppe (z. B. eine Standortnutzung) mit Anzeigetext.
/// </summary>
/// <typeparam name="TValue">Der Typ des Werts der Option.</typeparam>
public class ChoiceOptionViewModel<TValue> : BaseViewModel, IChoiceOption
    where TValue : struct, Enum
{
    private readonly Action<ChoiceOptionViewModel<TValue>> _onSelected;
    private bool _isSelected;

    /// <summary>
    /// Erstellt eine Option.
    /// </summary>
    /// <param name="value">Der Wert der Option.</param>
    /// <param name="label">Der Anzeigetext.</param>
    /// <param name="onSelected">Wird aufgerufen, wenn der Anwender die Option auswählt.</param>
    public ChoiceOptionViewModel(TValue value, string label, Action<ChoiceOptionViewModel<TValue>> onSelected)
    {
        Value = value;
        Label = label;
        AutomationKey = value.ToString();
        _onSelected = onSelected;
    }

    /// <summary>
    /// Der Wert der Option.
    /// </summary>
    public TValue Value { get; }

    /// <summary>
    /// Der Anzeigetext der Option.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Schlüssel für die AutomationId (Name des Enum-Werts).
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
    /// Setzt den Auswahlzustand, ohne die Änderung zu melden (für Laden und Abgleich der Gruppe).
    /// </summary>
    /// <param name="isSelected">Der neue Auswahlzustand.</param>
    internal void SetSelectedSilently(bool isSelected)
    {
        SetProperty(ref _isSelected, isSelected, nameof(IsSelected));
    }
}
