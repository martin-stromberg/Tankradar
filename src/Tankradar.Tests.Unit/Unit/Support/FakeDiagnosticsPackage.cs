using Tankradar.MAUI.Services.Diagnostics;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="ISensitiveValueProvider"/> für Tests mit vorgegebenen Werten.
/// </summary>
public sealed class FakeSensitiveValueProvider : ISensitiveValueProvider
{
    /// <summary>
    /// Die gelieferten Werte.
    /// </summary>
    public IReadOnlyList<string> Values { get; set; } = [];

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetValuesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Values);
    }
}

/// <summary>
/// <see cref="IDiagnosticsPackageService"/> für Tests; schreibt auf Wunsch eine echte Datei.
/// </summary>
public sealed class FakeDiagnosticsPackageService : IDiagnosticsPackageService
{
    /// <summary>
    /// Der gelieferte Vorschautext.
    /// </summary>
    public string Preview { get; set; } = "Vorschau des Paketinhalts";

    /// <summary>
    /// Der zurückgegebene Paketpfad.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public string PackagePath { get; set; } = Path.Combine(Path.GetTempPath(), "tankatlas-diagnose-test.zip");

    /// <summary>
    /// Wenn gesetzt, schlagen die Aufrufe fehl.
    /// </summary>
    public bool Throw { get; set; }

    /// <summary>
    /// Anzahl der Paketerzeugungen.
    /// </summary>
    public int CreateCount { get; private set; }

    /// <inheritdoc />
    public Task<string> CreatePreviewTextAsync(CancellationToken cancellationToken = default)
    {
        return Throw ? throw new InvalidOperationException("Fehler") : Task.FromResult(Preview);
    }

    /// <inheritdoc />
    public Task<string> CreatePackageAsync(CancellationToken cancellationToken = default)
    {
        if (Throw)
        {
            throw new InvalidOperationException("Fehler");
        }

        CreateCount++;
        return Task.FromResult(PackagePath);
    }
}

/// <summary>
/// <see cref="IDiagnosticsSharer"/> für Tests, der die Aufrufe aufzeichnet.
/// </summary>
public sealed class FakeDiagnosticsSharer : IDiagnosticsSharer
{
    /// <summary>
    /// Die geteilten Dateien.
    /// </summary>
    public List<string> Shared { get; } = [];

    /// <summary>
    /// Wenn gesetzt, schlägt das Teilen fehl.
    /// </summary>
    public bool Throw { get; set; }

    /// <inheritdoc />
    public Task ShareAsync(string fullPath, string title, CancellationToken cancellationToken = default)
    {
        if (Throw)
        {
            throw new InvalidOperationException("Fehler");
        }

        Shared.Add(fullPath);
        return Task.CompletedTask;
    }
}
