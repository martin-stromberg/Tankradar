using System.Windows.Input;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.ViewModels;

/// <summary>
/// Ein Fahrzeug in der Liste des Tankbuchs mit Name und Angaben; „Bearbeiten“ öffnet das Formular, „Löschen“ zeigt die Rückfrage im Eintrag.
/// </summary>
public sealed class VehicleItemViewModel : BaseViewModel
{
    private bool _isConfirmingDelete;
    private string _deleteQuestion = string.Empty;
    private string _confirmDeleteLabel = TankbookTexts.ConfirmDelete;
    private string _confirmDeleteDescription = string.Empty;

    /// <summary>
    /// Erstellt den Eintrag.
    /// </summary>
    /// <param name="vehicle">Das Fahrzeug.</param>
    /// <param name="edit">Öffnet das Formular zum Bearbeiten dieses Fahrzeugs.</param>
    /// <param name="delete">Startet das Löschen (liest die Zahl der Vorgänge und zeigt die Rückfrage).</param>
    /// <param name="confirmDelete">Löscht das Fahrzeug nach der Bestätigung.</param>
    /// <param name="canConfirmDelete">Gibt an, ob die Bestätigung gerade ausführbar ist (gesperrt, solange eine Änderung gespeichert wird).</param>
    public VehicleItemViewModel(
        Vehicle vehicle,
        Action<VehicleItemViewModel> edit,
        Action<VehicleItemViewModel> delete,
        Action<VehicleItemViewModel> confirmDelete,
        Func<bool> canConfirmDelete)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        Vehicle = vehicle;
        Details = TankbookTexts.FormatVehicleDetails(vehicle);
        EditDescription = TankbookTexts.FormatEditDescription(vehicle.Name);
        DeleteDescription = TankbookTexts.FormatDeleteDescription(vehicle.Name);
        CancelDeleteDescription = TankbookTexts.FormatCancelDeleteDescription(vehicle.Name);
        EditCommand = new Command(() => edit(this));
        DeleteCommand = new Command(() => delete(this));
        ConfirmDeleteCommand = new Command(() => confirmDelete(this), canConfirmDelete);
        CancelDeleteCommand = new Command(HideDeleteConfirmation);
    }

    /// <summary>
    /// Das Fahrzeug.
    /// </summary>
    public Vehicle Vehicle { get; }

    /// <summary>
    /// Die Kennung des Fahrzeugs (nur intern, wird nie angezeigt).
    /// </summary>
    public long Id
    {
        get { return Vehicle.Id; }
    }

    /// <summary>
    /// Der Name des Fahrzeugs.
    /// </summary>
    public string Name
    {
        get { return Vehicle.Name; }
    }

    /// <summary>
    /// Die Angaben des Fahrzeugs, z. B. „Diesel • Start 84.350 km • Startverbrauch 5,4 L/100 km“.
    /// </summary>
    public string Details { get; }

    /// <summary>
    /// Beschreibung der Schaltfläche „Bearbeiten“ für die Sprachausgabe, z. B. „Golf bearbeiten“.
    /// </summary>
    public string EditDescription { get; }

    /// <summary>
    /// Beschreibung der Schaltfläche „Löschen“ für die Sprachausgabe, z. B. „Golf löschen“.
    /// </summary>
    public string DeleteDescription { get; }

    /// <summary>
    /// Beschreibung der Schaltfläche „Abbrechen“ der Rückfrage für die Sprachausgabe.
    /// </summary>
    public string CancelDeleteDescription { get; }

    /// <summary>
    /// Beschreibung der Bestätigung der Rückfrage für die Sprachausgabe; nennt den Fahrzeugnamen und gegebenenfalls das Mitlöschen der Vorgänge.
    /// </summary>
    public string ConfirmDeleteDescription
    {
        get => _confirmDeleteDescription;
        private set => SetProperty(ref _confirmDeleteDescription, value);
    }

    /// <summary>
    /// Gibt an, ob die Rückfrage zum Löschen angezeigt wird.
    /// </summary>
    public bool IsConfirmingDelete
    {
        get => _isConfirmingDelete;
        private set => SetProperty(ref _isConfirmingDelete, value);
    }

    /// <summary>
    /// Die Rückfrage vor dem Löschen; nennt bei vorhandenen Vorgängen deren Zahl und das Mitlöschen.
    /// </summary>
    public string DeleteQuestion
    {
        get => _deleteQuestion;
        private set => SetProperty(ref _deleteQuestion, value);
    }

    /// <summary>
    /// Die Beschriftung der Bestätigung: „Ja, löschen“ ohne Vorgänge, sonst „Fahrzeug und Vorgänge löschen“.
    /// </summary>
    public string ConfirmDeleteLabel
    {
        get => _confirmDeleteLabel;
        private set => SetProperty(ref _confirmDeleteLabel, value);
    }

    /// <summary>
    /// Befehl „Bearbeiten“.
    /// </summary>
    public ICommand EditCommand { get; }

    /// <summary>
    /// Befehl „Löschen“: zeigt die Rückfrage.
    /// </summary>
    public ICommand DeleteCommand { get; }

    /// <summary>
    /// Befehl der Bestätigung der Rückfrage.
    /// </summary>
    public ICommand ConfirmDeleteCommand { get; }

    /// <summary>
    /// Befehl „Abbrechen“ der Rückfrage.
    /// </summary>
    public ICommand CancelDeleteCommand { get; }

    /// <summary>
    /// Zeigt die Rückfrage zum Löschen passend zur Zahl der Vorgänge.
    /// </summary>
    /// <param name="entryCount">Die Zahl der Tank- und Ladevorgänge des Fahrzeugs.</param>
    public void ShowDeleteConfirmation(int entryCount)
    {
        DeleteQuestion = TankbookTexts.FormatDeleteQuestion(Name, entryCount);
        ConfirmDeleteLabel = entryCount > 0 ? TankbookTexts.ConfirmDeleteWithEntries : TankbookTexts.ConfirmDelete;
        ConfirmDeleteDescription = TankbookTexts.FormatConfirmDeleteDescription(Name, entryCount);
        IsConfirmingDelete = true;
    }

    /// <summary>
    /// Blendet die Rückfrage zum Löschen aus.
    /// </summary>
    public void HideDeleteConfirmation()
    {
        IsConfirmingDelete = false;
    }

    /// <summary>
    /// Aktualisiert die Ausführbarkeit der Bestätigung, nachdem sich die Sperre während des Speicherns geändert hat.
    /// </summary>
    public void RefreshCommandStates()
    {
        ((Command)ConfirmDeleteCommand).ChangeCanExecute();
    }
}
