using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI;
using Tankradar.MAUI.Services.Geocoding;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Routing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass die Registrierung der Routingdienste in <see cref="MauiProgram"/> vollständig auflösbar ist.
/// </summary>
public class MauiProgramTests_RoutingServices : BaseTest
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IDelay>(new RecordingDelay());
        services.AddSingleton<IGeocodingService>(new FakeGeocodingService());
        services.AddSingleton<IFuelPriceService>(new FakeFuelPriceService());
        MauiProgram.AddRoutingServices(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    /// <summary>
    /// Prüft, dass Routingdienst, Stationssuche und Gesamtablauf aus der Dienstesammlung aufgelöst werden können.
    /// </summary>
    [Fact]
    public void AddRoutingServices_ResolvesAllRoutingServices()
    {
        using var provider = BuildProvider();

        Assert.IsType<OpenRouteServiceClient>(provider.GetRequiredService<IRoutingService>());
        Assert.IsType<RouteStationSearchService>(provider.GetRequiredService<IRouteStationSearchService>());
        Assert.IsType<RouteSearchService>(provider.GetRequiredService<IRouteSearchService>());
    }

    /// <summary>
    /// Prüft, dass die Dienste als Singleton registriert sind.
    /// </summary>
    [Fact]
    public void AddRoutingServices_RegistersSingletons()
    {
        using var provider = BuildProvider();

        Assert.Same(provider.GetRequiredService<IRouteSearchService>(), provider.GetRequiredService<IRouteSearchService>());
        Assert.Same(provider.GetRequiredService<IRoutingService>(), provider.GetRequiredService<IRoutingService>());
    }
}
