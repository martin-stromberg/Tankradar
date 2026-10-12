using System.Text.Json;
using System.Text.Json.Serialization;
using Tankradar.MAUI.Models.Backup;

namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Markierung eines laufenden Austauschs im Datenverzeichnis (<c>restore.journal</c>). Solange sie existiert, gilt der Austausch als nicht abgeschlossen
/// und wird beim nächsten Start zurückgenommen; das Löschen der Datei ist der Commit-Punkt.
/// </summary>
public sealed class RestoreJournal
{
    /// <summary>
    /// Gibt an, ob vor dem Austausch eine Datenbank vorhanden war.
    /// </summary>
    [JsonPropertyName("hadDatabase")]
    public bool HadDatabase { get; set; }

    /// <summary>
    /// Gibt an, ob vor dem Austausch ein Verzeichnis mit Belegfotos vorhanden war.
    /// </summary>
    [JsonPropertyName("hadReceipts")]
    public bool HadReceipts { get; set; }

    /// <summary>
    /// Ermittelt den Pfad des Journals.
    /// </summary>
    /// <param name="dataDirectory">Das Datenverzeichnis.</param>
    /// <returns>Der Pfad.</returns>
    public static string GetPath(string dataDirectory)
    {
        return Path.Combine(dataDirectory, BackupFormat.JournalFileName);
    }

    /// <summary>
    /// Schreibt das Journal (zuerst in eine temporäre Datei, die auf den Datenträger geschrieben wird, dann durch Umbenennen).
    /// </summary>
    /// <param name="dataDirectory">Das Datenverzeichnis.</param>
    /// <param name="journal">Der Inhalt.</param>
    public static void Write(string dataDirectory, RestoreJournal journal)
    {
        ArgumentNullException.ThrowIfNull(journal);
        var path = GetPath(dataDirectory);
        var temp = path + BackupFormat.TempExtension;
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(JsonSerializer.SerializeToUtf8Bytes(journal));
            stream.Flush(flushToDisk: true);
        }

        File.Move(temp, path, overwrite: true);
    }

    /// <summary>
    /// Liest das Journal.
    /// </summary>
    /// <param name="dataDirectory">Das Datenverzeichnis.</param>
    /// <returns>Der Inhalt oder <see langword="null"/>, wenn es fehlt oder nicht lesbar ist.</returns>
    public static RestoreJournal? Read(string dataDirectory)
    {
        try
        {
            return JsonSerializer.Deserialize<RestoreJournal>(File.ReadAllBytes(GetPath(dataDirectory)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gibt an, ob ein Journal existiert.
    /// </summary>
    /// <param name="dataDirectory">Das Datenverzeichnis.</param>
    /// <returns><see langword="true"/>, wenn ein Austausch nicht abgeschlossen wurde.</returns>
    public static bool Exists(string dataDirectory)
    {
        return File.Exists(GetPath(dataDirectory));
    }

    /// <summary>
    /// Löscht das Journal und eine eventuelle temporäre Datei.
    /// </summary>
    /// <param name="dataDirectory">Das Datenverzeichnis.</param>
    public static void Delete(string dataDirectory)
    {
        var path = GetPath(dataDirectory);
        File.Delete(path);
        File.Delete(path + BackupFormat.TempExtension);
    }
}
