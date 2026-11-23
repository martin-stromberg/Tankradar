using System.Diagnostics;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Prüft die best-effort-Löschung temporärer Testverzeichnisse (<see cref="TempDirectoryCleanup"/>): Attribut-Normalisierung,
/// gesperrte und fehlende Verzeichnisse ohne Ausnahme und das Prozess-Kill-Sicherheitsnetz.
/// </summary>
public sealed class TempDirectoryCleanupTests
{
    /// <summary>
    /// Prüft, dass ein Verzeichnis mit schreibgeschützter Datei mit Attribut-Normalisierung vollständig entfernt wird.
    /// </summary>
    [Fact]
    public void DeleteBestEffort_ExistingDirectory_DeletesIncludingReadOnlyFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "tempdir-cleanup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var readOnlyFile = Path.Combine(root, "readonly.txt");
        File.WriteAllText(readOnlyFile, "x");
        File.SetAttributes(readOnlyFile, FileAttributes.ReadOnly);

        TempDirectoryCleanup.DeleteBestEffort(root, normalizeAttributes: true);

        Assert.False(Directory.Exists(root));
    }

    /// <summary>
    /// Prüft, dass eine exklusiv geöffnete Datei alle Löschversuche scheitern lässt und die Methode trotzdem ohne Ausnahme
    /// zurückkehrt — ein Aufräumfehler darf nie den eigentlichen Assert überdecken.
    /// </summary>
    [Fact]
    public void DeleteBestEffort_LockedDirectory_DoesNotThrow()
    {
        var root = Path.Combine(Path.GetTempPath(), "tempdir-cleanup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var lockedFile = Path.Combine(root, "locked.txt");
        File.WriteAllText(lockedFile, "x");
        var stream = new FileStream(lockedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        try
        {
            var exception = Record.Exception(() => TempDirectoryCleanup.DeleteBestEffort(root));

            Assert.Null(exception);
        }
        finally
        {
            stream.Dispose();
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// Prüft, dass ein nicht vorhandener Pfad toleriert wird.
    /// </summary>
    [Fact]
    public void DeleteBestEffort_MissingDirectory_DoesNotThrow()
    {
        var root = Path.Combine(Path.GetTempPath(), "tempdir-cleanup-missing-" + Guid.NewGuid().ToString("N"));

        var exception = Record.Exception(() => TempDirectoryCleanup.DeleteBestEffort(root));

        Assert.Null(exception);
    }

    /// <summary>
    /// Prüft, dass das Kill-Sicherheitsnetz registrierte Prozesse vor dem Löschen beendet und ein bereits freigegebenes
    /// Prozessobjekt toleriert wird.
    /// </summary>
    [Fact]
    [Trait("Category", TestCategories.OsInterface)]
    public void DeleteBestEffort_ProcessesToKill_KillsProcessAndDeletesDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "tempdir-cleanup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "x.txt"), "x");
        var process = Process.Start(new ProcessStartInfo("ping", "-t 127.0.0.1")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        })!;
        var disposedProcess = Process.Start(new ProcessStartInfo("cmd.exe", "/c exit 0")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        })!;
        disposedProcess.Dispose();
        try
        {
            TempDirectoryCleanup.DeleteBestEffort(root, processesToKill: [process, disposedProcess]);

            Assert.True(process.HasExited, "registrierte Prozesse werden vor dem Löschen beendet");
            Assert.False(Directory.Exists(root));
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            process.Dispose();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
