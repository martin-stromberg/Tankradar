using System.Windows.Input;
using Tankradar.MAUI.Resources.Texts;

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

    /// <summary>
    /// Befehl, der die Option auswählt (für Chips und Schaltflächen).
    /// </summary>
    ICommand SelectCommand { get; }

    /// <summary>
    /// Zugänglichkeitshinweis zum Auswahlzustand („Ausgewählt“ oder leer).
    /// </summary>
    string SelectionHint { get; }
}

/// <summary>
/// Eine auswählbare Option einer Einstellungsgruppe (z. B. eine Standortnutzung) mit Anzeigetext.
/// </summary>
/// <typeparam name="TValue">Der Typ des Werts der Option.</typeparam>
public class ChoiceOptionViewModel<TValue> : ChoiceOptionBase
    where TValue : struct, Enum
{
    private readonly Action<ChoiceOptionViewModel<TValue>> _onSelected;

    /// <summary>
    /// Erstellt eine Option.
    /// </summary>
    /// <param name="value">Der Wert der Option.</param>
    /// <param name="label">Der Anzeigetext.</param>
    /// <param name="onSelected">Wird aufgerufen, wenn der Anwender die Option auswählt.</param>
    public ChoiceOptionViewModel(TValue value, string label, Action<ChoiceOptionViewModel<TValue>> onSelected)
        : base(label, value.ToString())
    {
        Value = value;
        _onSelected = onSelected;
    }

    /// <summary>
    /// Der Wert der Option.
    /// </summary>
    public TValue Value { get; }

    /// <inheritdoc />
    protected override void NotifySelected()
    {
        _onSelected(this);
    }
}
