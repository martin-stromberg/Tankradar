using System.Globalization;

namespace Tankradar.MAUI.Services;

/// <summary>
/// Kennung der App gegenüber externen Diensten: Produktname, tatsächliche App-Version und Kontaktangabe (Projekt-URL), wie es die Nutzungsrichtlinien
/// von OpenStreetMap (Kachelserver, Nominatim) verlangen.
/// </summary>
public static class AppIdentity
{
    /// <summary>
    /// Die Projekt-URL als Kontaktangabe im <c>User-Agent</c>.
    /// </summary>
    public const string ProjectUrl = "https://github.com/martin-stromberg/Tankradar";

    /// <summary>
    /// Die Version, die verwendet wird, wenn die App-Version nicht ermittelt werden kann.
    /// </summary>
    public const string UnknownVersion = "0.0.0";

    /// <summary>
    /// Baut den <c>User-Agent</c> aus Produktname, Version, Projekt-URL und App-Kennung: <c>Tankatlas/1.2.3 (+https://github.com/martin-stromberg/Tankradar; de.martinstromberg.tankradar)</c>.
    /// </summary>
    /// <param name="version">Die App-Version; leer oder ungültig führt zu <see cref="UnknownVersion"/>.</param>
    /// <param name="bundleId">Die App-Kennung.</param>
    /// <returns>Der Wert für den Anfragekopf.</returns>
    public static string BuildUserAgent(string? version, string bundleId = AppConfiguration.DefaultBundleId)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{AppConfiguration.AppDisplayName}/{NormalizeVersion(version)} (+{ProjectUrl}; {bundleId})");
    }

    /// <summary>
    /// Ermittelt die tatsächliche App-Version der Plattform; kann sie nicht gelesen werden, gilt <see cref="UnknownVersion"/>.
    /// </summary>
    /// <returns>Die Version.</returns>
    public static string GetAppVersion()
    {
        try
        {
            return NormalizeVersion(AppInfo.VersionString);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return UnknownVersion;
        }
    }

    private static string NormalizeVersion(string? version)
    {
        var trimmed = version?.Trim() ?? string.Empty;
        return trimmed.Length > 0 && trimmed.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '+')
            ? trimmed
            : UnknownVersion;
    }
}
