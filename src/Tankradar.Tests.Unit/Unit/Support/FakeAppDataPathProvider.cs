using Tankradar.MAUI.Services;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Testdouble für <see cref="IAppDataPathProvider"/> mit festem Verzeichnis.
/// </summary>
public class FakeAppDataPathProvider : IAppDataPathProvider
{
    private readonly string _directory;

    /// <summary>
    /// Erstellt den Provider.
    /// </summary>
    /// <param name="directory">Das zurückgegebene (bereits vorhandene) Verzeichnis.</param>
    public FakeAppDataPathProvider(string directory)
    {
        _directory = directory;
    }

    /// <inheritdoc />
    public string GetDataDirectory()
    {
        return _directory;
    }
}
