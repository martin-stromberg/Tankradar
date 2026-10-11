using System.Globalization;
using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Chip einer bekannten Tankstelle im Formular eines Tank- oder Ladevorgangs; zeigt nur den Namen (bei Namensgleichheit samt Ort), nie die Kennung.
/// </summary>
public sealed class KnownStationOptionViewModel : ChoiceOptionBase
{
    private readonly Action<KnownStationOptionViewModel> _onSelected;

    /// <summary>
    /// Erstellt den Chip.
    /// </summary>
    /// <param name="station">Die bekannte Tankstelle.</param>
    /// <param name="label">Der Anzeigetext.</param>
    /// <param name="index">Die Position in der Liste (Schlüssel der AutomationId).</param>
    /// <param name="onSelected">Wird aufgerufen, wenn der Nutzer die Tankstelle auswählt.</param>
    public KnownStationOptionViewModel(KnownStation station, string label, int index, Action<KnownStationOptionViewModel> onSelected)
        : base(label, index.ToString(CultureInfo.InvariantCulture))
    {
        ArgumentNullException.ThrowIfNull(station);
        Station = station;
        _onSelected = onSelected;
    }

    /// <summary>
    /// Die bekannte Tankstelle.
    /// </summary>
    public KnownStation Station { get; }

    /// <inheritdoc />
    protected override void NotifySelected()
    {
        _onSelected(this);
    }
}
