namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// In-Memory-Testdouble für <see cref="IPreferences"/>; Werte werden je Bereich (<c>sharedName</c>) und Schlüssel gehalten.
/// </summary>
public sealed class FakePreferences : IPreferences
{
    private readonly Dictionary<(string? SharedName, string Key), object?> _values = [];

    /// <inheritdoc />
    public bool ContainsKey(string key, string? sharedName = null)
    {
        return _values.ContainsKey((sharedName, key));
    }

    /// <inheritdoc />
    public void Remove(string key, string? sharedName = null)
    {
        _values.Remove((sharedName, key));
    }

    /// <inheritdoc />
    public void Clear(string? sharedName = null)
    {
        foreach (var entry in _values.Keys.Where(entry => entry.SharedName == sharedName).ToList())
        {
            _values.Remove(entry);
        }
    }

    /// <inheritdoc />
    public void Set<T>(string key, T value, string? sharedName = null)
    {
        _values[(sharedName, key)] = value;
    }

    /// <inheritdoc />
    public T Get<T>(string key, T defaultValue, string? sharedName = null)
    {
        return _values.TryGetValue((sharedName, key), out var value) && value is T typed ? typed : defaultValue;
    }
}
