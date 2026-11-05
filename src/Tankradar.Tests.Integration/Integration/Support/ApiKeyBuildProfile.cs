namespace Tankradar.Tests.Integration.Integration.Support;

/// <summary>
/// Beschreibt eine Schlüsselübernahme beim Build (Tankerkönig-Schlüssel).
/// </summary>
/// <param name="TargetsFileName">Der Dateiname des MSBuild-Ziels.</param>
/// <param name="PrimaryVariable">Der Name der vorrangigen Umgebungsvariable.</param>
/// <param name="SecretVariable">Der Name der Umgebungsvariable mit dem Namen des GitHub-Secrets.</param>
/// <param name="LocalPropsFileName">Der Dateiname der nicht versionierten lokalen Konfiguration.</param>
/// <param name="LocalPropsProperty">Der Name der MSBuild-Eigenschaft, die den Pfad der lokalen Konfiguration vorgibt.</param>
/// <param name="MetadataName">Der Name des Assembly-Metadatums und des Elements in der lokalen Konfiguration.</param>
/// <returns>Der Wert.</returns>
public sealed record ApiKeyBuildProfile(
    string TargetsFileName,
    string PrimaryVariable,
    string SecretVariable,
    string LocalPropsFileName,
    string LocalPropsProperty,
    string MetadataName)
{
    /// <summary>
    /// Die Schlüsselübernahme des Tankerkönig-Schlüssels.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public static ApiKeyBuildProfile Tankerkoenig { get; } = new(
        "TankerkoenigApiKey.targets",
        "TANKRADAR_FUEL_PRICE_API_KEY",
        "FUEL_PRICE_API_KEY",
        "tankerkoenig.local.props",
        "TankerkoenigLocalPropsFile",
        "TankerkoenigApiKey");
}
