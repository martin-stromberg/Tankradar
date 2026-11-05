using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.ViewModels;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Erzeugt das ViewModel der Einrichtung beim ersten Start mit einem <see cref="FakeSettingsService"/>.
/// </summary>
public static class SetupViewModelFactory
{
    /// <summary>
    /// Erstellt das ViewModel der Einrichtung.
    /// </summary>
    /// <param name="fake">Der Einstellungsdienst, über den gespeichert wird.</param>
    /// <returns>Das ViewModel.</returns>
    public static SetupViewModel CreateSetupViewModel(FakeSettingsService fake)
    {
        return new SetupViewModel(fake, NullLogger<SetupViewModel>.Instance);
    }
}
