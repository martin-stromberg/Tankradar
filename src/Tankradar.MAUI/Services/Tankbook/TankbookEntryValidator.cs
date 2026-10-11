using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Prüft die Angaben eines Tank- oder Ladevorgangs: feldbezogen für das Formular und als Schutzprüfung des Dienstes.
/// Die Kilometerregeln gegenüber Fahrzeug, Vorgänger und Nachfolger prüft der Dienst, weil sie gespeicherte Daten brauchen.
/// </summary>
public static class TankbookEntryValidator
{
    /// <summary>
    /// Prüft die Felder des Formulars; alle Feldfehler werden gleichzeitig ermittelt. Bei Erfolg enthält die Eingabe den aus zwei Werten berechneten dritten Wert.
    /// </summary>
    /// <param name="occurredAt">Datum und Uhrzeit (Sekunden werden verworfen) oder <see langword="null"/>, wenn nichts gewählt ist.</param>
    /// <param name="now">Die aktuelle lokale Zeit.</param>
    /// <param name="kind">Die gewählte Art.</param>
    /// <param name="stationName">Der Name der Tankstelle bzw. des Ladeorts.</param>
    /// <param name="stationId">Die Kennung einer ausgewählten Favoriten-Tankstelle oder <see langword="null"/>.</param>
    /// <param name="quantityText">Die Eingabe der Menge.</param>
    /// <param name="unitPriceText">Die Eingabe des Preises pro Einheit.</param>
    /// <param name="totalAmountText">Die Eingabe des Gesamtbetrags.</param>
    /// <param name="odometerText">Die Eingabe des Kilometerstands.</param>
    /// <param name="isPartial">Gibt an, ob es eine Teilbetankung ist.</param>
    /// <param name="note">Die Notiz.</param>
    /// <param name="receiptPhotoFileName">Der Dateiname des Belegfotos oder <see langword="null"/>.</param>
    /// <returns>Die Feldfehler und bei Erfolg die geprüfte Eingabe.</returns>
    public static TankbookEntryFormValidation ValidateForm(
        DateTime? occurredAt,
        DateTime now,
        TankbookEntryKind kind,
        string? stationName,
        string? stationId,
        string? quantityText,
        string? unitPriceText,
        string? totalAmountText,
        string? odometerText,
        bool isPartial,
        string? note,
        string? receiptPhotoFileName)
    {
        var occurredAtError = TankbookEntryFieldError.None;
        var occurred = occurredAt is { } value ? TruncateToMinute(value) : default;
        if (occurredAt is null)
        {
            occurredAtError = TankbookEntryFieldError.Required;
        }
        else if (IsInFuture(occurred, now))
        {
            occurredAtError = TankbookEntryFieldError.InFuture;
        }

        var trimmedStation = stationName?.Trim() ?? string.Empty;
        var stationError = trimmedStation.Length switch
        {
            0 => TankbookEntryFieldError.Required,
            > TankbookEntryLimits.MaxStationNameLength => TankbookEntryFieldError.TooLong,
            _ => TankbookEntryFieldError.None,
        };

        var quantityError = DecimalInputParser.Parse(quantityText, TankbookEntryLimits.MaxQuantityDecimals, TankbookEntryLimits.MaxQuantityExclusive, out var quantity);
        var unitPriceError = DecimalInputParser.Parse(unitPriceText, TankbookEntryLimits.MaxUnitPriceDecimals, TankbookEntryLimits.MaxUnitPriceExclusive, out var unitPrice);
        var totalAmountError = DecimalInputParser.Parse(totalAmountText, TankbookEntryLimits.MaxTotalAmountDecimals, TankbookEntryLimits.MaxTotalAmountExclusive, out var totalAmount);
        AmountCompletion? completion = null;
        decimal? expectedTotal = null;
        if (quantityError == TankbookEntryFieldError.None && unitPriceError == TankbookEntryFieldError.None && totalAmountError == TankbookEntryFieldError.None)
        {
            completion = AmountCalculator.Complete(quantity, unitPrice, totalAmount);
            if (completion.IsMissing)
            {
                totalAmountError = TankbookEntryFieldError.MissingAmounts;
            }
            else if (completion.HasMismatch)
            {
                totalAmountError = TankbookEntryFieldError.AmountMismatch;
                expectedTotal = completion.ExpectedTotal;
            }
            else
            {
                // Ein berechneter Wert kann außerhalb seines Bereichs liegen (z. B. Preis pro Einheit aus einem großen Gesamtbetrag); der Fehler steht dann am berechneten Feld.
                switch (completion.ComputedField)
                {
                    case AmountField.Quantity when !IsInRange(completion.Quantity, TankbookEntryLimits.MaxQuantityExclusive):
                        quantityError = TankbookEntryFieldError.OutOfRange;
                        break;
                    case AmountField.UnitPrice when !IsInRange(completion.UnitPrice, TankbookEntryLimits.MaxUnitPriceExclusive):
                        unitPriceError = TankbookEntryFieldError.OutOfRange;
                        break;
                    case AmountField.TotalAmount when !IsInRange(completion.TotalAmount, TankbookEntryLimits.MaxTotalAmountExclusive):
                        totalAmountError = TankbookEntryFieldError.OutOfRange;
                        break;
                }
            }
        }

        var odometerError = ToEntryError(VehicleInputParser.ParseOdometer(odometerText, out var odometer));
        var trimmedNote = note?.Trim() ?? string.Empty;
        var noteError = trimmedNote.Length > TankbookEntryLimits.MaxNoteLength ? TankbookEntryFieldError.TooLong : TankbookEntryFieldError.None;

        var validation = new TankbookEntryFormValidation(occurredAtError, stationError, quantityError, unitPriceError, totalAmountError, odometerError, noteError, expectedTotal, null);
        if (validation.HasErrors || completion is null)
        {
            return validation;
        }

        var input = new TankbookEntryInput(
            occurred,
            kind,
            completion.Quantity!.Value,
            completion.UnitPrice!.Value,
            completion.TotalAmount!.Value,
            odometer,
            isPartial,
            trimmedStation,
            string.IsNullOrWhiteSpace(stationId) ? null : stationId,
            trimmedNote.Length == 0 ? null : trimmedNote,
            string.IsNullOrWhiteSpace(receiptPhotoFileName) ? null : receiptPhotoFileName);
        return validation with { Input = input };
    }

    /// <summary>
    /// Prüft eine Eingabe vor dem Speichern (Schutzprüfung des Dienstes, ohne Kilometerregeln und ohne Art je Fahrzeug).
    /// </summary>
    /// <param name="input">Die Eingabe.</param>
    /// <param name="now">Die aktuelle lokale Zeit.</param>
    /// <returns><see cref="TankbookEntryResult.Ok"/> bei gültiger Eingabe, sonst der erste gefundene Fehler.</returns>
    public static TankbookEntryResult Validate(TankbookEntryInput input, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.OccurredAt == default || IsInFuture(input.OccurredAt, now))
        {
            return TankbookEntryResult.InvalidOccurredAt;
        }

        if (!Enum.IsDefined(input.Kind))
        {
            return TankbookEntryResult.KindNotAllowed;
        }

        if (DecimalInputParser.Check(input.Quantity, TankbookEntryLimits.MaxQuantityDecimals, TankbookEntryLimits.MaxQuantityExclusive) != TankbookEntryFieldError.None)
        {
            return TankbookEntryResult.InvalidQuantity;
        }

        if (DecimalInputParser.Check(input.UnitPrice, TankbookEntryLimits.MaxUnitPriceDecimals, TankbookEntryLimits.MaxUnitPriceExclusive) != TankbookEntryFieldError.None)
        {
            return TankbookEntryResult.InvalidUnitPrice;
        }

        if (DecimalInputParser.Check(input.TotalAmount, TankbookEntryLimits.MaxTotalAmountDecimals, TankbookEntryLimits.MaxTotalAmountExclusive) != TankbookEntryFieldError.None)
        {
            return TankbookEntryResult.InvalidTotalAmount;
        }

        if (!AmountCalculator.IsConsistent(input.Quantity, input.UnitPrice, input.TotalAmount))
        {
            return TankbookEntryResult.AmountMismatch;
        }

        if (input.OdometerKm < 0 || input.OdometerKm > VehicleLimits.MaxOdometerKm)
        {
            return TankbookEntryResult.InvalidOdometer;
        }

        var station = input.StationName?.Trim() ?? string.Empty;
        if (station.Length == 0 || station.Length > TankbookEntryLimits.MaxStationNameLength)
        {
            return TankbookEntryResult.InvalidStationName;
        }

        return (input.Note?.Trim().Length ?? 0) > TankbookEntryLimits.MaxNoteLength ? TankbookEntryResult.NoteTooLong : TankbookEntryResult.Ok;
    }

    /// <summary>
    /// Gibt an, ob eine Art zur Kraftstoffart eines Fahrzeugs passt: Benzin und Diesel nur Kraftstoff, Elektro nur Strom, Hybrid beides.
    /// </summary>
    /// <param name="fuelKind">Die Kraftstoffart des Fahrzeugs.</param>
    /// <param name="kind">Die Art des Vorgangs.</param>
    /// <returns><see langword="true"/>, wenn die Art erlaubt ist.</returns>
    public static bool IsKindAllowed(VehicleFuelKind fuelKind, TankbookEntryKind kind)
    {
        return fuelKind switch
        {
            VehicleFuelKind.Petrol => kind == TankbookEntryKind.Fuel,
            VehicleFuelKind.Diesel => kind == TankbookEntryKind.Fuel,
            VehicleFuelKind.Electric => kind == TankbookEntryKind.Electricity,
            VehicleFuelKind.Hybrid => kind is TankbookEntryKind.Fuel or TankbookEntryKind.Electricity,
            _ => false,
        };
    }

    /// <summary>
    /// Verwirft Sekunden und Sekundenbruchteile eines Zeitpunkts.
    /// </summary>
    /// <param name="value">Der Zeitpunkt.</param>
    /// <returns>Der Zeitpunkt minutengenau, mit <see cref="DateTimeKind.Unspecified"/>.</returns>
    public static DateTime TruncateToMinute(DateTime value)
    {
        return new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, DateTimeKind.Unspecified);
    }

    private static bool IsInFuture(DateTime occurredAt, DateTime now)
    {
        return occurredAt > now + TankbookEntryLimits.FutureTolerance;
    }

    private static bool IsInRange(decimal? value, decimal maxExclusive)
    {
        return value is { } number && number > 0m && number < maxExclusive;
    }

    private static TankbookEntryFieldError ToEntryError(VehicleFieldError error)
    {
        return error switch
        {
            VehicleFieldError.None => TankbookEntryFieldError.None,
            VehicleFieldError.Required => TankbookEntryFieldError.Required,
            VehicleFieldError.NotANumber => TankbookEntryFieldError.NotANumber,
            VehicleFieldError.Negative => TankbookEntryFieldError.Negative,
            VehicleFieldError.OutOfRange => TankbookEntryFieldError.OutOfRange,
            VehicleFieldError.TooManyDecimals => TankbookEntryFieldError.TooManyDecimals,
            VehicleFieldError.TooLong => TankbookEntryFieldError.TooLong,
            _ => TankbookEntryFieldError.NotANumber,
        };
    }
}
