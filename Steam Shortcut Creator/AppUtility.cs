using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace SteamShortcutCreator;

public static class AppUtility
{
    #region Fields

    private static readonly char[] _invalidFileNameChars;

    #endregion

    #region Constructors

    static AppUtility()
    {
        _invalidFileNameChars = Path.GetInvalidFileNameChars();
    }

    #endregion

    #region Methods

    public static string CreateWebUrlFile(string shortcutsDirectory, SteamApp app, string? clientIconPath)
    {
        var safeName = SanitizeFileName(app.Name);
        var contents = string.Format(Resources.Web_URL_Template, app.AppId, clientIconPath);
        var path = Path.Combine(shortcutsDirectory, $"{safeName}.url");
        path = path.SanitizePath();
        File.WriteAllText(path, contents);
        return path;
    }

    public static string SanitizePath(this string path)
    {
        path = path.Replace('/', '\\');
        path = path.Trim();
        return path;
    }

    public static bool IsValidSteamLibraryPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (!Directory.Exists(path))
        {
            return false;
        }

        var libraryFolderFilePath = Path.Combine(path, "libraryfolder.vdf");
        return File.Exists(libraryFolderFilePath);
    }

    public static string? GetSteamInstallPath()
    {
        /*
         * Thank you, SteamAppInfo
         * https://github.com/SteamDatabase/SteamAppInfo/blob/b1715faba87fbbc8cb56e8fe30c92ae6ed499aab/SteamAppInfoParser/Program.cs#L54C17-L54C67
         */
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.Error.WriteLine("Only windows is supported.");
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

    private static string SanitizeFileName(string appName)
    {
        foreach (var invalidChar in _invalidFileNameChars)
        {
            appName = appName.Replace(invalidChar.ToString(), "");
        }

        return appName;
    }

    #endregion
}