namespace Tankradar.MAUI.Services.Diagnostics;

/// <summary>
/// Weitergabe für den Testmodus: kopiert das Diagnosepaket ohne Systemdialog in ein Zielverzeichnis (<c>&lt;Daten&gt;/test-shared</c>).
/// </summary>
public sealed class TestDiagnosticsSharer : IDiagnosticsSharer
{
    private readonly string _targetDirectory;

    /// <summary>
    /// Erstellt die Weitergabe.
    /// </summary>
    /// <param name="targetDirectory">Das Zielverzeichnis (wird angelegt).</param>
    public TestDiagnosticsSharer(string targetDirectory)
    {
        _targetDirectory = targetDirectory;
    }

    /// <inheritdoc />
    public Task ShareAsync(string fullPath, string title, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Das Diagnosepaket ist nicht vorhanden.");
        }

        Directory.CreateDirectory(_targetDirectory);
        File.Copy(fullPath, Path.Combine(_targetDirectory, Path.GetFileName(fullPath)), overwrite: true);
        return Task.CompletedTask;
    }
}
