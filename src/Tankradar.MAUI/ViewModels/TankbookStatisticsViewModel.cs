using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Zustand des Bereichs „Auswertung“ der Seite „Tank- und Ladevorgänge“: ein Block je Art oder ein Hinweis.
/// Ein Ladefehler betrifft nur die Auswertung, nicht die Liste der Vorgänge. Protokolliert werden nie Werte, nur Ausnahmetypen.
/// </summary>
public sealed class TankbookStatisticsViewModel : BaseViewModel
{
    private readonly ITankbookStatisticsService _service;
    private readonly IScreenReaderAnnouncer _announcer;
    private readonly ILogger<TankbookStatisticsViewModel> _logger;
    private IReadOnlyList<TankbookKindStatisticsViewModel> _kinds = [];
    private string? _notice;
    private int _loadVersion;

    /// <summary>
    /// Erstellt das ViewModel.
    /// </summary>
    /// <param name="service">Der Auswertungsdienst.</param>
    /// <param name="announcer">Sagt Fehlermeldungen über die Sprachausgabe an.</param>
    /// <param name="logger">Logger (protokolliert nie Werte).</param>
    public TankbookStatisticsViewModel(ITankbookStatisticsService service, IScreenReaderAnnouncer announcer, ILogger<TankbookStatisticsViewModel> logger)
    {
        _service = service;
        _announcer = announcer;
        _logger = logger;
        Title = TankbookStatisticsTexts.Heading;
    }

    /// <summary>
    /// Die Blöcke je Art (Kraftstoff vor Strom); leer ohne Vorgänge.
    /// </summary>
    public IReadOnlyList<TankbookKindStatisticsViewModel> Kinds
    {
        get { return _kinds; }
        private set
        {
            if (SetProperty(ref _kinds, value))
            {
                OnPropertyChanged(nameof(HasKinds));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob es Blöcke gibt.
    /// </summary>
    public bool HasKinds
    {
        get { return Kinds.Count > 0; }
    }

    /// <summary>
    /// Ein Hinweis zum Bereich (keine Vorgänge, Ladefehler); <see langword="null"/>, wenn keiner anliegt.
    /// </summary>
    public string? Notice
    {
        get { return _notice; }
        private set
        {
            if (SetProperty(ref _notice, value))
            {
                OnPropertyChanged(nameof(HasNotice));
            }
        }
    }

    /// <summary>
    /// Gibt an, ob ein Hinweis anliegt.
    /// </summary>
    public bool HasNotice
    {
        get { return Notice is { Length: > 0 }; }
    }

    /// <summary>
    /// Lädt die Auswertung eines Fahrzeugs. Das Ergebnis eines überholten Ladevorgangs (neuerer Aufruf oder <see cref="Reset"/>) wird verworfen.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    /// <returns>Ein Task, der nach dem Laden abgeschlossen ist.</returns>
    public async Task LoadAsync(long vehicleId)
    {
        var version = ++_loadVersion;
        try
        {
            var load = await _service.GetStatisticsAsync(vehicleId).ConfigureAwait(true);
            if (version != _loadVersion)
            {
                return;
            }

            if (load.Status != TankbookStatisticsStatus.Ok || load.Statistics is not { } statistics)
            {
                Kinds = [];
                Notice = null;
                return;
            }

            Kinds = statistics.Kinds.Select(kind => new TankbookKindStatisticsViewModel(kind)).ToList();
            Notice = statistics.HasEntries ? null : TankbookStatisticsTexts.NoEntriesNotice;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (version != _loadVersion)
            {
                return;
            }

            _logger.LogWarning("Die Auswertung konnte nicht geladen werden ({ExceptionType}).", ex.GetType().Name);
            Kinds = [];
            Notice = TankbookStatisticsTexts.LoadFailed;
            _announcer.Announce(TankbookStatisticsTexts.LoadFailed);
        }
    }

    /// <summary>
    /// Setzt den Bereich zurück (kein Altzustand beim Wechsel des Fahrzeugs).
    /// </summary>
    public void Reset()
    {
        _loadVersion++;
        Kinds = [];
        Notice = null;
    }
}
