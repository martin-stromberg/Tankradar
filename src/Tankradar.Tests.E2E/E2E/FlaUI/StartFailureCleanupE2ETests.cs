using System.Runtime.CompilerServices;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// Prüft, dass das Aufräumen nach einem fehlgeschlagenen App-Start keine eigene Ausnahme auslöst und so die ursprüngliche Startausnahme nicht verdeckt.
/// Der Test startet die App nicht: Er simuliert den Zustand eines Konstruktors, der vor der Initialisierung der Felder abgebrochen ist.
/// </summary>
public class StartFailureCleanupE2ETests
{
    /// <summary>
    /// Prüft, dass Dispose einer nicht vollständig gestarteten Suchtestbasis (kein Mock-Server, keine Automation, keine App) nicht wirft.
    /// </summary>
    [Fact]
    public void Dispose_AfterFailedStart_DoesNotThrow()
    {
        var notStarted = (IDisposable)RuntimeHelpers.GetUninitializedObject(typeof(UnstartedSearchTest));

        var exception = Record.Exception(notStarted.Dispose);

        Assert.Null(exception);
    }

    private sealed class UnstartedSearchTest : SearchE2ETestBase
    {
    }
}
