using Tankradar.MAUI.Services.Backup;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass im Testmodus nie die echte Systemfreigabe genutzt wird, sondern in <c>test-shared</c> kopiert wird.
/// </summary>
public class BackupSharerSelectorTests_TestMode : BaseTest
{
    private static Func<string, string?> Env(params (string Name, string Value)[] values)
    {
        return name => values.Where(entry => entry.Name == name).Select(entry => entry.Value).FirstOrDefault();
    }

    /// <summary>
    /// Prüft, dass im Testmodus die Testweitergabe in das Unterverzeichnis <c>test-shared</c> kopiert und die Plattform nie erzeugt wird.
    /// </summary>
    [Fact]
    public async Task Create_TestMode_CopiesToTestShared()
    {
        using var temp = new TempDirectory();
        var source = temp.Combine("tankatlas-backup-1.zip");
        await File.WriteAllBytesAsync(source, [1, 2, 3]);
        var created = false;

        var sharer = BackupSharerSelector.Create(
            Env((TestDataPaths.TestDataPathEnvironmentVariable, temp.Path)),
            () =>
            {
                created = true;
                return new FakeBackupSharer();
            });
        await sharer.ShareAsync(source, "Sicherung");

        Assert.False(created);
        Assert.IsType<TestBackupSharer>(sharer);
        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(temp.Combine("test-shared", "tankatlas-backup-1.zip")));
    }

    /// <summary>
    /// Prüft, dass außerhalb des Testmodus die Plattformweitergabe erzeugt wird.
    /// </summary>
    [Fact]
    public void Create_Production_UsesPlatformSharer()
    {
        var platform = new FakeBackupSharer();

        Assert.Same(platform, BackupSharerSelector.Create(Env(), () => platform));
    }
}
