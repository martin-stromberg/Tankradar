using Tankradar.MAUI.Models;
using Tankradar.MAUI.Services;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Testdouble für <see cref="ISettingsService"/>, das Einstellungen im Speicher hält und Aufrufe protokolliert.
/// </summary>
public class FakeSettingsService : ISettingsService
{
    /// <summary>
    /// Erstellt den Dienst mit den Standardeinstellungen als gespeicherten Stand.
    /// </summary>
    public FakeSettingsService()
    {
        Stored = AppSettings.CreateDefault();
    }

    /// <summary>
    /// Die aktuell "gespeicherten" Einstellungen, die <see cref="LoadAsync"/> liefert.
    /// </summary>
    public AppSettings Stored { get; set; }

    /// <summary>
    /// Wenn gesetzt, wird sie von <see cref="LoadAsync"/> ausgelöst.
    /// </summary>
    public Exception? LoadException { get; set; }

    /// <summary>
    /// Wenn gesetzt, wird sie von <see cref="SaveAsync"/> ausgelöst.
    /// </summary>
    public Exception? SaveException { get; set; }

    /// <summary>
    /// Anzahl der Aufrufe von <see cref="LoadAsync"/>.
    /// </summary>
    public int LoadCount { get; private set; }

    /// <summary>
    /// Alle über <see cref="SaveAsync"/> gespeicherten Einstellungen in Aufrufreihenfolge.
    /// </summary>
    public List<AppSettings> Saved { get; } = [];

    /// <inheritdoc />
    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        LoadCount++;
        return LoadException is null ? Task.FromResult(Stored) : Task.FromException<AppSettings>(LoadException);
    }

    /// <inheritdoc />
    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        if (SaveException is not null)
        {
            return Task.FromException(SaveException);
        }

        Saved.Add(settings);
        Stored = settings;
        return Task.CompletedTask;
    }
}
