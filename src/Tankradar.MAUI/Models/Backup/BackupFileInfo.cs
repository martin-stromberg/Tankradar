namespace Tankradar.MAUI.Models.Backup;

/// <summary>
/// Ein Eintrag der Liste der Sicherungen.
/// </summary>
/// <param name="FileName">Der Dateiname im Sicherungsverzeichnis.</param>
/// <param name="CreatedUtc">Der Erstellungszeitpunkt (UTC) laut Manifest, bei nicht lesbaren und verschlüsselten Dateien das Änderungsdatum.</param>
/// <param name="SizeBytes">Die Dateigröße in Bytes.</param>
/// <param name="AppVersion">Die App-Version laut Manifest; leer bei nicht lesbaren und verschlüsselten Dateien.</param>
/// <param name="IsReadable">Gibt an, ob die Datei gelesen werden kann (bei verschlüsselten Dateien: ob der unverschlüsselte Header gültig ist).</param>
/// <param name="IsEncrypted">Gibt an, ob die Datei ein verschlüsselter Container ist (ADR 0019).</param>
/// <returns>Der Wert.</returns>
public sealed record BackupFileInfo(string FileName, DateTime CreatedUtc, long SizeBytes, string AppVersion, bool IsReadable, bool IsEncrypted);
