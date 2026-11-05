namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Tests der Startseite: Leerzustand, Gruppen mit Tankstellen, Preisen, Entfernung und Hinweisen sowie Einklappen.
/// </summary>
public class HomeE2ETests_Overview : HomeE2ETestBase
{
    /// <summary>
    /// Prüft, dass ohne Gruppen der Hinweis erscheint und weder Tankstellen noch „In der Nähe“ noch ein Preisabruf stattfinden.
    /// </summary>
    [Fact]
    public void WithoutGroups_ShowsHintOnly()
    {
        RunWithDiagnostics(() =>
        {
            OpenHome();

            Assert.False(Exists("Favorites.Station.Name"));
            Assert.False(Exists("Nearby.Heading"));
            Assert.False(Exists("Favorites.Group.Name"));
            Assert.Equal(0, Server.TotalRequests);
        });
    }

    /// <summary>
    /// Prüft Gruppen, Tankstellen, Preise mit Alter, Entfernung und Automatenhinweis bei zwei Gruppen.
    /// </summary>
    [Fact]
    public void TwoGroups_ShowStationsPricesDistanceAndHints()
    {
        RunWithDiagnostics(() =>
        {
            CreateTwoGroups();

            OpenHome("Arbeitsweg", "Heimat");

            WaitForTexts("Favorites.Station.Name", "Alpha Tankstelle", "Alpha Tankstelle", "Beta Tankstelle");
            WaitForTexts("Favorites.Station.Price.SuperE5", "1,859 €/L", "1,859 €/L", "1,879 €/L");
            WaitForTexts("Favorites.Station.Price.Diesel", "1,699 €/L", "1,699 €/L");
            Assert.Equal(3, SafeTexts("Favorites.Station.Age.SuperE5").Count);
            Assert.All(SafeTexts("Favorites.Station.Age.SuperE5"), text => Assert.Matches(AgePattern, text));
            Assert.Equal(3, SafeTexts("Favorites.Station.Distance").Count);
            Assert.All(SafeTexts("Favorites.Station.Distance"), text => Assert.Matches(DistancePattern, text));
            Assert.Equal(2, SafeTexts("Favorites.Station.Hint.Automated").Count);
            WaitForTexts("Favorites.Group.Count", "1 Tankstelle", "2 Tankstellen");
        });
    }

    /// <summary>
    /// Prüft, dass das Einklappen einer Gruppe ihre Tankstellen ausblendet und das Ausklappen sie wieder zeigt.
    /// </summary>
    [Fact]
    public void CollapseGroup_HidesStationsAndExpandShowsThemAgain()
    {
        RunWithDiagnostics(() =>
        {
            CreateTwoGroups();
            OpenHome("Arbeitsweg", "Heimat");
            WaitForTexts("Favorites.Station.Name", "Alpha Tankstelle", "Alpha Tankstelle", "Beta Tankstelle");

            FindAllOrdered("Favorites.Group.Toggle")[1].Patterns.Invoke.Pattern.Invoke();
            WaitForTexts("Favorites.Station.Name", "Alpha Tankstelle");

            FindAllOrdered("Favorites.Group.Toggle")[1].Patterns.Invoke.Pattern.Invoke();
            WaitForTexts("Favorites.Station.Name", "Alpha Tankstelle", "Alpha Tankstelle", "Beta Tankstelle");
        });
    }
}
