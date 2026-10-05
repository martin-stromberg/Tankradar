using System.Windows.Input;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Gemeinsame Grundlage auswählbarer Optionen (Chips und Auswahlfelder): Auswahlzustand, Auswahlbefehl und Hinweistext für Bedienhilfen.
/// </summary>
public abstract class ChoiceOptionBase : BaseViewModel, IChoiceOption
{
    private bool _isSelected;

    /// <summary>
    /// Erstellt die Option.
    /// </summary>
    /// <param name="label">Der Anzeigetext.</param>
    /// <param name="automationKey">Der Schlüssel für die AutomationId.</param>
    protected ChoiceOptionBase(string label, string automationKey)
    {
        Label = label;
        AutomationKey = automationKey;
        SelectCommand = new Command(() => IsSelected = true);
    }

    /// <inheritdoc />
    public string Label { get; }

    /// <inheritdoc />
    public string AutomationKey { get; }

    /// <inheritdoc />
    public ICommand SelectCommand { get; }

    /// <inheritdoc />
    public string SelectionHint => IsSelected ? ChoiceTexts.Selected : string.Empty;

    /// <summary>
    /// Gibt an, ob die Option ausgewählt ist; ein Auswählen durch den Anwender meldet die Änderung an den Besitzer.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(SelectionHint));
                if (value)
                {
                    NotifySelected();
                }
            }
        }
    }

    /// <summary>
    /// Setzt den Auswahlzustand, ohne die Änderung zu melden (für Aufbau und Abgleich der Gruppe).
    /// </summary>
    /// <param name="isSelected">Der neue Auswahlzustand.</param>
    internal void SetSelectedSilently(bool isSelected)
    {
        if (SetProperty(ref _isSelected, isSelected, nameof(IsSelected)))
        {
            OnPropertyChanged(nameof(SelectionHint));
        }
    }

    /// <summary>
    /// Meldet dem Besitzer, dass der Anwender die Option ausgewählt hat.
    /// </summary>
    protected abstract void NotifySelected();
}
