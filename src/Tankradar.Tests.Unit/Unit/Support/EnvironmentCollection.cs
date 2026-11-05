namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Sammlung für Tests, die prozessweite Umgebungsvariablen verändern; Klassen derselben Sammlung laufen nicht parallel.
/// </summary>
[CollectionDefinition(Name)]
public sealed class EnvironmentCollection
{
    /// <summary>
    /// Der Name der Sammlung.
    /// </summary>
    public const string Name = "TestDataPathEnvironment";
}
