using System.Globalization;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Umsetzung von <see cref="IVehicleSelectionStore"/> über die gerätelokalen Einstellungen (<see cref="IPreferences"/>).
/// </summary>
public sealed class PreferencesVehicleSelectionStore : IVehicleSelectionStore
{
    /// <summary>
    /// Der Schlüssel der gemerkten Kennung.
    /// </summary>
    public const string Key = "tankbook.selectedVehicleId";

    private readonly IPreferences _preferences;
    private readonly string? _sharedName;

    /// <summary>
    /// Erstellt den Speicher.
    /// </summary>
    /// <param name="preferences">Die gerätelokalen Einstellungen.</param>
    /// <param name="sharedName">Der Bereich der Einstellungen; <see langword="null"/> für den Standardbereich (im Testmodus je Testdatenverzeichnis ein eigener Bereich).</param>
    public PreferencesVehicleSelectionStore(IPreferences preferences, string? sharedName = null)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        _preferences = preferences;
        _sharedName = sharedName;
    }

    /// <inheritdoc />
    public long? Get()
    {
        var value = _preferences.Get<string?>(Key, null, _sharedName);
        return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var vehicleId) ? vehicleId : null;
    }

    /// <inheritdoc />
    public void Set(long vehicleId)
    {
        _preferences.Set(Key, vehicleId.ToString(CultureInfo.InvariantCulture), _sharedName);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _preferences.Remove(Key, _sharedName);
    }
}
