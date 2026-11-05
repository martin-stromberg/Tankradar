using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tankradar.MAUI.Data;

/// <summary>
/// Design-Time-Factory ausschließlich für <c>dotnet ef</c>; verwendet eine In-Memory-Datenquelle und legt keine Datei an.
/// </summary>
public class TankradarDbContextFactory : IDesignTimeDbContextFactory<TankradarDbContext>
{
    /// <inheritdoc />
    public TankradarDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TankradarDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        return new TankradarDbContext(options);
    }
}
