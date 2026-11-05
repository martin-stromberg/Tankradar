namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Sicherungen und wiederhergestellte Daten bleiben nach einem Neustart der App bestehen.
/// </summary>
public class BackupE2ETests_Persistence : BackupE2ETestBase
{
    /// <summary>
    /// Prüft, dass die Sicherung nach einem Neustart weiterhin in der Liste steht und der wiederhergestellte Stand einen weiteren Neustart übersteht.
    /// </summary>
    [Fact]
    public void BackupAndRestoredData_SurviveRestart()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "84350", "5,4");
            OpenBackup();
            CreateBackup();
            WaitForBackupFileCount(1);

            RestartApplication();
            OpenBackup();
            WaitForAutomationId(ItemId(1, "Title"));
            Assert.False(Exists("Settings.Backup.Empty"));

            OpenTankbook();
            CreateVehicle("Opel", "Petrol", "1000", string.Empty);
            OpenBackup();
            RestoreBackup(1);

            RestartApplication();
            OpenTankbook();
            Tankbook.WaitForTexts("Tankbook.Vehicle.Name", "Golf");
            OpenBackup();
            WaitForAutomationId(ItemId(1, "Title"));
            Assert.False(Directory.Exists(Path.Combine(TestDataDirectory, "restore-staging")));
            Assert.False(Directory.Exists(Path.Combine(TestDataDirectory, "restore-rollback")));
            Assert.False(File.Exists(Path.Combine(TestDataDirectory, "restore.journal")));
        });
    }
}
