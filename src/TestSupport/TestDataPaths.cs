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
}
