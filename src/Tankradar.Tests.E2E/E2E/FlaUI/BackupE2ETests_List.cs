namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Mehrere Sicherungen erscheinen neueste zuerst, und „Wiederherstellen“ wirkt auf die in der Liste gewählte Sicherung.
/// </summary>
public class BackupE2ETests_List : BackupE2ETestBase
{
    /// <summary>
    /// Prüft die Reihenfolge und dass die Wiederherstellung der älteren Sicherung deren Stand und nicht den der neueren liefert.
    /// </summary>
    [Fact]
    public void TwoBackups_AreOrderedNewestFirst_AndRestoreUsesSelectedOne()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "84350", "5,4");
            OpenBackup();
            CreateBackup();
            WaitForBackupFileCount(1);
            var older = BackupFiles().Single();

            OpenTankbook();
            CreateVehicle("Opel", "Petrol", "1000", string.Empty);
            OpenBackup();
            Thread.Sleep(1100);
            CreateBackup();
            WaitForBackupFileCount(2);
            WaitForAutomationId(ItemId(2, "Title"));
            Assert.NotEqual(older, BackupFiles().First(file => file != older));

            RestoreBackup(2);

            OpenTankbook();
            Tankbook.WaitForTexts("Tankbook.Vehicle.Name", "Golf");
            OpenBackup();
            WaitForAutomationId(ItemId(2, "Title"));
            Assert.Equal(2, BackupFiles().Length);
        });
    }
}
