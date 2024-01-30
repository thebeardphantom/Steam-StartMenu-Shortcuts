using SteamShortcutCreator;
using System.Text.RegularExpressions;

var steamInstallPath = AppUtility.GetSteamInstallPath();
if (steamInstallPath == default)
{
    Console.Error.WriteLine("No valid steam install path found.");
    return;
}

var appInfoVdfPath = Path.Combine(steamInstallPath, "appcache", "appinfo.vdf");
var appInfoVdf = new AppInfoVdf(appInfoVdfPath);
var clientIconsDirectory = Path.Combine(steamInstallPath, "steam", "games");

var libraryFoldersVdf = Path.Combine(steamInstallPath, "steamapps", "libraryfolders.vdf");
var libraryFoldersVdfContents = File.ReadAllText(libraryFoldersVdf);
var matches = Regex.Matches(libraryFoldersVdfContents, @"""path""\s+""(.+?)""$", RegexOptions.Multiline);

var shortcutDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
shortcutDirectory = Path.Combine(shortcutDirectory, "Steam");
shortcutDirectory = shortcutDirectory.SanitizePath();
Console.WriteLine($@"Outputting shortcuts to '{shortcutDirectory}'.");

foreach (Match match in matches)
{
    var libraryPath = match.Groups[1].Value.SanitizePath();
    var steamAppsDirectory = Path.Combine(libraryPath, "steamapps");

    var acfFilePaths = Directory.GetFiles(steamAppsDirectory, "*.acf");
    var steamApps = acfFilePaths.Select(SteamApp.CreateFromFile);

    var commonDirectory = Path.Combine(steamAppsDirectory, "common");
    foreach (var app in steamApps)
    {
        var appPath = Path.Combine(commonDirectory, app.InstallDir);
        if (!Directory.Exists(appPath))
        {
            continue;
        }

        var clientIconGuid = appInfoVdf[app.AppId];
        string? clientIconPath = default;
        if (clientIconGuid != null)
        {
            clientIconPath = Path.Combine(clientIconsDirectory, $"{clientIconGuid}.ico");
        }

        var outputPath = AppUtility.CreateWebUrlFile(shortcutDirectory, app, clientIconPath);
        Console.WriteLine(@$"Created file at '{outputPath}'.");
    }
}