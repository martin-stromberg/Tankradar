using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Basisklasse für alle ViewModels der Tankatlas-App mit MVVM-Grundfunktionalität.
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private bool _isBusy;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Der Titel der zugehörigen Seite.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// Gibt an, ob im Hintergrund eine Operation läuft (z. B. Laden von Daten).
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }
    }

    /// <summary>
    /// Kehrwert von <see cref="IsBusy"/> für Bindings, die nur im Ruhezustand aktiv sein sollen.
    /// </summary>
    public bool IsNotBusy => !IsBusy;

    /// <summary>
    /// Wird aufgerufen, wenn die zugehörige Seite sichtbar wird. Wird von abgeleiteten ViewModels überschrieben.
    /// </summary>
    public virtual void OnAppearing()
    {
    }

    /// <summary>
    /// Wird aufgerufen, wenn die zugehörige Seite nicht mehr sichtbar ist (z. B. zum Abmelden von Ereignissen). Wird von abgeleiteten ViewModels überschrieben.
    /// </summary>
    public virtual void OnDisappearing()
    {
    }

    /// <summary>
    /// Setzt den Wert eines Felds und löst <see cref="PropertyChanged"/> aus, wenn sich der Wert geändert hat.
    /// </summary>
    /// <typeparam name="T">Der Typ des Felds und Werts.</typeparam>
    /// <param name="field">Das zu aktualisierende Feld.</param>
    /// <param name="value">Der neue Wert.</param>
    /// <param name="propertyName">Der Name der Eigenschaft; wird automatisch vom Aufrufer ermittelt.</param>
    /// <returns><see langword="true"/>, wenn sich der Wert geändert hat, sonst <see langword="false"/>.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Löst das <see cref="PropertyChanged"/>-Ereignis für die angegebene Eigenschaft aus.
    /// </summary>
    /// <param name="propertyName">Der Name der geänderten Eigenschaft; wird automatisch vom Aufrufer ermittelt.</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
