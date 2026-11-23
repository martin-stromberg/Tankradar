namespace Tankradar.TestSupport;

/// <summary>
/// Zentrale Namen der xUnit-Testkategorien (Wert des <c>Category</c>-Traits und der <c>--filter</c>-Zeichenketten der CI).
/// </summary>
public static class TestCategories
{
    /// <summary>
    /// Tests mit echter Betriebssystem-Schnittstellen-Berührung (UI-Automation, Prozessstarts, Loopback-Listener); laufen in der best-effort-Lane.
    /// </summary>
    public const string OsInterface = "OsInterface";

    /// <summary>
    /// Nachweislich umgebungsflaky OS-Schnittstellen-Tests (nur in Kombination mit <see cref="OsInterface"/>); berechtigt zum einmaligen Re-Run in der best-effort-Lane.
    /// </summary>
    public const string Flaky = "Flaky";
}
