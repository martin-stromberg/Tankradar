using System.Globalization;
using Tankradar.MAUI.Models.Search;
using Tankradar.TestSupport;

namespace Tankradar.MAUI.Services.Location;

/// <summary>
/// Wählt den Standortdienst: Im Testmodus (<c>TANKATLAS_TEST_DATA_PATH</c> gesetzt) ausschließlich den festen Teststandort
/// aus <c>TANKATLAS_TEST_LOCATION</c>, sonst den Plattformdienst. Die echte Plattform wird im Testmodus nie erzeugt.
/// </summary>
public static class LocationServiceSelector
{
    /// <summary>
    /// Erstellt den passenden Standortdienst.
    /// </summary>
    /// <param name="getEnvironmentVariable">Liefert Umgebungsvariablen.</param>
    /// <param name="createPlatformService">Erzeugt den Plattformdienst.</param>
    /// <returns>Der Standortdienst.</returns>
    public static ILocationService Create(Func<string, string?> getEnvironmentVariable, Func<ILocationService> createPlatformService)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);
        ArgumentNullException.ThrowIfNull(createPlatformService);

        if (string.IsNullOrWhiteSpace(getEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable)))
        {
            return createPlatformService();
        }

        return TryParseLocation(getEnvironmentVariable(TestDataPaths.TestLocationEnvironmentVariable), out var position)
            ? new TestLocationService(position)
            : new TestLocationService(null);
    }

    /// <summary>
    /// Liest einen Standort im Format <c>breite,länge</c> (invariante Kultur, Dezimalpunkt).
    /// </summary>
    /// <param name="text">Der Text.</param>
    /// <param name="position">Die Position bei Erfolg.</param>
    /// <returns><see langword="true"/>, wenn der Text einen gültigen Standort beschreibt.</returns>
    public static bool TryParseLocation(string? text, out GeoPosition? position)
    {
        position = null;
        var parts = text?.Split(',');
        if (parts is not { Length: 2 }
            || !double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)
            || !double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
        {
            return false;
        }

        return GeoPosition.TryCreate(latitude, longitude, out position);
    }
}
