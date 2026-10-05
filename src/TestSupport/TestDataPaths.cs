namespace Tankradar.TestSupport;

/// <summary>
/// Gemeinsam genutzte Konstanten für die Testdaten-Isolation von Integrations- und E2E-Tests.
/// </summary>
public static class TestDataPaths
{
    /// <summary>
    /// Name der Umgebungsvariable, über die Tests ein isoliertes Testdatenverzeichnis vorgeben können.
    /// </summary>
    public const string TestDataPathEnvironmentVariable = "TEST_DATA_PATH";

    /// <summary>
    /// Name der Umgebungsvariable, die im Testmodus die Adresse des Preisdienstes (Mock-Server) vorgibt.
    /// </summary>
    public const string PriceApiUrlEnvironmentVariable = "TANKRADAR_PRICE_API_URL";

    /// <summary>
    /// Name der Umgebungsvariable, die im Testmodus einen Test-Schlüssel für den Preisdienst vorgibt.
    /// </summary>
    public const string PriceApiKeyEnvironmentVariable = "TANKRADAR_PRICE_API_KEY";
}
