namespace Tankradar.Tests.Integration.Integration.Support;

/// <summary>
/// Fasst alle Testklassen zusammen, die Sicherungen wiederherstellen. <c>SqliteConnection.ClearAllPools</c> wirkt auf den ganzen Prozess; ohne gemeinsame Sammlung
/// würde eine parallel laufende Wiederherstellung die gepoolte Verbindung eines anderen Tests schließen und dessen -wal-Datei entfernen.
/// </summary>
[CollectionDefinition(Name)]
public sealed class BackupCollection
{
    /// <summary>
    /// Der Name der Sammlung.
    /// </summary>
    public const string Name = "BackupRestoreSharedPools";
}
