using System.Reflection;

namespace Tankradar.TestSupport;

/// <summary>
/// Sichert die Konvention ab, dass die Kategorie <c>Flaky</c> nur in Kombination mit <c>OsInterface</c> vergeben wird —
/// ein Verstoß würde einen retry-berechtigten Test in die blockierende Lane schieben. Der Test ist in alle Testprojekte
/// per <c>&lt;Compile Include&gt;</c>-Link eingebunden; jede Projektkopie scannt ihre eigene Assembly.
/// </summary>
public sealed class FlakyConventionTests
{
    /// <summary>
    /// Jede Methode oder Klasse, deren Trait-Attribute <c>Category=Flaky</c> liefern, muss zusätzlich <c>Category=OsInterface</c> liefern.
    /// </summary>
    [Fact]
    public void FlakyMarkedTests_AlwaysCarryOsInterface()
    {
        var violations = new List<string>();
        var assembly = typeof(FlakyConventionTests).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            var typeTraits = TypeTraits(type).ToArray();
            if (HasCategory(typeTraits, TestCategories.Flaky) && !HasCategory(typeTraits, TestCategories.OsInterface))
            {
                violations.Add(type.FullName ?? type.Name);
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var methodTraits = MethodTraits(method).Concat(typeTraits).ToArray();
                if (HasCategory(methodTraits, TestCategories.Flaky) && !HasCategory(methodTraits, TestCategories.OsInterface))
                {
                    violations.Add($"{type.FullName}.{method.Name}");
                }
            }
        }

        Assert.True(violations.Count == 0, "Flaky darf nur in Kombination mit OsInterface vergeben werden: " + string.Join(", ", violations));
    }

    /// <summary>
    /// Liest Trait-Attribute inklusive Basisklassen aus. In xUnit 2.9.3 trägt <see cref="TraitAttribute"/>
    /// Name/Wert nicht öffentlich; die Konstruktorargumente der <see cref="CustomAttributeData"/> sind
    /// dagegen stabil und funktionieren auch für abgeleitete Attributtypen.
    /// </summary>
    /// <param name="type">Der zu untersuchende Typ; Basisklassen werden mitgescannt.</param>
    /// <returns>Alle Trait-Name/Wert-Paare des Typs und seiner Basisklassen.</returns>
    private static IEnumerable<KeyValuePair<string, string>> TypeTraits(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var trait in Traits(current.CustomAttributes))
            {
                yield return trait;
            }
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> MethodTraits(MethodInfo method)
    {
        var attributes = method.CustomAttributes;
        var baseDefinition = method.GetBaseDefinition();
        if (baseDefinition != method)
        {
            attributes = attributes.Concat(baseDefinition.CustomAttributes).ToArray();
        }

        return Traits(attributes);
    }

    private static IEnumerable<KeyValuePair<string, string>> Traits(IEnumerable<CustomAttributeData> attributes)
    {
        return attributes
            .Where(attribute => typeof(TraitAttribute).IsAssignableFrom(attribute.AttributeType))
            .Where(attribute => attribute.ConstructorArguments.Count == 2)
            .Select(attribute => new KeyValuePair<string, string>(
                (string)attribute.ConstructorArguments[0].Value!,
                (string)attribute.ConstructorArguments[1].Value!));
    }

    private static bool HasCategory(IEnumerable<KeyValuePair<string, string>> traits, string value)
    {
        return traits.Any(trait => trait.Key == "Category" && trait.Value == value);
    }
}
