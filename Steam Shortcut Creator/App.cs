using Microsoft.Win32;
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SteamShortcutCreator;

public partial class App
{
    #region Constructors

    public App()
    {
        const string LOG_FILE_PATH = "log.txt";
        File.Delete(LOG_FILE_PATH);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(LOG_FILE_PATH)
            .WriteTo.Console(LogEventLevel.Information)
            .CreateLogger();
    }

    #endregion

    #region Methods

    [GeneratedRegex(@"""path""\s+""(.+?)""$", RegexOptions.Multiline)]
    private static partial Regex LibraryPathsRegex();

    public void Run()
    {
        // Get Steam install path
        var steamInstallPath = GetSteamInstallPath();
        if (steamInstallPath == default)
        {
            Log.Error("No valid steam install path found.");
            return;
        }

        // Parse appinfo.vdf
        var appInfoVdfPath = Path.Combine(steamInstallPath, "appcache", "appinfo.vdf");
        var appInfoVdf = new AppInfoVdf(appInfoVdfPath);

        // Determine shortcuts output path
        var shortcutDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        shortcutDirectory = Path.Combine(shortcutDirectory, "Steam");
        shortcutDirectory = shortcutDirectory.SanitizePath();
        Log.Information(@"Outputting shortcuts to '{ShortcutDirectory}'.", shortcutDirectory);

        var clientIconsDirectory = Path.Combine(steamInstallPath, "steam", "games");

        // Get all library folder paths
        var libraryFoldersVdf = Path.Combine(steamInstallPath, "steamapps", "libraryfolders.vdf");
        var libraryFoldersVdfContents = File.ReadAllText(libraryFoldersVdf);
        var libraryFolderMatches = LibraryPathsRegex().Matches(libraryFoldersVdfContents);
        var libraryFolders = libraryFolderMatches.Select(m => m.Groups[1].Value.SanitizePath());

        foreach (var libraryPath in libraryFolders)
        {
            Log.Information(@"Processing library '{LibraryPath}'.", libraryPath);
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

                var outputPath = CreateWebUrlFile(shortcutDirectory, app, clientIconPath);
                Log.Debug(@"Created file at '{OutputPath}'.", outputPath);
            }
        }

        Log.Debug("Opening shortcut directory '{ShortcutDirectory}'", shortcutDirectory);
        Process.Start(
            new ProcessStartInfo
            {
                FileName = shortcutDirectory,
                UseShellExecute = true,
                Verb = "open"
            });
    }

    private string CreateWebUrlFile(string shortcutsDirectory, SteamApp app, string? clientIconPath)
    {
        var safeName = app.Name.SanitizeFileName();
        var contents = string.Format(Resources.Web_URL_Template, app.AppId, clientIconPath);
        var path = Path.Combine(shortcutsDirectory, $"{safeName}.url");
        path = path.SanitizePath();
        File.WriteAllText(path, contents);
        return path;
    }

    private string? GetSteamInstallPath()
    {
        /*
         * Thank you, SteamAppInfo
         * https://github.com/SteamDatabase/SteamAppInfo/blob/b1715faba87fbbc8cb56e8fe30c92ae6ed499aab/SteamAppInfoParser/Program.cs#L54C17-L54C67
         */
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Log.Error("Only windows is supported.");
            return null;
        }

        var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Valve\\Steam");
        if (key == null)
        {
            var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            key = baseKey.OpenSubKey("SOFTWARE\\Valve\\Steam");
        }

        if (key?.GetValue("SteamPath") is string installPath)
        {
            return installPath.SanitizePath();
        }

        return default;
    }

    #endregion
}