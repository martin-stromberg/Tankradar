using Tankradar.MAUI.Services;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Auflösung des App-Datenverzeichnisses durch <see cref="AppDataPathProvider"/>
/// in Abhängigkeit von der Umgebungsvariable <see cref="TestDataPaths.TestDataPathEnvironmentVariable"/>.
/// </summary>
[Collection(EnvironmentCollection.Name)]
public class AppDataPathProviderTests_DataDirectoryResolution : BaseTest
{
    /// <summary>
    /// Prüft, dass bei gesetzter Umgebungsvariable genau deren Wert zurückgegeben wird.
    /// </summary>
    [Fact]
    public void GetDataDirectory_WithTestDataPathSet_ReturnsEnvironmentVariableValue()
    {
        const string expectedPath = @"C:\FakeTestDataPath";

        Environment.SetEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable, expectedPath);
        try
        {
            var provider = new AppDataPathProvider(() => @"C:\ShouldNotBeUsed");

            var result = provider.GetDataDirectory();

            Assert.Equal(expectedPath, result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable, null);
        }
    }

    /// <summary>
    /// Prüft, dass ohne gesetzte Umgebungsvariable das Ergebnis der injizierten Default-Factory zurückgegeben wird.
    /// </summary>
    [Fact]
    public void GetDataDirectory_WithoutTestDataPathSet_ReturnsDefaultDirectoryFactoryResult()
    {
        const string expectedPath = @"C:\FakeDefault";

        Environment.SetEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable, null);
        var provider = new AppDataPathProvider(() => expectedPath);

        var result = provider.GetDataDirectory();

        Assert.Equal(expectedPath, result);
    }

    /// <summary>
    /// Prüft, dass bei einer nur aus Leerzeichen bestehenden Umgebungsvariable das Ergebnis der
    /// injizierten Default-Factory zurückgegeben wird (konsistent zu <c>Tankradar.Tests.Integration.TestDataContext</c>).
    /// </summary>
    [Fact]
    public void GetDataDirectory_WithWhitespaceOnlyTestDataPath_ReturnsDefaultDirectoryFactoryResult()
    {
        const string expectedPath = @"C:\FakeDefault";

        Environment.SetEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable, "   ");
        try
        {
            var provider = new AppDataPathProvider(() => expectedPath);

            var result = provider.GetDataDirectory();

            Assert.Equal(expectedPath, result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable, null);
        }
    }

    /// <summary>
    /// Prüft, dass der Zwischenspeicher (Kacheln) ohne Testverzeichnis in das Cache-Verzeichnis der Plattform fällt (unter iOS außerhalb der Datensicherung), nicht in das Datenverzeichnis.
    /// </summary>
    [Fact]
    public void GetCacheDirectory_WithoutTestDataPath_ReturnsCacheDirectoryNotDataDirectory()
    {
        Environment.SetEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable, null);
        var provider = new AppDataPathProvider(() => @"C:\Data", () => @"C:\Cache");

        Assert.Equal(@"C:\Cache", provider.GetCacheDirectory());
        Assert.Equal(@"C:\Data", provider.GetDataDirectory());
    }

    /// <summary>
    /// Prüft, dass im Testmodus auch der Zwischenspeicher im isolierten Testverzeichnis liegt.
    /// </summary>
    [Fact]
    public void GetCacheDirectory_WithTestDataPath_ReturnsTestDirectory()
    {
        Environment.SetEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable, @"C:\FakeTestDataPath");
        try
        {
            var provider = new AppDataPathProvider(() => @"C:\Data", () => @"C:\Cache");

            Assert.Equal(@"C:\FakeTestDataPath", provider.GetCacheDirectory());
        }
        finally
        {
            Environment.SetEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable, null);
        }
    }
}
