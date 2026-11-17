using Tankradar.MAUI.Services.Diagnostics;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass im Testmodus nie die echte Systemfreigabe genutzt wird, sondern in <c>test-shared</c> kopiert wird.
/// </summary>
public class DiagnosticsSharerSelectorTests_TestMode : BaseTest
{
    private static Func<string, string?> Env(params (string Name, string Value)[] values)
    {
        return name => values.Where(entry => entry.Name == name).Select(entry => entry.Value).FirstOrDefault();
    }

    /// <summary>
    /// Prüft, dass im Testmodus das Diagnosepaket in das Unterverzeichnis <c>test-shared</c> kopiert und die Plattformweitergabe nie erzeugt wird.
    /// </summary>
    [Fact]
    public async Task Create_TestMode_CopiesToTestShared()
    {
        using var temp = new TempDirectory();
        var source = temp.Combine("tankatlas-diagnose-1.zip");
        await File.WriteAllBytesAsync(source, [1, 2, 3]);
        var created = false;

        var sharer = DiagnosticsSharerSelector.Create(
            Env((TestDataPaths.TestDataPathEnvironmentVariable, temp.Path)),
            () =>
            {
                created = true;
                return new FakeDiagnosticsSharer();
            });
        await sharer.ShareAsync(source, "Diagnose");

        Assert.False(created);
        Assert.IsType<TestDiagnosticsSharer>(sharer);
        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(temp.Combine("test-shared", "tankatlas-diagnose-1.zip")));
    }

    /// <summary>
    /// Prüft, dass außerhalb des Testmodus die Plattformweitergabe erzeugt wird.
    /// </summary>
    [Fact]
    public void Create_Production_UsesPlatformSharer()
    {
        var platform = new FakeDiagnosticsSharer();

        Assert.Same(platform, DiagnosticsSharerSelector.Create(Env(), () => platform));
    }

    /// <summary>
    /// Prüft, dass die Testweitergabe bei einer fehlenden Datei einen Fehler meldet.
    /// </summary>
    [Fact]
    public async Task Share_MissingFile_Throws()
    {
        using var temp = new TempDirectory();
        var sharer = new TestDiagnosticsSharer(temp.Combine("out"));

        await Assert.ThrowsAsync<FileNotFoundException>(() => sharer.ShareAsync(temp.Combine("fehlt.zip"), "Diagnose"));
    }
}
