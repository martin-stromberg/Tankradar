using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Tankbook;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration.Support;

/// <summary>
/// Integrationsumgebung mit echter SQLite-Datei im isolierten Testdatenverzeichnis; nutzt Verbindungen ohne Pool und löscht das Verzeichnis beim Freigeben.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly TestDataContext _dataContext = new();

    /// <summary>
    /// Legt die Umgebung mit isoliertem Datenverzeichnis an.
    /// </summary>
    public TestDatabase()
    {
        DatabasePath = Path.Combine(_dataContext.DataDirectory, TankradarDbContext.DatabaseFileName);
    }

    /// <summary>
    /// Das isolierte Datenverzeichnis, in dem die Datenbankdatei liegt.
    /// </summary>
    public string DataDirectory => _dataContext.DataDirectory;

    /// <summary>
    /// Der vollständige Pfad der Datenbankdatei.
    /// </summary>
    public string DatabasePath { get; }

    /// <summary>
    /// Erstellt eine neue Kontext-Factory auf der Datenbankdatei (simuliert einen frischen App-Start).
    /// Die Verbindungen nutzen die Verbindungszeichenfolge der App (<c>Foreign Keys=True</c>), aber keinen Pool, damit kein Test die Verbindungen paralleler Testklassen global leeren muss.
    /// </summary>
    /// <returns>Die Factory.</returns>
    public IDbContextFactory<TankradarDbContext> CreateFactory()
    {
        var options = new DbContextOptionsBuilder<TankradarDbContext>()
            .UseSqlite(TankradarDbContext.BuildConnectionString(DatabasePath, pooling: false))
            .Options;
        return new FileDbContextFactory(options);
    }

    /// <summary>
    /// Erstellt einen <see cref="DatabaseInitializer"/> auf dem Datenverzeichnis dieser Umgebung.
    /// </summary>
    /// <param name="factory">Die zu verwendende Kontext-Factory.</param>
    /// <returns>Der Initializer.</returns>
    public DatabaseInitializer CreateInitializer(IDbContextFactory<TankradarDbContext> factory)
    {
        return new DatabaseInitializer(factory, new FixedPathProvider(DataDirectory), new NoOpProtector());
    }

    /// <summary>
    /// Erstellt die Ablage der Belegfotos im Datenverzeichnis dieser Umgebung.
    /// </summary>
    /// <returns>Die Ablage.</returns>
    public ReceiptPhotoStore CreatePhotoStore()
    {
        return new ReceiptPhotoStore(new FixedPathProvider(DataDirectory), new NoOpProtector(), new RecordingLogger<ReceiptPhotoStore>());
    }

    /// <summary>
    /// Löscht das Testdatenverzeichnis; ohne Pool sind alle Verbindungen nach dem Ende ihres Kontexts bereits geschlossen.
    /// </summary>
    public void Dispose()
    {
        _dataContext.Dispose();
    }

    private sealed class FileDbContextFactory : IDbContextFactory<TankradarDbContext>
    {
        private readonly DbContextOptions<TankradarDbContext> _options;

        public FileDbContextFactory(DbContextOptions<TankradarDbContext> options)
        {
            _options = options;
        }

        public TankradarDbContext CreateDbContext()
        {
            return new TankradarDbContext(_options);
        }
    }

    private sealed class FixedPathProvider : IAppDataPathProvider
    {
        private readonly string _directory;

        public FixedPathProvider(string directory)
        {
            _directory = directory;
        }

        public string GetDataDirectory()
        {
            return _directory;
        }

        public string GetCacheDirectory()
        {
            return Path.Combine(_directory, "cache");
        }
    }

    private sealed class NoOpProtector : IDatabaseFileProtector
    {
        public void Protect(string databasePath)
        {
        }

        public void ProtectFile(string filePath)
        {
        }
    }
}
