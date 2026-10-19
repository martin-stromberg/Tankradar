using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft den eigenen Anzeigenamen einer Tankstelle im Preis-Cache auf einer echten, migrierten SQLite-Datei: Vergeben, Ändern, Entfernen,
/// Ablehnung und dass Abrufe der Quelle den Namen nie überschreiben.
/// </summary>
public class PriceRepositoryTests_CustomName : IDisposable
{
    private const string StationA = "11111111-1111-4111-8111-111111111111";
    private const string StationB = "22222222-2222-4222-8222-222222222222";

    private readonly TestDatabase _database = new();

    private PriceRepository CreateRepository()
    {
        var factory = _database.CreateFactory();
        return new PriceRepository(factory, _database.CreateInitializer(factory));
    }

    private static StationInfo Station(string id, string name, string? brand)
    {
        return new StationInfo { Id = id, Name = name, Brand = brand, Latitude = 52.52, Longitude = 13.40 };
    }

    /// <summary>
    /// Prüft Vergeben, Ändern und Entfernen: Der Name wird getrimmt gespeichert, ein leerer Name entfernt ihn.
    /// </summary>
    [Fact]
    public async Task SetCustomName_SetsChangesAndRemoves()
    {
        var repository = CreateRepository();
        await repository.SaveAsync([Station(StationA, "Klaus Heinrich", "Westfalen")]);

        Assert.Equal(StationNameResult.Ok, await repository.SetCustomNameAsync(StationA, "  Zuhause  "));
        var stored = await repository.GetStationAsync(StationA);
        Assert.Equal("Zuhause", stored!.CustomName);
        Assert.Equal("Zuhause", stored.DisplayName);
        Assert.Equal("Klaus Heinrich", stored.SubName);

        Assert.Equal(StationNameResult.Ok, await repository.SetCustomNameAsync(StationA, "Arbeit"));
        Assert.Equal("Arbeit", (await repository.GetStationAsync(StationA))!.CustomName);

        Assert.Equal(StationNameResult.Ok, await repository.SetCustomNameAsync(StationA, "   "));
        var removed = await repository.GetStationAsync(StationA);
        Assert.Null(removed!.CustomName);
        Assert.Equal("Westfalen", removed.DisplayName);
    }

    /// <summary>
    /// Prüft die Ablehnung eines zu langen Namens (Zustand bleibt) und einer unbekannten Tankstelle.
    /// </summary>
    [Fact]
    public async Task SetCustomName_RejectsTooLongAndUnknown()
    {
        var repository = CreateRepository();
        await repository.SaveAsync([Station(StationA, "Alpha", null)]);
        await repository.SetCustomNameAsync(StationA, "Zuhause");

        Assert.Equal(StationNameResult.TooLong, await repository.SetCustomNameAsync(StationA, new string('x', StationNaming.MaxCustomNameLength + 1)));
        Assert.Equal("Zuhause", (await repository.GetStationAsync(StationA))!.CustomName);
        Assert.Equal(StationNameResult.Ok, await repository.SetCustomNameAsync(StationA, new string('x', StationNaming.MaxCustomNameLength)));
        Assert.Equal(StationNameResult.NotFound, await repository.SetCustomNameAsync("99999999-9999-4999-8999-999999999999", "Name"));
    }

    /// <summary>
    /// Prüft, dass ein erneutes Speichern abgerufener Stammdaten (neuer Name und Marke der Quelle) den eigenen Namen erhält.
    /// </summary>
    [Fact]
    public async Task SaveAsync_KeepsCustomName_WhileSourceDataChanges()
    {
        var repository = CreateRepository();
        await repository.SaveAsync([Station(StationA, "Klaus Heinrich", null)]);
        await repository.SetCustomNameAsync(StationA, "Zuhause");

        await repository.SaveAsync([Station(StationA, "Heinrich GmbH", "Westfalen")]);

        var stored = await repository.GetStationAsync(StationA);
        Assert.Equal("Zuhause", stored!.CustomName);
        Assert.Equal("Westfalen", stored.Brand);
        Assert.Equal("Heinrich GmbH", stored.Name);
    }

    /// <summary>
    /// Prüft, dass alle Lesewege (nach Kennungen, Umkreis, bekannte Details) den eigenen Namen liefern und die Abfrage der Namen nur vergebene enthält.
    /// </summary>
    [Fact]
    public async Task ReadPaths_ExposeCustomName()
    {
        var repository = CreateRepository();
        var detailed = new StationInfo
        {
            Id = StationA,
            Name = "Alpha",
            Latitude = 52.52,
            Longitude = 13.40,
            DetailsUpdatedUtc = new DateTime(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc),
            WholeDay = true,
        };
        await repository.SaveAsync([detailed, Station(StationB, "Beta", "Aral")]);
        await repository.SetCustomNameAsync(StationA, "Zuhause");

        Assert.Equal("Zuhause", (await repository.GetStationsAsync([StationA])).Single().CustomName);
        Assert.Equal("Zuhause", (await repository.FindNearbyAsync(52.52, 13.40, 5)).Single(s => s.Id == StationA).CustomName);
        Assert.Equal("Zuhause", (await repository.GetKnownDetailsAsync([StationA]))[StationA].CustomName);
        var names = await repository.GetCustomNamesAsync([StationA, StationB]);
        Assert.Equal(new Dictionary<string, string> { [StationA] = "Zuhause" }, names);
        Assert.Empty(await repository.GetCustomNamesAsync([]));
    }

    /// <summary>
    /// Prüft, dass die Spalte in der Datenbank auf 60 Zeichen begrenzt modelliert ist (Modell und Dienst stimmen überein).
    /// </summary>
    [Fact]
    public void Model_LimitsCustomNameLength()
    {
        using var context = _database.CreateFactory().CreateDbContext();

        var property = context.Model.FindEntityType(typeof(StationEntity))!.FindProperty(nameof(StationEntity.CustomName))!;

        Assert.Equal(StationNaming.MaxCustomNameLength, property.GetMaxLength());
        Assert.True(property.IsNullable);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}
