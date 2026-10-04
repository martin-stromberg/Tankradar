using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Services;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass <see cref="DatabaseFileProtector"/> außerhalb von iOS ohne Wirkung bleibt.
/// </summary>
public class DatabaseFileProtectorTests_Platform : BaseTest
{
    /// <summary>
    /// Prüft, dass unter Windows weder eine Ausnahme noch eine Dateisystemänderung entsteht, auch bei nicht existierendem Pfad.
    /// </summary>
    [Fact]
    public void Protect_OnWindows_DoesNothingAndDoesNotThrow()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "tankatlas.db");

        var exception = Record.Exception(() => new DatabaseFileProtector(NullLogger<DatabaseFileProtector>.Instance).Protect(missingPath));

        Assert.Null(exception);
        Assert.False(File.Exists(missingPath));
        Assert.False(Directory.Exists(Path.GetDirectoryName(missingPath)));
    }
}
