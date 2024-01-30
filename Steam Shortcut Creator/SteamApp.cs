using System.Text.RegularExpressions;

namespace SteamShortcutCreator;

public partial class SteamApp
{
    #region Properties

    public int AppId { get; }

    public string Name { get; }

    public string InstallDir { get; }

    #endregion

    #region Constructors

    private SteamApp(int appId, string name, string installDir)
    {
        AppId = appId;
        Name = name;
        InstallDir = installDir;
    }

    #endregion

    #region Methods

    public static SteamApp CreateFromFile(string filePath)
    {
        var fileContents = File.ReadAllText(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var appIdString = AppManifestRegex().Match(fileName).Groups[1].Value;
        var appId = int.Parse(appIdString);

        var keyValuePairMatches = KeyValueRegex().Matches(fileContents);

        var keyValues = new Dictionary<string, string>();
        foreach (Match match in keyValuePairMatches)
        {
            keyValues[match.Groups[1].Value] = match.Groups[2].Value;
        }

        var name = keyValues["name"];

        var installDir = keyValues["installdir"];

        return new SteamApp(appId, name, installDir);
    }

    [GeneratedRegex(@"""(\w+)""\s+\""(.+?)""")]
    private static partial Regex KeyValueRegex();

    [GeneratedRegex(@"appmanifest_(\d+)")]
    private static partial Regex AppManifestRegex();

    /// <inheritdoc />
    public override string ToString()
    {
        return $"[{AppId}] {Name}";
    }

    #endregion
}