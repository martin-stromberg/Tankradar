using System.Xml.Linq;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Backup-Entscheidung des Android-Manifests per XML-Auswertung: Auto-Backup und Gerätetransfer sind
/// abgeschaltet (<c>android:allowBackup="false"</c>, ADR 0022), und es sind keine widersprüchlichen Backup-Regeln
/// oder -Agenten deklariert.
/// </summary>
public class AndroidManifestTests_Backup : BaseTest
{
    private static readonly XNamespace Android = "http://schemas.android.com/apk/res/android";

    private static XElement ApplicationElement()
    {
        var manifest = XDocument.Load(MauiSourceFiles.PathOf("Platforms", "Android", "AndroidManifest.xml"));
        Assert.NotNull(manifest.Root);
        return Assert.Single(manifest.Root.Elements("application"));
    }

    /// <summary>
    /// Prüft, dass das application-Element Auto-Backup und Gerätetransfer mit <c>android:allowBackup="false"</c>
    /// abschaltet; die eigene Sicherung der App ist die alleinige Backup-Lösung (ADR 0022). Ein fehlendes Attribut
    /// würde den Plattform-Standard „true" reaktivieren und wird daher ebenfalls als Fehler erkannt.
    /// </summary>
    [Fact]
    public void Application_DisablesAutoBackup()
    {
        var application = ApplicationElement();

        Assert.Equal("false", application.Attribute(Android + "allowBackup")?.Value);
    }

    /// <summary>
    /// Prüft, dass das application-Element keine Backup-Regeln oder -Agenten deklariert: Bei abgeschaltetem
    /// Auto-Backup wären Regeln widersprüchlich bzw. wirkungslos; die Regel-Option wurde in ADR 0022 verworfen.
    /// </summary>
    /// <param name="attribute">Der lokale Name des nicht deklarierten android:-Attributs.</param>
    [Theory]
    [InlineData("dataExtractionRules")]
    [InlineData("fullBackupContent")]
    [InlineData("backupAgent")]
    [InlineData("restoreAnyVersion")]
    public void Application_DeclaresNoBackupRulesOrAgent(string attribute)
    {
        var application = ApplicationElement();

        Assert.Null(application.Attribute(Android + attribute));
    }
}
