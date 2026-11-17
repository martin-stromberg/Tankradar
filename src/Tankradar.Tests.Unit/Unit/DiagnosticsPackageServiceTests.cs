using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Services.Diagnostics;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Erzeugen des Diagnosepakets: ZIP-Inhalt, Redaktion, Begrenzungen und Retention.
/// </summary>
public class DiagnosticsPackageServiceTests : BaseTest
{
    private static DiagnosticsEnvironmentInfo Env()
    {
        return new DiagnosticsEnvironmentInfo("1.2.3", "TestOS", "TestGerät", ".NET 10.0.0");
    }

    private DiagnosticsPackageService CreateService(TempDirectory temp, DiagnosticsLogBuffer buffer, FakeSensitiveValueProvider secrets)
    {
        return new DiagnosticsPackageService(
            buffer,
            secrets,
            new ManualTimeProvider(),
            Env,
            temp.Combine("cache", DiagnosticsPackageService.DirectoryName),
            new ListLogger<DiagnosticsPackageService>());
    }

    private static async Task<Dictionary<string, string>> ReadEntriesAsync(string zipPath)
    {
        await using var stream = File.OpenRead(zipPath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var result = new Dictionary<string, string>();
        foreach (var entry in archive.Entries)
        {
            using var reader = new StreamReader(entry.Open());
            result[entry.Name] = await reader.ReadToEndAsync();
        }

        return result;
    }

    /// <summary>
    /// Prüft, dass das Paket die drei erwarteten Einträge mit den Rahmendaten enthält.
    /// </summary>
    [Fact]
    public async Task CreatePackage_WritesExpectedEntries()
    {
        using var temp = new TempDirectory();
        var service = CreateService(temp, new DiagnosticsLogBuffer(new ManualTimeProvider()), new FakeSensitiveValueProvider());

        var path = await service.CreatePackageAsync();

        Assert.True(File.Exists(path));
        Assert.StartsWith(DiagnosticsPackageService.FileNamePrefix, Path.GetFileName(path));
        var entries = await ReadEntriesAsync(path);
        Assert.Equal(["liesmich.txt", "paket-info.json", "protokoll.txt"], entries.Keys.OrderBy(k => k));
        Assert.Contains("1.2.3", entries["paket-info.json"]);
        Assert.Contains("TestOS", entries["paket-info.json"]);
        Assert.Contains("Tankatlas-Diagnosepaket", entries["liesmich.txt"]);
    }

    /// <summary>
    /// Prüft, dass gepufferte Protokolleinträge redigiert ins Paket übernommen werden.
    /// </summary>
    [Fact]
    public async Task CreatePackage_RedactsLogEntries()
    {
        using var temp = new TempDirectory();
        var buffer = new DiagnosticsLogBuffer(new ManualTimeProvider());
        const string key = "geheim-schluessel-4711";
        buffer.Add(LogLevel.Warning, "Kat", $"Abruf mit token={key} von lat=48,1371 fehlgeschlagen", null);
        var secrets = new FakeSensitiveValueProvider { Values = [key] };
        var service = CreateService(temp, buffer, secrets);

        var path = await service.CreatePackageAsync();
        var entries = await ReadEntriesAsync(path);
        var log = entries["protokoll.txt"];

        Assert.DoesNotContain(key, log);
        Assert.DoesNotContain("48,1371", log);
        Assert.Contains("Kat", log);
        Assert.Contains("fehlgeschlagen", log);
    }

    /// <summary>
    /// Prüft, dass die Vorschau den Paketinhalt zeigt, ohne eine Datei zu schreiben.
    /// </summary>
    [Fact]
    public async Task CreatePreview_ShowsContent_WritesNoFile()
    {
        using var temp = new TempDirectory();
        var buffer = new DiagnosticsLogBuffer(new ManualTimeProvider());
        buffer.Add(LogLevel.Error, "Kat", "Fehlertext", "InvalidOperationException");
        var service = CreateService(temp, buffer, new FakeSensitiveValueProvider());

        var preview = await service.CreatePreviewTextAsync();

        Assert.Contains("Tankatlas-Diagnosepaket", preview);
        Assert.Contains("Fehlertext", preview);
        Assert.Contains("1.2.3", preview);
        Assert.False(Directory.Exists(temp.Combine("cache", DiagnosticsPackageService.DirectoryName)));
    }

    /// <summary>
    /// Prüft, dass ältere Diagnosepakete beim Erzeugen entfernt werden (Retention: nur das neueste).
    /// </summary>
    [Fact]
    public async Task CreatePackage_RemovesOlderPackages()
    {
        using var temp = new TempDirectory();
        var dir = temp.Combine("cache", DiagnosticsPackageService.DirectoryName);
        Directory.CreateDirectory(dir);
        var old = Path.Combine(dir, DiagnosticsPackageService.FileNamePrefix + "20000101-000000.zip");
        var unrelated = Path.Combine(dir, "andere-datei.zip");
        await File.WriteAllBytesAsync(old, [1]);
        await File.WriteAllBytesAsync(unrelated, [2]);
        var service = CreateService(temp, new DiagnosticsLogBuffer(new ManualTimeProvider()), new FakeSensitiveValueProvider());

        var path = await service.CreatePackageAsync();

        Assert.False(File.Exists(old));
        Assert.True(File.Exists(unrelated));
        Assert.True(File.Exists(path));
    }

    /// <summary>
    /// Prüft, dass das Protokoll auf die Höchstzahl von Einträgen begrenzt ist und ein Hinweis ausgegeben wird.
    /// </summary>
    [Fact]
    public async Task CreatePackage_CapsEntries()
    {
        using var temp = new TempDirectory();
        var buffer = new DiagnosticsLogBuffer(new ManualTimeProvider(), capacity: DiagnosticsPackageService.MaxEntries + 20);
        for (var i = 0; i < DiagnosticsPackageService.MaxEntries + 20; i++)
        {
            buffer.Add(LogLevel.Warning, "Kat", "Meldung " + i, null);
        }

        var service = CreateService(temp, buffer, new FakeSensitiveValueProvider());
        var path = await service.CreatePackageAsync();
        var entries = await ReadEntriesAsync(path);
        var log = entries["protokoll.txt"];

        Assert.Equal(DiagnosticsPackageService.MaxEntries + 1, log.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.Contains("weggelassen", log);
        Assert.Contains("Meldung 0", log);
        Assert.DoesNotContain($"Meldung {DiagnosticsPackageService.MaxEntries + 19}", log);
    }

    /// <summary>
    /// Prüft, dass bei leerem Puffer ein Hinweis statt Einträgen geschrieben wird.
    /// </summary>
    [Fact]
    public async Task CreatePackage_EmptyBuffer_WritesHint()
    {
        using var temp = new TempDirectory();
        var service = CreateService(temp, new DiagnosticsLogBuffer(new ManualTimeProvider()), new FakeSensitiveValueProvider());

        var path = await service.CreatePackageAsync();
        var entries = await ReadEntriesAsync(path);

        Assert.Contains("Keine Protokolleinträge", entries["protokoll.txt"]);
    }

    /// <summary>
    /// Prüft, dass zu lange Eintragszeilen abgeschnitten werden.
    /// </summary>
    [Fact]
    public async Task CreatePackage_TruncatesLongLines()
    {
        using var temp = new TempDirectory();
        var buffer = new DiagnosticsLogBuffer(new ManualTimeProvider());
        buffer.Add(LogLevel.Warning, "Kat", new string('x', DiagnosticsPackageService.MaxEntryChars + 100), null);
        var service = CreateService(temp, buffer, new FakeSensitiveValueProvider());

        var path = await service.CreatePackageAsync();
        var entries = await ReadEntriesAsync(path);
        var line = entries["protokoll.txt"].Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];

        Assert.True(line.Length <= DiagnosticsPackageService.MaxEntryChars + 10);
    }
}
