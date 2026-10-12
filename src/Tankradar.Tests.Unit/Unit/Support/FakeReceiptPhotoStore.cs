using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Testdouble für <see cref="IReceiptPhotoStore"/> ohne Dateisystem: führt die vorhandenen Dateinamen in einer Menge und protokolliert Löschungen.
/// </summary>
public sealed class FakeReceiptPhotoStore : IReceiptPhotoStore
{
    /// <summary>
    /// Die Dateinamen der „vorhandenen“ Fotos.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public HashSet<string> Files { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Die Dateinamen, für die <see cref="Delete"/> aufgerufen wurde, in Aufrufreihenfolge.
    /// </summary>
    public List<string> Deleted { get; } = [];

    /// <summary>
    /// Die referenzierten Dateinamen der Aufrufe von <see cref="DeleteUnreferenced"/>.
    /// </summary>
    public List<IReadOnlySet<string>> UnreferencedCleanups { get; } = [];

    /// <summary>
    /// Der Fehler, den der nächste Aufruf von <see cref="ImportAsync"/> meldet; <see cref="ReceiptPhotoError.None"/> für Erfolg.
    /// </summary>
    public ReceiptPhotoError NextImportError { get; set; }

    /// <summary>
    /// Legt ein „vorhandenes“ Foto an.
    /// </summary>
    /// <param name="extension">Die Endung samt Punkt.</param>
    /// <returns>Der Dateiname.</returns>
    public string AddFile(string extension = ".jpg")
    {
        var name = Guid.NewGuid().ToString("N") + extension;
        Files.Add(name);
        return name;
    }

    /// <inheritdoc />
    public async Task<ReceiptPhotoImportResult> ImportAsync(Stream source, string extension, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);
        if (NextImportError != ReceiptPhotoError.None)
        {
            var error = NextImportError;
            NextImportError = ReceiptPhotoError.None;
            return new ReceiptPhotoImportResult(null, error);
        }

        return new ReceiptPhotoImportResult(AddFile(extension), ReceiptPhotoError.None);
    }

    /// <inheritdoc />
    public string? GetFullPath(string fileName)
    {
        return Path.Combine(Path.GetTempPath(), "tankatlas-fake-receipts", fileName);
    }

    /// <inheritdoc />
    public bool Exists(string fileName)
    {
        return Files.Contains(fileName);
    }

    /// <inheritdoc />
    public void Delete(string fileName)
    {
        Deleted.Add(fileName);
        Files.Remove(fileName);
    }

    /// <inheritdoc />
    public void DeleteUnreferenced(IReadOnlySet<string> referencedFileNames)
    {
        UnreferencedCleanups.Add(referencedFileNames);
        Files.RemoveWhere(name => !referencedFileNames.Contains(name));
    }
}
