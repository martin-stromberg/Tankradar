namespace Tankradar.MAUI.Services.Diagnostics;

/// <summary>
/// Unverfängliche Rahmendaten des Diagnosepakets (keine Standort-, Stations- oder Benutzerdaten).
/// </summary>
/// <param name="AppVersion">Die App-Version.</param>
/// <param name="OperatingSystem">Die Betriebssystembezeichnung.</param>
/// <param name="Device">Das Gerätemodell (Hersteller und Modell).</param>
/// <param name="Runtime">Die Laufzeitversion (.NET).</param>
/// <returns>Der Wert.</returns>
public sealed record DiagnosticsEnvironmentInfo(string AppVersion, string OperatingSystem, string Device, string Runtime);
