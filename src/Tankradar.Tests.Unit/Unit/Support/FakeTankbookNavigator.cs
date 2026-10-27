using Tankradar.MAUI.Services.Navigation;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Testdouble für <see cref="ITankbookNavigator"/>, das Aufrufe protokolliert.
/// </summary>
public sealed class FakeTankbookNavigator : ITankbookNavigator
{
    /// <summary>
    /// Die Aufrufe von <see cref="OpenVehicleManagementAsync"/> mit dem jeweiligen Wert von <c>startCreate</c>.
    /// </summary>
    public List<bool> VehicleManagementOpened { get; } = [];

    /// <summary>
    /// Gibt an, ob <see cref="OpenVehicleManagementAsync"/> mit einer Ausnahme scheitert.
    /// </summary>
    public bool FailOnOpen { get; set; }

    /// <inheritdoc />
    public Task OpenVehicleManagementAsync(bool startCreate)
    {
        VehicleManagementOpened.Add(startCreate);
        return FailOnOpen
            ? Task.FromException(new InvalidOperationException("Navigation fehlgeschlagen."))
            : Task.CompletedTask;
    }
}
