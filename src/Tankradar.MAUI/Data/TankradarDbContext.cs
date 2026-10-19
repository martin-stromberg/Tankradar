using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Tankbook;

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
    /// Die Tabelle der Favoritengruppen.
    /// </summary>
    public DbSet<FavoriteGroupEntity> FavoriteGroups { get; set; } = null!;

    /// <summary>
    /// Die Tabelle der Zuordnungen von Tankstellen zu Favoritengruppen.
    /// </summary>
    public DbSet<FavoriteEntryEntity> FavoriteEntries { get; set; } = null!;

    /// <summary>
    /// Die Tabelle der Fahrzeuge des Tankbuchs.
    /// </summary>
    public DbSet<VehicleEntity> Vehicles { get; set; } = null!;

    /// <summary>
    /// Die Tabelle der Tank- und Ladevorgänge des Tankbuchs.
    /// </summary>
    public DbSet<TankbookEntryEntity> TankbookEntries { get; set; } = null!;

    /// <summary>
    /// Ermittelt den vollständigen Pfad der Datenbankdatei.
    /// </summary>
    /// <param name="dataDirectory">Das App-Datenverzeichnis.</param>
    /// <returns>Der Pfad zur Datenbankdatei.</returns>
    public static string GetDatabasePath(string dataDirectory)
    {
        return Path.Combine(dataDirectory, DatabaseFileName);
    }

    /// <summary>
    /// Baut die Verbindungszeichenfolge der App: Fremdschlüssel sind ausdrücklich eingeschaltet (<c>Foreign Keys=True</c>),
    /// damit die Kaskaden nicht vom Standard der SQLite-Bibliothek abhängen.
    /// </summary>
    /// <param name="databasePath">Der Pfad der Datenbankdatei.</param>
    /// <param name="pooling"><see langword="false"/> schaltet das Verbindungspooling ab (Tests, die Dateien sofort löschen).</param>
    /// <returns>Die Verbindungszeichenfolge.</returns>
    public static string BuildConnectionString(string databasePath, bool pooling = true)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            ForeignKeys = true,
        };
        if (!pooling)
        {
            builder.Pooling = false;
        }

        return builder.ToString();
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
            entity.Property(e => e.ShowNearby).HasDefaultValue(false);
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
            entity.Property(e => e.CustomName).HasMaxLength(StationNaming.MaxCustomNameLength);
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

        modelBuilder.Entity<FavoriteGroupEntity>(entity =>
        {
            entity.ToTable("FavoriteGroups");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().UseCollation("NOCASE");
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<FavoriteEntryEntity>(entity =>
        {
            entity.ToTable("FavoriteEntries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StationId).IsRequired();
            entity.Property(e => e.Priority).IsRequired();
            entity.HasOne(e => e.Group)
                .WithMany(g => g.Entries)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Station)
                .WithMany()
                .HasForeignKey(e => e.StationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.GroupId, e.StationId }).IsUnique();
            entity.HasIndex(e => e.StationId);
        });

        modelBuilder.Entity<VehicleEntity>(entity =>
        {
            entity.ToTable("Vehicles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().UseCollation("NOCASE");
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.FuelKind).IsRequired();
            entity.Property(e => e.StartOdometerKm).IsRequired();
            entity.Property(e => e.StartConsumption).HasColumnType("TEXT");
            entity.Property(e => e.CreatedUtc).IsRequired();
        });

        modelBuilder.Entity<TankbookEntryEntity>(entity =>
        {
            entity.ToTable("TankbookEntries");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Vehicle)
                .WithMany(v => v.Entries)
                .HasForeignKey(e => e.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.VehicleId, e.OccurredAt });
            entity.Property(e => e.CreatedUtc).IsRequired();
            entity.Property(e => e.OccurredAt).IsRequired();
            entity.Property(e => e.Kind).IsRequired().HasDefaultValue(nameof(TankbookEntryKind.Fuel));
            entity.Property(e => e.Quantity).HasColumnType("TEXT").HasDefaultValue(0m);
            entity.Property(e => e.UnitPrice).HasColumnType("TEXT").HasDefaultValue(0m);
            entity.Property(e => e.TotalAmount).HasColumnType("TEXT").HasDefaultValue(0m);
            entity.Property(e => e.OdometerKm).IsRequired();
            entity.Property(e => e.IsPartial).HasDefaultValue(false);
            entity.Property(e => e.StationName).IsRequired().HasDefaultValue(string.Empty);
        });
    }
}
