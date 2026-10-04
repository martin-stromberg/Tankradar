using Microsoft.Extensions.Logging;
#if IOS
using Foundation;
#endif

namespace Tankradar.MAUI.Services;

/// <summary>
/// Setzt unter iOS den Dateischutz <c>NSFileProtectionCompleteUntilFirstUserAuthentication</c>; auf allen anderen Plattformen ohne Wirkung.
/// </summary>
public class DatabaseFileProtector : IDatabaseFileProtector
{
    private readonly ILogger<DatabaseFileProtector> _logger;

    /// <summary>
    /// Erstellt den Protector.
    /// </summary>
    /// <param name="logger">Logger für Fehlschläge beim Setzen des Dateischutzes.</param>
    public DatabaseFileProtector(ILogger<DatabaseFileProtector> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void Protect(string databasePath)
    {
#if IOS
        // Neue Dateien (z. B. -wal/-shm, die erst später entstehen) erben die Schutzklasse des Verzeichnisses;
        // vorhandene Dateien werden zusätzlich ausdrücklich geschützt.
        var targets = new List<string>();
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            targets.Add(directory);
        }

        targets.Add(databasePath);
        targets.Add(databasePath + "-wal");
        targets.Add(databasePath + "-shm");

        var attributes = new NSDictionary(NSFileManager.FileProtectionKey, NSFileProtectionType.CompleteUntilFirstUserAuthentication);
        foreach (var target in targets.Where(path => File.Exists(path) || Directory.Exists(path)))
        {
            if (!NSFileManager.DefaultManager.SetAttributes(attributes, target, out var error))
            {
                _logger.LogWarning("Der Dateischutz konnte für {Path} nicht gesetzt werden: {Error}", target, error?.LocalizedDescription);
            }
        }
#else
        _logger.LogDebug("Kein plattformspezifischer Dateischutz für {Path} erforderlich.", databasePath);
#endif
    }
}
