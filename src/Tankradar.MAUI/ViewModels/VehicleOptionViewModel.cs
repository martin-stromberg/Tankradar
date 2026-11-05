using System.Globalization;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Chip eines Fahrzeugs in der Auswahl im Kopf des Tankbuchs; zeigt nur den Namen, nie die Kennung.
/// </summary>
public sealed class VehicleOptionViewModel : ChoiceOptionBase
{
    private readonly Action<VehicleOptionViewModel> _onSelected;

    /// <summary>
    /// Erstellt den Chip.
    /// </summary>
    /// <param name="vehicle">Das Fahrzeug.</param>
    /// <param name="index">Die Position in der Liste nach Namen (Schlüssel der AutomationId).</param>
    /// <param name="onSelected">Wird aufgerufen, wenn der Nutzer das Fahrzeug auswählt.</param>
    public VehicleOptionViewModel(Vehicle vehicle, int index, Action<VehicleOptionViewModel> onSelected)
        : base(vehicle?.Name ?? throw new ArgumentNullException(nameof(vehicle)), index.ToString(CultureInfo.InvariantCulture))
    {
        Vehicle = vehicle;
        Hint = TankbookTexts.FormatSelectVehicleHint(vehicle.Name);
        _onSelected = onSelected;
    }

    /// <summary>
    /// Das Fahrzeug.
    /// </summary>
    public Vehicle Vehicle { get; }

    /// <summary>
    /// Hinweis für die Sprachausgabe, z. B. „Fahrzeug „Golf“ auswählen“; der vorgelesene Name bleibt der sichtbare Text.
    /// </summary>
    public string Hint { get; }

    /// <inheritdoc />
    protected override void NotifySelected()
    {
        _onSelected(this);
    }
}
