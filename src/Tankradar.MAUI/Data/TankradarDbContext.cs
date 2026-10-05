using Microsoft.EntityFrameworkCore;

namespace Tankradar.MAUI.Data;

/// <summary>
/// Entity-Framework-Kontext für die lokale SQLite-Datenbank der App.
/// </summary>
public class TankradarDbContext : DbContext
{
    /// <summary>
    /// Dateiname der Datenbank im Datenverzeichnis.
    /// </summary>
    public const string DatabaseFileName = "tankatlas.db";

    /// <summary>
    /// Erstellt den Kontext mit den angegebenen Optionen.
    /// </summary>
    /// <param name="options">Die Kontextoptionen.</param>
    public TankradarDbContext(DbContextOptions<TankradarDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Die Tabelle der skalaren Einstellungen.
    /// </summary>
    public DbSet<UserSettingsEntity> UserSettings { get; set; } = null!;

    /// <summary>
    /// Die Tabelle der Spritsorten-Einstellungen.
    /// </summary>
    public DbSet<FuelTypeSettingEntity> FuelTypeSettings { get; set; } = null!;

    /// <summary>
    /// Die Tabelle der lokal bekannten Tankstellen.
    /// </summary>
    public DbSet<StationEntity> Stations { get; set; } = null!;

    /// <summary>
    /// Die Tabelle der gespeicherten Preisstände.
    /// </summary>
    public DbSet<PriceEntryEntity> PriceEntries { get; set; } = null!;

    /// <summary>
    /// Ermittelt den vollständigen Pfad der Datenbankdatei.
    /// </summary>
    /// <param name="dataDirectory">Das App-Datenverzeichnis.</param>
    /// <returns>Der Pfad zur Datenbankdatei.</returns>
    public static string GetDatabasePath(string dataDirectory)
    {
        return Path.Combine(dataDirectory, DatabaseFileName);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSettingsEntity>(entity =>
        {
            entity.ToTable("UserSettings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.GpsUsage).IsRequired();
            entity.Property(e => e.ResultView).IsRequired();
            entity.Property(e => e.ResultSortOrder).IsRequired();
        });

        modelBuilder.Entity<FuelTypeSettingEntity>(entity =>
        {
            entity.ToTable("FuelTypeSettings");
            entity.HasKey(e => e.FuelTypeKey);
            entity.Property(e => e.FuelTypeKey).IsRequired();
            entity.Property(e => e.SortOrder).IsRequired();
            entity.Property(e => e.IsSelected).IsRequired();
        });

        modelBuilder.Entity<StationEntity>(entity =>
        {
            entity.ToTable("Stations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired();
            entity.HasIndex(e => new { e.Latitude, e.Longitude });
        });

        modelBuilder.Entity<PriceEntryEntity>(entity =>
        {
            entity.ToTable("PriceEntries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FuelTypeKey).IsRequired();
            entity.Property(e => e.Price).HasColumnType("TEXT");
            entity.HasOne(e => e.Station)
                .WithMany(s => s.PriceEntries)
                .HasForeignKey(e => e.StationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.StationId, e.FuelTypeKey, e.RetrievedUtc });
        });
    }
}
