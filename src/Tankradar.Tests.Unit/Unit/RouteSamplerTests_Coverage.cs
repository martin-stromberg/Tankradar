using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Services.Routing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Eigenschaftsbasierter Nachweis der lückenlosen Überdeckung: Jeder Punkt im Abstandsband um die Route liegt in mindestens einem Abfragekreis,
/// auch bei Kurven und an den Enden der Route.
/// </summary>
public class RouteSamplerTests_Coverage : BaseTest
{
    private const int SamplesPerCase = 4000;

    /// <summary>
    /// Prüft die Überdeckung für gerade und gekrümmte Routen und alle Abstandsstufen.
    /// </summary>
    /// <param name="distanceKm">Der Abstand zur Route.</param>
    /// <param name="shape">Die Form der Route (0 gerade, 1 sanft gekrümmt, 2 stark gekrümmt).</param>
    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(5, 0)]
    [InlineData(10, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(5, 1)]
    [InlineData(10, 1)]
    [InlineData(2, 2)]
    [InlineData(10, 2)]
    public void TryCreateQueryPoints_RandomPointsInBand_AreCoveredByAQueryCircle(int distanceKm, int shape)
    {
        var route = shape switch
        {
            0 => RouteTestData.NorthFromBerlin(3.2),
            1 => RouteTestData.Curved(3.2, 0.15),
            _ => RouteTestData.Curved(1.2, 0.3, 160),
        };
        Assert.True(route.LengthKm <= 400, "Die Testroute überschreitet die Obergrenze.");
        Assert.True(RouteSampler.TryCreateQueryPoints(route, distanceKm, new RouteSearchOptions(), out var queries));
        var random = new Random(4711 + (distanceKm * 10) + shape);
        var checkedPoints = 0;

        for (var i = 0; i < SamplesPerCase; i++)
        {
            // Zufälliger Punkt auf der Route (auch über die Enden hinaus, dort gilt der Abstand zum Endpunkt), dann seitlicher Versatz bis zum Abstand.
            var along = (random.NextDouble() * (route.LengthKm + (2 * distanceKm))) - distanceKm;
            var anchor = route.PointAt(along);
            var angle = random.NextDouble() * 2 * Math.PI;
            var radius = distanceKm * 0.98 * Math.Sqrt(random.NextDouble());
            var latitude = anchor.Latitude + (radius * Math.Cos(angle) / 111.195);
            var longitude = anchor.Longitude + (radius * Math.Sin(angle) / (111.195 * Math.Cos(anchor.Latitude * Math.PI / 180)));

            if (route.Locate(latitude, longitude).OffsetKm > distanceKm)
            {
                continue;
            }

            checkedPoints++;
            var covered = queries.Any(q => RouteGeometry.HaversineKm(latitude, longitude, q.Latitude, q.Longitude) <= q.RadiusKm);
            Assert.True(covered, $"Punkt im Band nicht überdeckt (Versuch {i}, Abstand {distanceKm} km, Form {shape}).");
        }

        Assert.True(checkedPoints > SamplesPerCase / 2, "Zu wenige Punkte lagen im Band; der Test wäre nicht aussagekräftig.");
    }

    /// <summary>
    /// Prüft, dass die Abfragekreise bei kurzer Route Start und Ziel einschließlich des Bandes überdecken.
    /// </summary>
    [Fact]
    public void TryCreateQueryPoints_StartAndDestination_AreCovered()
    {
        var route = RouteTestData.NorthFromBerlin(0.5);
        Assert.True(RouteSampler.TryCreateQueryPoints(route, 2, new RouteSearchOptions(), out var queries));

        foreach (var along in new[] { 0.0, route.LengthKm })
        {
            var end = route.PointAt(along);
            Assert.Contains(queries, q => RouteGeometry.HaversineKm(end.Latitude, end.Longitude, q.Latitude, q.Longitude) + 2 <= q.RadiusKm);
        }
    }
}
