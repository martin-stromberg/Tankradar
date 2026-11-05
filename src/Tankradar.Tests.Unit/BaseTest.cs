namespace Tankradar.Tests.Unit;

/// <summary>
/// Basisklasse für Unit-Tests mit einheitlicher Aufräumlogik über <see cref="IDisposable"/>.
/// </summary>
public abstract class BaseTest : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Gibt die vom Test verwendeten Ressourcen frei.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gibt verwaltete Ressourcen frei, sofern noch nicht geschehen.
    /// </summary>
    /// <param name="disposing"><see langword="true"/>, wenn der Aufruf von <see cref="Dispose()"/> stammt.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }
}
