using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="IApiKeyStore"/> für Tests im Speicher; kann Fehler der sicheren Ablage simulieren.
/// </summary>
public sealed class FakeApiKeyStore : IApiKeyStore
{
    /// <summary>
    /// Der gespeicherte Wert.
    /// </summary>
    public string? Stored { get; set; }

    /// <summary>
    /// Wenn gesetzt, schlägt das Lesen fehl.
    /// </summary>
    public bool ThrowOnGet { get; set; }

    /// <summary>
    /// Wenn gesetzt, schlägt das Speichern fehl.
    /// </summary>
    public bool ThrowOnSet { get; set; }

    /// <summary>
    /// Anzahl der Speichervorgänge.
    /// </summary>
    public int SetCount { get; private set; }

    /// <inheritdoc />
    public Task<string?> GetAsync()
    {
        return ThrowOnGet ? throw new InvalidOperationException("Ablage nicht lesbar.") : Task.FromResult(Stored);
    }

    /// <inheritdoc />
    public Task SetAsync(string apiKey)
    {
        SetCount++;
        if (ThrowOnSet)
        {
            throw new InvalidOperationException("Ablage nicht beschreibbar.");
        }

        Stored = apiKey;
        return Task.CompletedTask;
    }
}

/// <summary>
/// <see cref="IApiKeyProvider"/> für Tests mit fest vorgegebenem Wert.
/// </summary>
public sealed class FakeApiKeyProvider : IApiKeyProvider
{
    /// <summary>
    /// Der gelieferte Wert; <see langword="null"/> bedeutet „kein Schlüssel“.
    /// </summary>
    public string? Value { get; set; } = "test-credential";

    /// <inheritdoc />
    public Task<string?> GetApiKeyAsync()
    {
        return Task.FromResult(Value);
    }
}
