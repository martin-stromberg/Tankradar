using System.Globalization;
using Tankradar.MAUI.Models.Backup;

namespace Tankradar.MAUI.Resources.Texts;

/// <summary>
/// Zentrale deutsche UI-Texte der Karte „Sicherung“ in den Optionen einschließlich der Meldungen je Ergebnis und der Formatierung von Datum und Größe.
/// </summary>
public static class BackupTexts
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>
    /// Überschrift der Karte.
    /// </summary>
    public const string Heading = "Sicherung";

    /// <summary>
    /// Hinweis zum Umfang und zur Ablage der Sicherung.
    /// </summary>
    public const string Hint = "Eine Sicherung enthält deine Einstellungen, Favoriten, Fahrzeuge, Tank- und Ladevorgänge mit Belegfotos und die gespeicherten Preisstände. "
        + "Nicht enthalten sind der Zugangsschlüssel zum Preisdienst, Kartenkacheln und dein Standort. "
        + "Sicherungen entstehen nur auf deine Veranlassung und liegen nur auf diesem Gerät; die App überträgt sie nicht automatisch. "
        + "Wenn du eine Sicherung teilst oder speicherst, ist die Datei nicht verschlüsselt.";

    /// <summary>
    /// Beschriftung der Schaltfläche „Sicherung erstellen“.
    /// </summary>
    public const string CreateButton = "Sicherung erstellen";

    /// <summary>
    /// Beschriftung der Schaltfläche zum Einspielen einer Datei.
    /// </summary>
    public const string ImportButton = "Sicherung aus Datei einspielen";

    /// <summary>
    /// Beschriftung der Schaltfläche „Wiederherstellen“ eines Eintrags.
    /// </summary>
    public const string RestoreButton = "Wiederherstellen";

    /// <summary>
    /// Beschriftung der Schaltfläche „Teilen“ eines Eintrags.
    /// </summary>
    public const string ShareButton = "Teilen";

    /// <summary>
    /// Beschriftung der Schaltfläche „Löschen“ eines Eintrags.
    /// </summary>
    public const string DeleteButton = "Löschen";

    /// <summary>
    /// Beschriftung der Bestätigung der Wiederherstellung.
    /// </summary>
    public const string ConfirmRestore = "Ja, wiederherstellen";

    /// <summary>
    /// Beschriftung der Bestätigung des Löschens.
    /// </summary>
    public const string ConfirmDelete = "Ja, löschen";

    /// <summary>
    /// Beschriftung von „Abbrechen“ in den Rückfragen.
    /// </summary>
    public const string Cancel = "Abbrechen";

    /// <summary>
    /// Hinweis bei leerer Liste.
    /// </summary>
    public const string Empty = "Es gibt noch keine Sicherung. Mit „Sicherung erstellen“ legst du die erste an.";

    /// <summary>
    /// Kennzeichnung einer nicht lesbaren Sicherungsdatei.
    /// </summary>
    public const string Unreadable = "nicht lesbar";

    /// <summary>
    /// Meldung nach dem Erstellen.
    /// </summary>
    public const string Created = "Sicherung erstellt.";

    /// <summary>
    /// Meldung nach der Wiederherstellung.
    /// </summary>
    public const string Restored = "Sicherung wiederhergestellt. Die Daten sind auf den Stand der Sicherung zurückgesetzt.";

    /// <summary>
    /// Meldung nach dem Löschen.
    /// </summary>
    public const string Deleted = "Sicherung gelöscht.";

    /// <summary>
    /// Meldung, wenn das Erstellen fehlschlägt.
    /// </summary>
    public const string CreateFailed = "Die Sicherung konnte nicht erstellt werden. Deine Daten wurden nicht verändert.";

    /// <summary>
    /// Meldung, wenn die Liste nicht geladen werden konnte.
    /// </summary>
    public const string ListFailed = "Die Sicherungen konnten nicht gelesen werden.";

    /// <summary>
    /// Meldung, wenn das Teilen fehlschlägt.
    /// </summary>
    public const string ShareFailed = "Die Sicherung konnte nicht weitergegeben werden.";

    /// <summary>
    /// Meldung, wenn der Dateiauswahldialog fehlschlägt.
    /// </summary>
    public const string PickFailed = "Die Datei konnte nicht ausgewählt werden.";

    /// <summary>
    /// Meldung, wenn die Sicherung nicht gefunden wurde.
    /// </summary>
    public const string NotFound = "Die Sicherung wurde nicht gefunden.";

    /// <summary>
    /// Meldung, wenn bereits ein anderer Vorgang läuft.
    /// </summary>
    public const string Busy = "Es läuft bereits ein anderer Sicherungsvorgang. Bitte versuche es danach noch einmal.";

    /// <summary>
    /// Meldung bei einer ungültigen oder beschädigten Datei.
    /// </summary>
    public const string InvalidFile = "Die Datei ist keine gültige Tankatlas-Sicherung oder beschädigt. Deine Daten wurden nicht verändert.";

    /// <summary>
    /// Meldung bei einem allgemeinen Fehler der Wiederherstellung.
    /// </summary>
    public const string Failed = "Die Aktion konnte nicht ausgeführt werden. Deine Daten wurden nicht verändert.";

    /// <summary>
    /// Meldung nach erfolgreicher Übernahme einer Datei in die Liste (Hinweis zur folgenden Rückfrage).
    /// </summary>
    public const string Imported = "Die Datei ist eine gültige Sicherung und wurde in die Liste übernommen.";

    /// <summary>
    /// Liefert die Meldung zu einem Ergebnis.
    /// </summary>
    /// <param name="result">Das Ergebnis.</param>
    /// <param name="appVersion">Die App-Version der Sicherung (nur für <see cref="BackupResult.TooNew"/>); darf leer sein.</param>
    /// <returns>Der Text.</returns>
    public static string GetMessage(BackupResult result, string? appVersion = null)
    {
        return result switch
        {
            BackupResult.Ok => "Der Vorgang wurde abgeschlossen.",
            BackupResult.NotFound => NotFound,
            BackupResult.InvalidFile => InvalidFile,
            BackupResult.TooNew => FormatTooNew(appVersion),
            BackupResult.Busy => Busy,
            BackupResult.Failed => Failed,
            _ => Failed,
        };
    }

    /// <summary>
    /// Liefert die Meldung für eine zu neue Sicherung.
    /// </summary>
    /// <param name="appVersion">Die App-Version der Sicherung; darf leer sein.</param>
    /// <returns>Der Text.</returns>
    public static string FormatTooNew(string? appVersion)
    {
        var version = string.IsNullOrWhiteSpace(appVersion) ? string.Empty : $" ({appVersion})";
        return $"Diese Sicherung stammt aus einer neueren Version von Tankatlas{version}. Bitte aktualisiere die App. Deine Daten wurden nicht verändert.";
    }

    /// <summary>
    /// Formatiert den Erstellungszeitpunkt in Ortszeit, z. B. „12.10.2026 13:15“.
    /// </summary>
    /// <param name="createdUtc">Der Zeitpunkt (UTC).</param>
    /// <returns>Der Text.</returns>
    public static string FormatDate(DateTime createdUtc)
    {
        var utc = DateTime.SpecifyKind(createdUtc, DateTimeKind.Utc);
        return utc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", German);
    }

    /// <summary>
    /// Formatiert eine Dateigröße mit deutschem Dezimalzeichen, z. B. „1,2 MB“.
    /// </summary>
    /// <param name="bytes">Die Größe in Bytes.</param>
    /// <returns>Der Text.</returns>
    public static string FormatSize(long bytes)
    {
        const double Kilo = 1024d;
        if (bytes < 1024)
        {
            return string.Create(German, $"{Math.Max(bytes, 0)} B");
        }

        var value = bytes / Kilo;
        if (value < Kilo)
        {
            return string.Create(German, $"{value:0.#} KB");
        }

        value /= Kilo;
        if (value < Kilo)
        {
            return string.Create(German, $"{value:0.#} MB");
        }

        return string.Create(German, $"{value / Kilo:0.#} GB");
    }

    /// <summary>
    /// Liefert den Titel eines Listeneintrags (Datum, bei nicht lesbaren Dateien mit Kennzeichnung).
    /// </summary>
    /// <param name="info">Der Eintrag.</param>
    /// <returns>Der Text.</returns>
    public static string FormatItemTitle(BackupFileInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        var date = FormatDate(info.CreatedUtc);
        return info.IsReadable ? date : $"{date} ({Unreadable})";
    }

    /// <summary>
    /// Liefert die Rückfrage vor der Wiederherstellung mit Datum, App-Version und Zählwerten.
    /// </summary>
    /// <param name="createdUtc">Der Erstellungszeitpunkt (UTC).</param>
    /// <param name="appVersion">Die App-Version der Sicherung; darf leer sein.</param>
    /// <param name="counts">Die Zählwerte; darf <see langword="null"/> sein.</param>
    /// <returns>Der Text.</returns>
    public static string FormatRestoreQuestion(DateTime createdUtc, string? appVersion, BackupManifestCounts? counts)
    {
        var version = string.IsNullOrWhiteSpace(appVersion) ? string.Empty : $", Version {appVersion}";
        var content = counts is null ? string.Empty : $" Sie enthält {FormatCounts(counts)}.";
        return $"Sicherung vom {FormatDate(createdUtc)}{version} wiederherstellen?{content} "
            + "Die aktuellen Daten werden vollständig ersetzt. Das lässt sich nicht rückgängig machen. "
            + "Erstelle vorher eine Sicherung, wenn du den aktuellen Stand behalten möchtest.";
    }

    /// <summary>
    /// Liefert die Rückfrage vor dem Löschen einer Sicherung.
    /// </summary>
    /// <param name="createdUtc">Der Erstellungszeitpunkt (UTC).</param>
    /// <returns>Der Text.</returns>
    public static string FormatDeleteQuestion(DateTime createdUtc)
    {
        return $"Sicherung vom {FormatDate(createdUtc)} löschen? Die Sicherungsdatei wird endgültig gelöscht. Deine aktuellen Daten bleiben unverändert.";
    }

    /// <summary>
    /// Liefert die Zählwerte als Aufzählung, z. B. „2 Fahrzeuge, 14 Tank- und Ladevorgänge, …“.
    /// </summary>
    /// <param name="counts">Die Zählwerte.</param>
    /// <returns>Der Text.</returns>
    public static string FormatCounts(BackupManifestCounts counts)
    {
        ArgumentNullException.ThrowIfNull(counts);
        var parts = new[]
        {
            FormatCount(counts.Vehicles, "Fahrzeug", "Fahrzeuge"),
            FormatCount(counts.Entries, "Tank- oder Ladevorgang", "Tank- und Ladevorgänge"),
            FormatCount(counts.Groups, "Favoritengruppe", "Favoritengruppen"),
            FormatCount(counts.Favorites, "Favorit", "Favoriten"),
            FormatCount(counts.Stations, "Tankstelle", "Tankstellen"),
            FormatCount(counts.Prices, "Preisstand", "Preisstände"),
            FormatCount(counts.Photos, "Belegfoto", "Belegfotos"),
        };
        return string.Join(", ", parts[..^1]) + " und " + parts[^1];
    }

    /// <summary>
    /// Liefert den Hinweis der Schaltfläche „Wiederherstellen“ für die Sprachausgabe.
    /// </summary>
    /// <param name="createdUtc">Der Erstellungszeitpunkt (UTC).</param>
    /// <returns>Der Text.</returns>
    public static string FormatRestoreHint(DateTime createdUtc)
    {
        return $"Sicherung vom {FormatDate(createdUtc)} wiederherstellen";
    }

    /// <summary>
    /// Liefert den Hinweis der Schaltfläche „Teilen“ für die Sprachausgabe.
    /// </summary>
    /// <param name="createdUtc">Der Erstellungszeitpunkt (UTC).</param>
    /// <returns>Der Text.</returns>
    public static string FormatShareHint(DateTime createdUtc)
    {
        return $"Sicherung vom {FormatDate(createdUtc)} teilen oder speichern";
    }

    /// <summary>
    /// Liefert den Hinweis der Schaltfläche „Löschen“ für die Sprachausgabe.
    /// </summary>
    /// <param name="createdUtc">Der Erstellungszeitpunkt (UTC).</param>
    /// <returns>Der Text.</returns>
    public static string FormatDeleteHint(DateTime createdUtc)
    {
        return $"Sicherung vom {FormatDate(createdUtc)} löschen";
    }

    private static string FormatCount(int count, string singular, string plural)
    {
        return string.Create(German, $"{count} {(count == 1 ? singular : plural)}");
    }
}
