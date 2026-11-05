using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;
using Tankradar.Tests.E2E;
using Tankradar.Tests.E2E.E2E.FlaUI;
using Tankradar.TestSupport;

namespace PagesScreenshots;

/// <summary>
/// Fährt die im Testmodus laufende App über die vorhandenen E2E-Hilfsmethoden (Mock-Preisdienst, -Ortssuche und -Routing)
/// durch ihre Bereiche und nimmt dabei echte Fenster-Screenshots für die GitHub-Pages-Präsentation auf.
/// </summary>
public sealed class CaptureSession : HomeE2ETestBase
{
    private const int PortraitWidth = 640;
    private const int PortraitHeight = 880;
    private static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(1500);

    /// <summary>
    /// Startet die App einmalig mit den Mock-Diensten und dann erneut mit denselben Daten, aber dem echten
    /// OpenStreetMap-Kachelserver: So zeigt der Karten-Screenshot eine echte Karte statt der einfarbigen Testkachel.
    /// </summary>
    public CaptureSession()
    {
        var environment = CreateEnvironment(
            Server.BaseUrl,
            TestLocation,
            MockTankerkoenigServer.AcceptedKey,
            geocodingUrl: Geocoding.BaseUrl,
            tileUrl: new Uri("https://tile.openstreetmap.org/"),
            routingUrl: Routing.BaseUrl,
            routingCredential: MockOpenRouteServiceServer.AcceptedKey);
        RestartApplication(environment);
    }

    /// <summary>
    /// Erzeugt die fünf Screenshots der Präsentationsseite im angegebenen Verzeichnis.
    /// </summary>
    /// <param name="outputDirectory">Das Zielverzeichnis der PNG-Dateien.</param>
    public void CaptureAll(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        TryResizeWindow(PortraitWidth, PortraitHeight);

        // 1. Favoriten: zwei Gruppen über die echte Bedienung anlegen und die Startseite zeigen.
        CreateTwoGroups();
        OpenHome("Arbeitsweg", "Heimat");
        Capture(outputDirectory, "favoriten");

        // 2. Karte: Umkreissuche ausführen; Trefferliste und Preismarkierungen bleiben sichtbar.
        // Zusätzlich warten, bis die echten Kartenkacheln nachgeladen sind.
        OpenSearch();
        Submit();
        WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
        Thread.Sleep(4000);
        Capture(outputDirectory, "karte");

        // 3. Tankstellen-Details der ersten Tankstelle.
        OpenStationDetail(0, "Alpha Tankstelle");
        Capture(outputDirectory, "details");
        LeaveDetail();
        NavigateToTab("Favoriten");

        // 4. Tankbuch: Fahrzeug über die Oberfläche anlegen, realistische Vorgänge über mehrere Wochen in die
        // Testdatenbank schreiben und die Ansicht neu laden (Neustart setzt die Fenstergröße zurück).
        var tankbook = new TankbookE2EDriver(() => MainWindow, SetFieldValue, WaitUntil, NavigateToTab);
        tankbook.OpenTankbook();
        tankbook.CreateVehicle("Golf VII", "Diesel", "84350", "5,4");
        tankbook.LeaveVehicleManagement();
        SeedTankbookEntries("Golf VII");
        RestartApplication();
        TryResizeWindow(PortraitWidth, PortraitHeight);
        tankbook.OpenTab();
        Capture(outputDirectory, "tankbuch");

        // 5. Optionen: zur Datenquellen-Karte scrollen, sodass die Sicherungskarte darüber sichtbar bleibt.
        OpenSettings();
        ScrollIntoView("Settings.Attribution");
        Capture(outputDirectory, "optionen");
    }

    /// <summary>
    /// Schreibt fünf plausible Tankvorgänge über zwei Monate in die Testdatenbank des Fahrzeugs
    /// (aufsteigender Kilometerstand, wechselnde Stationen und Preise).
    /// </summary>
    /// <param name="vehicleName">Der Name des zuvor angelegten Fahrzeugs.</param>
    private void SeedTankbookEntries(string vehicleName)
    {
        var today = DateTime.Today;
        (int DaysAgo, string Station, decimal Quantity, decimal UnitPrice, int OdometerKm)[] entries =
        [
            (56, "Alpha Tankstelle", 41.2m, 1.719m, 84500),
            (40, "Beta Tankstelle", 38.6m, 1.679m, 85120),
            (25, "Alpha Tankstelle", 43.4m, 1.829m, 85800),
            (11, "Beta Tankstelle", 39.8m, 1.749m, 86420),
            (2, "Alpha Tankstelle", 44.1m, 1.689m, 87060),
        ];

        using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=false");
        connection.Open();
        using var lookup = connection.CreateCommand();
        lookup.CommandText = "SELECT Id FROM Vehicles WHERE Name = $name";
        lookup.Parameters.AddWithValue("$name", vehicleName);
        var vehicleId = Convert.ToInt64(lookup.ExecuteScalar(), CultureInfo.InvariantCulture);
        if (vehicleId == 0)
        {
            throw new InvalidOperationException($"Das Fahrzeug '{vehicleName}' fehlt in der Testdatenbank.");
        }

        using var transaction = connection.BeginTransaction();
        foreach (var entry in entries)
        {
            var total = Math.Round(entry.Quantity * entry.UnitPrice, 2);
            var occurred = today.AddDays(-entry.DaysAgo).AddHours(18).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText =
                "INSERT INTO TankbookEntries (VehicleId, CreatedUtc, OccurredAt, Kind, Quantity, UnitPrice, TotalAmount, OdometerKm, IsPartial, StationName) "
                + "VALUES ($vehicleId, $created, $occurred, 'Fuel', $quantity, $unitPrice, $total, $odometer, 0, $station)";
            insert.Parameters.AddWithValue("$vehicleId", vehicleId);
            insert.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            insert.Parameters.AddWithValue("$occurred", occurred);
            insert.Parameters.AddWithValue("$quantity", entry.Quantity.ToString("0.0", CultureInfo.InvariantCulture));
            insert.Parameters.AddWithValue("$unitPrice", entry.UnitPrice.ToString("0.000", CultureInfo.InvariantCulture));
            insert.Parameters.AddWithValue("$total", total.ToString("0.00", CultureInfo.InvariantCulture));
            insert.Parameters.AddWithValue("$odometer", entry.OdometerKm);
            insert.Parameters.AddWithValue("$station", entry.Station);
            insert.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    /// <summary>
    /// Scrollt das Element mit der AutomationId in den sichtbaren Bereich seiner ScrollView (kein Mausklick nötig).
    /// </summary>
    /// <param name="automationId">Die AutomationId des Zielelements.</param>
    private void ScrollIntoView(string automationId)
    {
        var element = MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
        element?.Patterns.ScrollItem.PatternOrDefault?.ScrollIntoView();
    }

    /// <summary>
    /// Nimmt das App-Fenster per <c>PrintWindow</c> auf (funktioniert auch im Off-Screen-Betrieb) und speichert es als PNG.
    /// Vor der Aufnahme wird kurz gewartet, damit Seitenübergänge und nachgeladene Inhalte abgeschlossen sind.
    /// </summary>
    /// <param name="directory">Das Zielverzeichnis.</param>
    /// <param name="name">Der Dateiname ohne Endung.</param>
    private void Capture(string directory, string name)
    {
        Thread.Sleep(SettleDelay);

        var handle = MainWindow.Properties.NativeWindowHandle.ValueOrDefault;
        if (handle == IntPtr.Zero || !GetWindowRect(handle, out var rect))
        {
            throw new InvalidOperationException("Das App-Fenster ist nicht aufnehmbar.");
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width < 100 || height < 100)
        {
            throw new InvalidOperationException($"Das App-Fenster ist zu klein für einen Screenshot ({width}x{height}).");
        }

        var path = Path.Combine(directory, name + ".png");
        using var bitmap = new Bitmap(width, height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            var deviceContext = graphics.GetHdc();
            try
            {
                if (!PrintWindow(handle, deviceContext, PwRenderFullContent))
                {
                    throw new InvalidOperationException($"PrintWindow für '{name}' fehlgeschlagen.");
                }
            }
            finally
            {
                graphics.ReleaseHdc(deviceContext);
            }
        }

        bitmap.Save(path, ImageFormat.Png);
        Console.WriteLine($"Screenshot gespeichert: {path}");
    }

    /// <summary>
    /// Stellt das App-Fenster auf ein hochformatiges Maß, damit die Screenshots dem mobilen Erscheinungsbild näherkommen.
    /// Nicht unterstützte Größenänderungen sind unkritisch: die Aufnahme erfolgt dann in der Standardgröße.
    /// </summary>
    /// <param name="width">Die Zielbreite des Fensters in Pixeln.</param>
    /// <param name="height">Die Zielhöhe des Fensters in Pixeln.</param>
    private void TryResizeWindow(int width, int height)
    {
        var handle = MainWindow.Properties.NativeWindowHandle.ValueOrDefault;
        if (handle != IntPtr.Zero)
        {
            MoveWindow(handle, 0, 0, width, height, repaint: true);
        }
    }

    private const uint PwRenderFullContent = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, [MarshalAs(UnmanagedType.Bool)] bool repaint);
}
