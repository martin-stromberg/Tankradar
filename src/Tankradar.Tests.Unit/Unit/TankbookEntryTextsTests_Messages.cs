using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Feld-, Foto- und Ergebnismeldungen von <see cref="TankbookEntryTexts"/> für alle Enum-Werte.
/// </summary>
public class TankbookEntryTextsTests_Messages : BaseTest
{
    /// <summary>
    /// Prüft die Meldungen aller Feldfehler, Fotofehler und Ergebnisse; Kilometerfehler nennen den Grenzwert.
    /// </summary>
    [Fact]
    public void FieldAndResultMessages_CoverAllValues()
    {
        Assert.Equal("Bitte wähle Datum und Uhrzeit.", TankbookEntryTexts.GetOccurredAtError(TankbookEntryFieldError.Required));
        Assert.Equal("Der Zeitpunkt darf nicht in der Zukunft liegen.", TankbookEntryTexts.GetOccurredAtError(TankbookEntryFieldError.InFuture));
        Assert.Equal("Der Name der Tankstelle darf höchstens 60 Zeichen lang sein.", TankbookEntryTexts.GetStationError(TankbookEntryFieldError.TooLong));
        Assert.Equal("Bitte gib die Menge in kWh als Zahl ein, z. B. 40,5.", TankbookEntryTexts.GetQuantityError(TankbookEntryFieldError.NotANumber, TankbookEntryKind.Electricity));
        Assert.Equal("Die Menge darf nicht negativ sein.", TankbookEntryTexts.GetQuantityError(TankbookEntryFieldError.Negative, TankbookEntryKind.Fuel));
        Assert.Equal("Die Menge muss größer als 0 und kleiner als 1.000 L sein.", TankbookEntryTexts.GetQuantityError(TankbookEntryFieldError.OutOfRange, TankbookEntryKind.Fuel));
        Assert.Equal("Die Menge darf höchstens 3 Nachkommastellen haben.", TankbookEntryTexts.GetQuantityError(TankbookEntryFieldError.TooManyDecimals, TankbookEntryKind.Fuel));
        Assert.Equal("Der Preis muss größer als 0 und kleiner als 10 €/L sein.", TankbookEntryTexts.GetUnitPriceError(TankbookEntryFieldError.OutOfRange, TankbookEntryKind.Fuel));
        Assert.Equal("Der Gesamtbetrag darf höchstens 2 Nachkommastellen haben.", TankbookEntryTexts.GetTotalAmountError(TankbookEntryFieldError.TooManyDecimals, TankbookEntryKind.Fuel, null, null));
        Assert.Equal(
            "Bitte gib mindestens zwei der drei Werte Menge, Preis pro Liter und Gesamtbetrag an.",
            TankbookEntryTexts.GetTotalAmountError(TankbookEntryFieldError.MissingAmounts, TankbookEntryKind.Fuel, null, null));
        Assert.Equal(
            "Menge × Preis ergibt 74,36 €, nicht 75,00 €. Bitte prüfe die Werte.",
            TankbookEntryTexts.GetTotalAmountError(TankbookEntryFieldError.AmountMismatch, TankbookEntryKind.Fuel, 74.36m, 75m));
        Assert.Equal("Bitte gib den Kilometerstand ein.", TankbookEntryTexts.GetOdometerError(TankbookEntryFieldError.Required));
        Assert.Equal("Die Notiz darf höchstens 500 Zeichen lang sein.", TankbookEntryTexts.GetNoteError(TankbookEntryFieldError.TooLong));
        Assert.Equal(string.Empty, TankbookEntryTexts.GetNoteError(TankbookEntryFieldError.None));

        Assert.Equal(string.Empty, TankbookEntryTexts.GetPhotoError(ReceiptPhotoError.None));
        Assert.Equal("Das Foto ist zu groß (höchstens 20 MB).", TankbookEntryTexts.GetPhotoError(ReceiptPhotoError.TooLarge));
        Assert.Equal("Dieses Dateiformat wird nicht unterstützt. Bitte wähle ein JPG-, PNG- oder HEIC-Foto.", TankbookEntryTexts.GetPhotoError(ReceiptPhotoError.UnsupportedFormat));
        Assert.Equal("Das Foto konnte nicht übernommen werden.", TankbookEntryTexts.GetPhotoError(ReceiptPhotoError.Failed));
        Assert.Equal("Kein Zugriff auf die Kamera. Erlaube den Zugriff in den Einstellungen unter Tankatlas.", TankbookEntryTexts.GetPhotoAccessError(ReceiptPhotoAccessProblem.PermissionDenied, capture: true));
        Assert.Equal("Kein Zugriff auf die Fotos. Erlaube den Zugriff in den Einstellungen unter Tankatlas.", TankbookEntryTexts.GetPhotoAccessError(ReceiptPhotoAccessProblem.PermissionDenied, capture: false));
        Assert.Equal("Auf diesem Gerät ist keine Fotoaufnahme möglich. Du kannst stattdessen ein Foto auswählen.", TankbookEntryTexts.GetPhotoAccessError(ReceiptPhotoAccessProblem.NotSupported, capture: true));
        Assert.Equal("Auf diesem Gerät ist keine Fotoauswahl möglich.", TankbookEntryTexts.GetPhotoAccessError(ReceiptPhotoAccessProblem.NotSupported, capture: false));

        Assert.Equal(string.Empty, TankbookEntryTexts.GetResultMessage(TankbookEntryResult.Ok, null));
        Assert.Equal("Der Kilometerstand darf nicht unter dem Start-Kilometerstand (10.000 km) liegen.", TankbookEntryTexts.GetResultMessage(TankbookEntryResult.OdometerBelowVehicleStart, 10_000));
        Assert.Equal("Der Kilometerstand darf nicht unter 10.500 km (vorheriger Vorgang) liegen.", TankbookEntryTexts.GetResultMessage(TankbookEntryResult.OdometerBelowPrevious, 10_500));
        Assert.Equal("Der Kilometerstand darf nicht über 11.000 km (nächster Vorgang) liegen.", TankbookEntryTexts.GetResultMessage(TankbookEntryResult.OdometerAboveNext, 11_000));
        Assert.Equal("Das Fahrzeug gibt es nicht mehr.", TankbookEntryTexts.GetResultMessage(TankbookEntryResult.VehicleNotFound, null));
        Assert.Equal("Den Vorgang gibt es nicht mehr.", TankbookEntryTexts.GetResultMessage(TankbookEntryResult.EntryNotFound, null));

        TankbookEntryResult[] others =
        [
            TankbookEntryResult.InvalidOccurredAt,
            TankbookEntryResult.KindNotAllowed,
            TankbookEntryResult.InvalidQuantity,
            TankbookEntryResult.InvalidUnitPrice,
            TankbookEntryResult.InvalidTotalAmount,
            TankbookEntryResult.AmountMismatch,
            TankbookEntryResult.InvalidOdometer,
            TankbookEntryResult.InvalidStationName,
            TankbookEntryResult.NoteTooLong,
        ];
        foreach (var result in others.Concat(Enum.GetValues<TankbookEntryResult>().Where(value => value != TankbookEntryResult.Ok)))
        {
            Assert.NotEmpty(TankbookEntryTexts.GetResultMessage(result, 1));
        }

        foreach (var error in Enum.GetValues<TankbookEntryFieldError>().Where(value => value is not TankbookEntryFieldError.None))
        {
            var texts = new[]
            {
                TankbookEntryTexts.GetOccurredAtError(error),
                TankbookEntryTexts.GetStationError(error),
                TankbookEntryTexts.GetQuantityError(error, TankbookEntryKind.Fuel),
                TankbookEntryTexts.GetUnitPriceError(error, TankbookEntryKind.Fuel),
                TankbookEntryTexts.GetTotalAmountError(error, TankbookEntryKind.Fuel, 1m, 2m),
                TankbookEntryTexts.GetOdometerError(error),
                TankbookEntryTexts.GetNoteError(error),
            };
            Assert.Contains(texts, text => text.Length > 0);
        }
    }

    /// <summary>
    /// Prüft, dass der Hinweis zur Teilbetankung den ersten vollen Vorgang und die Kosten nennt.
    /// </summary>
    [Fact]
    public void PartialHint_MentionsFirstFullEntryAndCosts()
    {
        Assert.Contains("beim ersten vollen Vorgang ab dem Start-Kilometerstand", TankbookEntryTexts.PartialHint, StringComparison.Ordinal);
        Assert.Contains("fließt in den nächsten vollen Vorgang ein", TankbookEntryTexts.PartialHint, StringComparison.Ordinal);
        Assert.Contains("Monatskosten", TankbookEntryTexts.PartialHint, StringComparison.Ordinal);
    }
}
