using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace Tankradar.Tests.Integration.Integration.Support;

/// <summary>
/// Baut ein winziges Testprojekt, das die echte Schlüsselübernahme (<c>TankerkoenigApiKey.targets</c> oder <c>RoutingApiKey.targets</c>) einbindet, und liefert Log, Binlog und den in die Assembly eingebetteten Schlüssel.
/// Die Prozessumgebung des Builds wird vollständig kontrolliert: Die echten Variablen des Anwenders werden entfernt, es kommen ausschließlich Platzhalter zum Einsatz.
/// </summary>
public sealed class ApiKeyBuildRunner : IDisposable
{
    private static readonly string[] AllVariables =
    [
        "TANKRADAR_FUEL_PRICE_API_KEY",
        "FUEL_PRICE_API_KEY",
        "TANKRADAR_ROUTING_API_KEY",
        "ROUTING_API_KEY",
    ];

    private readonly string _directory;
    private readonly ApiKeyBuildProfile _profile;

    /// <summary>
    /// Legt ein temporäres Verzeichnis für das Testprojekt an.
    /// </summary>
    /// <param name="profile">Die einzubindende Schlüsselübernahme; ohne Angabe die des Tankerkönig-Schlüssels.</param>
    public ApiKeyBuildRunner(ApiKeyBuildProfile? profile = null)
    {
        _profile = profile ?? ApiKeyBuildProfile.Tankerkoenig;
        _directory = Path.Combine(Path.GetTempPath(), "tankradar-keybuild-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        LocalPropsPath = Path.Combine(_directory, _profile.LocalPropsFileName);
    }

    /// <summary>
    /// Der Pfad der lokalen props-Datei dieses Laufs.
    /// </summary>
    public string LocalPropsPath { get; }

    /// <summary>
    /// Schreibt die lokale props-Datei im bisherigen Format mit dem angegebenen Platzhalter.
    /// </summary>
    /// <param name="key">Der Platzhalterschlüssel.</param>
    public void WriteLocalProps(string key)
    {
        File.WriteAllText(LocalPropsPath, "<Project><PropertyGroup><" + _profile.MetadataName + ">" + key + "</" + _profile.MetadataName + "></PropertyGroup></Project>");
    }

    /// <summary>
    /// Baut das Testprojekt mit diagnostischer Ausführlichkeit und Binlog.
    /// </summary>
    /// <param name="primary">Wert der vorrangigen Umgebungsvariable des Profils oder <see langword="null"/>.</param>
    /// <param name="secret">Wert der Umgebungsvariable des Profils mit dem Namen des Secrets oder <see langword="null"/>.</param>
    /// <param name="extraEnvironment">Weitere Variablen (z. B. die des anderen Schlüssels, um deren Trennung zu prüfen) oder <see langword="null"/>.</param>
    /// <returns>Das Ergebnis des Builds.</returns>
    public ApiKeyBuildResult Build(string? primary, string? secret, IReadOnlyDictionary<string, string>? extraEnvironment = null)
    {
        var targets = LocateTargets();
        var project = Path.Combine(_directory, "KeyProbe.csproj");
        File.WriteAllText(
            project,
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n"
            + "  <PropertyGroup>\n"
            + "    <TargetFramework>net10.0</TargetFramework>\n"
            + "    <OutputType>Library</OutputType>\n"
            + "    <" + _profile.LocalPropsProperty + ">" + LocalPropsPath + "</" + _profile.LocalPropsProperty + ">\n"
            + "  </PropertyGroup>\n"
            + "  <Import Project=\"" + targets + "\" />\n"
            + "</Project>\n");
        File.WriteAllText(Path.Combine(_directory, "Probe.cs"), "namespace Probe; public class Marker { }");

        var binlog = Path.Combine(_directory, "build.binlog");
        var info = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = _directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in new[] { "build", project, "-v:diag", "-nologo", "-nr:false", "-bl:" + binlog })
        {
            info.ArgumentList.Add(argument);
        }

        // Die echten Variablen des Anwenders dürfen nie in den Test gelangen.
        foreach (var variable in AllVariables)
        {
            info.Environment.Remove(variable);
        }

        info.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        info.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        if (primary is not null)
        {
            info.Environment[_profile.PrimaryVariable] = primary;
        }

        if (secret is not null)
        {
            info.Environment[_profile.SecretVariable] = secret;
        }

        foreach (var (name, value) in extraEnvironment ?? new Dictionary<string, string>())
        {
            info.Environment[name] = value;
        }

        using var process = Process.Start(info) ?? throw new InvalidOperationException("dotnet konnte nicht gestartet werden.");
        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) => AppendLine(output, e.Data);
        process.ErrorDataReceived += (_, e) => AppendLine(output, e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        if (!process.WaitForExit(TimeSpan.FromMinutes(5)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("Der Testbuild hat nicht rechtzeitig geendet.");
        }

        process.WaitForExit();
        var assemblyPath = Path.Combine(_directory, "bin", "Debug", "net10.0", "KeyProbe.dll");
        return new ApiKeyBuildResult(process.ExitCode, output.ToString(), ReadBinlog(binlog), assemblyPath);
    }

    /// <summary>
    /// Liest das Assembly-Metadatum <c>TankerkoenigApiKey</c> der gebauten Testassembly.
    /// </summary>
    /// <param name="assemblyPath">Der Pfad der Assembly.</param>
    /// <returns>Der eingebettete Wert.</returns>
    public static string? ReadEmbeddedKey(string assemblyPath)
    {
        return ReadEmbeddedKey(assemblyPath, ApiKeyBuildProfile.Tankerkoenig.MetadataName);
    }

    /// <summary>
    /// Liest ein Assembly-Metadatum der gebauten Testassembly.
    /// </summary>
    /// <param name="assemblyPath">Der Pfad der Assembly.</param>
    /// <param name="metadataName">Der Name des Metadatums.</param>
    /// <returns>Der eingebettete Wert oder <see langword="null"/>, wenn das Metadatum fehlt.</returns>
    public static string? ReadEmbeddedKey(string assemblyPath, string metadataName)
    {
        var context = new AssemblyLoadContext("key-probe", isCollectible: true);
        try
        {
            using var stream = new MemoryStream(File.ReadAllBytes(assemblyPath));
            var assembly = context.LoadFromStream(stream);
            return assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == metadataName)?.Value;
        }
        finally
        {
            context.Unload();
        }
    }

    /// <summary>
    /// Entfernt das temporäre Verzeichnis.
    /// </summary>
    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // Aufräumen ist best effort; das Verzeichnis liegt im Temp-Ordner.
        }
        catch (UnauthorizedAccessException)
        {
            // Siehe oben.
        }
    }

    private static void AppendLine(StringBuilder output, string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (output)
        {
            output.AppendLine(line);
        }
    }

    private static string ReadBinlog(string path)
    {
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        // Ein Binlog ist GZip-komprimiert; die Zeichenketten stehen als UTF-8 darin. Für die Suche genügt Latin-1 (byte-treu).
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var memory = new MemoryStream();
        gzip.CopyTo(memory);
        return Encoding.Latin1.GetString(memory.ToArray());
    }

    private string LocateTargets()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Tankradar.MAUI", "MSBuild", _profile.TargetsFileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(_profile.TargetsFileName + " wurde nicht gefunden.");
    }
}

/// <summary>
/// Das Ergebnis eines Testbuilds.
/// </summary>
/// <param name="ExitCode">Der Exitcode von <c>dotnet build</c>.</param>
/// <param name="ConsoleLog">Die komplette Konsolenausgabe (diagnostisch).</param>
/// <param name="BinaryLog">Der Inhalt des Binlogs (entpackt, Latin-1).</param>
/// <param name="AssemblyPath">Der Pfad der gebauten Assembly.</param>
/// <returns>Der Wert.</returns>
public sealed record ApiKeyBuildResult(int ExitCode, string ConsoleLog, string BinaryLog, string AssemblyPath);
