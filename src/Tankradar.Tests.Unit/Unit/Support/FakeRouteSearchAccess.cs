using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Services.Navigation;
using Tankradar.MAUI.Services.Routing;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="IRouteSearchAccess"/> für Tests im Speicher: standardmäßig mit gespeichertem Platzhalter-Schlüssel und eingeschalteter Routensuche.
/// </summary>
public sealed class FakeRouteSearchAccess : IRouteSearchAccess
{
    /// <summary>
    /// Der gespeicherte Platzhalter-Schlüssel; <see langword="null"/> bedeutet „kein Schlüssel“.
    /// </summary>
    public string? Key { get; set; } = "platzhalter-schluessel";

    /// <summary>
    /// Der Zustand des Schalters.
    /// </summary>
    public bool Switched { get; set; } = true;

    /// <summary>
    /// Wenn gesetzt, schlagen schreibende Aufrufe fehl.
    /// </summary>
    public bool ThrowOnWrite { get; set; }

    /// <summary>
    /// Wenn gesetzt, schlägt <see cref="IsEnabledAsync"/> fehl.
    /// </summary>
    public bool ThrowOnRead { get; set; }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <summary>
    /// Löst <see cref="Changed"/> aus.
    /// </summary>
    public void RaiseChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Die Zahl der Abonnenten von <see cref="Changed"/>.
    /// </summary>
    public int SubscriberCount
    {
        get { return Changed?.GetInvocationList().Length ?? 0; }
    }

    /// <inheritdoc />
    public Task<bool> HasKeyAsync()
    {
        return Task.FromResult(Key is not null);
    }

    /// <inheritdoc />
    public Task<bool> IsEnabledAsync()
    {
        return ThrowOnRead ? throw new InvalidOperationException("Ablage nicht lesbar.") : Task.FromResult(Switched && Key is not null);
    }

    /// <inheritdoc />
    public Task<string?> GetStoredKeyAsync()
    {
        return Task.FromResult(Key);
    }

    /// <inheritdoc />
    public Task<string?> GetApiKeyAsync()
    {
        return Task.FromResult(Switched ? Key : null);
    }

    /// <inheritdoc />
    public Task SaveKeyAsync(string key)
    {
        if (ThrowOnWrite)
        {
            throw new InvalidOperationException("Ablage nicht beschreibbar.");
        }

        Key = key;
        RaiseChanged();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveKeyAsync()
    {
        if (ThrowOnWrite)
        {
            throw new InvalidOperationException("Ablage nicht beschreibbar.");
        }

        Key = null;
        Switched = false;
        RaiseChanged();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> SetEnabledAsync(bool enabled)
    {
        if (ThrowOnWrite)
        {
            throw new InvalidOperationException("Ablage nicht beschreibbar.");
        }

        if (enabled && Key is null)
        {
            return Task.FromResult(false);
        }

        Switched = enabled;
        RaiseChanged();
        return Task.FromResult(true);
    }
}

/// <summary>
/// <see cref="IRoutingKeyChecker"/> für Tests mit vorgegebenem Ergebnis.
/// </summary>
public sealed class FakeRoutingKeyChecker : IRoutingKeyChecker
{
    /// <summary>
    /// Das gelieferte Ergebnis.
    /// </summary>
    public RoutingKeyCheckResult Result { get; set; } = RoutingKeyCheckResult.Valid;

    /// <summary>
    /// Wenn gesetzt, löst die Prüfung diese Ausnahme aus.
    /// </summary>
    public Exception? Failure { get; set; }

    /// <summary>
    /// Die geprüften Schlüssel in Reihenfolge.
    /// </summary>
    public List<string> CheckedKeys { get; } = [];

    /// <inheritdoc />
    public Task<RoutingKeyCheckResult> CheckAsync(string key, CancellationToken cancellationToken = default)
    {
        CheckedKeys.Add(key);
        return Failure is not null ? throw Failure : Task.FromResult(Result);
    }
}

/// <summary>
/// <see cref="ILinkOpener"/> für Tests, der geöffnete Adressen aufzeichnet.
/// </summary>
public sealed class FakeLinkOpener : ILinkOpener
{
    /// <summary>
    /// Die geöffneten Adressen.
    /// </summary>
    public List<Uri> Opened { get; } = [];

    /// <inheritdoc />
    public Task OpenAsync(Uri uri)
    {
        Opened.Add(uri);
        return Task.CompletedTask;
    }
}
