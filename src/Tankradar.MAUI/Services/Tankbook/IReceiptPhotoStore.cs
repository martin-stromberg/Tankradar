using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Ablage der Belegfotos im App-Datenverzeichnis (Unterverzeichnis <c>receipts</c>) unter dem systemseitigen Dateischutz.
/// Gespeichert wird je Vorgang nur der Dateiname; nur Namen im Format <c>&lt;Guid&gt;.&lt;Endung&gt;</c> werden akzeptiert.
/// </summary>
public interface IReceiptPhotoStore
{
    /// <summary>
    /// Kopiert ein Foto unverändert in die Ablage, sofern Endung und Größe erlaubt sind.
    /// </summary>
    /// <param name="source">Der Inhalt des Fotos.</param>
    /// <param name="extension">Die Dateiendung samt Punkt, z. B. <c>.jpg</c>.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Der neue Dateiname oder der Fehler.</returns>
    Task<ReceiptPhotoImportResult> ImportAsync(Stream source, string extension, CancellationToken cancellationToken = default);

    /// <summary>
    /// Liefert den vollständigen Pfad eines Fotos in der Ablage.
    /// </summary>
    /// <param name="fileName">Der Dateiname.</param>
    /// <returns>Der Pfad; <see langword="null"/> bei einem ungültigen Dateinamen.</returns>
    string? GetFullPath(string fileName);

    /// <summary>
    /// Gibt an, ob ein Foto in der Ablage vorhanden ist.
    /// </summary>
    /// <param name="fileName">Der Dateiname.</param>
    /// <returns><see langword="true"/>, wenn der Name gültig ist und die Datei existiert.</returns>
    bool Exists(string fileName);

    /// <summary>
    /// Löscht ein Foto aus der Ablage; ungültige Namen und fehlende Dateien werden ignoriert. Wirft nicht: Fehler beim Löschen werden nur protokolliert.
    /// </summary>
    /// <param name="fileName">Der Dateiname.</param>
    void Delete(string fileName);

    /// <summary>
    /// Löscht alle Fotos der Ablage, die keiner der angegebenen Dateinamen referenziert.
    /// </summary>
    /// <param name="referencedFileNames">Die von Vorgängen referenzierten Dateinamen.</param>
    void DeleteUnreferenced(IReadOnlySet<string> referencedFileNames);
}
