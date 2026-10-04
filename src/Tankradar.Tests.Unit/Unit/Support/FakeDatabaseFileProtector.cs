using Tankradar.MAUI.Services;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Testdouble für <see cref="IDatabaseFileProtector"/>, das Aufrufe protokolliert und optional eine Aktion ausführt.
/// </summary>
public class FakeDatabaseFileProtector : IDatabaseFileProtector
{
    /// <summary>
    /// Die Pfade, für die <see cref="Protect"/> aufgerufen wurde.
    /// </summary>
    public List<string> ProtectedPaths { get; } = [];

    /// <summary>
    /// Wird bei jedem Aufruf von <see cref="Protect"/> ausgeführt.
    /// </summary>
    public Action? OnProtect { get; set; }

    /// <inheritdoc />
    public void Protect(string databasePath)
    {
        ProtectedPaths.Add(databasePath);
        OnProtect?.Invoke();
    }
}
