using System.Globalization;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Eine Radiusstufe der Umkreissuche, die als Chip gewählt wird.
/// </summary>
public class RadiusOptionViewModel : ChoiceOptionBase
{
    private readonly Action<RadiusOptionViewModel> _onSelected;

    /// <summary>
    /// Erstellt eine Radiusstufe.
    /// </summary>
    /// <param name="radiusKm">Der Radius in Kilometern.</param>
    /// <param name="onSelected">Wird aufgerufen, wenn der Anwender die Stufe auswählt.</param>
    public RadiusOptionViewModel(int radiusKm, Action<RadiusOptionViewModel> onSelected)
        : base(radiusKm.ToString(CultureInfo.InvariantCulture) + " " + SearchTexts.RadiusUnit, radiusKm.ToString(CultureInfo.InvariantCulture))
    {
        RadiusKm = radiusKm;
        _onSelected = onSelected;
    }

    /// <summary>
    /// Der Radius in Kilometern.
    /// </summary>
    public int RadiusKm { get; }

    /// <inheritdoc />
    protected override void NotifySelected()
    {
        _onSelected(this);
    }
}
